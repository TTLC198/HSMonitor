using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Threading;
using HSMonitor.Services.OtaUpdateService.Parts;
using HSMonitor.Utils.Logger;

namespace HSMonitor.Services.OtaUpdateService;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class OtaUpdateService(ILogger<OtaUpdateService> logger)
{
  private const int Chunk = 1024;

  public void SendOtaUpdate(byte[] data, string portName)
  {
    var crc = Crc32.Compute(data);
    
    using var sp = new SerialPort(portName, 921600, Parity.None, 8, StopBits.One);
    sp.ReadTimeout = 10000;
    sp.WriteTimeout = 10000;
    sp.DtrEnable = true;
    sp.RtsEnable = true;

    sp.Open();
        
    sp.DiscardInBuffer();
    sp.DiscardOutBuffer();
    Thread.Sleep(200);

    var client = new UsbOtaClient(sp);

    client.Hello();

    client.Begin((uint)data.Length, crc);

    int offset = 0;
    uint seq = 10;
    var started = DateTime.UtcNow;

    while (offset < data.Length)
    {
      var n = Math.Min(Chunk, data.Length - offset);
      client.Data(seq++, (uint)offset, data.AsSpan(offset, n));
      offset += n;

      if (seq % 50 == 0)
      {
        var pct = 100.0 * offset / data.Length;
        logger.Info($"{pct:0.0}% ({offset}/{data.Length})");
      }
    }

    client.End(seq);

    var elapsed = DateTime.UtcNow - started;
    logger.Info($"Done. Sent {data.Length} bytes in {elapsed.TotalSeconds:0.0}s");
    logger.Info("Device should reboot into the new firmware.");
  }
}
