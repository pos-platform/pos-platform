using System.Media;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Online buyurtmalar uchun SignalR klient (/hubs/orders). Kassa ochiq turgan butun vaqt ulanib turadi:
/// yangi buyurtmada ovozli signal beriladi va faol buyurtmalar soni yangilanadi (oyna yopiq bo'lsa ham).
/// Ulanish uzilsa avtomatik qayta ulanadi; ulangach ro'yxat API'dan qayta olinadi (o'tkazib yuborilgan xabar bo'lmasin).
/// </summary>
public sealed class OnlineOrdersHub : IAsyncDisposable
{
    private HubConnection? _connection;
    private CancellationTokenSource? _startCts;

    /// <summary>"Yangi" buyurtma qabul qilinmaguncha har 30 soniyada ovozni takrorlaydi (faqat StartAsync'dan keyin).</summary>
    private System.Threading.Timer? _repeatTimer;
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromSeconds(30);

    /// <summary>Yangi buyurtma keldi.</summary>
    public event Action<OnlineOrderEvent>? OrderCreated;

    /// <summary>Buyurtma holati o'zgardi.</summary>
    public event Action<OnlineOrderEvent>? OrderUpdated;

    /// <summary>Faol (Yangi) buyurtmalar soni o'zgardi.</summary>
    public event Action<int>? NewCountChanged;

    /// <summary>Ulanish holati o'zgardi (true — ulangan).</summary>
    public event Action<bool>? ConnectionChanged;

    public int NewCount { get; private set; }
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_connection is not null) return;

        _connection = new HubConnectionBuilder()
            .WithUrl(AppState.BaseUrl.TrimEnd('/') + "/hubs/orders", o =>
                o.AccessTokenProvider = () => Task.FromResult<string?>(AppState.Token))
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)])
            .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        _connection.On<OnlineOrderEvent>("OrderCreated", e =>
        {
            PlayNewOrderSound();
            // takroriy signal shu signaldan 30 s keyin davom etadi
            _repeatTimer?.Change(RepeatInterval, RepeatInterval);
            OrderCreated?.Invoke(e);
            _ = RefreshCountAsync();
        });
        _connection.On<OnlineOrderEvent>("OrderUpdated", e =>
        {
            OrderUpdated?.Invoke(e);
            _ = RefreshCountAsync();
        });

        _connection.Reconnecting += _ => { ConnectionChanged?.Invoke(false); return Task.CompletedTask; };
        _connection.Reconnected += async _ => { ConnectionChanged?.Invoke(true); await RefreshCountAsync(); };
        _connection.Closed += async _ =>
        {
            ConnectionChanged?.Invoke(false);
            // Avtomatik qayta ulanish taslim bo'lgach — o'zimiz har 30 soniyada urinamiz.
            await ConnectLoopAsync();
        };

        _startCts = new CancellationTokenSource();
        _repeatTimer = new System.Threading.Timer(_ =>
        {
            // Hub yopilgan bo'lsa yoki "Yangi" buyurtma qolmagan bo'lsa — jim.
            if (_connection is not null && NewCount > 0) PlayNewOrderSound();
        }, null, RepeatInterval, RepeatInterval);
        _ = ConnectLoopAsync();
        await RefreshCountAsync();
    }

    private async Task ConnectLoopAsync()
    {
        var ct = _startCts?.Token ?? CancellationToken.None;
        while (_connection is not null && !ct.IsCancellationRequested
               && _connection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _connection.StartAsync(ct);
                ConnectionChanged?.Invoke(true);
                await RefreshCountAsync();
                return;
            }
            catch when (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch { return; }
            }
            catch
            {
                return;
            }
        }
    }

    /// <summary>"Yangi" buyurtmalar sonini API'dan qayta oladi (badge uchun).</summary>
    public async Task RefreshCountAsync()
    {
        if (_connection is null) return; // hub ishga tushmagan yoki yopilgan
        try
        {
            var list = await AppState.Backend.GetOnlineOrdersAsync(active: true);
            var count = list.Count(o => o.Status == "New");
            NewCount = count;
            NewCountChanged?.Invoke(count);
        }
        catch
        {
            // tarmoq yo'q yoki huquq yo'q — badge o'zgarmaydi
        }
    }

    private static void PlayNewOrderSound()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // ovoz qurilmasi yo'q — e'tiborsiz
        }
    }

    public async ValueTask DisposeAsync()
    {
        _startCts?.Cancel();
        _repeatTimer?.Dispose();
        _repeatTimer = null;
        var c = _connection;
        _connection = null;
        if (c is not null)
        {
            try { await c.DisposeAsync(); } catch { /* yopilmoqda */ }
        }
    }
}
