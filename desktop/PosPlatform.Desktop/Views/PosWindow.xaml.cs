using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class PosWindow : Window
{
    private readonly List<SaleSession> _sessions = new();
    private int _currentIndex;
    private ObservableCollection<CartLine> _cart;
    private List<ProductDto> _allProducts = new();
    private ShiftDto? _shift;
    private readonly ReceiptPrinter _printer = new();
    private ReceiptData? _lastReceipt;
    private List<CategoryDto> _categories = new();
    private Guid? _selectedCategoryId;
    private System.Windows.Threading.DispatcherTimer? _syncTimer;

    public PosWindow()
    {
        InitializeComponent();
        var logo = App.CreateLogoIcon();
        Icon = logo;
        TopLogoImage.Source = logo;
        _sessions.Add(new SaleSession());
        _cart = _sessions[0].Cart;
        CartItems.ItemsSource = _cart;
        UserText.Text = $"{AppState.Username} ({AppState.Role})";
        BuildSaleTabs();
        Loaded += async (_, _) =>
        {
            await LoadShiftAsync();
            await LoadCategoriesAsync();
            await LoadProductsAsync();
            StartSyncTimer();
            AppState.Scanner.BarcodeReceived += OnBarcodeScanned;
            AppState.RestartScanner();
            SearchBox.Focus();
        };
        Closed += (_, _) =>
        {
            _syncTimer?.Stop();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            AppState.Scanner.BarcodeReceived -= OnBarcodeScanned;
            AppState.Scanner.Stop();
        };
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
            await AppState.Backend.CloseShiftAsync(countedCash.Value);
            AppState.Offline.ClearPendingShiftClose();
            Dispatcher.Invoke(() => ShowMessage("✓ Oflayn smena yopish serverga yuborildi", error: false));
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
        s.PaymentIndex = PaymentCombo.SelectedIndex;
        s.DebtorId = (DebtorCombo.SelectedItem as DebtorDto)?.Id;
    }

    private void LoadCurrentSession()
    {
        var s = _sessions[_currentIndex];
        _cart = s.Cart;
        CartItems.ItemsSource = _cart;
        PaidBox.Text = s.Paid;
        PaymentCombo.SelectedIndex = s.PaymentIndex;
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
            PaymentCombo.SelectedIndex = 0;
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

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        IEnumerable<ProductDto> items = _allProducts;

        // Kategoriya bo'yicha filtr (null = barchasi).
        if (_selectedCategoryId is Guid cid)
            items = items.Where(p => p.CategoryId == cid);

        if (!string.IsNullOrEmpty(q))
        {
            items = items.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(q)) ||
                p.MxikCode.ToLowerInvariant().Contains(q));
        }
        ProductsControl.ItemsSource = items.ToList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    // COM port skanerdan barcode kelganda (ThreadPool thread) UI thread'da qayta ishlash
    private void OnBarcodeScanned(string barcode)
    {
        Dispatcher.Invoke(() =>
        {
            var product = _allProducts.FirstOrDefault(p =>
                p.Barcode != null && string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            if (product is not null)
            {
                AddToCart(product);
                SearchBox.Clear();
            }
            else
            {
                SearchBox.Text = barcode;
                ApplyFilter();
                ShowMessage($"Topilmadi: {barcode}", error: true);
            }
        });
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;

        var product =
            _allProducts.FirstOrDefault(p => p.Barcode != null &&
                                             string.Equals(p.Barcode, q, StringComparison.OrdinalIgnoreCase))
            ?? (ProductsControl.ItemsSource as IEnumerable<ProductDto>)?.FirstOrDefault();

        if (product is not null)
        {
            AddToCart(product);
            SearchBox.Clear();
        }
        else
        {
            ShowMessage($"Topilmadi: {q}", error: true);
        }
    }

    private void ProductCard_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is ProductDto p) AddToCart(p);
    }

    // ---------- Savatcha ----------

    private void AddToCart(ProductDto product)
    {
        var line = _cart.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null)
        {
            _cart.Add(new CartLine(product, 1));
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
        if (((FrameworkElement)sender).Tag is CartLine l) { l.Quantity += 1; RecalcTotals(); }
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l)
        {
            if (l.Quantity <= 1) _cart.Remove(l);
            else l.Quantity -= 1;
            RecalcTotals();
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l) { _cart.Remove(l); RecalcTotals(); }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        PaidBox.Text = "0";
        RecalcTotals();
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
            await AppState.Backend.CloseShiftAsync(countedCash);
            AppState.Offline.SaveShift(null);
            AppState.Offline.ClearPendingShift();
            AppState.Offline.ClearPendingShiftClose();
            _shift = null;
            RenderShiftBar();
            ShowMessage("Smena yopildi", error: false);
        }
        catch (Exception ex) { ShowMessage(ex.Message, error: true); }
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

    private async void Checkout_Click(object sender, RoutedEventArgs e)
    {
        if (_shift is null) { ShowMessage("Avval smenani oching", error: true); return; }
        if (_cart.Count == 0) { ShowMessage("Savatcha bo'sh", error: true); return; }

        var total = CartTotal();
        var paid = ParseDecimal(PaidBox.Text);
        var payment = (PaymentCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        if (payment == "Cash" && paid < total)
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

        CheckoutButton.IsEnabled = false;
        try
        {
            var req = new CreateSaleRequest
            {
                PaymentMethod = payment,
                PaidAmount = payment == "Debt" ? 0 : (paid > 0 ? paid : total),
                DebtorId = debtorId,
                Items = _cart.Select(l => new SaleItemRequest { ProductId = l.Product.Id, Quantity = l.Quantity }).ToList()
            };
            var receipt = new ReceiptData
            {
                Cashier = AppState.Username,
                Payment = payment,
                Total = total,
                Paid = req.PaidAmount,
                Lines = _cart.Select(l => new ReceiptLine
                {
                    Name = l.Product.Name, Quantity = l.Quantity, Price = l.Product.Price, LineTotal = l.LineTotal
                }).ToList()
            };

            if (AppState.IsOnline)
            {
                // ─── Onlayn: serverga yuboramiz ────────────────────────────
                var sale = await AppState.Backend.CreateSaleAsync(req);
                receipt.ReceiptNumber = sale.ReceiptNumber;
                receipt.Change = sale.ChangeAmount;
                _lastReceipt = receipt;
                ReprintButton.IsEnabled = true;
                ShowMessage($"✓ Chek {sale.ReceiptNumber} · Jami {sale.TotalAmount:N0} · Qaytim {sale.ChangeAmount:N0}", error: false);
                CloseCurrentSessionAfterSale();
                await LoadProductsAsync();
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

            new ReceiptPreviewWindow(receipt) { Owner = this }.ShowDialog();
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
        => new SettingsWindow { Owner = this }.ShowDialog();

    // ---------- Nasiya (qarz) ----------

    private async void PaymentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DebtorPanel is null) return; // init paytida hali yaratilmagan bo'lishi mumkin
        var payment = (PaymentCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (payment == "Debt")
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

    // ---------- Chek chop etish / qaytarish ----------

    private void Reprint_Click(object sender, RoutedEventArgs e)
    {
        if (_lastReceipt is null) return;
        new ReceiptPreviewWindow(_lastReceipt) { Owner = this }.ShowDialog();
    }

    private void PrintReceiptSafe(ReceiptData receipt)
    {
        try
        {
            _printer.Print(receipt, AppState.PrinterName);
        }
        catch (Exception ex)
        {
            ShowMessage("Chek chop etilmadi: " + ex.Message, error: true);
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
