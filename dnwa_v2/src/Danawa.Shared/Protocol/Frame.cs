namespace Danawa.Shared.Protocol;

public sealed class Frame
{
    public required string Json { get; init; }

    public byte[]? Blob { get; init; }
}
