using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using Danawa.Shared.Contracts;
using Danawa.Shared.Protocol;

namespace Danawa.Client.Services;

/// <summary>서버 접속, req_id 매칭, 연결 상태 알림 담당 — 연결 상태 표시등(COM-08)은 4단계에서 이 클래스의 ConnectionChanged 이벤트에 연결</summary>
public sealed class ServerConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Frame>> _pending = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _reqIdSeq;
    private string _host = string.Empty;
    private int _port;

    public event Action<bool, string?>? ConnectionChanged;

    /// <summary>현재 연결 상태의 스냅샷 — ConnectAsync가 ShellViewModel 생성보다 먼저 끝나 ConnectionChanged 이벤트(최초 연결)를 놓치는 경우 대비</summary>
    public bool IsConnected { get; private set; }

    public string? LastConnectionMessage { get; private set; }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        // 접속 실패해도 주소는 기억 — 점검 화면의 [재접속]이 같은 주소로 재시도
        _host = host;
        _port = port;

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);
            _client = client;
            _stream = client.GetStream();
            SetConnectionState(true, null);

            _ = ReceiveLoopAsync(_stream, ct);
        }
        catch (Exception ex)
        {
            SetConnectionState(false, ex.Message);
            throw;
        }
    }

    /// <summary>끊긴 뒤 같은 주소로 새 TCP 연결(서버 점검 화면의 [재접속]) — 새 연결은 새 세션, 서버 쪽 로그인 상태는 안 이어짐</summary>
    public Task ReconnectAsync(CancellationToken ct = default) => ConnectAsync(_host, _port, ct);

    private void SetConnectionState(bool connected, string? message)
    {
        IsConnected = connected;
        LastConnectionMessage = message;
        ConnectionChanged?.Invoke(connected, message);
    }

    public Task<ResponseBase> SendAsync(RequestBase request, CancellationToken ct = default) =>
        SendAsync<ResponseBase>(request, ct);

    /// <summary>응답을 TResponse(ResponseBase 파생 타입)로 역직렬화해 반환, 인터페이스별 응답 필드(예: LoginResponse.Role)가 필요할 때 이 오버로드 사용</summary>
    public async Task<TResponse> SendAsync<TResponse>(RequestBase request, CancellationToken ct = default)
        where TResponse : ResponseBase
    {
        Frame frame = await SendCoreAsync(request, ct);
        return JsonSerializer.Deserialize<TResponse>(frame.Json)!;
    }

    /// <summary>image_get처럼 Blob 영역이 함께 오는 응답을 받을 때 사용</summary>
    public async Task<(TResponse Response, byte[]? Blob)> SendForBlobAsync<TResponse>(
        RequestBase request, CancellationToken ct = default)
        where TResponse : ResponseBase
    {
        Frame frame = await SendCoreAsync(request, ct);
        return (JsonSerializer.Deserialize<TResponse>(frame.Json)!, frame.Blob);
    }

    private async Task<Frame> SendCoreAsync(RequestBase request, CancellationToken ct)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다");
        }

        int reqId = Interlocked.Increment(ref _reqIdSeq);
        request.ReqId = reqId;

        var tcs = new TaskCompletionSource<Frame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[reqId] = tcs;

        var outFrame = new Frame { Json = JsonSerializer.Serialize(request, request.GetType()) };

        await _sendLock.WaitAsync(ct);
        try
        {
            await FrameCodec.WriteAsync(_stream, outFrame, ct);
        }
        finally
        {
            _sendLock.Release();
        }

        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    }

    /// <summary>수신 루프는 자기가 시작한 연결의 스트림만 읽음 — 재접속 뒤 옛 루프가 새 연결 상태를 건드리지 않기 위함</summary>
    private async Task ReceiveLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Frame frame = await FrameCodec.ReadAsync(stream, ct);
                using JsonDocument doc = JsonDocument.Parse(frame.Json);
                int reqId = doc.RootElement.GetProperty("req_id").GetInt32();
                if (_pending.TryRemove(reqId, out var tcs))
                {
                    tcs.TrySetResult(frame);
                }
            }
        }
        catch (Exception ex)
        {
            if (!ReferenceEquals(stream, _stream))
            {
                return;
            }

            // 끊긴 연결은 버림 — 이후 SendAsync는 쓰기 시도 없이 바로 예외 던짐
            _stream = null;
            _client?.Dispose();
            _client = null;

            foreach (int reqId in _pending.Keys)
            {
                if (_pending.TryRemove(reqId, out var pending))
                {
                    pending.TrySetException(ex);
                }
            }

            SetConnectionState(false, ex.Message);
        }
    }
}
