using System.Text.Json;
using System.Text.RegularExpressions;
using Danawa.Server.Data;
using Danawa.Server.Net;
using Danawa.Server.Security;
using Danawa.Shared.Contracts;
using MySqlConnector;

namespace Danawa.Server.Handlers;

/// <summary>
/// IF-AUTH-001~004: check_id / signup / login / logout. docs/03_API명세.md 3절 그대로.
/// </summary>
public static partial class AuthHandler
{
    [GeneratedRegex("^[A-Za-z0-9]{4,30}$")]
    private static partial Regex LoginIdPattern();

    public static async Task<ResponseBase> CheckIdAsync(string json)
    {
        var req = JsonSerializer.Deserialize<CheckIdRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE login_id = @loginId", conn);
        cmd.Parameters.AddWithValue("@loginId", req.LoginId);
        long count = (long)(await cmd.ExecuteScalarAsync())!;

        return new CheckIdResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            Available = count == 0
        };
    }

    public static async Task<ResponseBase> SignupAsync(string json)
    {
        var req = JsonSerializer.Deserialize<SignupRequest>(json)!;

        if (!LoginIdPattern().IsMatch(req.LoginId) || req.Password.Length < 8)
        {
            return Error(req, ResultCode.Validation, "아이디는 4~30자 영문/숫자, 비밀번호는 8자 이상이어야 합니다.");
        }

        var (hash, salt, iter) = PasswordHasher.Hash(req.Password);

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO users (login_id, pw_hash, pw_salt, pw_iter, role) VALUES (@loginId, @hash, @salt, @iter, 'USER')",
            conn);
        cmd.Parameters.AddWithValue("@loginId", req.LoginId);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@salt", salt);
        cmd.Parameters.AddWithValue("@iter", iter);

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(req, ResultCode.Conflict, "이미 사용 중인 아이디입니다.");
        }

        return Ok(req);
    }

    public static async Task<ResponseBase> LoginAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<LoginRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        int userId;
        byte[] hash;
        byte[] salt;
        int iter;
        string role;

        await using (var cmd = new MySqlCommand(
            "SELECT user_id, pw_hash, pw_salt, pw_iter, role FROM users WHERE login_id = @loginId AND is_active = 1",
            conn))
        {
            cmd.Parameters.AddWithValue("@loginId", req.LoginId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return Error(req, ResultCode.Unauthorized, "아이디 또는 비밀번호가 올바르지 않습니다.");
            }

            userId = reader.GetInt32("user_id");
            hash = (byte[])reader["pw_hash"];
            salt = (byte[])reader["pw_salt"];
            iter = reader.GetInt32("pw_iter");
            role = reader.GetString("role");
        }

        if (!PasswordHasher.Verify(req.Password, hash, salt, iter))
        {
            return Error(req, ResultCode.Unauthorized, "아이디 또는 비밀번호가 올바르지 않습니다.");
        }

        session.UserId = userId;
        session.Role = role;

        await InsertAccessLogAsync(conn, userId, "LOGIN", session.RemoteIp);

        return new LoginResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            LoginId = req.LoginId,
            Role = role
        };
    }

    public static async Task<ResponseBase> LogoutAsync(RequestBase req, ClientSession session)
    {
        if (session.UserId is null)
        {
            return Error(req, ResultCode.Unauthorized, "로그인이 필요합니다.");
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await InsertAccessLogAsync(conn, session.UserId.Value, "LOGOUT", session.RemoteIp);

        session.UserId = null;
        session.Role = null;

        return Ok(req);
    }

    private static async Task InsertAccessLogAsync(MySqlConnection conn, int userId, string action, string ipAddr)
    {
        await using var cmd = new MySqlCommand(
            "INSERT INTO access_log (user_id, action, ip_addr) VALUES (@userId, @action, @ip)", conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@ip", ipAddr);
        await cmd.ExecuteNonQueryAsync();
    }

    private static ResponseBase Ok(RequestBase req) => new()
    {
        Type = req.Type,
        ReqId = req.ReqId,
        Result = "OK",
        Code = ResultCode.Ok
    };

    private static ResponseBase Error(RequestBase req, string code, string message) => new()
    {
        Type = req.Type,
        ReqId = req.ReqId,
        Result = "ERROR",
        Code = code,
        Message = message
    };
}
