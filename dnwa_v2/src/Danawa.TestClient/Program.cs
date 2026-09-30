using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Danawa.Shared.Contracts;
using Danawa.Shared.Protocol;
using MySqlConnector;

const string Host = "127.0.0.1";
const int Port = 9000;
// Pooling=false: 검증용 조회 연결이 풀에 남아 V9-pool의 "서버 DB 연결 수" 집계에 섞이지 않게 한다.
const string DbConnectionString = "Server=127.0.0.1;Port=3306;Database=danawa_car_v2;User ID=danawa_app;Password=App!Danawa2026;Pooling=false;";

int passed = 0;
int failed = 0;

// V9-4는 DB(MySQL80 서비스)를 멈춘 상태에서만 의미가 있어 따로 실행한다: dotnet Danawa.TestClient.dll --db-down
if (args.Length > 0 && args[0] == "--db-down")
{
    await RunAsync("V9-4 DB 중단 -> SERVER_ERROR, 연결·서버 유지", V94_DbDownServerError);
    Console.WriteLine();
    Console.WriteLine($"=== 결과: {passed}개 통과 / {failed}개 실패 ===");
    Environment.Exit(failed == 0 ? 0 : 1);
}

await RunAsync("V3-1 정상 왕복", V31_NormalRoundTrip);
await RunAsync("V3-2 부분 수신 조립", V32_PartialReceive);
await RunAsync("V3-3 Magic 불일치", V33_BadMagic);
await RunAsync("V3-4 크기 상한 초과", V34_OversizedJson);
await RunAsync("V3-5 모르는 type", V35_UnknownType);
await RunAsync("V3-6 한글 왕복", V36_KoreanRoundTrip);
await RunAsync("V3-7 다중 접속", V37_MultipleConnections);

await RunAsync("V4-1 회원가입 -> DB 확인", V41_SignupPersisted);
await RunAsync("V4-2 role 위조 시도", V42_RoleForgeryIgnored);
await RunAsync("V4-3 아이디 중복 가입", V43_DuplicateSignup);
await RunAsync("V4-4 없는 아이디/틀린 비번 동일 메시지", V44_LoginFailureMessagesMatch);
await RunAsync("V4-5 같은 비밀번호 두 계정 -> 해시 다름", V45_SamePasswordDifferentHash);
await RunAsync("V4-6 로그인->로그아웃 access_log 기록", V46_LoginLogoutAccessLog);
await RunAsync("V4-7 비로그인 상태 로그인 필요 기능 호출", V47_UnauthorizedWithoutLogin);
await RunAsync("V4-admin admin 계정 로그인", V4admin_AdminLogin);

await RunAsync("V5-1 브랜드 -> 차량 목록", V51_BrandToCarList);
await RunAsync("V5-1b GNB 전체차량/국산차/수입차", V51b_CarListAllAndOrigin);
await RunAsync("V5-2 검색 \"아반\" (차량명 부분일치)", V52_SearchByCarName);
await RunAsync("V5-3 검색 \"현대\" (브랜드명 부분일치)", V53_SearchByBrandName);
await RunAsync("V5-4 검색 \"zzzz\" -> OK 빈 배열", V54_SearchNoResults);
await RunAsync("V5-5 소프트 삭제 후 목록/검색/상세에서 제외", V55_SoftDeleteExcluded);
await RunAsync("V5-7 최근 7일 경계 (관심 TOP5)", V57_FavoriteTop5WeekBoundary);
await RunAsync("V5-daily 오늘의 인기 차량", V5daily_MainDailyTop);
await RunAsync("V5-detail 차량 상세(옵션/관심여부)", V5detail_CarDetail);
await RunAsync("V5-summary 메인 견적 현황", V5summary_MainSummary);
await RunAsync("V5-10 이미지 Blob 수신", V510_ImageGet);
await RunAsync("V5-img 이미지 없음/경로 탈출 차단", V5img_NotFoundAndTraversalBlocked);

await RunAsync("V6-1 견적 금액 위조 시도 무시", V61_QuoteAmountForgeryIgnored);
await RunAsync("V6-2 다른 차량 옵션 -> VALIDATION + ROLLBACK", V62_WrongCarOptionRejected);
await RunAsync("V6-3 정상 견적 저장", V63_NormalQuoteSaved);
await RunAsync("V6-4 옵션 가격 변경 후에도 견적함 금액 그대로", V64_PriceChangeDoesNotAffectSavedQuote);
await RunAsync("V6-5 견적함 조회 -> 카드 1개", V65_QuoteListSingleCard);
await RunAsync("V6-6 견적 삭제 -> 소프트 삭제", V66_QuoteDeleteSoftDelete);
await RunAsync("V6-7 남의 견적 삭제 시도 -> NOT_FOUND", V67_DeleteOthersQuoteNotFound);
await RunAsync("V6-8 하트 두 번 클릭 -> 등록/해제, 중복 없음", V68_FavoriteToggleIdempotent);
await RunAsync("V6-9 ADMIN 관심/견적 요청 -> FORBIDDEN", V69_AdminForbidden);
await RunAsync("V6-10 user_id 위조 시도 무시", V610_UserIdForgeryIgnored);

await RunAsync("V7-1 USER 세션 admin_car_list -> FORBIDDEN", V71_UserForbiddenAdminCarList);
await RunAsync("V7-2 비로그인 admin_* -> UNAUTHORIZED", V72_UnauthenticatedAdminUnauthorized);
await RunAsync("V7-3 같은 브랜드 같은 차량명 -> CONFLICT", V73_DuplicateCarNameConflict);
await RunAsync("V7-4 차량 삭제 -> 소프트 삭제, 견적 보존", V74_CarDeleteSoftDeletePreservesQuotes);
await RunAsync("V7-5 옵션 등록/수정/삭제", V75_OptionCrud);
await RunAsync("V7-6 같은 차량 같은 옵션명 -> CONFLICT", V76_DuplicateOptionNameConflict);
await RunAsync("V7-8 기간별 추이 -> 0건 날짜 제외 + 날짜 역전 VALIDATION", V78_StatsDailyExcludesZeroDays);
await RunAsync("V7-9 주간 TOP5 견적 기준 (DB 직접 대조)", V79_WeeklyTop5MatchesQuoteBasedDbQuery);
await RunAsync("V7-10 로그 조회 -> LOGIN/LOGOUT, IP/시각", V710_LogListShowsLoginAndLogout);

await RunAsync("V8-1 정렬 POPULAR -> 최근 7일 관심 수 (DB 직접 대조)", V81_SortPopularMatchesDb);
await RunAsync("V8-2 정렬 PRICE_ASC -> 가격 오름차순, 목록 구성 동일", V82_SortPriceAsc);
await RunAsync("V8-3 모르는 정렬값 -> VALIDATION, 연결 유지", V83_InvalidSortRejected);
await RunAsync("V8-4 관심 목록 option_total (DB 직접 대조)", V84_FavoriteListOptionTotal);
await RunAsync("V8-5 견적함 brand_name/image_url (DB 직접 대조)", V85_QuoteListBrandAndImage);
await RunAsync("V8-6 관리자 요약/주간 TOP5 brand_name (DB 직접 대조)", V86_AdminStatsBrandName);

await RunAsync("V9-1 동시 접속 5개 각자 견적 저장 -> 섞이지 않음", V91_ConcurrentQuotesNotMixed);
await RunAsync("V9-2 한 연결 연속 발사 20건 -> req_id 매칭", V92_PipelinedRequestsMatchReqId);
await RunAsync("V9-pool 동시 12세션 부하 -> 서버 DB 연결 5개 이하", V9pool_ServerDbConnectionsAtMostFive);
await RunAsync("V9-5a 브랜드 수정 (ADM-02)", V95a_AdminBrandUpdate);

Console.WriteLine();
Console.WriteLine($"=== 결과: {passed}개 통과 / {failed}개 실패 ===");
Environment.Exit(failed == 0 ? 0 : 1);

async Task RunAsync(string name, Func<Task> scenario)
{
    try
    {
        await scenario();
        Console.WriteLine($"[PASS] {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {name} - {ex.Message}");
        failed++;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

static async Task<TcpClient> ConnectAsync()
{
    var client = new TcpClient();
    await client.ConnectAsync(Host, Port);
    return client;
}

static async Task V31_NormalRoundTrip()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var req = new RequestBase { Type = RequestType.Ping, ReqId = 1 };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req) });

    Frame respFrame = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;

    Assert(resp.ReqId == 1, $"req_id 불일치: {resp.ReqId}");
    Assert(resp.Result == "OK", $"result 불일치: {resp.Result}");
}

static async Task V32_PartialReceive()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var req = new RequestBase { Type = RequestType.Ping, ReqId = 2 };
    string json = JsonSerializer.Serialize(req);
    byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

    byte[] header = new byte[ProtocolLimits.HeaderSize];
    ProtocolLimits.Magic.CopyTo(header, 0);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), (uint)jsonBytes.Length);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8, 4), 0u);

    byte[] full = new byte[header.Length + jsonBytes.Length];
    header.CopyTo(full, 0);
    jsonBytes.CopyTo(full, header.Length);

    // 프레임을 3~4조각으로 쪼개 지연을 두고 전송한다.
    int chunkCount = 4;
    int chunkSize = Math.Max(1, full.Length / chunkCount);
    for (int offset = 0; offset < full.Length; offset += chunkSize)
    {
        int len = Math.Min(chunkSize, full.Length - offset);
        await stream.WriteAsync(full.AsMemory(offset, len));
        await Task.Delay(50);
    }

    Frame respFrame = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;

    Assert(resp.ReqId == 2, $"req_id 불일치: {resp.ReqId}");
    Assert(resp.Result == "OK", $"result 불일치: {resp.Result}");
}

static async Task V33_BadMagic()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    byte[] header = new byte[ProtocolLimits.HeaderSize];
    Encoding.ASCII.GetBytes("XXXX").CopyTo(header, 0);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), 2u);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8, 4), 0u);
    await stream.WriteAsync(header);
    await stream.WriteAsync(Encoding.UTF8.GetBytes("{}"));

    bool threwOrClosed = false;
    try
    {
        byte[] probe = new byte[1];
        int read = await stream.ReadAsync(probe);
        threwOrClosed = read == 0; // 0바이트 읽힘 = 연결 종료(EOF), 응답 없음
    }
    catch
    {
        threwOrClosed = true;
    }

    Assert(threwOrClosed, "Magic 불일치인데도 응답을 받았거나 연결이 유지됨");
}

static async Task V34_OversizedJson()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    byte[] header = new byte[ProtocolLimits.HeaderSize];
    ProtocolLimits.Magic.CopyTo(header, 0);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), 2_000_000u);
    BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8, 4), 0u);
    await stream.WriteAsync(header);

    bool threwOrClosed = false;
    try
    {
        byte[] probe = new byte[1];
        int read = await stream.ReadAsync(probe);
        threwOrClosed = read == 0;
    }
    catch
    {
        threwOrClosed = true;
    }

    Assert(threwOrClosed, "크기 상한 초과인데도 응답을 받았거나 연결이 유지됨");
}

static async Task V35_UnknownType()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var unknownReq = new RequestBase { Type = "nope", ReqId = 5 };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(unknownReq) });

    Frame respFrame = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;
    Assert(resp.Code == ResultCode.UnknownType, $"code 불일치: {resp.Code}");
    Assert(resp.Result == "ERROR", $"result 불일치: {resp.Result}");

    // 연결이 유지되는지 이어서 ping으로 확인
    var pingReq = new RequestBase { Type = RequestType.Ping, ReqId = 6 };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(pingReq) });
    Frame pingRespFrame = await FrameCodec.ReadAsync(stream);
    var pingResp = JsonSerializer.Deserialize<ResponseBase>(pingRespFrame.Json)!;
    Assert(pingResp.ReqId == 6 && pingResp.Result == "OK", "UNKNOWN_TYPE 이후 연결이 유지되지 않음");
}

static async Task V36_KoreanRoundTrip()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    // ping 요청에 한글 필드를 얹어 UTF-8 바이트 길이 기준 프레이밍을 검증한다.
    string json = $"{{\"type\":\"{RequestType.Ping}\",\"req_id\":7,\"note\":\"안녕하세요 다나와 자동차\"}}";
    int byteLen = Encoding.UTF8.GetByteCount(json);
    Assert(byteLen != json.Length, "테스트 전제 오류: 한글 문자열의 문자 수와 바이트 수가 같음");

    await FrameCodec.WriteAsync(stream, new Frame { Json = json });

    Frame respFrame = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;
    Assert(resp.ReqId == 7, $"req_id 불일치(프레이밍이 바이트 수 기준이 아니면 깨짐): {resp.ReqId}");
    Assert(resp.Result == "OK", $"result 불일치: {resp.Result}");
}

static async Task V37_MultipleConnections()
{
    Task<bool>[] tasks = Enumerable.Range(0, 3).Select(async i =>
    {
        using TcpClient client = await ConnectAsync();
        NetworkStream stream = client.GetStream();
        var req = new RequestBase { Type = RequestType.Ping, ReqId = 100 + i };
        await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req) });
        Frame respFrame = await FrameCodec.ReadAsync(stream);
        var resp = JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;
        return resp.ReqId == 100 + i && resp.Result == "OK";
    }).ToArray();

    bool[] results = await Task.WhenAll(tasks);
    Assert(results.All(r => r), "동시 접속 중 일부가 실패함");
}

static string UniqueLoginId(string prefix) => $"{prefix}{DateTime.UtcNow.Ticks % 100_000_000}";

static async Task<ResponseBase> SendRawAsync(NetworkStream stream, string json)
{
    await FrameCodec.WriteAsync(stream, new Frame { Json = json });
    Frame respFrame = await FrameCodec.ReadAsync(stream);
    return JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;
}

static async Task<TResponse> SendTypedAsync<TResponse>(NetworkStream stream, RequestBase req) where TResponse : ResponseBase
{
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req, req.GetType()) });
    Frame respFrame = await FrameCodec.ReadAsync(stream);
    return JsonSerializer.Deserialize<TResponse>(respFrame.Json)!;
}

static async Task<Frame> RecvFrameAsync(NetworkStream stream, RequestBase req)
{
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req, req.GetType()) });
    return await FrameCodec.ReadAsync(stream);
}

static async Task SetCarActiveAsync(int carId, bool active)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("UPDATE cars SET is_active = @active WHERE car_id = @carId", conn);
    cmd.Parameters.AddWithValue("@active", active);
    cmd.Parameters.AddWithValue("@carId", carId);
    await cmd.ExecuteNonQueryAsync();
}

static async Task<ResponseBase> SignupAsync(NetworkStream stream, int reqId, string loginId, string password)
{
    var req = new SignupRequest { Type = RequestType.Signup, ReqId = reqId, LoginId = loginId, Password = password };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req) });
    Frame respFrame = await FrameCodec.ReadAsync(stream);
    return JsonSerializer.Deserialize<ResponseBase>(respFrame.Json)!;
}

static async Task<(byte[] Hash, string Role)> ReadUserAsync(string loginId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT pw_hash, role FROM users WHERE login_id = @id", conn);
    cmd.Parameters.AddWithValue("@id", loginId);
    await using var reader = await cmd.ExecuteReaderAsync();
    Assert(await reader.ReadAsync(), $"DB에서 {loginId}를 찾지 못함");
    return ((byte[])reader["pw_hash"], (string)reader["role"]);
}

static async Task<int> CountAccessLogAsync(string loginId, string action)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(
        "SELECT COUNT(*) FROM access_log l JOIN users u ON u.user_id = l.user_id WHERE u.login_id = @id AND l.action = @action",
        conn);
    cmd.Parameters.AddWithValue("@id", loginId);
    cmd.Parameters.AddWithValue("@action", action);
    return (int)(long)(await cmd.ExecuteScalarAsync())!;
}

static async Task V41_SignupPersisted()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId("v41");

    var resp = await SignupAsync(stream, 41, loginId, "Passw0rd1!");
    Assert(resp.Result == "OK", $"회원가입 실패: {resp.Code} {resp.Message}");

    var (hash, role) = await ReadUserAsync(loginId);
    Assert(role == "USER", $"role이 USER가 아님: {role}");
    Assert(hash.Length == 32, $"pw_hash 길이가 32바이트가 아님(평문 저장 의심): {hash.Length}");
}

static async Task V42_RoleForgeryIgnored()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId("v42");

    string json = $"{{\"type\":\"{RequestType.Signup}\",\"req_id\":42,\"login_id\":\"{loginId}\",\"password\":\"Passw0rd1!\",\"role\":\"ADMIN\"}}";
    var resp = await SendRawAsync(stream, json);
    Assert(resp.Result == "OK", $"회원가입 실패: {resp.Code} {resp.Message}");

    var (_, role) = await ReadUserAsync(loginId);
    Assert(role == "USER", $"role 위조가 통함: {role}");
}

static async Task V43_DuplicateSignup()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId("v43");

    var first = await SignupAsync(stream, 431, loginId, "Passw0rd1!");
    Assert(first.Result == "OK", $"첫 가입 실패: {first.Code}");

    var second = await SignupAsync(stream, 432, loginId, "Passw0rd1!");
    Assert(second.Result == "ERROR", "중복 가입인데 성공함");
    Assert(second.Code == ResultCode.Conflict, $"code 불일치: {second.Code}");
}

static async Task V44_LoginFailureMessagesMatch()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId("v44");

    var signup = await SignupAsync(stream, 440, loginId, "Passw0rd1!");
    Assert(signup.Result == "OK", $"가입 실패: {signup.Code}");

    var noSuchId = new LoginRequest { Type = RequestType.Login, ReqId = 441, LoginId = "no_such_" + loginId, Password = "whatever1" };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(noSuchId) });
    Frame f1 = await FrameCodec.ReadAsync(stream);
    var r1 = JsonSerializer.Deserialize<ResponseBase>(f1.Json)!;

    var wrongPw = new LoginRequest { Type = RequestType.Login, ReqId = 442, LoginId = loginId, Password = "WrongPassword9" };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(wrongPw) });
    Frame f2 = await FrameCodec.ReadAsync(stream);
    var r2 = JsonSerializer.Deserialize<ResponseBase>(f2.Json)!;

    Assert(r1.Code == ResultCode.Unauthorized && r2.Code == ResultCode.Unauthorized, $"code 불일치: {r1.Code} / {r2.Code}");
    Assert(r1.Message == r2.Message, $"메시지가 서로 다름(아이디/비번 구분 가능): '{r1.Message}' vs '{r2.Message}'");
}

static async Task V45_SamePasswordDifferentHash()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId1 = UniqueLoginId("v45a");
    string loginId2 = UniqueLoginId("v45b");

    var r1 = await SignupAsync(stream, 451, loginId1, "SamePassword1!");
    var r2 = await SignupAsync(stream, 452, loginId2, "SamePassword1!");
    Assert(r1.Result == "OK" && r2.Result == "OK", "가입 실패");

    var (hash1, _) = await ReadUserAsync(loginId1);
    var (hash2, _) = await ReadUserAsync(loginId2);
    Assert(!hash1.SequenceEqual(hash2), "같은 비밀번호인데 해시가 같음(salt 미적용 의심)");
}

static async Task V46_LoginLogoutAccessLog()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId("v46");

    var signup = await SignupAsync(stream, 460, loginId, "Passw0rd1!");
    Assert(signup.Result == "OK", $"가입 실패: {signup.Code}");

    var loginReq = new LoginRequest { Type = RequestType.Login, ReqId = 461, LoginId = loginId, Password = "Passw0rd1!" };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(loginReq) });
    Frame loginFrame = await FrameCodec.ReadAsync(stream);
    var loginResp = JsonSerializer.Deserialize<LoginResponse>(loginFrame.Json)!;
    Assert(loginResp.Result == "OK", $"로그인 실패: {loginResp.Code} {loginResp.Message}");
    Assert(loginResp.LoginId == loginId && loginResp.Role == "USER", "login_id/role 응답 불일치");

    var logoutReq = new RequestBase { Type = RequestType.Logout, ReqId = 462 };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(logoutReq) });
    Frame logoutFrame = await FrameCodec.ReadAsync(stream);
    var logoutResp = JsonSerializer.Deserialize<ResponseBase>(logoutFrame.Json)!;
    Assert(logoutResp.Result == "OK", $"로그아웃 실패: {logoutResp.Code}");

    int loginCount = await CountAccessLogAsync(loginId, "LOGIN");
    int logoutCount = await CountAccessLogAsync(loginId, "LOGOUT");
    Assert(loginCount == 1, $"LOGIN 로그 수 불일치: {loginCount}");
    Assert(logoutCount == 1, $"LOGOUT 로그 수 불일치: {logoutCount}");
}

static async Task V4admin_AdminLogin()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var loginReq = new LoginRequest { Type = RequestType.Login, ReqId = 490, LoginId = "admin", Password = "Admin1234!" };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(loginReq) });
    Frame f = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<LoginResponse>(f.Json)!;

    Assert(resp.Result == "OK", $"admin 로그인 실패: {resp.Code} {resp.Message}");
    Assert(resp.Role == "ADMIN", $"role이 ADMIN이 아님: {resp.Role}");
}

static async Task V47_UnauthorizedWithoutLogin()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    // 로그인 없이 로그인 전용 기능(logout)을 호출한다.
    var logoutReq = new RequestBase { Type = RequestType.Logout, ReqId = 47 };
    await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(logoutReq) });
    Frame f = await FrameCodec.ReadAsync(stream);
    var resp = JsonSerializer.Deserialize<ResponseBase>(f.Json)!;

    Assert(resp.Result == "ERROR", "비로그인 상태인데 성공함");
    Assert(resp.Code == ResultCode.Unauthorized, $"code 불일치: {resp.Code}");
}

static async Task V51_BrandToCarList()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var brandResp = await SendTypedAsync<BrandListResponse>(stream, new RequestBase { Type = RequestType.BrandList });
    Assert(brandResp.Result == "OK", $"brand_list 실패: {brandResp.Code}");
    BrandDto? hyundai = brandResp.Brands.FirstOrDefault(b => b.BrandName == "현대");
    Assert(hyundai is not null, "브랜드 목록에 현대가 없음");

    var carResp = await SendTypedAsync<CarListResponse>(
        stream, new CarListRequest { Type = RequestType.CarList, BrandId = hyundai!.BrandId });
    Assert(carResp.Result == "OK", $"car_list 실패: {carResp.Code}");
    Assert(carResp.Cars.Count == 3, $"현대 차량 수가 3이 아님: {carResp.Cars.Count}");
    Assert(carResp.Cars.All(c => c.BrandName == "현대"), "다른 브랜드 차량이 섞여 나옴");
}

static async Task V51b_CarListAllAndOrigin()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var allResp = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList });
    Assert(allResp.Result == "OK", $"전체차량 조회 실패: {allResp.Code}");
    Assert(allResp.Cars.Count == 10, $"전체 활성 차량 수가 예상과 다름: {allResp.Cars.Count}");

    var domesticResp = await SendTypedAsync<CarListResponse>(
        stream, new CarListRequest { Type = RequestType.CarList, OriginType = "DOMESTIC" });
    Assert(domesticResp.Result == "OK", $"국산차 조회 실패: {domesticResp.Code}");
    Assert(domesticResp.Cars.Count == 6, $"국산차 수가 예상과 다름: {domesticResp.Cars.Count}");

    var importResp = await SendTypedAsync<CarListResponse>(
        stream, new CarListRequest { Type = RequestType.CarList, OriginType = "IMPORT" });
    Assert(importResp.Result == "OK", $"수입차 조회 실패: {importResp.Code}");
    Assert(importResp.Cars.Count == 4, $"수입차 수가 예상과 다름: {importResp.Cars.Count}");
}

static async Task V52_SearchByCarName()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<CarListResponse>(
        stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "아반" });
    Assert(resp.Result == "OK", $"검색 실패: {resp.Code}");
    Assert(resp.Cars.Count == 1 && resp.Cars[0].CarName == "아반떼",
        $"차량명 부분일치 결과가 예상과 다름: {resp.Cars.Count}건");
}

static async Task V53_SearchByBrandName()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<CarListResponse>(
        stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "현대" });
    Assert(resp.Result == "OK", $"검색 실패: {resp.Code}");
    Assert(resp.Cars.Count == 3 && resp.Cars.All(c => c.BrandName == "현대"),
        $"브랜드명 부분일치 결과가 예상과 다름: {resp.Cars.Count}건");
}

static async Task V54_SearchNoResults()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<CarListResponse>(
        stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "zzzz" });
    Assert(resp.Result == "OK", $"결과 0건인데 ERROR임: {resp.Code}");
    Assert(resp.Cars.Count == 0, $"빈 배열이 아님: {resp.Cars.Count}");
}

static async Task V55_SoftDeleteExcluded()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var searchResp = await SendTypedAsync<CarListResponse>(
        stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "쏘나타" });
    Assert(searchResp.Cars.Count == 1, "쏘나타를 찾지 못함(사전조건 실패)");
    int carId = searchResp.Cars[0].CarId;

    await SetCarActiveAsync(carId, false);
    try
    {
        var afterSearch = await SendTypedAsync<CarListResponse>(
            stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "쏘나타" });
        Assert(afterSearch.Cars.Count == 0, "비활성화했는데 검색 결과에 남아있음");

        var detailResp = await SendTypedAsync<CarDetailResponse>(
            stream, new CarDetailRequest { Type = RequestType.CarDetail, CarId = carId });
        Assert(detailResp.Result == "ERROR" && detailResp.Code == ResultCode.NotFound,
            $"비활성 차량 상세가 NOT_FOUND가 아님: {detailResp.Code}");
    }
    finally
    {
        await SetCarActiveAsync(carId, true);
    }

    var restoredDetail = await SendTypedAsync<CarDetailResponse>(
        stream, new CarDetailRequest { Type = RequestType.CarDetail, CarId = carId });
    Assert(restoredDetail.Result == "OK", "원복 후에도 상세 조회가 안 됨");
}

static async Task V57_FavoriteTop5WeekBoundary()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<MainFavoriteTop5Response>(
        stream, new RequestBase { Type = RequestType.MainFavoriteTop5 });
    Assert(resp.Result == "OK", $"main_favorite_top5 실패: {resp.Code}");

    bool has7DaysAgo = resp.Domestic.Any(c => c.CarName == "K5");
    bool has8DaysAgo = resp.Domestic.Any(c => c.CarName == "G80");
    Assert(has7DaysAgo, "7일 전 관심 등록(K5, INTERVAL 6 DAY 경계)이 결과에서 빠짐");
    Assert(!has8DaysAgo, "8일 전 관심 등록만 있는 차(G80)가 결과에 포함됨");
}

static async Task V5daily_MainDailyTop()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<MainDailyTopResponse>(
        stream, new RequestBase { Type = RequestType.MainDailyTop });
    Assert(resp.Result == "OK", $"main_daily_top 실패: {resp.Code}");
    Assert(resp.Car is not null, "오늘 견적이 있는데 car가 null");

    // 하드코딩된 seed 건수 대신 DB를 직접 조회해 기대값을 계산한다.
    // (다른 테스트/수동 조작이 오늘 날짜로 견적을 계속 쌓기 때문에 seed 원본 건수와 달라질 수 있다.)
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(
        "SELECT c.car_name, COUNT(*) AS quote_count " +
        "FROM quotes q JOIN cars c ON c.car_id = q.car_id " +
        "WHERE q.created_at >= CURDATE() AND q.created_at < CURDATE() + INTERVAL 1 DAY " +
        "  AND q.is_deleted = 0 AND c.is_active = 1 " +
        "GROUP BY c.car_id ORDER BY quote_count DESC LIMIT 1", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    Assert(await reader.ReadAsync(), "DB에 오늘 견적이 없음(사전조건 실패)");
    string expectedCarName = reader.GetString("car_name");
    int expectedCount = reader.GetInt32("quote_count");

    Assert(resp.Car!.CarName == expectedCarName,
        $"1위 차량이 DB와 다름: 응답={resp.Car.CarName}, DB={expectedCarName}");
    Assert(resp.Car.QuoteCount == expectedCount,
        $"오늘 견적 건수가 DB와 다름: 응답={resp.Car.QuoteCount}, DB={expectedCount}");
}

static async Task V5detail_CarDetail()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<CarDetailResponse>(
        stream, new CarDetailRequest { Type = RequestType.CarDetail, CarId = 1 });
    Assert(resp.Result == "OK", $"차량 상세 실패: {resp.Code}");
    Assert(resp.Options.Count == 3, $"아반떼 옵션 수가 3이 아님: {resp.Options.Count}");
    Assert(!resp.IsFavorite, "비로그인인데 is_favorite가 true");

    string loginId = UniqueLoginId("v5det");
    var signup = await SignupAsync(stream, 5001, loginId, "Passw0rd1!");
    Assert(signup.Result == "OK", "가입 실패");

    var loginResp = await SendTypedAsync<LoginResponse>(
        stream, new LoginRequest { Type = RequestType.Login, LoginId = loginId, Password = "Passw0rd1!" });
    Assert(loginResp.Result == "OK", "로그인 실패");

    var loggedInDetail = await SendTypedAsync<CarDetailResponse>(
        stream, new CarDetailRequest { Type = RequestType.CarDetail, CarId = 1 });
    Assert(!loggedInDetail.IsFavorite, "새로 가입한 계정인데 is_favorite가 true (관심 등록한 적 없음)");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V5summary_MainSummary()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<MainSummaryResponse>(
        stream, new RequestBase { Type = RequestType.MainSummary });
    Assert(resp.Result == "OK", $"main_summary 실패: {resp.Code}");

    // 하드코딩된 seed 건수 대신 DB를 직접 조회해 기대값을 계산한다.
    // (test_constraints.sh가 검증 과정에서 실제 UPDATE/DELETE를 남기므로 seed 원본 건수와 달라질 수 있다.)
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    int expectedToday = await ScalarIntAsync(conn,
        "SELECT COUNT(*) FROM quotes WHERE created_at >= CURDATE() AND created_at < CURDATE() + INTERVAL 1 DAY");
    int expectedTotal = await ScalarIntAsync(conn, "SELECT COUNT(*) FROM quotes WHERE is_deleted = 0");

    Assert(resp.TodayQuotes == expectedToday, $"오늘 견적 건수가 DB와 다름: 응답={resp.TodayQuotes}, DB={expectedToday}");
    Assert(resp.TotalQuotes == expectedTotal, $"누적 견적 건수가 DB와 다름: 응답={resp.TotalQuotes}, DB={expectedTotal}");
    Assert(resp.MyQuotes == 0 && resp.MyFavorites == 0, "비로그인인데 my_* 값이 0이 아님");
}

static async Task<int> ScalarIntAsync(MySqlConnection conn, string sql)
{
    await using var cmd = new MySqlCommand(sql, conn);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

static async Task V510_ImageGet()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    Frame respFrame = await RecvFrameAsync(
        stream, new ImageGetRequest { Type = RequestType.ImageGet, ImageUrl = "img/brand/hyundai.png" });
    var resp = JsonSerializer.Deserialize<ImageGetResponse>(respFrame.Json)!;

    Assert(resp.Result == "OK", $"이미지 조회 실패: {resp.Code}");
    Assert(resp.ContentType == "image/png", $"content_type 불일치: {resp.ContentType}");
    Assert(respFrame.Blob is { Length: > 0 }, "blob이 비어있음");
}

static async Task V5img_NotFoundAndTraversalBlocked()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    Frame notFoundFrame = await RecvFrameAsync(
        stream, new ImageGetRequest { Type = RequestType.ImageGet, ImageUrl = "img/brand/nope.png" });
    var notFound = JsonSerializer.Deserialize<ImageGetResponse>(notFoundFrame.Json)!;
    Assert(notFound.Code == ResultCode.NotFound, $"없는 이미지인데 NOT_FOUND가 아님: {notFound.Code}");
    Assert(notFoundFrame.Blob is null or { Length: 0 }, "없는 이미지인데 blob이 있음");

    Frame traversalFrame = await RecvFrameAsync(
        stream, new ImageGetRequest { Type = RequestType.ImageGet, ImageUrl = "../../../Danawa.Server.dll" });
    var traversal = JsonSerializer.Deserialize<ImageGetResponse>(traversalFrame.Json)!;
    Assert(traversal.Code == ResultCode.NotFound, $"경로 탈출 시도가 차단되지 않음: {traversal.Code}");
    Assert(traversalFrame.Blob is null or { Length: 0 }, "경로 탈출 시도인데 blob이 반환됨");
}

static async Task<TResponse> SendRawTypedAsync<TResponse>(NetworkStream stream, string json) where TResponse : ResponseBase
{
    await FrameCodec.WriteAsync(stream, new Frame { Json = json });
    Frame respFrame = await FrameCodec.ReadAsync(stream);
    return JsonSerializer.Deserialize<TResponse>(respFrame.Json)!;
}

static async Task<(TcpClient Client, NetworkStream Stream, int UserId)> SignupAndLoginAsync(string prefix, string password = "Passw0rd1!")
{
    TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    string loginId = UniqueLoginId(prefix);

    var signup = await SignupAsync(stream, 6000, loginId, password);
    Assert(signup.Result == "OK", $"가입 실패(사전조건): {signup.Code}");

    var loginResp = await SendTypedAsync<LoginResponse>(
        stream, new LoginRequest { Type = RequestType.Login, LoginId = loginId, Password = password });
    Assert(loginResp.Result == "OK", $"로그인 실패(사전조건): {loginResp.Code}");

    int userId = await GetUserIdAsync(loginId);
    return (client, stream, userId);
}

static async Task<int> GetUserIdAsync(string loginId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT user_id FROM users WHERE login_id = @id", conn);
    cmd.Parameters.AddWithValue("@id", loginId);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

static async Task<int> CountQuotesAsync(int userId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM quotes WHERE user_id = @uid", conn);
    cmd.Parameters.AddWithValue("@uid", userId);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

static async Task<(bool IsDeleted, int UserId)?> GetQuoteStateAsync(int quoteId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT is_deleted, user_id FROM quotes WHERE quote_id = @id", conn);
    cmd.Parameters.AddWithValue("@id", quoteId);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return null;
    }

    return (reader.GetBoolean("is_deleted"), reader.GetInt32("user_id"));
}

static async Task SetOptionPriceAsync(int optionId, int price)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("UPDATE car_options SET price = @price WHERE option_id = @id", conn);
    cmd.Parameters.AddWithValue("@price", price);
    cmd.Parameters.AddWithValue("@id", optionId);
    await cmd.ExecuteNonQueryAsync();
}

static async Task<int> CountFavoritesAsync(int userId, int carId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM favorites WHERE user_id = @uid AND car_id = @carId", conn);
    cmd.Parameters.AddWithValue("@uid", userId);
    cmd.Parameters.AddWithValue("@carId", carId);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

static async Task V61_QuoteAmountForgeryIgnored()
{
    var (client, stream, _) = await SignupAndLoginAsync("v61");
    using (client)
    {
        string json = "{\"type\":\"" + RequestType.QuoteAdd + "\",\"req_id\":6001,\"car_id\":1,\"option_ids\":[1,2],\"total_price\":1}";
        var resp = await SendRawTypedAsync<QuoteAddResponse>(stream, json);
        Assert(resp.Result == "OK", $"견적 저장 실패: {resp.Code} {resp.Message}");
        Assert(resp.TotalPrice == 20_600_000, $"금액 위조가 반영됨: {resp.TotalPrice}");
    }
}

static async Task V62_WrongCarOptionRejected()
{
    var (client, stream, userId) = await SignupAndLoginAsync("v62");
    using (client)
    {
        int before = await CountQuotesAsync(userId);

        // option_id 4는 차량 2(쏘나타)의 옵션이다. car_id=1(아반떼)로 요청한다.
        var resp = await SendTypedAsync<ResponseBase>(
            stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int> { 4 } });
        Assert(resp.Result == "ERROR" && resp.Code == ResultCode.Validation, $"code 불일치: {resp.Code}");

        int after = await CountQuotesAsync(userId);
        Assert(before == after, "검증 실패인데 quotes에 행이 남음(ROLLBACK 확인 실패)");
    }
}

static async Task V63_NormalQuoteSaved()
{
    var (client, stream, _) = await SignupAndLoginAsync("v63");
    using (client)
    {
        var resp = await SendTypedAsync<QuoteAddResponse>(
            stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int> { 1, 2, 3 } });
        Assert(resp.Result == "OK", $"저장 실패: {resp.Code}");
        Assert(resp.TotalPrice == 19_000_000 + 900_000 + 700_000 + 1_200_000, $"총액이 손계산과 다름: {resp.TotalPrice}");

        var listResp = await SendTypedAsync<QuoteListResponse>(stream, new RequestBase { Type = RequestType.QuoteList });
        var saved = listResp.Quotes.First(q => q.QuoteId == resp.QuoteId);
        Assert(saved.Options.Count == 3, $"옵션 수가 3이 아님: {saved.Options.Count}");
        Assert(saved.TotalPrice == resp.TotalPrice, "목록 총액과 저장 응답 총액이 다름");
    }
}

static async Task V64_PriceChangeDoesNotAffectSavedQuote()
{
    var (client, stream, _) = await SignupAndLoginAsync("v64");
    using (client)
    {
        var addResp = await SendTypedAsync<QuoteAddResponse>(
            stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int> { 1 } });
        Assert(addResp.Result == "OK", "저장 실패");
        int originalTotal = addResp.TotalPrice;

        await SetOptionPriceAsync(1, 9_999_999);
        try
        {
            var listResp = await SendTypedAsync<QuoteListResponse>(stream, new RequestBase { Type = RequestType.QuoteList });
            var quote = listResp.Quotes.First(q => q.QuoteId == addResp.QuoteId);
            Assert(quote.TotalPrice == originalTotal, $"옵션 가격 변경이 저장된 견적에 영향을 줌: {quote.TotalPrice} vs {originalTotal}");
            Assert(quote.Options[0].Price == 900_000, $"옵션 스냅샷 가격이 바뀜: {quote.Options[0].Price}");
        }
        finally
        {
            await SetOptionPriceAsync(1, 900_000);
        }
    }
}

static async Task V65_QuoteListSingleCard()
{
    var (client, stream, _) = await SignupAndLoginAsync("v65");
    using (client)
    {
        var addResp = await SendTypedAsync<QuoteAddResponse>(
            stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int> { 1, 2, 3 } });
        Assert(addResp.Result == "OK", "저장 실패");

        var listResp = await SendTypedAsync<QuoteListResponse>(stream, new RequestBase { Type = RequestType.QuoteList });
        Assert(listResp.Quotes.Count == 1, $"카드 수가 1이 아님(중복 출력 의심): {listResp.Quotes.Count}");
        Assert(listResp.Quotes[0].Options.Count == 3, $"옵션 3개가 카드 안에 안 담김: {listResp.Quotes[0].Options.Count}");
    }
}

static async Task V66_QuoteDeleteSoftDelete()
{
    var (client, stream, _) = await SignupAndLoginAsync("v66");
    using (client)
    {
        var addResp = await SendTypedAsync<QuoteAddResponse>(
            stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int>() });
        Assert(addResp.Result == "OK", "저장 실패");

        var delResp = await SendTypedAsync<ResponseBase>(
            stream, new QuoteDeleteRequest { Type = RequestType.QuoteDelete, QuoteId = addResp.QuoteId });
        Assert(delResp.Result == "OK", $"삭제 실패: {delResp.Code}");

        var listResp = await SendTypedAsync<QuoteListResponse>(stream, new RequestBase { Type = RequestType.QuoteList });
        Assert(listResp.Quotes.All(q => q.QuoteId != addResp.QuoteId), "삭제됐는데 목록에 남아있음");

        var state = await GetQuoteStateAsync(addResp.QuoteId);
        Assert(state is not null, "DB에서 행 자체가 사라짐(하드 삭제됨)");
        Assert(state!.Value.IsDeleted, "is_deleted가 1로 안 바뀜");
    }
}

static async Task V67_DeleteOthersQuoteNotFound()
{
    var (clientA, streamA, _) = await SignupAndLoginAsync("v67a");
    var (clientB, streamB, _) = await SignupAndLoginAsync("v67b");
    using (clientA)
    using (clientB)
    {
        var addResp = await SendTypedAsync<QuoteAddResponse>(
            streamA, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int>() });
        Assert(addResp.Result == "OK", "A의 견적 저장 실패(사전조건)");

        var delResp = await SendTypedAsync<ResponseBase>(
            streamB, new QuoteDeleteRequest { Type = RequestType.QuoteDelete, QuoteId = addResp.QuoteId });
        Assert(delResp.Result == "ERROR" && delResp.Code == ResultCode.NotFound,
            $"남의 견적 삭제 시도 code 불일치: {delResp.Code}");
    }
}

static async Task V68_FavoriteToggleIdempotent()
{
    var (client, stream, userId) = await SignupAndLoginAsync("v68");
    using (client)
    {
        var on1 = await SendTypedAsync<FavoriteSetResponse>(
            stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = 1, On = true });
        Assert(on1.Result == "OK" && on1.IsFavorite, $"등록 실패: {on1.Code}");

        var on2 = await SendTypedAsync<FavoriteSetResponse>(
            stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = 1, On = true });
        Assert(on2.Result == "OK" && on2.IsFavorite, "재등록(멱등) 실패");

        int countAfterDoubleOn = await CountFavoritesAsync(userId, 1);
        Assert(countAfterDoubleOn == 1, $"중복 행이 생김: {countAfterDoubleOn}");

        var off = await SendTypedAsync<FavoriteSetResponse>(
            stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = 1, On = false });
        Assert(off.Result == "OK" && !off.IsFavorite, $"해제 실패: {off.Code}");

        int countAfterOff = await CountFavoritesAsync(userId, 1);
        Assert(countAfterOff == 0, $"해제됐는데 행이 남음: {countAfterOff}");

        var listResp = await SendTypedAsync<FavoriteListResponse>(stream, new RequestBase { Type = RequestType.FavoriteList });
        Assert(listResp.Result == "OK" && listResp.Favorites.Count == 0, "해제 후에도 목록에 남아있음");
    }
}

static async Task V69_AdminForbidden()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var loginResp = await SendTypedAsync<LoginResponse>(
        stream, new LoginRequest { Type = RequestType.Login, LoginId = "admin", Password = "Admin1234!" });
    Assert(loginResp.Result == "OK" && loginResp.Role == "ADMIN", "admin 로그인 실패(사전조건)");

    var favSet = await SendTypedAsync<ResponseBase>(
        stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = 1, On = true });
    Assert(favSet.Code == ResultCode.Forbidden, $"favorite_set code 불일치: {favSet.Code}");

    var favList = await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.FavoriteList });
    Assert(favList.Code == ResultCode.Forbidden, $"favorite_list code 불일치: {favList.Code}");

    var quoteAdd = await SendTypedAsync<ResponseBase>(
        stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int>() });
    Assert(quoteAdd.Code == ResultCode.Forbidden, $"quote_add code 불일치: {quoteAdd.Code}");

    var quoteList = await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.QuoteList });
    Assert(quoteList.Code == ResultCode.Forbidden, $"quote_list code 불일치: {quoteList.Code}");

    var quoteDelete = await SendTypedAsync<ResponseBase>(
        stream, new QuoteDeleteRequest { Type = RequestType.QuoteDelete, QuoteId = 1 });
    Assert(quoteDelete.Code == ResultCode.Forbidden, $"quote_delete code 불일치: {quoteDelete.Code}");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V610_UserIdForgeryIgnored()
{
    var (client, stream, userId) = await SignupAndLoginAsync("v610");
    using (client)
    {
        string json = "{\"type\":\"" + RequestType.QuoteAdd + "\",\"req_id\":6100,\"car_id\":1,\"option_ids\":[],\"user_id\":999999}";
        var resp = await SendRawTypedAsync<QuoteAddResponse>(stream, json);
        Assert(resp.Result == "OK", $"저장 실패: {resp.Code}");

        var state = await GetQuoteStateAsync(resp.QuoteId);
        Assert(state is not null && state.Value.UserId == userId,
            $"저장된 견적의 user_id가 세션 사용자와 다름(위조 반영됨): {state?.UserId} vs {userId}");
    }
}

static async Task AdminLoginAsync(NetworkStream stream)
{
    var resp = await SendTypedAsync<LoginResponse>(
        stream, new LoginRequest { Type = RequestType.Login, LoginId = "admin", Password = "Admin1234!" });
    Assert(resp.Result == "OK" && resp.Role == "ADMIN", "admin 로그인 실패(사전조건)");
}

static async Task<string> GetLoginIdAsync(int userId)
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT login_id FROM users WHERE user_id = @id", conn);
    cmd.Parameters.AddWithValue("@id", userId);
    return (string)(await cmd.ExecuteScalarAsync())!;
}

static async Task V71_UserForbiddenAdminCarList()
{
    var (client, stream, _) = await SignupAndLoginAsync("v71");
    using (client)
    {
        var resp = await SendTypedAsync<ResponseBase>(stream, new AdminCarListRequest { Type = RequestType.AdminCarList });
        Assert(resp.Result == "ERROR" && resp.Code == ResultCode.Forbidden, $"code 불일치: {resp.Code}");
    }
}

static async Task V72_UnauthenticatedAdminUnauthorized()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var resp = await SendTypedAsync<ResponseBase>(stream, new AdminCarListRequest { Type = RequestType.AdminCarList });
    Assert(resp.Result == "ERROR" && resp.Code == ResultCode.Unauthorized, $"code 불일치: {resp.Code}");
}

static async Task V73_DuplicateCarNameConflict()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    var brandListResp = await SendTypedAsync<AdminBrandListResponse>(stream, new RequestBase { Type = RequestType.AdminBrandList });
    AdminBrandDto hyundai = brandListResp.Brands.First(b => b.BrandName == "현대");

    // brand_id=현대에는 이미 "아반떼"가 있다 (seed).
    var resp = await SendTypedAsync<ResponseBase>(stream,
        new AdminCarSaveRequest { Type = RequestType.AdminCarSave, BrandId = hyundai.BrandId, CarName = "아반떼", CarPrice = 1 });
    Assert(resp.Result == "ERROR" && resp.Code == ResultCode.Conflict, $"code 불일치: {resp.Code}");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V74_CarDeleteSoftDeletePreservesQuotes()
{
    using TcpClient adminClient = await ConnectAsync();
    NetworkStream adminStream = adminClient.GetStream();
    await AdminLoginAsync(adminStream);

    var brandResp = await SendTypedAsync<AdminBrandSaveResponse>(adminStream, new AdminBrandSaveRequest
    {
        Type = RequestType.AdminBrandSave,
        BrandName = UniqueLoginId("brand74"),
        OriginType = "DOMESTIC"
    });
    Assert(brandResp.Result == "OK", $"브랜드 생성 실패: {brandResp.Code}");

    var carResp = await SendTypedAsync<AdminCarSaveResponse>(adminStream, new AdminCarSaveRequest
    {
        Type = RequestType.AdminCarSave,
        BrandId = brandResp.BrandId,
        CarName = "테스트카74",
        CarPrice = 10_000_000
    });
    Assert(carResp.Result == "OK", $"차량 생성 실패: {carResp.Code}");
    int carId = carResp.CarId;

    var (userClient, userStream, _) = await SignupAndLoginAsync("v74");
    using (userClient)
    {
        var quoteResp = await SendTypedAsync<QuoteAddResponse>(userStream,
            new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = carId, OptionIds = new List<int>() });
        Assert(quoteResp.Result == "OK", $"견적 저장 실패(사전조건): {quoteResp.Code}");

        var delResp = await SendTypedAsync<ResponseBase>(adminStream,
            new AdminCarDeleteRequest { Type = RequestType.AdminCarDelete, CarId = carId });
        Assert(delResp.Result == "OK", $"삭제 실패: {delResp.Code}");

        var detailResp = await SendTypedAsync<CarDetailResponse>(userStream,
            new CarDetailRequest { Type = RequestType.CarDetail, CarId = carId });
        Assert(detailResp.Code == ResultCode.NotFound, $"삭제된 차량이 여전히 일반 조회됨: {detailResp.Code}");

        var listResp = await SendTypedAsync<QuoteListResponse>(userStream, new RequestBase { Type = RequestType.QuoteList });
        Assert(listResp.Quotes.Any(q => q.QuoteId == quoteResp.QuoteId), "차량 삭제 후 기존 견적이 사라짐(보존 안 됨)");
    }

    await SendTypedAsync<ResponseBase>(adminStream, new RequestBase { Type = RequestType.Logout });
}

static async Task V75_OptionCrud()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    string optionName = UniqueLoginId("opt75");
    var saveResp = await SendTypedAsync<AdminOptionSaveResponse>(stream,
        new AdminOptionSaveRequest { Type = RequestType.AdminOptionSave, CarId = 1, OptionName = optionName, Price = 123000 });
    Assert(saveResp.Result == "OK", $"옵션 등록 실패: {saveResp.Code}");
    int optionId = saveResp.OptionId;

    var listResp = await SendTypedAsync<AdminOptionListResponse>(stream,
        new AdminOptionListRequest { Type = RequestType.AdminOptionList, CarId = 1 });
    Assert(listResp.Options.Any(o => o.OptionId == optionId && o.Price == 123000), "등록한 옵션이 목록에 없음");

    var updateResp = await SendTypedAsync<AdminOptionSaveResponse>(stream, new AdminOptionSaveRequest
    {
        Type = RequestType.AdminOptionSave,
        OptionId = optionId,
        CarId = 1,
        OptionName = optionName,
        Price = 456000
    });
    Assert(updateResp.Result == "OK", $"옵션 수정 실패: {updateResp.Code}");

    var afterUpdateList = await SendTypedAsync<AdminOptionListResponse>(stream,
        new AdminOptionListRequest { Type = RequestType.AdminOptionList, CarId = 1 });
    AdminOptionDto updated = afterUpdateList.Options.First(o => o.OptionId == optionId);
    Assert(updated.Price == 456000, $"수정된 가격이 반영 안 됨: {updated.Price}");

    var deleteResp = await SendTypedAsync<ResponseBase>(stream,
        new AdminOptionDeleteRequest { Type = RequestType.AdminOptionDelete, OptionId = optionId });
    Assert(deleteResp.Result == "OK", $"옵션 삭제 실패: {deleteResp.Code}");

    var afterDeleteList = await SendTypedAsync<AdminOptionListResponse>(stream,
        new AdminOptionListRequest { Type = RequestType.AdminOptionList, CarId = 1 });
    AdminOptionDto deleted = afterDeleteList.Options.First(o => o.OptionId == optionId);
    Assert(!deleted.IsActive, "삭제(is_active=0) 처리가 반영 안 됨");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V76_DuplicateOptionNameConflict()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    // car_id=1(아반떼)에는 이미 "선루프"가 있다 (seed).
    var resp = await SendTypedAsync<ResponseBase>(stream,
        new AdminOptionSaveRequest { Type = RequestType.AdminOptionSave, CarId = 1, OptionName = "선루프", Price = 100 });
    Assert(resp.Result == "ERROR" && resp.Code == ResultCode.Conflict, $"code 불일치: {resp.Code}");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V78_StatsDailyExcludesZeroDays()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    var resp = await SendTypedAsync<AdminStatsDailyResponse>(stream,
        new AdminStatsDailyRequest { Type = RequestType.AdminStatsDaily, DateFrom = "2020-01-01", DateTo = "2020-01-02" });
    Assert(resp.Result == "OK", $"조회 실패: {resp.Code}");
    Assert(resp.Days.Count == 0, $"견적이 없는 기간인데 결과가 있음: {resp.Days.Count}건");

    var invalidResp = await SendTypedAsync<ResponseBase>(stream,
        new AdminStatsDailyRequest { Type = RequestType.AdminStatsDaily, DateFrom = "2026-09-25", DateTo = "2026-09-20" });
    Assert(invalidResp.Result == "ERROR" && invalidResp.Code == ResultCode.Validation,
        $"시작일>종료일인데 VALIDATION이 아님: {invalidResp.Code}");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V79_WeeklyTop5MatchesQuoteBasedDbQuery()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    var resp = await SendTypedAsync<AdminStatsWeeklyTop5Response>(stream, new RequestBase { Type = RequestType.AdminStatsWeeklyTop5 });
    Assert(resp.Result == "OK", $"조회 실패: {resp.Code}");

    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(
        "SELECT c.car_id, COUNT(*) AS cnt FROM quotes q " +
        "JOIN cars c ON c.car_id = q.car_id JOIN brands b ON b.brand_id = c.brand_id " +
        "WHERE q.created_at >= (CURDATE() - INTERVAL 6 DAY) AND q.created_at < CURDATE() + INTERVAL 1 DAY " +
        "  AND q.is_deleted = 0 AND b.origin_type = 'DOMESTIC' " +
        "GROUP BY c.car_id ORDER BY cnt DESC LIMIT 1", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    Assert(await reader.ReadAsync(), "DB에 최근 7일 국산 견적 데이터가 없음(사전조건 실패)");
    int expectedTopCarId = reader.GetInt32("car_id");

    Assert(resp.Domestic.Count > 0 && resp.Domestic[0].CarId == expectedTopCarId,
        $"국산 1위가 DB 직접 계산과 다름: 응답={(resp.Domestic.Count > 0 ? resp.Domestic[0].CarId : -1)}, DB={expectedTopCarId}");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V710_LogListShowsLoginAndLogout()
{
    var (userClient, userStream, userId) = await SignupAndLoginAsync("v710");
    using (userClient)
    {
        await SendTypedAsync<ResponseBase>(userStream, new RequestBase { Type = RequestType.Logout });
    }

    using TcpClient adminClient = await ConnectAsync();
    NetworkStream adminStream = adminClient.GetStream();
    await AdminLoginAsync(adminStream);

    string today = DateTime.Now.ToString("yyyy-MM-dd");
    var resp = await SendTypedAsync<AdminLogListResponse>(adminStream,
        new AdminLogListRequest { Type = RequestType.AdminLogList, DateFrom = today, DateTo = today });
    Assert(resp.Result == "OK", $"조회 실패: {resp.Code}");

    string expectedLoginId = await GetLoginIdAsync(userId);
    Assert(resp.Logs.Any(l => l.LoginId == expectedLoginId && l.Action == "LOGIN"), "LOGIN 로그가 없음");
    Assert(resp.Logs.Any(l => l.LoginId == expectedLoginId && l.Action == "LOGOUT"), "LOGOUT 로그가 없음");
    Assert(resp.Logs.All(l => !string.IsNullOrEmpty(l.IpAddr) && !string.IsNullOrEmpty(l.CreatedAt)), "IP 또는 시각이 비어있는 로그가 있음");

    await SendTypedAsync<ResponseBase>(adminStream, new RequestBase { Type = RequestType.Logout });
}

// ===== 8단계: 와이어프레임 반영으로 추가한 필드·정렬 =====

static async Task V81_SortPopularMatchesDb()
{
    // 순위에 변화를 주기 위해 car_id가 가장 큰 활성 차량에 관심을 하나 더한다 (seed 건수는 하드코딩하지 않는다).
    var (client, stream, _) = await SignupAndLoginAsync("v81");
    using (client)
    {
        var allResp = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList });
        int lastCarId = allResp.Cars.Max(c => c.CarId);
        await SendTypedAsync<FavoriteSetResponse>(stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = lastCarId, On = true });

        var resp = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList, Sort = "POPULAR" });
        Assert(resp.Result == "OK", $"조회 실패: {resp.Code}");

        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT c.car_id FROM cars c WHERE c.is_active = 1 ORDER BY " +
            "(SELECT COUNT(*) FROM favorites f WHERE f.car_id = c.car_id " +
            "   AND f.created_at >= (CURDATE() - INTERVAL 6 DAY) AND f.created_at < CURDATE() + INTERVAL 1 DAY) DESC, c.car_id", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var expected = new List<int>();
        while (await reader.ReadAsync())
        {
            expected.Add(reader.GetInt32("car_id"));
        }

        var actual = resp.Cars.Select(c => c.CarId).ToList();
        Assert(actual.SequenceEqual(expected), $"순서가 DB 직접 계산과 다름: 응답=[{string.Join(",", actual)}], DB=[{string.Join(",", expected)}]");
        Assert(!actual.SequenceEqual(actual.OrderBy(id => id)), "인기순인데 car_id순과 같음 — 정렬이 적용되지 않았을 수 있음");
    }
}

static async Task V82_SortPriceAsc()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    // 검색어는 차량이 가장 많은 브랜드명 — 결과가 2건 이상이어야 정렬 검증이 의미 있다.
    var all = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList });
    string keyword = all.Cars.GroupBy(c => c.BrandName).OrderByDescending(g => g.Count()).First().Key;

    var plain = await SendTypedAsync<CarListResponse>(stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = keyword });
    var sorted = await SendTypedAsync<CarListResponse>(stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = keyword, Sort = "PRICE_ASC" });
    Assert(sorted.Result == "OK", $"조회 실패: {sorted.Code}");
    Assert(sorted.Cars.Count >= 2, $"검색 결과가 2건 미만이라 정렬 검증 불가 (사전조건 실패): {keyword}");

    var prices = sorted.Cars.Select(c => c.CarPrice).ToList();
    Assert(prices.SequenceEqual(prices.OrderBy(p => p)), $"가격 오름차순이 아님: [{string.Join(",", prices)}]");
    Assert(sorted.Cars.Select(c => c.CarId).OrderBy(id => id).SequenceEqual(plain.Cars.Select(c => c.CarId).OrderBy(id => id)),
        "정렬 전후 목록 구성이 다름");

    var list = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList, OriginType = "IMPORT", Sort = "PRICE_ASC" });
    var listPrices = list.Cars.Select(c => c.CarPrice).ToList();
    Assert(list.Result == "OK" && list.Cars.Count >= 2 && listPrices.SequenceEqual(listPrices.OrderBy(p => p)),
        $"car_list 가격 오름차순이 아님: [{string.Join(",", listPrices)}]");
}

static async Task V83_InvalidSortRejected()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var bad = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList, Sort = "car_price; DROP TABLE cars" });
    Assert(bad.Result == "ERROR" && bad.Code == ResultCode.Validation, $"VALIDATION이 아님: {bad.Code}");

    var badSearch = await SendTypedAsync<CarListResponse>(stream, new CarSearchRequest { Type = RequestType.CarSearch, Keyword = "a", Sort = "price_asc" });
    Assert(badSearch.Code == ResultCode.Validation, $"대소문자 다른 값도 VALIDATION이어야 함: {badSearch.Code}");

    var ok = await SendTypedAsync<CarListResponse>(stream, new CarListRequest { Type = RequestType.CarList });
    Assert(ok.Result == "OK" && ok.Cars.Count > 0, "거부 뒤 같은 연결의 다음 요청이 실패함");
}

static async Task V84_FavoriteListOptionTotal()
{
    var (client, stream, _) = await SignupAndLoginAsync("v84");
    using (client)
    {
        await SendTypedAsync<FavoriteSetResponse>(stream, new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = 1, On = true });
        var resp = await SendTypedAsync<FavoriteListResponse>(stream, new RequestBase { Type = RequestType.FavoriteList });
        Assert(resp.Result == "OK", $"조회 실패: {resp.Code}");
        var fav = resp.Favorites.Single(f => f.CarId == 1);

        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COALESCE(SUM(price), 0) FROM car_options WHERE car_id = 1 AND is_active = 1", conn);
        long expected = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        Assert(fav.OptionTotal == expected, $"option_total이 DB와 다름: 응답={fav.OptionTotal}, DB={expected}");
    }
}

static async Task V85_QuoteListBrandAndImage()
{
    var (client, stream, _) = await SignupAndLoginAsync("v85");
    using (client)
    {
        var add = await SendTypedAsync<QuoteAddResponse>(stream, new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = 1, OptionIds = new List<int>() });
        Assert(add.Result == "OK", $"저장 실패: {add.Code}");
        var resp = await SendTypedAsync<QuoteListResponse>(stream, new RequestBase { Type = RequestType.QuoteList });
        var quote = resp.Quotes.Single(q => q.QuoteId == add.QuoteId);

        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT b.brand_name, c.image_url FROM cars c JOIN brands b ON b.brand_id = c.brand_id WHERE c.car_id = 1", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        string expectedBrand = reader.GetString("brand_name");
        string? expectedImage = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString("image_url");

        Assert(quote.BrandName == expectedBrand, $"brand_name이 DB와 다름: 응답={quote.BrandName}, DB={expectedBrand}");
        Assert(quote.ImageUrl == expectedImage, $"image_url이 DB와 다름: 응답={quote.ImageUrl}, DB={expectedImage}");
    }
}

static async Task V86_AdminStatsBrandName()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    var summary = await SendTypedAsync<AdminStatsSummaryResponse>(stream, new RequestBase { Type = RequestType.AdminStatsSummary });
    var weekly = await SendTypedAsync<AdminStatsWeeklyTop5Response>(stream, new RequestBase { Type = RequestType.AdminStatsWeeklyTop5 });
    Assert(summary.Result == "OK" && weekly.Result == "OK", "조회 실패");

    // 사전조건: 이 실행에서 V6/V8이 오늘 견적을 저장했으므로 daily_top과 국산 TOP5가 비어 있지 않다.
    Assert(summary.DailyTop is not null, "오늘 견적 데이터가 없음(사전조건 실패)");
    Assert(weekly.Domestic.Count > 0, "최근 7일 국산 견적 데이터가 없음(사전조건 실패)");

    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    DailyTopSummaryDto dailyTop = summary.DailyTop!;
    foreach (var (carId, actual) in new[] { (dailyTop.CarId, dailyTop.BrandName) }
                 .Concat(weekly.Domestic.Concat(weekly.Imported).Select(c => (c.CarId, c.BrandName))))
    {
        await using var cmd = new MySqlCommand(
            "SELECT b.brand_name FROM cars c JOIN brands b ON b.brand_id = c.brand_id WHERE c.car_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", carId);
        string expected = (string)(await cmd.ExecuteScalarAsync())!;
        Assert(actual == expected, $"car_id={carId} brand_name이 DB와 다름: 응답={actual}, DB={expected}");
    }

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

// ===== 9단계: 통합 테스트 =====

static async Task<List<(int CarId, List<(int OptionId, int Price)> Options, int CarPrice)>> LoadActiveCarsWithOptionsAsync()
{
    await using var conn = new MySqlConnection(DbConnectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand(
        "SELECT c.car_id, c.car_price, o.option_id, o.price FROM cars c " +
        "LEFT JOIN car_options o ON o.car_id = c.car_id AND o.is_active = 1 " +
        "WHERE c.is_active = 1 ORDER BY c.car_id, o.option_id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    var byCar = new Dictionary<int, (int CarPrice, List<(int OptionId, int Price)> Options)>();
    while (await reader.ReadAsync())
    {
        int carId = reader.GetInt32("car_id");
        if (!byCar.TryGetValue(carId, out var entry))
        {
            entry = (reader.GetInt32("car_price"), new List<(int OptionId, int Price)>());
            byCar[carId] = entry;
        }
        if (!reader.IsDBNull(reader.GetOrdinal("option_id")))
        {
            entry.Options.Add((reader.GetInt32("option_id"), reader.GetInt32("price")));
        }
    }
    return byCar.Select(kv => (kv.Key, kv.Value.Options, kv.Value.CarPrice)).ToList();
}

static async Task V91_ConcurrentQuotesNotMixed()
{
    // 5명을 먼저 만든다(가입 아이디 충돌을 피하려고 접두사를 다르게, 순서대로).
    var users = new List<(TcpClient Client, NetworkStream Stream, int UserId)>();
    foreach (string prefix in new[] { "v91a", "v91b", "v91c", "v91d", "v91e" })
    {
        users.Add(await SignupAndLoginAsync(prefix));
    }

    try
    {
        // 사람마다 다른 차량·옵션 조합. 기대 총액은 DB 가격으로 직접 계산한다(seed 값 하드코딩 금지).
        var cars = await LoadActiveCarsWithOptionsAsync();
        Assert(cars.Count >= 5, "활성 차량이 5대 미만(사전조건 실패)");
        var plans = users.Select((u, i) =>
        {
            var car = cars[i];
            var picked = car.Options.Take(i % (car.Options.Count + 1)).ToList();
            long expected = car.CarPrice + picked.Sum(o => (long)o.Price);
            return (User: u, CarId: car.CarId, OptionIds: picked.Select(o => o.OptionId).ToList(), Expected: expected);
        }).ToList();

        // 동시에 저장
        var saves = await Task.WhenAll(plans.Select(pl => SendTypedAsync<QuoteAddResponse>(pl.User.Stream,
            new QuoteAddRequest { Type = RequestType.QuoteAdd, CarId = pl.CarId, OptionIds = pl.OptionIds })));
        for (int i = 0; i < plans.Count; i++)
        {
            Assert(saves[i].Result == "OK", $"{i}번 사용자 저장 실패: {saves[i].Code}");
            Assert(saves[i].TotalPrice == plans[i].Expected, $"{i}번 총액이 DB 계산과 다름: {saves[i].TotalPrice} != {plans[i].Expected}");
        }

        // 각자 견적함에는 자기 견적 1건만
        var lists = await Task.WhenAll(plans.Select(pl =>
            SendTypedAsync<QuoteListResponse>(pl.User.Stream, new RequestBase { Type = RequestType.QuoteList })));
        for (int i = 0; i < plans.Count; i++)
        {
            Assert(lists[i].Quotes.Count == 1, $"{i}번 견적함 건수가 1이 아님: {lists[i].Quotes.Count}");
            var q = lists[i].Quotes[0];
            Assert(q.QuoteId == saves[i].QuoteId && q.CarId == plans[i].CarId && q.Options.Count == plans[i].OptionIds.Count,
                $"{i}번 견적함에 남의 견적이 보임: quote_id={q.QuoteId}, car_id={q.CarId}");
        }

        // DB에서도 quote.user_id가 저장한 사람과 일치
        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();
        for (int i = 0; i < plans.Count; i++)
        {
            await using var cmd = new MySqlCommand("SELECT user_id FROM quotes WHERE quote_id = @q", conn);
            cmd.Parameters.AddWithValue("@q", saves[i].QuoteId);
            int owner = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert(owner == plans[i].User.UserId, $"{i}번 견적의 DB user_id가 다름: {owner} != {plans[i].User.UserId}");
        }
    }
    finally
    {
        foreach (var u in users)
        {
            u.Client.Dispose();
        }
    }
}

static async Task V92_PipelinedRequestsMatchReqId()
{
    var cars = await LoadActiveCarsWithOptionsAsync();
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    // 응답을 기다리지 않고 20건을 연달아 보낸다. req_id마다 요청한 car_id를 기억한다.
    var expected = new Dictionary<int, int>();
    for (int i = 0; i < 20; i++)
    {
        int reqId = 9200 + i;
        int carId = cars[i % cars.Count].CarId;
        expected[reqId] = carId;
        var req = new CarDetailRequest { Type = RequestType.CarDetail, ReqId = reqId, CarId = carId };
        await FrameCodec.WriteAsync(stream, new Frame { Json = JsonSerializer.Serialize(req) });
    }

    var seen = new HashSet<int>();
    for (int i = 0; i < 20; i++)
    {
        Frame frame = await FrameCodec.ReadAsync(stream);
        var resp = JsonSerializer.Deserialize<CarDetailResponse>(frame.Json)!;
        Assert(resp.Result == "OK", $"req_id={resp.ReqId} 실패: {resp.Code}");
        Assert(expected.TryGetValue(resp.ReqId, out int carId), $"모르는 req_id 응답: {resp.ReqId}");
        Assert(resp.Car!.CarId == carId, $"req_id={resp.ReqId} 응답 차량이 다름: {resp.Car.CarId} != {carId}");
        Assert(seen.Add(resp.ReqId), $"req_id={resp.ReqId} 응답이 두 번 옴");
    }
    Assert(seen.Count == 20, $"응답 {seen.Count}/20건");
}

static async Task V9pool_ServerDbConnectionsAtMostFive()
{
    // 12세션이 동시에 main_summary(쿼리 여러 개)를 반복 → 서버가 동시에 여는 DB 연결을 늘리려 한다.
    // 서버 풀 상한이 5면 대기했다가 처리되므로 전부 OK이고, 서버의 danawa_app 연결은 5개를 넘지 않는다.
    var sessions = new List<TcpClient>();
    for (int i = 0; i < 12; i++)
    {
        sessions.Add(await ConnectAsync());
    }

    try
    {
        var results = await Task.WhenAll(sessions.Select(async c =>
        {
            NetworkStream st = c.GetStream();
            var codes = new List<string>();
            for (int k = 0; k < 10; k++)
            {
                var r = await SendTypedAsync<MainSummaryResponse>(st, new RequestBase { Type = RequestType.MainSummary });
                codes.Add(r.Code);
            }
            return codes;
        }));
        var notOk = results.SelectMany(c => c).Where(c => c != ResultCode.Ok).ToList();
        Assert(notOk.Count == 0, $"실패 응답 {notOk.Count}건: {string.Join(",", notOk.Distinct())}");

        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.PROCESSLIST WHERE USER = 'danawa_app' AND ID <> CONNECTION_ID()", conn);
        int serverConns = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Console.WriteLine($"      (서버의 danawa_app DB 연결 수: {serverConns})");
        Assert(serverConns >= 1 && serverConns <= 5, $"서버 DB 연결 수가 1~5 범위가 아님: {serverConns}");
    }
    finally
    {
        foreach (var c in sessions)
        {
            c.Dispose();
        }
    }
}

static async Task V95a_AdminBrandUpdate()
{
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();
    await AdminLoginAsync(stream);

    string name = UniqueLoginId("brand95");
    var created = await SendTypedAsync<AdminBrandSaveResponse>(stream, new AdminBrandSaveRequest
    {
        Type = RequestType.AdminBrandSave, BrandName = name, OriginType = "DOMESTIC"
    });
    Assert(created.Result == "OK", $"등록 실패: {created.Code}");

    // 같은 brand_id로 이름·국산/수입·노출을 바꾼다 → 새 행이 생기지 않고 그 행이 바뀐다 (v1 B1: 이름이 PK라 새 행이 생기던 문제)
    string renamed = name + "x";
    var updated = await SendTypedAsync<AdminBrandSaveResponse>(stream, new AdminBrandSaveRequest
    {
        Type = RequestType.AdminBrandSave, BrandId = created.BrandId, BrandName = renamed, OriginType = "IMPORT", IsActive = false
    });
    Assert(updated.Result == "OK" && updated.BrandId == created.BrandId, $"수정 실패: {updated.Code}, brand_id={updated.BrandId}");

    var list = await SendTypedAsync<AdminBrandListResponse>(stream, new RequestBase { Type = RequestType.AdminBrandList });
    var row = list.Brands.Single(b => b.BrandId == created.BrandId);
    Assert(row.BrandName == renamed && row.OriginType == "IMPORT" && !row.IsActive,
        $"수정 내용이 목록에 반영 안 됨: {row.BrandName}/{row.OriginType}/{row.IsActive}");
    Assert(!list.Brands.Any(b => b.BrandName == name), "옛 이름의 행이 남아 있음(새 행이 생김)");

    await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Logout });
}

static async Task V94_DbDownServerError()
{
    // 전제: 서버는 떠 있고 MySQL80 서비스는 멈춘 상태.
    using TcpClient client = await ConnectAsync();
    NetworkStream stream = client.GetStream();

    var ping = await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Ping });
    Assert(ping.Result == "OK", "ping(DB 안 씀)이 실패함");

    foreach (RequestBase req in new RequestBase[]
    {
        new RequestBase { Type = RequestType.BrandList },
        new LoginRequest { Type = RequestType.Login, LoginId = "admin", Password = "Admin1234!" },
        new CarListRequest { Type = RequestType.CarList },
    })
    {
        var resp = await SendTypedAsync<ResponseBase>(stream, req);
        Assert(resp.Result == "ERROR" && resp.Code == ResultCode.ServerError, $"{req.Type}: SERVER_ERROR가 아님 ({resp.Code})");
    }

    // 같은 연결로 계속 요청할 수 있고, 새 연결도 받는다 = 서버 프로세스가 살아 있다.
    var pingAgain = await SendTypedAsync<ResponseBase>(stream, new RequestBase { Type = RequestType.Ping });
    Assert(pingAgain.Result == "OK", "오류 뒤 같은 연결이 끊김");
    using TcpClient second = await ConnectAsync();
    var pingNew = await SendTypedAsync<ResponseBase>(second.GetStream(), new RequestBase { Type = RequestType.Ping });
    Assert(pingNew.Result == "OK", "새 연결을 받지 못함");
}
