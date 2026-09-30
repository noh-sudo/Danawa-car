namespace Danawa.Shared.Protocol;

public static class ProtocolLimits
{
    public static readonly byte[] Magic = { (byte)'D', (byte)'N', (byte)'W', (byte)'A' };

    public const int HeaderSize = 12;

    public const int MaxJsonSize = 1 * 1024 * 1024;

    public const int MaxBlobSize = 10 * 1024 * 1024;
}
