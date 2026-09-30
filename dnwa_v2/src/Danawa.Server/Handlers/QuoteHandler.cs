using System.Text.Json;
using Danawa.Server.Data;
using Danawa.Server.Net;
using Danawa.Shared.Contracts;
using MySqlConnector;

namespace Danawa.Server.Handlers;

/// <summary>IF-QUO-001~003, docs/03_API명세.md 3절 그대로, 전부 USER 전용(ADMIN 세션은 FORBIDDEN, QUO-08) — 금액은 항상 서버가 DB 가격으로 재계산(R4, QUO-02), 요청 DTO에 total_price 같은 필드 자체가 없음</summary>
public static class QuoteHandler
{
    public static async Task<ResponseBase> QuoteAddAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<QuoteAddRequest>(json)!;

        ResponseBase? authError = CheckUserAuth(req, session, out int userId);
        if (authError is not null)
        {
            return authError;
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        string carName;
        int carPrice;
        await using (var cmd = new MySqlCommand(
            "SELECT car_name, car_price FROM cars WHERE car_id = @carId AND is_active = 1", conn))
        {
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return Error(req, ResultCode.Validation, "차량을 찾을 수 없습니다.");
            }

            carName = reader.GetString("car_name");
            carPrice = reader.GetInt32("car_price");
        }

        List<int> requestedOptionIds = req.OptionIds.Distinct().ToList();
        var options = new List<(int OptionId, string Name, int Price)>();

        if (requestedOptionIds.Count > 0)
        {
            string placeholders = string.Join(",", requestedOptionIds.Select((_, i) => $"@opt{i}"));
            await using var cmd = new MySqlCommand(
                $"SELECT option_id, option_name, price FROM car_options " +
                $"WHERE car_id = @carId AND is_active = 1 AND option_id IN ({placeholders})", conn);
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            for (int i = 0; i < requestedOptionIds.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@opt{i}", requestedOptionIds[i]);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                options.Add((reader.GetInt32("option_id"), reader.GetString("option_name"), reader.GetInt32("price")));
            }
        }

        if (options.Count != requestedOptionIds.Count)
        {
            // 요청 옵션 중 일부가 이 차량 것이 아니거나 비활성 옵션 (QUO-03)
            return Error(req, ResultCode.Validation, "선택한 옵션이 이 차량의 옵션이 아닙니다.");
        }

        int totalPrice = carPrice + options.Sum(o => o.Price);

        await using MySqlTransaction transaction = await conn.BeginTransactionAsync();
        try
        {
            int quoteId;
            await using (var cmd = new MySqlCommand(
                "INSERT INTO quotes (user_id, car_id, car_name_snap, car_price_snap, total_price) " +
                "VALUES (@uid, @carId, @carName, @carPrice, @total)", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@carId", req.CarId);
                cmd.Parameters.AddWithValue("@carName", carName);
                cmd.Parameters.AddWithValue("@carPrice", carPrice);
                cmd.Parameters.AddWithValue("@total", totalPrice);
                await cmd.ExecuteNonQueryAsync();
                quoteId = (int)cmd.LastInsertedId;
            }

            foreach ((int optionId, string name, int price) in options)
            {
                await using var cmd = new MySqlCommand(
                    "INSERT INTO quote_options (quote_id, option_id, option_name_snap, option_price_snap) " +
                    "VALUES (@quoteId, @optionId, @name, @price)", conn, transaction);
                cmd.Parameters.AddWithValue("@quoteId", quoteId);
                cmd.Parameters.AddWithValue("@optionId", optionId);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@price", price);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return new QuoteAddResponse
            {
                Type = req.Type,
                ReqId = req.ReqId,
                Result = "OK",
                Code = ResultCode.Ok,
                QuoteId = quoteId,
                TotalPrice = totalPrice
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<ResponseBase> QuoteListAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        ResponseBase? authError = CheckUserAuth(req, session, out int userId);
        if (authError is not null)
        {
            return authError;
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        var quotes = new List<QuoteDto>();
        var quoteById = new Dictionary<int, QuoteDto>();

        await using (var cmd = new MySqlCommand(
            // brand_name·image_url은 스냅샷이 아니라 현재 값(8단계, 견적함 카드 표시용) — 차량은 소프트 삭제라 JOIN 안 끊김
            "SELECT q.quote_id, q.car_id, q.car_name_snap, b.brand_name, c.image_url, q.car_price_snap, q.total_price, q.created_at " +
            "FROM quotes q JOIN cars c ON c.car_id = q.car_id JOIN brands b ON b.brand_id = c.brand_id " +
            "WHERE q.user_id = @uid AND q.is_deleted = 0 ORDER BY q.created_at DESC, q.quote_id DESC", conn))
        {
            cmd.Parameters.AddWithValue("@uid", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dto = new QuoteDto
                {
                    QuoteId = reader.GetInt32("quote_id"),
                    CarId = reader.GetInt32("car_id"),
                    CarName = reader.GetString("car_name_snap"),
                    BrandName = reader.GetString("brand_name"),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString("image_url"),
                    CarPrice = reader.GetInt32("car_price_snap"),
                    TotalPrice = reader.GetInt32("total_price"),
                    CreatedAt = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm:ss")
                };
                quotes.Add(dto);
                quoteById[dto.QuoteId] = dto;
            }
        }

        if (quoteById.Count > 0)
        {
            string placeholders = string.Join(",", quoteById.Keys.Select((_, i) => $"@q{i}"));
            await using var cmd = new MySqlCommand(
                $"SELECT quote_id, option_name_snap, option_price_snap FROM quote_options " +
                $"WHERE quote_id IN ({placeholders}) ORDER BY quote_id, option_id", conn);
            int i = 0;
            foreach (int quoteId in quoteById.Keys)
            {
                cmd.Parameters.AddWithValue($"@q{i}", quoteId);
                i++;
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int quoteId = reader.GetInt32("quote_id");
                quoteById[quoteId].Options.Add(new QuoteOptionDto
                {
                    OptionName = reader.GetString("option_name_snap"),
                    Price = reader.GetInt32("option_price_snap")
                });
            }
        }

        return new QuoteListResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            Quotes = quotes
        };
    }

    public static async Task<ResponseBase> QuoteDeleteAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<QuoteDeleteRequest>(json)!;

        ResponseBase? authError = CheckUserAuth(req, session, out int userId);
        if (authError is not null)
        {
            return authError;
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "UPDATE quotes SET is_deleted = 1 WHERE quote_id = @quoteId AND user_id = @uid AND is_deleted = 0", conn);
        cmd.Parameters.AddWithValue("@quoteId", req.QuoteId);
        cmd.Parameters.AddWithValue("@uid", userId);
        int affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
        {
            // 내 견적이 아니거나 없음 — FORBIDDEN을 쓰면 "남의 견적으로 존재한다"는 사실이 새어나감 (QUO-07)
            return Error(req, ResultCode.NotFound, "견적을 찾을 수 없습니다.");
        }

        return new ResponseBase { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok };
    }

    /// <summary>비로그인이면 UNAUTHORIZED, ADMIN이면 FORBIDDEN(QUO-08), 통과 시 null과 함께 userId 채움</summary>
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
            return Error(req, ResultCode.Forbidden, "관리자는 견적 기능을 사용할 수 없습니다.");
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
