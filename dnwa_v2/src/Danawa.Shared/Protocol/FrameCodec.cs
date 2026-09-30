using System.Buffers.Binary;
using System.Text;

namespace Danawa.Shared.Protocol;

public static class FrameCodec
{
    public static async Task WriteAsync(Stream stream, Frame frame, CancellationToken ct = default)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(frame.Json);
        byte[] blobBytes = frame.Blob ?? Array.Empty<byte>();

        byte[] buffer = new byte[ProtocolLimits.HeaderSize + jsonBytes.Length + blobBytes.Length];
        ProtocolLimits.Magic.CopyTo(buffer, 0);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4, 4), (uint)jsonBytes.Length);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(8, 4), (uint)blobBytes.Length);
        jsonBytes.CopyTo(buffer, ProtocolLimits.HeaderSize);
        blobBytes.CopyTo(buffer, ProtocolLimits.HeaderSize + jsonBytes.Length);

        await stream.WriteAsync(buffer, ct);
    }

    public static async Task<Frame> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] header = new byte[ProtocolLimits.HeaderSize];
        await stream.ReadExactlyAsync(header, ct);

        if (!header.AsSpan(0, 4).SequenceEqual(ProtocolLimits.Magic))
        {
            throw new FrameProtocolException("Magic 불일치");
        }

        uint jsonSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4, 4));
        uint blobSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8, 4));

        if (jsonSize > ProtocolLimits.MaxJsonSize || blobSize > ProtocolLimits.MaxBlobSize)
        {
            throw new FrameProtocolException("크기 상한 초과");
        }

        byte[] jsonBytes = new byte[jsonSize];
        await stream.ReadExactlyAsync(jsonBytes, ct);

        byte[]? blob = null;
        if (blobSize > 0)
        {
            blob = new byte[blobSize];
            await stream.ReadExactlyAsync(blob, ct);
        }

        return new Frame
        {
            Json = Encoding.UTF8.GetString(jsonBytes),
            Blob = blob
        };
    }
}
