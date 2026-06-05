using System.IO.Ports;

namespace PosPlatform.Desktop.Services;

/// <summary>COM port orqali barcode skanerdan ma'lumot o'qiydi.</summary>
public sealed class BarcodeScanner : IDisposable
{
    private SerialPort? _port;
    private string _buffer = "";

    public event Action<string>? BarcodeReceived;

    public bool IsOpen => _port?.IsOpen == true;

    public void Start(string portName, int baudRate = 9600)
    {
        Stop();
        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        _port.DataReceived += OnDataReceived;
        _port.Open();
        _buffer = "";
    }

    public void Stop()
    {
        if (_port is null) return;
        _port.DataReceived -= OnDataReceived;
        try { _port.Close(); } catch { }
        _port.Dispose();
        _port = null;
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port is null || !_port.IsOpen) return;
        try
        {
            _buffer += _port.ReadExisting();
            // Barcode skanerlar CR (\r) yoki CR+LF (\r\n) bilan tugaydi
            while (true)
            {
                var idx = _buffer.IndexOfAny(['\r', '\n']);
                if (idx < 0) break;
                var barcode = _buffer[..idx].Trim();
                _buffer = _buffer[(idx + 1)..].TrimStart('\n');
                if (barcode.Length > 0)
                    BarcodeReceived?.Invoke(barcode);
            }
        }
        catch { /* port yopilgan bo'lishi mumkin */ }
    }

    public void Dispose() => Stop();

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
}
