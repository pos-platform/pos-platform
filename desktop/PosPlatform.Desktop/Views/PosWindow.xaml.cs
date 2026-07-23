using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class PosWindow : Window
{
    private readonly List<SaleSession> _sessions = new();
    private int _currentIndex;
    private ObservableCollection<CartLine> _cart;
    private List<ProductDto> _allProducts = new();
    // ListBox virtualizatsiyasi uchun mahsulotlar 4 tadan qatorlarga bo'linadi.
    private readonly ObservableCollection<IReadOnlyList<ProductDto>> _productRows = new();
    private const int ProductColumns = 4;
    private DispatcherTimer? _searchDebounce;
    private ShiftDto? _shift;
    private readonly ReceiptPrinter _printer = new();
    private ReceiptData? _lastReceipt;
    private List<CategoryDto> _categories = new();
    private Guid? _selectedCategoryId;
    private System.Windows.Threading.DispatcherTimer? _syncTimer;
    // Standart to'lov turi — karta (kassada eng ko'p ishlatiladigan tur).
    private string _payment = "Card";

    public PosWindow()
    {
        InitializeComponent();
        var logo = App.CreateLogoIcon();
        Icon = logo;
        TopLogoImage.Source = logo;
        _sessions.Add(new SaleSession());
        _cart = _sessions[0].Cart;
        CartItems.ItemsSource = _cart;
        ProductsControl.ItemsSource = _productRows;
        UserText.Text = $"{AppState.Username} ({AppState.Role})";
        ShowLicenseReminder();
        BuildSaleTabs();
        Loaded += async (_, _) =>
        {
            // Uchala so'rov bir-biriga bog'liq emas — parallel yuklaymiz (startni tezlashtiradi).
            await Task.WhenAll(LoadShiftAsync(), LoadCategoriesAsync(), LoadProductsAsync());
            StartSyncTimer();
            AppState.Scanner.BarcodeReceived += OnBarcodeScanned;
            AppState.RestartScanner();
            SearchBox.Focus();
            _ = CheckUpdateAsync();
        };
        Closed += (_, _) =>
        {
            _syncTimer?.Stop();
            _searchDebounce?.Stop();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            AppState.Scanner.BarcodeReceived -= OnBarcodeScanned;
            AppState.Scanner.Stop();
        };
    }

    /// <summary>Obuna muddati 7 kun ichida tugayotgan bo'lsa yuqorida eslatma ko'rsatadi.
    /// Doimiy litsenziya bo'lsa yoki muddat ko'p qolgan bo'lsa — ko'rsatilmaydi.</summary>
    private void ShowLicenseReminder()
    {
        var daysLeft = AppState.LicenseDaysLeft;
        if (daysLeft is null || daysLeft.Value > 7) return;

        var until = AppState.LicensedUntil?.ToLocalTime().ToString("dd.MM.yyyy");
        LicenseBannerText.Text = daysLeft.Value <= 0
            ? "⚠ Hurmatli mijoz! Obunangiz muddati bugun tugaydi. Tizim to'xtab qolmasligi uchun to'lovni amalga oshiring."
            : $"⚠ Hurmatli mijoz! Obunangiz tugashiga {daysLeft.Value} kun qoldi ({until} gacha). " +
              "Tizimdan uzluksiz foydalanish uchun to'lovni o'z vaqtida amalga oshiring.";
        LicenseBanner.Visibility = Visibility.Visible;
    }

    // ─── Online/offline holat ────────────────────────────────────────────────

    private void SetOnlineState(bool online)
    {
        AppState.IsOnline = online;
        if (online)
        {
            OnlineBadge.Background = System.Windows.Media.Brushes.Transparent;
            OnlineBadge.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xDC, 0xFC, 0xE7));
            OnlineText.Text = "● Onlayn";
            OnlineText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x16, 0xA3, 0x4A));
            OnlineBadge.ToolTip = "Internet bor — onlayn rejim";
        }
        else
        {
            OnlineBadge.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFE, 0xE2, 0xE2));
            OnlineText.Text = "● Oflayn";
            OnlineText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26));
            OnlineBadge.ToolTip = "Internet yo'q — oflayn rejim (savdolar saqlanadi)";
        }
        RefreshSyncButton();
    }

    private void RefreshSyncButton()
    {
        var count = AppState.Offline.PendingCount();
        if (count > 0 && AppState.IsOnline)
        {
            SyncText.Text = $"↑ Sync ({count})";
            SyncButton.Visibility = Visibility.Visible;
        }
        else
        {
            SyncButton.Visibility = Visibility.Collapsed;
        }
    }

    // ─── Background sync: network event + fallback timer ────────────────────

    private void StartSyncTimer()
    {
        // 1) OS darajasida network o'zgarishi — darhol ishga tushadi
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

        // 2) Fallback timer: har 15 sek (masalan, backend restart bo'lganda)
        _syncTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _syncTimer.Tick += async (_, _) => await CheckAndSyncAsync();
        _syncTimer.Start();
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        // Bu event ThreadPool thread'da keladi.
        // BeginInvoke(async () =>) — UI thread'da async void sifatida ishga tushadi;
        // await'lar ham UI thread'da davom etadi (SynchronizationContext captured).
        Dispatcher.BeginInvoke(async () =>
        {
            if (!e.IsAvailable)
            {
                SetOnlineState(false);
                return;
            }
            // Tarmoq OS'da ko'rinsa ham router/backend bir-ikki soniya kechikishi mumkin
            await Task.Delay(1500);
            await CheckAndSyncAsync();
        });
    }

    private async Task CheckAndSyncAsync()
    {
        var wasOffline = !AppState.IsOnline;
        var online = await AppState.Backend.PingAsync();
        SetOnlineState(online);

        if (!online) return;

        // Avval lokal smena serverga yuborilsin (savdolar unga bog'liq)
        if (AppState.Offline.HasPendingShift())
            await SyncPendingShiftAsync();

        if (AppState.Offline.PendingCount() > 0)
            await SyncPendingSalesAsync(silent: !wasOffline);

        // Navbatdagi smena yopish serverga yuborilsin
        if (AppState.Offline.HasPendingShiftClose())
            await SyncPendingShiftCloseAsync();
    }

    private async Task SyncPendingShiftCloseAsync()
    {
        var countedCash = AppState.Offline.GetPendingShiftClose();
        if (countedCash is null) return;
        try
        {
            var closed = await AppState.Backend.CloseShiftAsync(countedCash.Value, printedLocally: true);
            AppState.Offline.ClearPendingShiftClose();
            Dispatcher.Invoke(() =>
            {
                ShowMessage("✓ Oflayn smena yopish serverga yuborildi", error: false);
                // Oflaynda hisobot chiqmagan edi — server yakunini endi chop etamiz.
                if (closed is not null) PrintShiftReportSafe(closed);
            });
        }
        catch { /* keyingi urinishda qayta sinab ko'riladi */ }
    }

    private async Task SyncPendingSalesAsync(bool silent = false)
    {
        var pending = AppState.Offline.GetPendingSales();
        if (pending.Count == 0) return;

        var synced = 0;
        foreach (var (id, req) in pending)
        {
            try
            {
                await AppState.Backend.CreateSaleAsync(req);
                AppState.Offline.RemovePendingSale(id);
                synced++;
            }
            catch
            {
                break; // birinchi xatoda to'xtaymiz
            }
        }

        if (synced > 0)
        {
            if (!silent)
                ShowMessage($"✓ {synced} ta oflayn savdo serverga yuborildi", error: false);
            await LoadProductsAsync(); // qoldiqlar yangilandi
        }
        RefreshSyncButton();
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        SyncButton.IsEnabled = false;
        try { await SyncPendingSalesAsync(silent: false); }
        finally { SyncButton.IsEnabled = true; }
    }

    // ---------- Parallel savdolar (tablar) ----------

    private void BuildSaleTabs()
    {
        SaleTabs.Children.Clear();
        var chip = (Style)FindResource("Chip");
        var active = (Style)FindResource("ChipActive");
        var canClose = _sessions.Count > 1;
        for (var i = 0; i < _sessions.Count; i++)
        {
            // Tab + yopish (×) tugmasi bitta Grid ichida.
            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 8, 0) };

            var btn = new System.Windows.Controls.Button
            {
                Content = $"Savdo {i + 1}",
                Tag = i,
                Margin = new Thickness(0),
                Padding = canClose ? new Thickness(16, 8, 30, 8) : new Thickness(16, 8, 16, 8),
                Style = i == _currentIndex ? active : chip
            };
            btn.Click += SaleTab_Click;
            grid.Children.Add(btn);

            if (canClose)
            {
                var close = new System.Windows.Controls.Button
                {
                    Content = "×",
                    Tag = i,
                    ToolTip = "Savdoni o'chirish",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 6, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = i == _currentIndex
                        ? System.Windows.Media.Brushes.White
                        : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#9AA3AF")!
                };
                close.Click += CloseSale_Click;
                grid.Children.Add(close);
            }

            SaleTabs.Children.Add(grid);
        }
        var add = new System.Windows.Controls.Button
        {
            Content = "＋",
            Style = (Style)FindResource("Chip"),
            ToolTip = "Yangi savdo (parallel)"
        };
        add.Click += AddSale_Click;
        SaleTabs.Children.Add(add);
    }

    private void SaleTab_Click(object sender, RoutedEventArgs e)
    {
        var i = (int)((System.Windows.Controls.Button)sender).Tag;
        if (i == _currentIndex) return;
        SwitchSession(i);
    }

    private void CloseSale_Click(object sender, RoutedEventArgs e)
    {
        var i = (int)((System.Windows.Controls.Button)sender).Tag;
        if (_sessions.Count <= 1) return;

        var s = _sessions[i];
        if (s.Cart.Count > 0)
        {
            var ok = MessageBox.Show(
                $"Savdo {i + 1} da {s.Cart.Count} ta tovar bor. O'chirilsinmi?",
                "Savdoni o'chirish", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;
        }

        // Joriy tabni o'chirayotgan bo'lsak holatni saqlashga hojat yo'q;
        // boshqasini o'chirsak — joriy holat o'zgarmaydi.
        if (i != _currentIndex) SaveCurrentSession();

        _sessions.RemoveAt(i);
        if (_currentIndex > i) _currentIndex--;
        else if (_currentIndex >= _sessions.Count) _currentIndex = _sessions.Count - 1;

        LoadCurrentSession();
        BuildSaleTabs();
        SearchBox.Focus();
    }

    private void AddSale_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentSession();
        _sessions.Add(new SaleSession());
        _currentIndex = _sessions.Count - 1;
        LoadCurrentSession();
        BuildSaleTabs();
        SearchBox.Focus();
    }

    private void SwitchSession(int index)
    {
        SaveCurrentSession();
        _currentIndex = index;
        LoadCurrentSession();
        BuildSaleTabs();
        SearchBox.Focus();
    }

    private void SaveCurrentSession()
    {
        var s = _sessions[_currentIndex];
        s.Paid = PaidBox.Text;
        s.PaymentMethod = _payment;
        s.DebtorId = (DebtorCombo.SelectedItem as DebtorDto)?.Id;
    }

    private void LoadCurrentSession()
    {
        var s = _sessions[_currentIndex];
        _cart = s.Cart;
        CartItems.ItemsSource = _cart;
        PaidBox.Text = s.Paid;
        SetPayment(s.PaymentMethod, refocus: false);
        if (s.DebtorId is Guid did && DebtorCombo.ItemsSource is IEnumerable<DebtorDto> debtors)
            DebtorCombo.SelectedItem = debtors.FirstOrDefault(d => d.Id == did);
        else
            DebtorCombo.SelectedIndex = -1;
        RecalcTotals();
    }

    /// <summary>Sotuvdan keyin: bir nechta savdo ochiq bo'lsa shuni o'chiradi, aks holda tozalaydi.</summary>
    private void CloseCurrentSessionAfterSale()
    {
        if (_sessions.Count > 1)
        {
            _sessions.RemoveAt(_currentIndex);
            if (_currentIndex >= _sessions.Count) _currentIndex = _sessions.Count - 1;
            LoadCurrentSession();
            BuildSaleTabs();
        }
        else
        {
            _cart.Clear();
            PaidBox.Text = "0";
            SetPayment("Card", refocus: false);
            DebtorCombo.SelectedIndex = -1;
            RecalcTotals();
        }
        SearchBox.Focus();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _categories = await AppState.Backend.GetCategoriesAsync();
            AppState.Offline.SaveCategories(_categories);
        }
        catch
        {
            _categories = AppState.Offline.LoadCategories();
        }
        BuildCategoryChips();
    }

    private void BuildCategoryChips()
    {
        CategoryBar.Children.Clear();
        AddChip("Barchasi", null);
        foreach (var c in _categories.OrderBy(c => c.Name))
            AddChip(c.Name, c.Id);
        RefreshChipStyles();
    }

    private void AddChip(string text, Guid? categoryId)
    {
        var btn = new System.Windows.Controls.Button
        {
            Content = text,
            Tag = categoryId,
            Style = (Style)FindResource("Chip")
        };
        btn.Click += CategoryChip_Click;
        CategoryBar.Children.Add(btn);
    }

    private void CategoryChip_Click(object sender, RoutedEventArgs e)
    {
        _selectedCategoryId = ((System.Windows.Controls.Button)sender).Tag as Guid?;
        RefreshChipStyles();
        ApplyFilter();
    }

    private void RefreshChipStyles()
    {
        var chip = (Style)FindResource("Chip");
        var active = (Style)FindResource("ChipActive");
        foreach (var child in CategoryBar.Children)
        {
            if (child is System.Windows.Controls.Button b)
            {
                var id = b.Tag as Guid?;
                b.Style = id == _selectedCategoryId ? active : chip;
            }
        }
    }

    // ---------- Yuklash ----------

    private async Task LoadShiftAsync()
    {
        try
        {
            _shift = await AppState.Backend.GetCurrentShiftAsync();
            AppState.Offline.SaveShift(_shift);
            SetOnlineState(true);
        }
        catch
        {
            _shift = AppState.Offline.LoadShift();
            SetOnlineState(false);
        }
        RenderShiftBar();
    }

    private async Task LoadProductsAsync()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        try
        {
            _allProducts = await AppState.Backend.GetProductsAsync();
            AppState.Offline.SaveProducts(_allProducts);
            SetOnlineState(true);
        }
        catch
        {
            _allProducts = AppState.Offline.LoadProducts();
            SetOnlineState(false);
            if (_allProducts.Count > 0)
                ShowMessage("Oflayn rejim — keshdan yuklandi", error: false);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
        ApplyFilter();
    }

    private void RenderShiftBar()
    {
        if (_shift is not null)
        {
            ShiftInfo.Text = $"Smena ochiq · boshlang'ich naqd {_shift.OpeningCash:N0}";
            OpenShiftPanel.Visibility = Visibility.Collapsed;
            CloseShiftButton.Visibility = Visibility.Visible;
        }
        else
        {
            ShiftInfo.Text = "Smena yopiq — savdo uchun smenani oching";
            OpenShiftPanel.Visibility = Visibility.Visible;
            CloseShiftButton.Visibility = Visibility.Collapsed;
        }
    }

    // ---------- Mahsulot ro'yxati / qidiruv ----------

    private List<ProductDto> ComputeFiltered()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        IEnumerable<ProductDto> items = _allProducts;
        if (_selectedCategoryId is Guid cid)
            items = items.Where(p => p.CategoryId == cid);
        if (!string.IsNullOrEmpty(q))
            items = items.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(q)) ||
                p.MxikCode.ToLowerInvariant().Contains(q));
        return items.ToList();
    }

    private void ApplyFilter()
    {
        // Virtualizatsiya tufayli faqat ko'rinadigan qatorlar yaratiladi — qatorlarni
        // bir zumda o'rnataveramiz (avvalgidek 50 tadan asta qo'shish shart emas).
        var items = ComputeFiltered();
        _productRows.Clear();
        for (var i = 0; i < items.Count; i += ProductColumns)
            _productRows.Add(items.GetRange(i, Math.Min(ProductColumns, items.Count - i)));
    }

    // Har bir bosilishda butun ro'yxatni qayta qurmaslik uchun qidiruvni "debounce" qilamiz.
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchDebounce is null)
        {
            _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            _searchDebounce.Tick += (_, _) =>
            {
                _searchDebounce!.Stop();
                ApplyFilter();
            };
        }
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    // COM port skanerdan barcode kelganda (ThreadPool thread) UI thread'da qayta ishlash
    private void OnBarcodeScanned(string barcode)
    {
        Dispatcher.Invoke(() => ProcessScannedCode(barcode, fromScanner: true));
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;
        ProcessScannedCode(q, fromScanner: false);
    }

    /// <summary>
    /// Skaner yoki qidiruv maydonidan kelgan kodni qayta ishlaydi.
    /// Tarozi (vaznli) shtrix-kodlarni — "2" bilan boshlanadigan 13 xonali EAN-13 —
    /// ajratib, vaznni miqdor sifatida savatga qo'shadi.
    /// <paramref name="fromScanner"/> true bo'lsa kod aniq shtrix-kod sifatida qaraladi:
    /// topilmasa "Tovar topilmadi" ko'rsatiladi (tasodifiy mahsulot qo'shilmaydi).
    /// false bo'lsa (qidiruvda Enter) — nom bo'yicha ham qidiriladi.
    /// </summary>
    private void ProcessScannedCode(string code, bool fromScanner)
    {
        // 1) Tarozi vazn-kodi: "2" + 6 xonali mahsulot kodi + 5 xonali vazn (gramm) + nazorat raqami.
        //    Vazn deb faqat topilgan mahsulot ROSTDAN tarozili bo'lsagina qabul qilamiz:
        //    IsWeighed belgisi qo'yilgan YOKI birligi Kilogram (import IsWeighed'ni yozmasligi mumkin).
        //    Aks holda ichki generatsiya qilingan "2..." shtrix-kodli Dona tovarlar xato ravishda
        //    vaznga aylanib qolardi (masalan 2297438359950 → 35.995 kg).
        if (TryParseScaleBarcode(code, out var itemCode, out var weightKg))
        {
            var weighed = FindByScaleItemCode(itemCode);
            if (weighed is not null && (weighed.IsWeighed || weighed.Unit == "Kilogram"))
            {
                AddWeightToCart(weighed, weightKg);
                SearchBox.Clear();
                return;
            }
            // Tarozili mahsulot emas — pastdagi oddiy shtrix-kod mosligiga o'tamiz.
        }

        // 2) Oddiy shtrix-kod: aniq moslik (barcode yoki MXIK kodi bo'yicha)
        var product = _allProducts.FirstOrDefault(p =>
            (p.Barcode != null && string.Equals(p.Barcode, code, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(p.MxikCode, code, StringComparison.OrdinalIgnoreCase));

        // 2b) Markirovka (GS1 DataMatrix) kodi: "01" + 14 xonali GTIN + seriya, masalan
        //     "01089352708230852174...". Butun satr mahsulotga mos kelmaydi — GTIN'ni ajratib,
        //     shtrix-kod bo'yicha qidiramiz. Seriya qismi klaviatura layout'i (kirill) tufayli
        //     buzilishi mumkin, shuning uchun faqat boshidagi GTIN raqamlariga tayanamiz.
        //     Topilsa — butun DataMatrix satrini fiskal chek uchun (Labels) saqlab qolamiz.
        string? markingCode = null;
        if (product is null && TryParseGtinFromMarking(code, out var gtin))
        {
            product = FindByGtin(gtin);
            if (product is not null)
                markingCode = code;
        }

        // Qidiruv maydonida Enter bosilganda (skaner emas) ko'rinib turgan
        // birinchi mahsulotni ham qabul qilamiz — nom bo'yicha tez qo'shish uchun.
        // Qidiruv "debounce" bo'lgani uchun ro'yxatni shu yerda yangidan hisoblaymiz.
        if (product is null && !fromScanner)
            product = ComputeFiltered().FirstOrDefault();

        if (product is not null)
        {
            AddToCart(product, markingCode);
            SearchBox.Clear();
            return;
        }

        // Topilmadi — tasodifiy mahsulot qo'shmaymiz, aniq xabar beramiz.
        SearchBox.Clear();
        ShowNotFound($"Tovar topilmadi: {code}");
    }

    /// <summary>Topilmagani haqida ko'zga ko'rinarli, o'zi yo'qoladigan ogohlantirish.</summary>
    private System.Windows.Threading.DispatcherTimer? _toastTimer;
    private void ShowNotFound(string text)
    {
        try { System.Media.SystemSounds.Hand.Play(); } catch { /* ovoz bo'lmasligi mumkin */ }
        ScanToastText.Text = "❌  " + text;
        ScanToast.Visibility = Visibility.Visible;
        ShowMessage(text, error: true);

        _toastTimer?.Stop();
        _toastTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer?.Stop();
            ScanToast.Visibility = Visibility.Collapsed;
        };
        _toastTimer.Start();
        FocusScanInput();
    }

    /// <summary>
    /// Tarozi tomonidan chop etilgan vazn-kodli EAN-13 ni ajratadi.
    /// Format: "2" + 6 xonali mahsulot kodi + 5 xonali vazn (gramm) + 1 nazorat raqami.
    /// Masalan 2000888000862 → kod=000888, vazn=00086 → 0.086 kg.
    /// </summary>
    private static bool TryParseScaleBarcode(string code, out string itemCode, out decimal weightKg)
    {
        itemCode = "";
        weightKg = 0m;
        if (code.Length != 13 || code[0] != '2' || !code.All(char.IsDigit))
            return false;
        itemCode = code.Substring(1, 6);
        if (!int.TryParse(code.Substring(7, 5), out var grams))
            return false;
        weightKg = grams / 1000m;
        return weightKg > 0m;
    }

    /// <summary>
    /// Tarozi shtrix-kodidagi mahsulot kodini katalogdagi mahsulot bilan moslaydi.
    /// Asosiy moslik — mahsulotning PLU raqami (tarozi shu raqam bo'yicha sozlanadi),
    /// shu sabab katalogdagi barcha vaznli mahsulotlar avtomatik tanib olinadi.
    /// Ehtiyot uchun shtrix-kod maydoni ham tekshiriladi.
    /// </summary>
    private ProductDto? FindByScaleItemCode(string itemCode)
    {
        // 1) PLU bo'yicha (asosiy yo'l): 000888 → 888
        if (int.TryParse(itemCode, out var plu) && plu > 0)
        {
            var byPlu = _allProducts.FirstOrDefault(p => p.PluNumber == plu);
            if (byPlu is not null) return byPlu;
        }

        // 2) Shtrix-kod maydoni bo'yicha (agar kod aynan o'sha joyga yozilgan bo'lsa)
        var trimmed = itemCode.TrimStart('0');
        return _allProducts.FirstOrDefault(p =>
        {
            var bc = p.Barcode?.Trim();
            if (string.IsNullOrEmpty(bc)) return false;
            if (string.Equals(bc, itemCode, StringComparison.Ordinal)) return true;        // 000888
            if (string.Equals(bc, "2" + itemCode, StringComparison.Ordinal)) return true;  // 2000888
            if (bc.Length == 13 && bc[0] == '2' && bc.Substring(1, 6) == itemCode)         // o'zi vazn-kod
                return true;
            if (trimmed.Length > 0 && bc.TrimStart('0') == trimmed) return true;           // boshidagi nollarsiz
            return false;
        });
    }

    /// <summary>
    /// GS1 DataMatrix markirovka kodidan GTIN-14 ni ajratadi: "01" bilan boshlanib, keyin
    /// 14 ta raqam keladi. Masalan "01089352708230852174м1И..." → "08935270823085".
    /// Seriya (undan keyingi) qism kirill/layout tufayli buzilgan bo'lsa ham GTIN raqamlari toza.
    /// </summary>
    private static bool TryParseGtinFromMarking(string code, out string gtin)
    {
        gtin = "";
        if (string.IsNullOrEmpty(code) || code.Length < 16) return false;
        if (code[0] != '0' || code[1] != '1') return false;
        var candidate = code.Substring(2, 14);
        foreach (var ch in candidate)
            if (!char.IsDigit(ch)) return false;
        gtin = candidate;
        return true;
    }

    /// <summary>GTIN-14 bo'yicha mahsulotni topadi — shtrix-kod GTIN-14, EAN-13 (boshidagi 0 siz)
    /// yoki boshidagi nollarsiz ko'rinishda saqlangan bo'lishi mumkin.</summary>
    private ProductDto? FindByGtin(string gtin14)
    {
        var ean13 = gtin14.Length == 14 ? gtin14.Substring(1) : gtin14;   // 8935270823085
        var trimmed = gtin14.TrimStart('0');
        return _allProducts.FirstOrDefault(p =>
        {
            var bc = p.Barcode?.Trim();
            if (string.IsNullOrEmpty(bc)) return false;
            return bc == gtin14 || bc == ean13
                   || (trimmed.Length > 0 && bc.TrimStart('0') == trimmed);
        });
    }

    /// <summary>Vaznli mahsulotni o'lchangan vazn bilan savatga qo'shadi (mavjud bo'lsa ustiga qo'shadi).</summary>
    private void AddWeightToCart(ProductDto product, decimal weightKg)
    {
        var line = _cart.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null)
            _cart.Insert(0, new CartLine(product, weightKg));
        else
            line.Quantity += weightKg;
        RecalcTotals();
        SearchBox.Focus();
    }

    private void ProductCard_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is ProductDto p) AddToCart(p);
    }

    // ---------- Savatcha ----------

    private void AddToCart(ProductDto product, string? markingCode = null)
    {
        // Markirovkali (Asl Belgisi) tovar: har bir jismoniy dona uchun DataMatrix kodi noyob,
        // shuning uchun kod majburiy va har skan alohida qator bo'ladi (miqdor 1, ustiga qo'shilmaydi).
        // Fiskal chekda shu kod Labels maydoniga ketib, tovar oborotdan chiqadi.
        if (product.IsMarked)
        {
            if (string.IsNullOrWhiteSpace(markingCode))
            {
                // Tugmadan bosildi yoki oddiy shtrix-kod skanerlandi — DataMatrix talab qilamiz.
                ShowNotFound($"{product.Name}: markirovka kodini (DataMatrix) skanerlang");
                return;
            }
            // Ayni kod savatda bo'lsa — bitta donani ikki marta skanerlash (xato).
            if (_cart.Any(l => string.Equals(l.MarkingCode, markingCode, StringComparison.OrdinalIgnoreCase)))
            {
                ShowNotFound($"{product.Name}: bu kod allaqachon savatda");
                return;
            }
            _cart.Insert(0, new CartLine(product, 1, markingCode.Trim()));
            RecalcTotals();
            SearchBox.Focus();
            return;
        }

        // Oddiy tovar — mavjud qatorga qo'shiladi (markirovkasiz qatorlar birlashadi).
        var line = _cart.FirstOrDefault(l => l.Product.Id == product.Id && l.MarkingCode is null);
        if (line is null)
        {
            // Oxirgi qo'shilgan tovar ro'yxatning boshida (tepada) tursin.
            _cart.Insert(0, new CartLine(product, 1));
        }
        else
        {
            line.Quantity += 1;
        }
        RecalcTotals();
        // Shtrix-skaner ketma-ket o'qishi uchun fokus qidiruv maydonida qolsin.
        SearchBox.Focus();
    }

    private void Inc_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l)
        {
            // Markirovkali tovarda har dona alohida kod bilan skanerlanadi — qo'lda ko'paytirilmaydi.
            if (l.IsMarked)
            {
                ShowMessage("Markirovkali tovar — har bir donani alohida skanerlang", error: true);
                FocusScanInput();
                return;
            }
            l.Quantity += 1;
            RecalcTotals();
        }
        FocusScanInput();
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l)
        {
            if (l.Quantity <= 1) _cart.Remove(l);
            else l.Quantity -= 1;
            RecalcTotals();
        }
        FocusScanInput();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l) { _cart.Remove(l); RecalcTotals(); }
        FocusScanInput();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        PaidBox.Text = "0";
        RecalcTotals();
        // Savatni tozalagandan keyin ham shtrix-skaner darhol ishlashi uchun
        // fokusni qidiruv maydoniga qaytaramiz (HID skaner kiritishi shu yerga tushadi).
        FocusScanInput();
    }

    /// <summary>Shtrix-skaner (HID) kiritishi doimo qidiruv maydoniga tushishi uchun fokusni qaytaradi.</summary>
    private void FocusScanInput()
    {
        // Foydalanuvchi summa kiritayotgan bo'lsa, fokusni undan tortib olmaymiz.
        if (Keyboard.FocusedElement is TextBox tb && tb != SearchBox) return;
        SearchBox.Focus();
    }

    private void ExactCash_Click(object sender, RoutedEventArgs e)
    {
        PaidBox.Text = CartTotal().ToString("0");
    }

    private decimal CartTotal() => _cart.Sum(l => l.LineTotal);

    private void RecalcTotals()
    {
        // InitializeComponent paytida PaidBox Text="0" TextChanged'ni ishga tushiradi,
        // lekin ba'zi elementlar hali yaratilmagan bo'lishi mumkin.
        if (TotalText is null || ChangeText is null) return;

        var total = CartTotal();
        TotalText.Text = total.ToString("N0");
        var paid = ParseDecimal(PaidBox.Text);
        var change = Math.Max(0, paid - total);
        ChangeText.Text = change.ToString("N0");

        if (ItemCountText is not null && ItemCountRow is not null)
        {
            var qty = _cart.Sum(l => l.Quantity);
            ItemCountText.Text = qty.ToString("0.###") + " ta";
            ItemCountRow.Visibility = qty > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (EmptyCartHint is not null)
            EmptyCartHint.Visibility = _cart.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PaidBox_TextChanged(object sender, TextChangedEventArgs e) => RecalcTotals();

    // ---------- Smena ----------

    private async void OpenShift_Click(object sender, RoutedEventArgs e)
    {
        var cash = ParseDecimal(OpeningCashBox.Text);
        if (AppState.IsOnline)
        {
            try
            {
                _shift = await AppState.Backend.OpenShiftAsync(cash);
                AppState.Offline.SaveShift(_shift);
                AppState.Offline.ClearPendingShift();
                RenderShiftBar();
                ShowMessage("Smena ochildi", error: false);
            }
            catch (Exception ex) { ShowMessage(ex.Message, error: true); }
        }
        else
        {
            // Oflayn: lokal smena yaratamiz, internet kelganda serverga yuboramiz
            AppState.Offline.SavePendingShift(cash);
            _shift = new ShiftDto
            {
                Id = Guid.NewGuid(),
                OpeningCash = cash,
                OpenedAt = DateTime.Now,
                Status = "Open"
            };
            AppState.Offline.SaveShift(_shift);
            RenderShiftBar();
            ShowMessage("Smena lokal ochildi — internet tiklansa serverga yuboriladi", error: false);
        }
    }

    private async void CloseShift_Click(object sender, RoutedEventArgs e)
    {
        var input = PromptInput("Sanab chiqilgan naqd summasini kiriting:", "Smenani yopish", "0");
        if (input is null) return;

        var countedCash = ParseDecimal(input);

        if (!AppState.IsOnline)
        {
            // Oflayn: lokal smena yoki server smena — yopish navbatga qo'yiladi
            AppState.Offline.SavePendingShiftClose(countedCash);
            AppState.Offline.ClearPendingShift();
            AppState.Offline.SaveShift(null);
            _shift = null;
            RenderShiftBar();
            ShowMessage("Smena yopish navbatga qo'yildi — internet tiklansa serverga yuboriladi", error: false);
            return;
        }

        try
        {
            var closed = await AppState.Backend.CloseShiftAsync(countedCash, printedLocally: true);
            AppState.Offline.SaveShift(null);
            AppState.Offline.ClearPendingShift();
            AppState.Offline.ClearPendingShiftClose();
            _shift = null;
            RenderShiftBar();
            ShowMessage("Smena yopildi", error: false);

            if (closed is not null) PrintShiftReportSafe(closed);
        }
        catch (Exception ex) { ShowMessage(ex.Message, error: true); }
    }

    /// <summary>Smena yakuni (Z-hisobot) chekini chop etadi; xato bo'lsa smena yopilishiga to'sqinlik qilmaydi.</summary>
    private void PrintShiftReportSafe(ShiftDto shift)
    {
        try
        {
            _printer.PrintShiftReport(new ShiftReportData
            {
                Shift = shift,
                StoreName = AppState.StoreName,
                Stir = AppState.OrgStir,
                Address = AppState.BranchAddress,
                Kassa = AppState.TerminalId,
                Cashier = AppState.CashierDisplay
            }, AppState.PrinterName);
        }
        catch (Exception ex)
        {
            ShowMessage("Smena hisoboti chop etilmadi: " + ex.Message, error: true);
        }
    }

    /// <summary>Oflayn ochilgan smenani serverda ro'yxatdan o'tkazadi.</summary>
    private async Task SyncPendingShiftAsync()
    {
        var pending = AppState.Offline.GetPendingShift();
        if (pending is null) return;
        try
        {
            var serverShift = await AppState.Backend.OpenShiftAsync(pending.Value.OpeningCash);
            AppState.Offline.SaveShift(serverShift);
            AppState.Offline.ClearPendingShift();
            _shift = serverShift;
            Dispatcher.Invoke(RenderShiftBar);
        }
        catch { /* keyingi urinishda qayta sinab ko'riladi */ }
    }

    // ---------- To'lash ----------

    // Tepadagi yashil "To'lash va Chek chiqarish" (va F12) — fiskalizatsiyani kutadi.
    private void Checkout_Click(object sender, RoutedEventArgs e) => _ = CheckoutAsync(allowImmediatePrint: false);

    /// <param name="allowImmediatePrint">Chek oynasi fiskalizatsiya kelishini kutmasdan
    /// "Chop etish" tugmasini darhol ko'rsatadimi (pastdagi "Chop etish" tugmasi uchun true).</param>
    private async Task CheckoutAsync(bool allowImmediatePrint)
    {
        if (_shift is null) { ShowMessage("Avval smenani oching", error: true); return; }
        if (_cart.Count == 0) { ShowMessage("Savatcha bo'sh", error: true); return; }

        var total = CartTotal();
        var paid = ParseDecimal(PaidBox.Text);
        var payment = _payment;
        // Tarozili mahsulotlarda jami kasrli bo'lishi mumkin (masalan 223 070,4);
        // ekranda va "To'liq summani qo'yish"da butun so'm ko'rinadi — o'sha bilan solishtiramiz.
        if (payment == "Cash" && paid < Math.Round(total, 0, MidpointRounding.AwayFromZero))
        {
            ShowMessage("Berilgan summa yetarli emas", error: true);
            return;
        }

        Guid? debtorId = null;
        if (payment == "Debt")
        {
            if (DebtorCombo.SelectedItem is not DebtorDto d)
            {
                ShowMessage("Nasiya uchun qarzdorni tanlang", error: true);
                return;
            }
            debtorId = d.Id;
        }

        // O'tkazma: mijozga QR (karta raqami) ko'rsatamiz, kassir pul tushganini tasdiqlagachgina savdo yaratiladi
        if (payment == "Transfer")
        {
            var qrWindow = new TransferQrWindow(total) { Owner = this };
            if (qrWindow.ShowDialog() != true)
            {
                FocusScanInput();
                return;
            }
        }

        CheckoutButton.IsEnabled = false;
        try
        {
            var req = new CreateSaleRequest
            {
                PaymentMethod = payment,
                PaidAmount = payment == "Debt" ? 0 : (paid > 0 ? paid : total),
                DebtorId = debtorId,
                TerminalId = AppState.TerminalId,
                Items = _cart.Select(l => new SaleItemRequest { ProductId = l.Product.Id, Quantity = l.Quantity, MarkingCode = l.MarkingCode }).ToList()
            };
            var receipt = new ReceiptData
            {
                StoreName = AppState.StoreName,
                Stir = AppState.OrgStir,
                Address = AppState.BranchAddress,
                LegalAddress = AppState.LegalAddress,
                Kassa = AppState.TerminalId,
                Cashier = AppState.CashierDisplay,
                ExternalFiscalDevice = AppState.ExternalFiscalDevice,
                Payment = payment,
                Total = total,
                Paid = req.PaidAmount,
                Lines = _cart.Select(l => new ReceiptLine
                {
                    Name = l.Product.Name, Quantity = l.Quantity, Price = l.Product.Price, LineTotal = l.LineTotal,
                    MxikCode = l.Product.MxikCode, Barcode = l.Product.Barcode, MarkingCode = l.MarkingCode, IsVat = l.Product.IsVat
                }).ToList()
            };

            if (AppState.IsOnline)
            {
                // ─── Onlayn: serverga yuboramiz ────────────────────────────
                var sale = await AppState.Backend.CreateSaleAsync(req);
                receipt.ReceiptNumber = sale.ReceiptNumber;
                receipt.Change = sale.ChangeAmount;
                receipt.SaleId = sale.Id; // fiskal QR'ni keyin so'rab olish uchun
                _lastReceipt = receipt;
                ReprintButton.IsEnabled = true;
                ShowMessage($"✓ Chek {sale.ReceiptNumber} · Jami {sale.TotalAmount:N0} · Qaytim {sale.ChangeAmount:N0}", error: false);

                // Qoldiqlarni lokal kamaytiramiz — butun katalogni serverdan qayta yuklamaymiz
                // (har savdodan keyingi to'liq yuklash sotuvni sezilarli sekinlashtirardi).
                foreach (var line in _cart)
                {
                    var cached = _allProducts.FirstOrDefault(p => p.Id == line.Product.Id);
                    if (cached is not null) cached.Quantity -= line.Quantity;
                }
                AppState.Offline.SaveProducts(_allProducts);
                CloseCurrentSessionAfterSale();
                ApplyFilter();
            }
            else
            {
                // ─── Oflayn: lokal keshga saqlaymiz ────────────────────────
                var localId = $"OFF-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
                receipt.ReceiptNumber = localId;
                receipt.Change = Math.Max(0, req.PaidAmount - total);
                AppState.Offline.EnqueueSale(localId, req);

                // Lokal mahsulot qoldiqlarini kamaytiramiz (kesh)
                foreach (var line in _cart)
                {
                    var cached = _allProducts.FirstOrDefault(p => p.Id == line.Product.Id);
                    if (cached is not null) cached.Quantity -= line.Quantity;
                }
                AppState.Offline.SaveProducts(_allProducts);

                _lastReceipt = receipt;
                ReprintButton.IsEnabled = true;
                RefreshSyncButton();
                ShowMessage($"✓ Oflayn saqlandi: {localId}. Internet tiklansa avtomatik yuboriladi.", error: false);
                CloseCurrentSessionAfterSale();
                ApplyFilter();
            }

            // Pastdagi "Chop etish": kassir allaqachon chop etishni tanlagan — ko'rish oynasida
            // yana bir marta tugma bosdirmaymiz, chekni to'g'ridan-to'g'ri printerga yuboramiz.
            // O'tkazmada bu "Pul tushdi" tasdig'idan keyin darhol chek chiqishini bildiradi.
            if (allowImmediatePrint)
                PrintReceiptSafe(receipt);
            else
                new ReceiptPreviewWindow(receipt, ReceiptViewMode.Fiscal) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
        finally
        {
            CheckoutButton.IsEnabled = true;
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
        RefreshUpdateBadge();
    }

    /// <summary>Serverda yangi versiya bor-yo'qligini tekshirib, ⚙ tugmadagi belgini yangilaydi.</summary>
    private async Task CheckUpdateAsync()
    {
        await UpdateService.CheckAsync();
        RefreshUpdateBadge();
    }

    private void RefreshUpdateBadge()
    {
        var upd = UpdateService.Available;
        UpdateBadge.Visibility = upd is null ? Visibility.Collapsed : Visibility.Visible;
        SettingsButton.ToolTip = upd is null
            ? "Sozlamalar"
            : $"Sozlamalar — yangi versiya {upd.Version} mavjud";
    }

    // ---------- Nasiya (qarz) ----------

    private void PayTab_Click(object sender, RoutedEventArgs e)
    {
        var method = (string)((System.Windows.Controls.Button)sender).Tag;
        SetPayment(method);
    }

    /// <summary>To'lov turini o'rnatadi, tab uslublarini va qarzdor panelini yangilaydi.</summary>
    private void SetPayment(string method, bool refocus = true)
    {
        _payment = method;
        RefreshPayTabs();
        UpdateDebtorPanel();
        if (refocus) FocusScanInput();
    }

    private void RefreshPayTabs()
    {
        if (PayCash is null) return; // init paytida hali yaratilmagan bo'lishi mumkin
        var tab = (Style)FindResource("PayTab");
        var active = (Style)FindResource("PayTabActive");
        PayCash.Style = _payment == "Cash" ? active : tab;
        PayCard.Style = _payment == "Card" ? active : tab;
        PayTransfer.Style = _payment == "Transfer" ? active : tab;
        PayMixed.Style = _payment == "Mixed" ? active : tab;
        PayDebt.Style = _payment == "Debt" ? active : tab;
    }

    private async void UpdateDebtorPanel()
    {
        if (DebtorPanel is null) return; // init paytida hali yaratilmagan bo'lishi mumkin
        if (_payment == "Debt")
        {
            DebtorPanel.Visibility = Visibility.Visible;
            if (DebtorCombo.ItemsSource is null)
            {
                try { DebtorCombo.ItemsSource = await AppState.Backend.GetDebtorsAsync(); }
                catch (Exception ex) { ShowMessage(ex.Message, error: true); }
            }
        }
        else
        {
            DebtorPanel.Visibility = Visibility.Collapsed;
        }
    }

    // ---------- Klaviatura tezkor tugmalari ----------

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F2:
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                break;
            case Key.F9:
                Clear_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F12:
                Checkout_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    // ─── Oyna darajasidagi shtrix-skaner ushlagichi ─────────────────────────
    // HID (klaviatura-emulyatsiya) skanerlar belgilarni juda tez yuboradi va Enter
    // bilan tugatadi. Fokus qaysi tugmada bo'lishidan qat'i nazar (savatni tozalash,
    // miqdor tugmalari va h.k.) skaner kodi savatga tushishi uchun belgilarni shu yerda
    // to'playmiz. Fokus tahrirlanadigan matn maydonida bo'lsa (qidiruv, summa) —
    // o'sha maydon o'zi ishlaydi, biz aralashmaymiz.
    private string _scanBuffer = "";
    private DateTime _lastScanKey = DateTime.MinValue;

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox) return; // matn maydoni o'zi qabul qiladi
        if (string.IsNullOrEmpty(e.Text)) return;

        // Belgilar orasidagi tanaffus uzoq bo'lsa — yangi skan boshlanadi.
        if ((DateTime.Now - _lastScanKey).TotalMilliseconds > 120)
            _scanBuffer = "";
        _lastScanKey = DateTime.Now;
        _scanBuffer += e.Text;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Keyboard.FocusedElement is TextBox) return; // SearchBox/summa o'z Enter'ini ishlaydi
        if (_scanBuffer.Length == 0) return;

        var code = _scanBuffer.Trim();
        _scanBuffer = "";
        if (code.Length > 0)
        {
            ProcessScannedCode(code, fromScanner: true);
            e.Handled = true;
        }
    }

    // ---------- Chek chop etish / qaytarish ----------

    private void Reprint_Click(object sender, RoutedEventArgs e)
    {
        // Savatchada mahsulot bo'lsa — bu tugma ham to'lovni bajaradi (yashil tugma kabi),
        // ammo chek oynasi fiskalizatsiyani kutmasdan darhol chop etishga ruxsat beradi.
        if (_cart.Count > 0)
        {
            _ = CheckoutAsync(allowImmediatePrint: true);
            return;
        }

        // Savatcha bo'sh — oxirgi to'lov qilingan chekni qayta chop etadi.
        if (_lastReceipt is null)
        {
            ShowMessage("Chop etadigan chek yo'q — avval to'lov qiling", error: true);
            return;
        }
        if (PrintReceiptSafe(_lastReceipt))
            ShowMessage("✓ Chek chop etildi", error: false);
    }

    private bool PrintReceiptSafe(ReceiptData receipt)
    {
        try
        {
            // Pastdagi "Chop etish" (qayta bosish) — oddiy nusxa, Shtrix/MXIK kodlarsiz.
            _printer.Print(receipt, AppState.PrinterName, showCodes: false);
            return true;
        }
        catch (Exception ex)
        {
            ShowMessage("Chek chop etilmadi: " + ex.Message, error: true);
            return false;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            await LoadProductsAsync();
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            SearchBox.Focus();
        }
    }

    private async void Refund_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RefundWindow { Owner = this };
        dlg.ShowDialog();
        // Qaytarishdan keyin qoldiq o'zgargan bo'lishi mumkin.
        await LoadProductsAsync();
    }

    private async void SalesHistory_Click(object sender, RoutedEventArgs e)
    {
        new SalesHistoryWindow { Owner = this }.ShowDialog();
        // Oynada qaytarish qilingan bo'lishi mumkin — qoldiqni yangilaymiz.
        await LoadProductsAsync();
    }

    // ---------- Yordamchilar ----------

    private void ShowMessage(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x16, 0xA3, 0x4A));
        MessageText.Visibility = Visibility.Visible;
    }

    /// <summary>Oddiy modal kiritish dialogi (matn qaytaradi yoki bekor bo'lsa null).</summary>
    private string? PromptInput(string message, string title, string initial)
    {
        var dlg = new Window
        {
            Title = title, Width = 340, Height = 170, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize, Background = Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) });
        var box = new TextBox { Text = initial, Height = 30, Padding = new Thickness(6, 4, 6, 4) };
        panel.Children.Add(box);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        string? result = null;
        var ok = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Bekor", Width = 80, Height = 30, IsCancel = true };
        ok.Click += (_, _) => { result = box.Text; dlg.DialogResult = true; };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        dlg.Content = panel;
        box.Focus();
        box.SelectAll();
        return dlg.ShowDialog() == true ? result : null;
    }

    private static decimal ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim().Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
