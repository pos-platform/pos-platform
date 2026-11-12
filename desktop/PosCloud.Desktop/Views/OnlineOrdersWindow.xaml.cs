using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>
/// Online buyurtmalar doskasi (Yangi / Yig'ilmoqda / Tayyor / Yo'lda) va yig'ish paneli.
/// Ro'yxat SignalR xabarida va "Yangilash"da API'dan qayta olinadi. Belgilash (checkbox) — faqat shu kassada,
/// serverga miqdor/olib tashlash/almashtirish yuboriladi.
/// </summary>
public partial class OnlineOrdersWindow : Window
{
    private static readonly string[] PickingStatuses = ["Accepted", "Picking"];
    private static readonly string[] ReadyStatuses = ["Ready", "AwaitingTransfer", "ReadyForPickup"];

    /// <summary>Kassada belgilangan qatorlar (buyurtma → qator Id). Oyna qayta ochilganda ham saqlanadi.</summary>
    private static readonly Dictionary<Guid, HashSet<Guid>> CheckedItems = new();

    /// <summary>Oxirgi kiritilgan kuryer (sessiya davomida xotirada) — keyingi dispatchda taklif qilinadi.</summary>
    private static string _lastCourierName = "";
    private static string _lastCourierPhone = "";

    private readonly OnlineOrdersHub? _hub;
    private OnlineOrderDto? _selected;
    private List<PickRow> _rows = new();
    private bool _busy;
    private bool _loadingSettings;

    public OnlineOrdersWindow(OnlineOrdersHub? hub)
    {
        InitializeComponent();
        _hub = hub;

        Loaded += async (_, _) =>
        {
            AppState.ScanOverride = code => Dispatcher.Invoke(() => HandleScan(code));
            if (_hub is not null)
            {
                _hub.OrderCreated += OnHubEvent;
                _hub.OrderUpdated += OnHubEvent;
                _hub.ConnectionChanged += OnConnectionChanged;
                OnConnectionChanged(_hub.IsConnected);
            }
            await LoadSettingsAsync();
            await ReloadAsync();
            await RefreshCourierCashCountAsync();
        };
        Closed += (_, _) =>
        {
            AppState.ScanOverride = null;
            if (_hub is not null)
            {
                _hub.OrderCreated -= OnHubEvent;
                _hub.OrderUpdated -= OnHubEvent;
                _hub.ConnectionChanged -= OnConnectionChanged;
            }
        };
    }

    // ───────────── yuklash ─────────────

    private void OnHubEvent(OnlineOrderEvent e) => Dispatcher.InvokeAsync(async () =>
    {
        await ReloadAsync();
        if (_selected?.Id == e.Id) await SelectAsync(e.Id);
        // Kuryer "Yetkazildi" bosganda yoki naqd topshirilganda — hisoblagich yangilanadi.
        await RefreshCourierCashCountAsync();
    });

    /// <summary>Sarlavhadagi "Kuryer pullari (N)" hisoblagichi.</summary>
    private async Task RefreshCourierCashCountAsync()
    {
        try
        {
            var list = await AppState.Backend.GetCourierCashAsync();
            CourierCashButton.Content = list.Count > 0
                ? $"Kuryer pullari ({list.Count}) · {list.Sum(x => x.Amount):N0}"
                : "Kuryer pullari (0)";
        }
        catch
        {
            // Hisoblagich ikkinchi darajali — xato bo'lsa oddiy matn qoladi (masalan, backend hali yangilanmagan).
            CourierCashButton.Content = "Kuryer pullari";
        }
    }

    private async void CourierCash_Click(object sender, RoutedEventArgs e)
    {
        var w = new CourierCashWindow { Owner = this };
        w.ShowDialog();
        await RefreshCourierCashCountAsync();
        if (w.Changed && _selected is not null) await SelectAsync(_selected.Id);
    }

    private void OnConnectionChanged(bool connected) => Dispatcher.InvokeAsync(() =>
    {
        ConnectionText.Text = connected ? "● Real vaqt ulangan" : "● Real vaqt uzilgan — \"Yangilash\"ni bosing";
        ConnectionText.Foreground = connected ? Brushes.LightGreen : Brushes.Orange;
    });

    private async Task LoadSettingsAsync()
    {
        try
        {
            var s = await AppState.Backend.GetOnlineStoreSettingsAsync();
            _loadingSettings = true;
            AcceptingCheck.IsChecked = s?.IsAcceptingOrders ?? false;
            if (s is not null && !s.IsVisible)
                ShowMessage("Do'kon hozir ilovada ko'rinmaydi (SuperAdmin yoqmagan yoki bloklangan).", error: true);
        }
        catch (Exception ex)
        {
            ShowMessage("Sozlamalar olinmadi: " + ex.Message, error: true);
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            var list = await AppState.Backend.GetOnlineOrdersAsync(active: true);
            var ordered = list.OrderBy(o => o.CreatedAt).ToList();

            Fill(NewList, NewHeader, "Yangi", ordered.Where(o => o.Status == "New"));
            Fill(PickingList, PickingHeader, "Yig'ilmoqda", ordered.Where(o => PickingStatuses.Contains(o.Status)));
            Fill(ReadyList, ReadyHeader, "Tayyor", ordered.Where(o => ReadyStatuses.Contains(o.Status)));
            Fill(WayList, WayHeader, "Yo'lda", ordered.Where(o => o.Status == "OnTheWay"));

            // Yakunlangan buyurtmalarning belgilarini tozalaymiz.
            foreach (var id in CheckedItems.Keys.Where(k => list.All(o => o.Id != k)).ToList())
                CheckedItems.Remove(id);
        }
        catch (Exception ex)
        {
            ShowMessage("Buyurtmalar olinmadi: " + ex.Message, error: true);
        }
    }

    private static void Fill(ItemsControl list, TextBlock header, string title, IEnumerable<OnlineOrderDto> orders)
    {
        var items = orders.ToList();
        list.ItemsSource = items;
        header.Text = $"{title}  {items.Count}";
    }

    private async void Card_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is OnlineOrderDto o)
            await SelectAsync(o.Id);
    }

    private async Task SelectAsync(Guid id)
    {
        try
        {
            ShowOrder(await AppState.Backend.GetOnlineOrderAsync(id));
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    // ───────────── tafsilot paneli ─────────────

    private void ShowOrder(OnlineOrderDto? o)
    {
        _selected = o;
        if (o is null || o.Status is "Delivered" or "Rejected" or "Cancelled")
        {
            if (o is not null) ShowMessage($"#{o.Number} — {o.StatusText}.", error: false);
            _selected = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            EmptyDetail.Visibility = Visibility.Visible;
            return;
        }

        EmptyDetail.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        DetailStatus.Text = o.StatusText;
        DetailTitle.Text = $"Buyurtma #{o.Number}";
        DetailCustomer.Text = $"{o.CustomerName} · {o.CustomerPhone}" +
                              (o.ScheduledFor is { } s ? $" · {s.ToLocalTime():dd.MM HH:mm} ga" : " · imkon qadar tez");
        DetailAddress.Text = o.AddressText + (o.Contactless ? " · kontaktsiz" : "");
        DetailComment.Text = string.IsNullOrWhiteSpace(o.Comment) ? "" : "Izoh: " + o.Comment;
        // Kuryer + naqd holati (kuryer ilovada olgan naqd, kassaga topshirilganmi).
        var courierText = o.CourierText;
        if (o.CourierCashCollected is { } cash)
            courierText += (courierText.Length > 0 ? " · " : "") + $"naqd kuryerda: {cash:N0} so'm" +
                           (o.CourierCashHandedOverAt is { } h ? $" · kassaga topshirildi {h.ToLocalTime():dd.MM HH:mm}" : " · kassaga topshirilmagan");
        else if (o.CourierCashHandedOverAt is { } h2)
            courierText += (courierText.Length > 0 ? " · " : "") + $"naqd kassaga topshirildi {h2.ToLocalTime():dd.MM HH:mm}";
        DetailCourier.Text = courierText;
        DetailCourier.Visibility = courierText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        DetailPolicy.Text = o.PolicyText;
        DetailPayment.Text = o.PaymentText + (o.DeliveryFee > 0 ? $" · yetkazish {o.DeliveryFee:N0}" : "");
        DetailTotal.Text = $"{o.TotalAmount:N0} so'm";

        // Promokod va bonus (server hisoblagan) — jami summaga kirgan chegirmalar.
        var discounts = new List<string>();
        if (!string.IsNullOrWhiteSpace(o.PromoCode) || o.PromoDiscount > 0)
            discounts.Add($"Promokod{(string.IsNullOrWhiteSpace(o.PromoCode) ? "" : " " + o.PromoCode)}: −{o.PromoDiscount:N0} so'm");
        if (o.BonusUsed > 0)
            discounts.Add($"Bonus: −{o.BonusUsed:N0} so'm");
        if (discounts.Count > 0)
            discounts.Insert(0, $"Mahsulotlar: {o.ItemsTotal:N0} so'm");
        DetailDiscounts.Text = string.Join(" · ", discounts);
        DetailDiscounts.Visibility = discounts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var isPicking = o.Status == "Picking";
        ScanBox.Visibility = isPicking ? Visibility.Visible : Visibility.Collapsed;

        if (!CheckedItems.TryGetValue(o.Id, out var set))
            CheckedItems[o.Id] = set = new HashSet<Guid>();

        _rows = o.Items.Select(i => new PickRow(i, set, isPicking, UpdateProgress)).ToList();
        ItemsList.ItemsSource = _rows;
        UpdateProgress();
        BuildActions(o);

        if (isPicking) ScanInput.Focus();
    }

    private void UpdateProgress()
    {
        var active = _rows.Where(r => !r.Item.IsRemoved).ToList();
        var done = active.Count(r => r.IsChecked);
        PickedText.Text = $"{done} / {active.Count}";
        PickedProgress.Maximum = Math.Max(1, active.Count);
        PickedProgress.Value = done;
    }

    private void BuildActions(OnlineOrderDto o)
    {
        ActionsPanel.Children.Clear();
        var transferUnpaid = o.PaymentMethod == "Transfer" && !o.IsPaid;

        switch (o.Status)
        {
            case "New":
                AddAction("Qabul qilish", primary: true, () => Act(o, "accept"));
                AddAction("Rad etish", primary: false, () => Reject(o));
                break;
            case "Accepted":
                AddAction("Yig'ishni boshlash", primary: true, () => Act(o, "start-picking"));
                AddAction("Rad etish", primary: false, () => Reject(o));
                break;
            case "Picking":
                AddAction("Tayyor · fiskal chek", primary: true, () => Ready(o));
                AddAction("Mahsulot qo'shish", primary: false, () => AddProduct(o, replaces: null));
                AddAction("Rad etish", primary: false, () => Reject(o));
                break;
            case "AwaitingTransfer":
                AddAction("To'lov tushdi", primary: true, () => Act(o, "payment-received"));
                break;
            case "Ready":
                AddAction("Kuryerga berildi", primary: true, () => Dispatch(o));
                if (transferUnpaid) AddAction("To'lov tushdi", primary: false, () => Act(o, "payment-received"));
                break;
            case "ReadyForPickup":
                AddAction("Mijozga topshirildi", primary: true, () => Act(o, "delivered"));
                if (transferUnpaid) AddAction("To'lov tushdi", primary: false, () => Act(o, "payment-received"));
                break;
            case "OnTheWay":
                AddAction(o.PaymentMethod == "Cash" && !o.IsPaid ? "Yetkazildi · kuryer pulini oldim" : "Yetkazildi",
                    primary: true, () => Delivered(o));
                if (transferUnpaid) AddAction("To'lov tushdi", primary: false, () => Act(o, "payment-received"));
                break;
        }

        // "Tayyor"dan keyin bekor qilish — savdo qaytariladi (refund).
        if (o.Status is "Ready" or "AwaitingTransfer" or "ReadyForPickup" or "OnTheWay")
            AddAction("Bekor qilish (qaytarish)", primary: false, () => CancelWithRefund(o));

        if (!string.IsNullOrWhiteSpace(o.TransferProofUrl))
            AddAction("O'tkazma cheki", primary: false, () => { OpenUrl(o.TransferProofUrl!); return Task.CompletedTask; });
        if (o.SaleId is not null)
            AddAction("Chek", primary: false, () => ShowReceipt(o.SaleId.Value));
    }

    private void AddAction(string text, bool primary, Func<Task> action)
    {
        var b = new Button { Content = text, Style = (Style)FindResource(primary ? "PrimaryBtn" : "ActionBtn") };
        b.Click += async (_, _) =>
        {
            if (_busy) return;
            _busy = true;
            ActionsPanel.IsEnabled = false;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, error: true);
            }
            finally
            {
                _busy = false;
                ActionsPanel.IsEnabled = true;
            }
        };
        ActionsPanel.Children.Add(b);
    }

    // ───────────── amallar ─────────────

    private async Task Act(OnlineOrderDto o, string action)
    {
        var updated = await AppState.Backend.OnlineOrderActionAsync(o.Id, action);
        ShowMessage($"✓ #{updated.Number}: {updated.StatusText}", error: false);
        ShowOrder(updated);
        await ReloadAsync();
    }

    private async Task Reject(OnlineOrderDto o)
    {
        var reason = PromptWindow.Ask(this, "Rad etish", $"#{o.Number} buyurtmani rad etish sababi (mijozga ko'rinadi):");
        if (string.IsNullOrWhiteSpace(reason)) return;
        var updated = await AppState.Backend.RejectOnlineOrderAsync(o.Id, reason);
        ShowMessage($"#{updated.Number} rad etildi.", error: false);
        ShowOrder(updated);
        await ReloadAsync();
    }

    /// <summary>
    /// Kuryer tanlash: tizimdagi kuryer akkaunti (courierUserId) yoki "Boshqa (qo'lda)" — ism/telefon.
    /// Kuryerlar ro'yxati olinmasa ham qo'lda kiritish imkoni qoladi.
    /// </summary>
    private async Task Dispatch(OnlineOrderDto o)
    {
        List<OnlineCourierDto> couriers = new();
        string? loadError = null;
        try
        {
            couriers = await AppState.Backend.GetOnlineCouriersAsync(o.BranchId == Guid.Empty ? null : o.BranchId);
        }
        catch (Exception ex)
        {
            loadError = ex.Message;
        }

        var pick = CourierPickWindow.Ask(this, o.Number, couriers, loadError);
        if (pick is null) return;

        if (!pick.Manual && pick.Selected is { } courier)
        {
            var assigned = await AppState.Backend.DispatchOnlineOrderToCourierAsync(o.Id, courier.Id);
            ShowMessage($"✓ #{assigned.Number}: {assigned.StatusText}" + (assigned.HasCourier ? $" · {assigned.CourierText}" : ""), error: false);
            ShowOrder(assigned);
            await ReloadAsync();
            return;
        }

        await DispatchManual(o);
    }

    /// <summary>Kuryer ismi va telefonini qo'lda so'raydi (ikkalasi ixtiyoriy) va dispatch qiladi.</summary>
    private async Task DispatchManual(OnlineOrderDto o)
    {
        var name = PromptWindow.Ask(this, "Kuryerga berildi", $"#{o.Number} — kuryer ismi (ixtiyoriy):", _lastCourierName);
        if (name is null) return;
        var phone = PromptWindow.Ask(this, "Kuryerga berildi", $"#{o.Number} — kuryer telefoni (ixtiyoriy):", _lastCourierPhone);
        if (phone is null) return;

        _lastCourierName = name;
        _lastCourierPhone = phone;

        var updated = await AppState.Backend.DispatchOnlineOrderAsync(o.Id,
            string.IsNullOrWhiteSpace(name) ? null : name,
            string.IsNullOrWhiteSpace(phone) ? null : phone);
        ShowMessage($"✓ #{updated.Number}: {updated.StatusText}" + (updated.HasCourier ? $" · {updated.CourierText}" : ""), error: false);
        ShowOrder(updated);
        await ReloadAsync();
    }

    /// <summary>Tayyor/yo'ldagi buyurtmani bekor qiladi — server savdoni qaytaradi (refund).</summary>
    private async Task CancelWithRefund(OnlineOrderDto o)
    {
        var reason = PromptWindow.Ask(this, "Bekor qilish (qaytarish)",
            $"#{o.Number} buyurtmani bekor qilish sababi (mijozga ko'rinadi). Savdo qaytariladi:");
        if (string.IsNullOrWhiteSpace(reason)) return;
        if (MessageBox.Show($"#{o.Number} bekor qilinsinmi?\nSavdo ({o.TotalAmount:N0} so'm) qaytariladi (refund).",
                "Bekor qilish", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var updated = await AppState.Backend.CancelOnlineOrderAsync(o.Id, reason);
        CheckedItems.Remove(o.Id);
        ShowMessage($"#{updated.Number} bekor qilindi, savdo qaytarildi (refund).", error: false);
        ShowOrder(updated);
        await ReloadAsync();
    }

    private async Task Delivered(OnlineOrderDto o)
    {
        if (o.PaymentMethod == "Cash" && !o.IsPaid &&
            MessageBox.Show($"Kuryerdan {o.TotalAmount:N0} so'm naqd pul olindimi?", "Yetkazildi",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        await Act(o, "delivered");
    }

    private async Task Ready(OnlineOrderDto o)
    {
        // Markirovkali qatorlarda kodlar to'liq bo'lmasa — server ham 400 qaytaradi; oldindan aniq xabar.
        var missing = o.Items.Where(i => i.IsMarked && !i.IsRemoved && !i.MarkingComplete).ToList();
        if (missing.Count > 0)
        {
            var lines = string.Join("\n", missing.Select(i => $"• {i.ProductName}: {i.MarkingCodes.Count} / {i.RequiredMarkingCodes}"));
            MessageBox.Show($"Markirovka kodlari to'liq emas:\n{lines}\n\nDataMatrix kodlarini skanerlang yoki \"Markirovka kodi\" tugmasi orqali kiriting.",
                "Tayyor", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowMessage("Markirovka kodlari to'liq emas — fiskal chek chiqarilmadi.", error: true);
            return;
        }

        var unchecked_ = _rows.Count(r => !r.Item.IsRemoved && !r.IsChecked);
        if (unchecked_ > 0 &&
            MessageBox.Show($"{unchecked_} ta qator belgilanmagan. Baribir tayyor deb fiskal chek chiqarilsinmi?",
                "Tayyor", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var updated = await AppState.Backend.ReadyOnlineOrderAsync(o.Id);
        CheckedItems.Remove(o.Id);
        ShowMessage($"✓ #{updated.Number}: {updated.StatusText}. Chek: {updated.ReceiptNumber}", error: false);
        ShowOrder(updated);
        await ReloadAsync();

        if (updated.SaleId is { } saleId)
            await ShowReceipt(saleId, fiscal: true);
    }

    private async Task ShowReceipt(Guid saleId) => await ShowReceipt(saleId, fiscal: false);

    private async Task ShowReceipt(Guid saleId, bool fiscal)
    {
        var sale = await AppState.Backend.GetSaleAsync(saleId);
        if (sale is null)
        {
            ShowMessage("Savdo topilmadi.", error: true);
            return;
        }
        var receipt = SalesHistoryWindow.ToReceiptData(sale);
        new ReceiptPreviewWindow(receipt, fiscal ? ReceiptViewMode.Fiscal : ReceiptViewMode.History) { Owner = this }.ShowDialog();
    }

    // ───────────── yig'ish: qator amallari ─────────────

    private async void ItemQty_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null || (sender as FrameworkElement)?.Tag is not PickRow row) return;
        var text = PromptWindow.Ask(this, "Olingan miqdor",
            $"{row.Item.ProductName}: so'ralgan {row.Item.Quantity:0.###}. Haqiqatda olingan miqdor:",
            row.Item.EffectiveQuantity.ToString("0.###", CultureInfo.CurrentCulture));
        if (text is null) return;
        if (!TryParseQty(text, out var qty) || qty <= 0)
        {
            ShowMessage("Miqdor noto'g'ri.", error: true);
            return;
        }

        await RunItemsUpdate(new
        {
            items = new[] { new { itemId = row.Item.Id, pickedQuantity = (decimal?)qty, isRemoved = false } },
            add = Array.Empty<object>()
        });
        CheckedItems[_selected!.Id].Add(row.Item.Id);
        UpdateProgressAfterReload();
    }

    /// <summary>Markirovka kodini qo'lda biriktirish (skaner GTIN bo'yicha topa olmaganda). Bo'sh qiymat — kodlarni tozalash.</summary>
    private async void ItemMarking_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null || (sender as FrameworkElement)?.Tag is not PickRow row) return;
        var item = row.Item;
        var code = PromptWindow.Ask(this, "Markirovka kodi",
            $"{item.ProductName}: {item.MarkingCodes.Count} / {item.RequiredMarkingCodes} kod.\n" +
            "DataMatrix kodini skanerlang yoki kiriting (bo'sh qoldirib OK — shu qatordagi barcha kodlarni o'chirish):");
        if (code is null) return;

        if (code.Length == 0)
        {
            if (item.MarkingCodes.Count == 0) return;
            if (MessageBox.Show($"\"{item.ProductName}\" uchun {item.MarkingCodes.Count} ta markirovka kodi o'chirilsinmi?",
                    "Markirovka kodi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            await SendMarkingCodes(item, new List<string>());
            return;
        }

        await AttachMarkingCode(item, code);
    }

    /// <summary>Kodni qatorga qo'shadi: takrorlanish va ortiqcha kod tekshiriladi, keyin serverga yuboriladi.</summary>
    private async Task AttachMarkingCode(OnlineOrderItemDto item, string code)
    {
        if (_selected is null) return;
        if (_selected.Items.Any(i => i.MarkingCodes.Contains(code)))
        {
            SystemSoundsBeep();
            ShowMessage("Bu markirovka kodi allaqachon skanerlangan.", error: true);
            return;
        }
        if (item.MarkingComplete)
        {
            SystemSoundsBeep();
            ShowMessage($"{item.ProductName}: kodlar to'liq ({item.MarkingCodes.Count} / {item.RequiredMarkingCodes}).", error: true);
            return;
        }

        var codes = item.MarkingCodes.Append(code).ToList();
        if (await SendMarkingCodes(item, codes))
        {
            var updated = _selected?.Items.FirstOrDefault(i => i.Id == item.Id);
            // Kodlar to'liq bo'lsa qator avtomatik belgilanadi.
            if (updated is not null && updated.MarkingComplete && _selected is not null)
            {
                CheckedItems[_selected.Id].Add(updated.Id);
                UpdateProgressAfterReload();
            }
            ShowMessage($"✓ {item.ProductName}: markirovka {updated?.MarkingCodes.Count ?? codes.Count} / {updated?.RequiredMarkingCodes ?? item.RequiredMarkingCodes}",
                error: false);
        }
    }

    /// <summary>PUT {id}/items — qatorning to'liq kodlar ro'yxati (almashtiriladi). Miqdor joriy holicha yuboriladi.</summary>
    private async Task<bool> SendMarkingCodes(OnlineOrderItemDto item, List<string> codes)
    {
        if (_selected is null) return false;
        try
        {
            var updated = await AppState.Backend.UpdateOnlineOrderItemsAsync(_selected.Id, new
            {
                items = new[]
                {
                    new { itemId = item.Id, pickedQuantity = (decimal?)item.EffectiveQuantity, isRemoved = false, markingCodes = codes }
                },
                add = Array.Empty<object>()
            });
            ShowOrder(updated);
            return true;
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
            return false;
        }
    }

    private async void ItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null || (sender as FrameworkElement)?.Tag is not PickRow row) return;
        if (MessageBox.Show($"\"{row.Item.ProductName}\" buyurtmadan olib tashlansinmi?\n{_selected.PolicyText}",
                "Mahsulot yo'q", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        await RunItemsUpdate(new
        {
            items = new[] { new { itemId = row.Item.Id, pickedQuantity = (decimal?)null, isRemoved = true } },
            add = Array.Empty<object>()
        });
    }

    private async void ItemReplace_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null || (sender as FrameworkElement)?.Tag is not PickRow row) return;
        try
        {
            await AddProduct(_selected, replaces: row.Item);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    /// <summary>Shtrix-kod bo'yicha mahsulot qo'shadi (almashtirishda eski qator olib tashlanadi).</summary>
    private async Task AddProduct(OnlineOrderDto o, OnlineOrderItemDto? replaces)
    {
        var code = PromptWindow.Ask(this, replaces is null ? "Mahsulot qo'shish" : "Almashtirish",
            (replaces is null ? "" : $"\"{replaces.ProductName}\" o'rniga. ") + "Yangi mahsulot shtrix-kodini skanerlang:");
        if (string.IsNullOrWhiteSpace(code)) return;

        var product = AppState.Offline.LoadProducts().FirstOrDefault(p => p.Barcode == code);
        if (product is null)
        {
            ShowMessage($"Shtrix-kod {code} bo'yicha mahsulot topilmadi (tovarlar ro'yxatini yangilang).", error: true);
            return;
        }

        var qtyText = PromptWindow.Ask(this, "Miqdor", $"{product.Name} — miqdor:",
            (replaces?.Quantity ?? 1).ToString("0.###", CultureInfo.CurrentCulture));
        if (qtyText is null) return;
        if (!TryParseQty(qtyText, out var qty) || qty <= 0)
        {
            ShowMessage("Miqdor noto'g'ri.", error: true);
            return;
        }

        await RunItemsUpdate(new
        {
            items = Array.Empty<object>(),
            add = new[] { new { productId = product.Id, quantity = qty, replacesItemId = replaces?.Id } }
        });
        ShowMessage($"✓ {product.Name} qo'shildi. Mijozga narx o'zgarishini ayting.", error: false);
    }

    private async Task RunItemsUpdate(object request)
    {
        if (_selected is null) return;
        try
        {
            var updated = await AppState.Backend.UpdateOnlineOrderItemsAsync(_selected.Id, request);
            ShowOrder(updated);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private void UpdateProgressAfterReload()
    {
        foreach (var r in _rows) r.Refresh();
        UpdateProgress();
    }

    // ───────────── skaner ─────────────

    private void ScanInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var code = ScanInput.Text.Trim();
        ScanInput.Clear();
        if (code.Length > 0) HandleScan(code);
    }

    private async void HandleScan(string code)
    {
        if (_selected?.Status != "Picking")
        {
            ShowMessage("Skanerlash uchun yig'ilayotgan buyurtmani tanlang.", error: true);
            return;
        }

        var row = _rows.FirstOrDefault(r => !r.Item.IsRemoved && !r.IsChecked && r.Item.Barcode == code)
                  ?? _rows.FirstOrDefault(r => !r.Item.IsRemoved && r.Item.Barcode == code);

        // Oddiy shtrix-kodga mos kelmadi — DataMatrix (markirovka) bo'lsa GTIN bo'yicha markirovkali qatorga biriktiramiz.
        if (row is null && LooksLikeDataMatrix(code))
        {
            var gtin = ExtractGtin(code);
            var candidates = _rows.Where(r => r.Item.IsMarked && !r.Item.IsRemoved && gtin is not null &&
                                              NormalizeGtin(r.Item.Barcode) == gtin).ToList();
            var target = candidates.FirstOrDefault(r => !r.Item.MarkingComplete) ?? candidates.FirstOrDefault();
            if (target is null)
            {
                SystemSoundsBeep();
                ShowMessage("Markirovka kodi bu buyurtmadagi tovarga mos kelmadi (GTIN). " +
                            "Kerakli qatordagi \"Markirovka kodi\" tugmasi orqali qo'lda biriktiring.", error: true);
                return;
            }
            ItemsList.ScrollIntoView(target);
            await AttachMarkingCode(target.Item, code);
            return;
        }

        if (row is null)
        {
            SystemSoundsBeep();
            ShowMessage($"{code} — bu buyurtmada yo'q.", error: true);
            return;
        }

        row.IsChecked = true;
        ItemsList.ScrollIntoView(row);
        ShowMessage($"✓ {row.Item.ProductName}", error: false);
    }

    /// <summary>
    /// DataMatrix (Asl belgisi) kodimi: GS (0x1D) belgisi bor yoki "01" + 14 raqamli GTIN bilan boshlanadigan uzun kod.
    /// </summary>
    private static bool LooksLikeDataMatrix(string code)
    {
        var c = StripDmPrefix(code);
        return code.Contains('\u001D') ||
               (c.Length >= 20 && c.StartsWith("01") && c.Skip(2).Take(14).All(char.IsDigit));
    }

    /// <summary>Ba'zi skanerlar qo'shadigan "]d2" (AIM) prefiksi va boshidagi GS belgisini olib tashlaydi.</summary>
    private static string StripDmPrefix(string code)
    {
        var c = code.StartsWith("]d2") ? code[3..] : code;
        return c.TrimStart('\u001D');
    }

    /// <summary>GTIN — koddagi 3..16-belgilar (14 raqam), boshidagi nollarsiz. Topilmasa null.</summary>
    private static string? ExtractGtin(string code)
    {
        var c = StripDmPrefix(code);
        if (c.Length < 16 || !c.StartsWith("01")) return null;
        var gtin = c.Substring(2, 14);
        return gtin.All(char.IsDigit) ? NormalizeGtin(gtin) : null;
    }

    /// <summary>Shtrix-kod/GTIN ni solishtirish uchun: faqat raqamlar, boshidagi nollarsiz (EAN-13 ↔ GTIN-14).</summary>
    private static string? NormalizeGtin(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var digits = barcode.Trim();
        if (!digits.All(char.IsDigit)) return null;
        var trimmed = digits.TrimStart('0');
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static void SystemSoundsBeep()
    {
        try { System.Media.SystemSounds.Hand.Play(); } catch { /* ovoz yo'q */ }
    }

    // ───────────── yig'ish varag'i ─────────────

    private void PrintSheet_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } o) return;
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var doc = new FlowDocument
        {
            PageWidth = dlg.PrintableAreaWidth,
            ColumnWidth = dlg.PrintableAreaWidth,
            PagePadding = new Thickness(24),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };
        doc.Blocks.Add(new Paragraph(new Bold(new Run($"Yig'ish varag'i — #{o.Number}"))) { FontSize = 16 });
        doc.Blocks.Add(new Paragraph(new Run(
            $"{o.CustomerName} · {o.CustomerPhone}\n{o.TypeText}: {o.AddressText}\n{o.PaymentText} · {o.PolicyText}" +
            (string.IsNullOrWhiteSpace(o.Comment) ? "" : $"\nIzoh: {o.Comment}"))));

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(30) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(120) });
        table.Columns.Add(new TableColumn { Width = new GridLength(70) });
        var group = new TableRowGroup();
        foreach (var i in o.Items.Where(i => !i.IsRemoved))
        {
            var tr = new TableRow();
            tr.Cells.Add(new TableCell(new Paragraph(new Run("☐"))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(i.ProductName))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(i.Barcode ?? ""))));
            tr.Cells.Add(new TableCell(new Paragraph(new Bold(new Run($"× {i.EffectiveQuantity:0.###}")))));
            group.Rows.Add(tr);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);
        doc.Blocks.Add(new Paragraph(new Bold(new Run($"Jami: {o.TotalAmount:N0} so'm"))));

        try
        {
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Yig'ish #{o.Number}");
        }
        catch (Exception ex)
        {
            ShowMessage("Chop etilmadi: " + ex.Message, error: true);
        }
    }

    // ───────────── sarlavha ─────────────

    private async void Accepting_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || !IsLoaded) return;
        var value = AcceptingCheck.IsChecked == true;
        try
        {
            var s = await AppState.Backend.SetAcceptingOrdersAsync(value);
            ShowMessage(s.IsAcceptingOrders ? "✓ Buyurtma qabul qilish yoqildi." : "Buyurtma qabul qilish to'xtatildi.", error: false);
        }
        catch (Exception ex)
        {
            _loadingSettings = true;
            AcceptingCheck.IsChecked = !value;
            _loadingSettings = false;
            ShowMessage(ex.Message, error: true);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
        if (_selected is not null) await SelectAsync(_selected.Id);
    }

    // ───────────── yordamchilar ─────────────

    private void ShowMessage(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = error ? Brushes.Firebrick : new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45));
    }

    private void OpenUrl(string url)
    {
        try
        {
            var full = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : AppState.BaseUrl.TrimEnd('/') + url;
            Process.Start(new ProcessStartInfo(full) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private static bool TryParseQty(string text, out decimal qty) =>
        decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out qty);

    /// <summary>Yig'ish ro'yxati qatori (belgilash faqat kassada).</summary>
    public sealed class PickRow : INotifyPropertyChanged
    {
        private readonly HashSet<Guid> _checked;
        private readonly Action _changed;

        public PickRow(OnlineOrderItemDto item, HashSet<Guid> checkedSet, bool editable, Action changed)
        {
            Item = item;
            _checked = checkedSet;
            _changed = changed;
            CanCheck = editable && !item.IsRemoved;
            EditVisibility = editable && !item.IsRemoved ? Visibility.Visible : Visibility.Collapsed;
            MarkingEditVisibility = editable && !item.IsRemoved && item.IsMarked ? Visibility.Visible : Visibility.Collapsed;
        }

        public OnlineOrderItemDto Item { get; }
        public bool CanCheck { get; }
        public Visibility EditVisibility { get; }
        /// <summary>"Markirovka kodi" tugmasi — faqat yig'ilayotgan markirovkali qatorda.</summary>
        public Visibility MarkingEditVisibility { get; }
        public TextDecorationCollection? Strike => Item.IsRemoved ? TextDecorations.Strikethrough : null;

        public bool IsChecked
        {
            get => _checked.Contains(Item.Id);
            set
            {
                if (value) _checked.Add(Item.Id); else _checked.Remove(Item.Id);
                Refresh();
                _changed();
            }
        }

        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
