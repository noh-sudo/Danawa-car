namespace Danawa.Shared.Protocol;

/// <summary>Magic 불일치 또는 크기 상한 초과 — 호출자가 응답 없이 연결 종료</summary>
public sealed class FrameProtocolException : Exception
{
    public FrameProtocolException(string message) : base(message)
    {
    }
}
