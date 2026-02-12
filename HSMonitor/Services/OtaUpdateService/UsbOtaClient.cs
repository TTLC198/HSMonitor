using System.IO.Ports;
using HSMonitor.Services.OtaUpdateService.Parts;

namespace HSMonitor.Services.OtaUpdateService;

sealed class UsbOtaClient
{
    private readonly SerialPort _sp;

    // Must match firmware.
    private const uint Magic = 0x544F5348; // 'HSOT' LE
    private const ushort HdrSize = 24;

    private enum Cmd : byte
    {
        Hello = 1,
        Begin = 2,
        Data  = 3,
        End   = 4,
        Abort = 5,
        Status = 6,
        Version = 7
    }

    private enum StatusCode : byte
    {
        Ok = 0,
        ErrBadFrame = 1,
        ErrBusy = 2,
        ErrOta = 3,
        ErrCrc = 4,
        ErrOffset = 5,
        ErrSize = 6,
    }

    public UsbOtaClient(SerialPort sp) => _sp = sp;

    public void Hello()
    {
        SendFrame(Cmd.Hello, seq: 1, payload: ReadOnlySpan<byte>.Empty);
        var st = ReadStatus();
        EnsureOk(st);
    }

    public void Begin(uint imageSize, uint imageCrc32)
    {
        Span<byte> payload = stackalloc byte[8];
        WriteU32(payload, 0, imageSize);
        WriteU32(payload, 4, imageCrc32);

        SendFrame(Cmd.Begin, seq: 2, payload);
        var st = ReadStatus();
        EnsureOk(st);
    }
    
    /// <summary>
    /// Запросить у устройства текущую версию проекта (ESP-IDF app version).
    /// </summary>
    public string GetProjectVersion(uint seq = 10)
    {
        SendFrame(Cmd.Version, seq, ReadOnlySpan<byte>.Empty);

        var fr = ReadFrame();

        // Старые прошивки вернут Status(ERR_NOT_SUPPORTED) как неизвестную команду.
        if (fr.Cmd == Cmd.Status)
        {
            var st = ParseStatusPayload(fr.Payload);
            EnsureOk(st);
            throw new InvalidOperationException("Device did not return version payload. Firmware is likely too old.");
        }

        if (fr.Cmd != Cmd.Version)
            throw new InvalidOperationException($"Unexpected response: cmd={fr.Cmd} payloadLen={fr.Payload.Length}");

        // payload = StatusPayload(12) + version[32]
        if (fr.Payload.Length < 12)
            throw new InvalidOperationException($"Bad Version payload length: {fr.Payload.Length}");

        var st2 = ParseStatusPayload(fr.Payload.AsSpan(0, 12));
        EnsureOk(st2);

        var verSpan = fr.Payload.AsSpan(12);
        int z = verSpan.IndexOf((byte)0);
        if (z >= 0) verSpan = verSpan.Slice(0, z);
        return System.Text.Encoding.UTF8.GetString(verSpan);
    }

    public void Data(uint seq, uint offset, ReadOnlySpan<byte> chunk)
    {
        // payload = [offset u32][bytes...]
        byte[] payload = new byte[4 + chunk.Length];
        WriteU32(payload, 0, offset);
        chunk.CopyTo(payload.AsSpan(4));

        SendFrame(Cmd.Data, seq, payload);
        var st = ReadStatus();
        if (st.StatusCode == StatusCode.ErrOffset)
        {
            // Firmware tells us expected offset in st.Received.
            // Simple resend strategy (caller can loop).
            throw new InvalidOperationException($"Offset mismatch. Device expects {st.Received}, host sent {offset}.");
        }
        EnsureOk(st);
    }
    
    public void Abort(uint seq = 3)
    {
        SendFrame(Cmd.Abort, seq, ReadOnlySpan<byte>.Empty);
        var st = ReadStatus();
        EnsureOk(st);
    }

    public void End(uint seq)
    {
        SendFrame(Cmd.End, seq, ReadOnlySpan<byte>.Empty);
        var st = ReadStatus();
        EnsureOk(st);
    }

    // ----- framing -----

    private readonly struct Status
    {
        public readonly StatusCode StatusCode;
        public readonly uint Received;
        public readonly int EspErr;

        public Status(StatusCode statusCode, uint received, int espErr)
        {
            StatusCode = statusCode;
            Received = received;
            EspErr = espErr;
        }
    }

    private void EnsureOk(Status st)
    {
        if (st.StatusCode != StatusCode.Ok)
        {
            throw new InvalidOperationException($"Device status={st.StatusCode}, received={st.Received}, esp_err={st.EspErr}");
        }
    }

    private void SendFrame(Cmd cmd, uint seq, ReadOnlySpan<byte> payload)
    {
        var hdr = new byte[HdrSize];

        WriteU32(hdr, 0, Magic);
        hdr[4] = (byte)cmd;
        hdr[5] = 0; // flags
        WriteU16(hdr, 6, HdrSize);
        WriteU32(hdr, 8, (uint)payload.Length);
        WriteU32(hdr, 12, seq);
        uint crc = Crc32.Compute(payload);
        WriteU32(hdr, 16, crc);
        WriteU32(hdr, 20, 0);

        _sp.Write(hdr, 0, hdr.Length);
        if (payload.Length <= 0)
            return;
        
        // SerialPort doesn't accept span directly on old frameworks.
        byte[] buf = payload.ToArray();
        _sp.Write(buf, 0, buf.Length);
    }
    
    private Status ParseStatusPayload(ReadOnlySpan<byte> payload)
    {
        var st = (StatusCode)payload[0];
        var received = ReadU32(payload, 4);
        var espErr = unchecked((int)ReadU32(payload, 8));
        return new Status(st, received, espErr);
    }

    private Status ReadStatus()
    {
        // FrameHdr 24 bytes + StatusPayload 12 bytes
        var hdr = ReadExact(HdrSize);
        var magic = ReadU32(hdr, 0);
        if (magic != Magic) throw new InvalidOperationException("Bad magic from device.");

        var cmd = (Cmd)hdr[4];
        var hdrSize = ReadU16(hdr, 6);
        var payloadLen = ReadU32(hdr, 8);
        ReadU32(hdr, 12);
        var crc = ReadU32(hdr, 16);

        if (cmd != Cmd.Status || hdrSize != HdrSize || payloadLen != 12)
            throw new InvalidOperationException($"Unexpected response: cmd={cmd} payloadLen={payloadLen}");

        var payload = ReadExact((int)payloadLen);
        var calc = Crc32.Compute(payload);
        if (calc != crc) throw new InvalidOperationException("Bad CRC from device.");

        var st = (StatusCode)payload[0];
        var received = ReadU32(payload, 4);
        var espErr = unchecked((int)ReadU32(payload, 8));

        return new Status(st, received, espErr);
    }

    private byte[] ReadExact(int len)
    {
        var buf = new byte[len];
        int off = 0;

        while (off < len)
        {
            int n = _sp.Read(buf, off, len - off);
            if (n > 0) off += n;
        }
        return buf;
    }
    
    private Frame ReadFrame()
    {
        var hdr = ReadExact(HdrSize);
        var magic = ReadU32(hdr, 0);
        if (magic != Magic) throw new InvalidOperationException("Bad magic from device.");

        var cmd = (Cmd)hdr[4];
        var hdrSize = ReadU16(hdr, 6);
        var payloadLen = ReadU32(hdr, 8);
        var seq = ReadU32(hdr, 12);
        var crc = ReadU32(hdr, 16);

        if (hdrSize != HdrSize)
            throw new InvalidOperationException($"Unexpected header size: {hdrSize}");
        if (payloadLen > 256 * 1024)
            throw new InvalidOperationException($"Payload too large: {payloadLen}");

        var payload = payloadLen == 0 ? [] : ReadExact((int)payloadLen);
        var calc = Crc32.Compute(payload);
        if (calc != crc) throw new InvalidOperationException("Bad CRC from device.");

        return new Frame(cmd, seq, payload);
    }

    // ----- little-endian helpers -----
    private static void WriteU16(Span<byte> b, int o, ushort v)
    {
        b[o + 0] = (byte)(v & 0xFF);
        b[o + 1] = (byte)((v >> 8) & 0xFF);
    }

    private static void WriteU32(Span<byte> b, int o, uint v)
    {
        b[o + 0] = (byte)(v & 0xFF);
        b[o + 1] = (byte)((v >> 8) & 0xFF);
        b[o + 2] = (byte)((v >> 16) & 0xFF);
        b[o + 3] = (byte)((v >> 24) & 0xFF);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> b, int o)
        => (ushort)(b[o + 0] | (b[o + 1] << 8));

    private static uint ReadU32(ReadOnlySpan<byte> b, int o)
        => (uint)(b[o + 0] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
    
    private readonly struct Frame
    {
        public readonly Cmd Cmd;
        public readonly uint Seq;
        public readonly byte[] Payload;

        public Frame(Cmd cmd, uint seq, byte[] payload)
        {
            Cmd = cmd;
            Seq = seq;
            Payload = payload;
        }
    }
}
