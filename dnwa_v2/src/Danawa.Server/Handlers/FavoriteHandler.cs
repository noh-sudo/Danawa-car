using System.Text.Json;
using Danawa.Server.Data;
using Danawa.Server.Net;
using Danawa.Shared.Contracts;
using MySqlConnector;

namespace Danawa.Server.Handlers;

/// <summary>
/// IF-FAV-001~002. docs/03_API명세.md 3절 그대로. 둘 다 USER 전용 — ADMIN 세션은 FORBIDDEN (FAV-05).
/// </summary>
public static class FavoriteHandler
{
    public static async Task<ResponseBase> FavoriteSetAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<FavoriteSetRequest>(json)!;

        ResponseBase? authError = CheckUserAuth(req, session, out int userId);
        if (authError is not null)
        {
            return authError;
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        if (req.On)
        {
            await using var cmd = new MySqlCommand(
                "INSERT IGNORE INTO favorites (user_id, car_id) VALUES (@uid, @carId)", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await using var cmd = new MySqlCommand(
                "DELETE FROM favorites WHERE user_id = @uid AND car_id = @carId", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            await cmd.ExecuteNonQueryAsync();
        }

        return new FavoriteSetResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            CarId = req.CarId,
            IsFavorite = req.On
        };
    }

    public static async Task<ResponseBase> FavoriteListAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        ResponseBase? authError = CheckUserAuth(req, session, out int userId);
        if (authError is not null)
        {
            return authError;
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(@"
            SELECT c.car_id, c.car_name, b.brand_name, c.image_url, c.car_price,
                   COALESCE((SELECT SUM(o.price) FROM car_options o WHERE o.car_id = c.car_id AND o.is_active = 1), 0) AS option_total,
                   f.created_at
            FROM favorites f
            JOIN cars c ON c.car_id = f.car_id
            JOIN brands b ON b.brand_id = c.brand_id
            WHERE f.user_id = @uid
            ORDER BY f.created_at DESC", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var favorites = new List<FavoriteDto>();
        while (await reader.ReadAsync())
        {
            favorites.Add(new FavoriteDto
            {
                CarId = reader.GetInt32("car_id"),
                CarName = reader.GetString("car_name"),
                BrandName = reader.GetString("brand_name"),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString("image_url"),
                CarPrice = reader.GetInt32("car_price"),
                OptionTotal = reader.GetInt64("option_total"),
                CreatedAt = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        return new FavoriteListResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            Favorites = favorites
        };
    }

    /// <summary>비로그인이면 UNAUTHORIZED, ADMIN이면 FORBIDDEN(FAV-05), 통과 시 null과 함께 userId 채움</summary>
    private static ResponseBase? CheckUserAuth(RequestBase req, ClientSession session, out int userId)
    {
        if (session.UserId is not int uid)
        {
            userId = 0;
            return Error(req, ResultCode.Unauthorized, "로그인이 필요합니다.");
        }

        if (session.Role == "ADMIN")
        {
            userId = 0;
            return Error(req, ResultCode.Forbidden, "관리자는 관심 기능을 사용할 수 없습니다.");
        }

        userId = uid;
        return null;
    }

    private static ResponseBase Error(RequestBase req, string code, string message) => new()
    {
        Type = req.Type,
        ReqId = req.ReqId,
        Result = "ERROR",
        Code = code,
        Message = message
    };
}
