using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Danawa.Server.Handlers;
using Danawa.Shared.Contracts;
using Danawa.Shared.Protocol;

namespace Danawa.Server.Net;

/// <summary>연결 1개 = 세션 1개</summary>
public sealed class ClientSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly string _remote;

    public int? UserId { get; set; }
    public string? Role { get; set; }
    public string RemoteIp { get; }

    public ClientSession(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        _remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        RemoteIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "unknown";
    }

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Frame request;
                try
                {
                    request = await FrameCodec.ReadAsync(_stream, ct);
                }
                catch (FrameProtocolException ex)
                {
                    Console.WriteLine($"[{_remote}] 프로토콜 위반으로 연결 종료: {ex.Message}");
                    return;
                }
                catch (Exception)
                {
                    // 클라이언트가 연결을 끊음 (EndOfStream 등)
                    return;
                }

                await HandleRequestAsync(request, ct);
            }
        }
        finally
        {
            _client.Close();
        }
    }

    private async Task HandleRequestAsync(Frame request, CancellationToken ct)
    {
        RequestBase? req;
        try
        {
            req = JsonSerializer.Deserialize<RequestBase>(request.Json);
        }
        catch (JsonException)
        {
            await SendAsync(new ResponseBase
            {
                Type = string.Empty,
                ReqId = 0,
                Result = "ERROR",
                Code = ResultCode.Validation,
                Message = "잘못된 JSON"
            }, ct);
            return;
        }

        if (req is null)
        {
            return;
        }

        try
        {
            if (req.Type == RequestType.ImageGet)
            {
                (ResponseBase imageResponse, byte[]? blob) = await CarHandler.ImageGetAsync(request.Json);
                await SendAsync(imageResponse, ct, blob);
                return;
            }

            // admin_* type은 전부 여기 한 곳에서만 권한 검사(ADM-01) — 핸들러들은 이미 ADMIN인 걸 전제
            if (req.Type.StartsWith("admin_", StringComparison.Ordinal))
            {
                if (UserId is null)
                {
                    await SendAsync(new ResponseBase
                    {
                        Type = req.Type,
                        ReqId = req.ReqId,
                        Result = "ERROR",
                        Code = ResultCode.Unauthorized,
                        Message = "로그인이 필요합니다."
                    }, ct);
                    return;
                }

                if (Role != "ADMIN")
                {
                    await SendAsync(new ResponseBase
                    {
                        Type = req.Type,
                        ReqId = req.ReqId,
                        Result = "ERROR",
                        Code = ResultCode.Forbidden,
                        Message = "관리자만 사용할 수 있습니다."
                    }, ct);
                    return;
                }
            }

            Task<ResponseBase> responseTask = req.Type switch
            {
                RequestType.Ping => Task.FromResult(new ResponseBase
                {
                    Type = req.Type,
                    ReqId = req.ReqId,
                    Result = "OK",
                    Code = ResultCode.Ok
                }),
                RequestType.CheckId => AuthHandler.CheckIdAsync(request.Json),
                RequestType.Signup => AuthHandler.SignupAsync(request.Json),
                RequestType.Login => AuthHandler.LoginAsync(request.Json, this),
                RequestType.Logout => AuthHandler.LogoutAsync(req, this),
                RequestType.BrandList => CarHandler.BrandListAsync(request.Json),
                RequestType.CarList => CarHandler.CarListAsync(request.Json),
                RequestType.CarSearch => CarHandler.CarSearchAsync(request.Json),
                RequestType.CarDetail => CarHandler.CarDetailAsync(request.Json, this),
                RequestType.MainDailyTop => CarHandler.MainDailyTopAsync(request.Json),
                RequestType.MainFavoriteTop5 => CarHandler.MainFavoriteTop5Async(request.Json),
                RequestType.MainSummary => CarHandler.MainSummaryAsync(request.Json, this),
                RequestType.FavoriteSet => FavoriteHandler.FavoriteSetAsync(request.Json, this),
                RequestType.FavoriteList => FavoriteHandler.FavoriteListAsync(request.Json, this),
                RequestType.QuoteAdd => QuoteHandler.QuoteAddAsync(request.Json, this),
                RequestType.QuoteList => QuoteHandler.QuoteListAsync(request.Json, this),
                RequestType.QuoteDelete => QuoteHandler.QuoteDeleteAsync(request.Json, this),
                RequestType.AdminBrandList => AdminHandler.BrandListAsync(request.Json),
                RequestType.AdminBrandSave => AdminHandler.BrandSaveAsync(request.Json),
                RequestType.AdminCarList => AdminHandler.CarListAsync(request.Json),
                RequestType.AdminCarSave => AdminHandler.CarSaveAsync(request.Json),
                RequestType.AdminCarDelete => AdminHandler.CarDeleteAsync(request.Json),
                RequestType.AdminOptionList => AdminHandler.OptionListAsync(request.Json),
                RequestType.AdminOptionSave => AdminHandler.OptionSaveAsync(request.Json),
                RequestType.AdminOptionDelete => AdminHandler.OptionDeleteAsync(request.Json),
                RequestType.AdminStatsSummary => AdminHandler.StatsSummaryAsync(request.Json),
                RequestType.AdminStatsWeeklyTop5 => AdminHandler.StatsWeeklyTop5Async(request.Json),
                RequestType.AdminStatsDaily => AdminHandler.StatsDailyAsync(request.Json),
                RequestType.AdminLogList => AdminHandler.LogListAsync(request.Json),
                _ => Task.FromResult(new ResponseBase
                {
                    Type = req.Type,
                    ReqId = req.ReqId,
                    Result = "ERROR",
                    Code = ResultCode.UnknownType,
                    Message = $"알 수 없는 type: {req.Type}"
                })
            };

            ResponseBase response = await responseTask;
            await SendAsync(response, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_remote}] 핸들러 예외: {ex}");
            await SendAsync(new ResponseBase
            {
                Type = req.Type,
                ReqId = req.ReqId,
                Result = "ERROR",
                Code = ResultCode.ServerError,
                Message = "서버 오류가 발생했습니다"
            }, ct);
        }
    }

    private async Task SendAsync(ResponseBase response, CancellationToken ct, byte[]? blob = null)
    {
        var frame = new Frame { Json = JsonSerializer.Serialize(response, response.GetType()), Blob = blob };

        await _sendLock.WaitAsync(ct);
        try
        {
            await FrameCodec.WriteAsync(_stream, frame, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
