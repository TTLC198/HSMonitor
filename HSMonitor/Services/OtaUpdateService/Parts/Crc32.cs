namespace HSMonitor.Services.OtaUpdateService.Parts;

static class Crc32
{
    // IEEE CRC32 (PKZIP), same as in firmware.
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var t in data)
        {
            crc ^= t;
            for (var k = 0; k < 8; k++)
            {
                var mask = (uint)-(int)(crc & 1u);
                crc = (crc >> 1) ^ (0xEDB88320u & mask);
            }
        }
        return ~crc;
    }
}
