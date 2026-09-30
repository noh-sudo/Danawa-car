using System.Text.Json;
using Danawa.Server.Data;
using Danawa.Shared.Contracts;
using MySqlConnector;

namespace Danawa.Server.Handlers;

/// <summary>IF-ADM-001~012, docs/03_API명세.md 3절 그대로 — 권한 검사(ADM-01)는 여기서 하지 않음, ClientSession의 admin_* 분기 지점 한 곳에서 이미 확인 완료</summary>
public static class AdminHandler
{
    // ---- 브랜드 관리 (IF-ADM-001~002) ----

    public static async Task<ResponseBase> BrandListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT brand_id, brand_name, brand_icon, origin_type, is_active FROM brands ORDER BY brand_id", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var brands = new List<AdminBrandDto>();
        while (await reader.ReadAsync())
        {
            brands.Add(new AdminBrandDto
            {
                BrandId = reader.GetInt32("brand_id"),
                BrandName = reader.GetString("brand_name"),
                BrandIcon = reader.IsDBNull(reader.GetOrdinal("brand_icon")) ? null : reader.GetString("brand_icon"),
                OriginType = reader.GetString("origin_type"),
                IsActive = reader.GetBoolean("is_active")
            });
        }

        return new AdminBrandListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Brands = brands };
    }

    public static async Task<ResponseBase> BrandSaveAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminBrandSaveRequest>(json)!;
        bool isActive = req.IsActive ?? true;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        try
        {
            if (req.BrandId is int brandId)
            {
                await using var cmd = new MySqlCommand(
                    "UPDATE brands SET brand_name = @name, brand_icon = @icon, origin_type = @origin, is_active = @active " +
                    "WHERE brand_id = @id", conn);
                cmd.Parameters.AddWithValue("@name", req.BrandName);
                cmd.Parameters.AddWithValue("@icon", (object?)req.BrandIcon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@origin", req.OriginType);
                cmd.Parameters.AddWithValue("@active", isActive);
                cmd.Parameters.AddWithValue("@id", brandId);
                await cmd.ExecuteNonQueryAsync();

                return new AdminBrandSaveResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, BrandId = brandId };
            }
            else
            {
                await using var cmd = new MySqlCommand(
                    "INSERT INTO brands (brand_name, brand_icon, origin_type, is_active) VALUES (@name, @icon, @origin, @active)", conn);
                cmd.Parameters.AddWithValue("@name", req.BrandName);
                cmd.Parameters.AddWithValue("@icon", (object?)req.BrandIcon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@origin", req.OriginType);
                cmd.Parameters.AddWithValue("@active", isActive);
                await cmd.ExecuteNonQueryAsync();

                return new AdminBrandSaveResponse
                {
                    Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, BrandId = (int)cmd.LastInsertedId
                };
            }
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(req, ResultCode.Conflict, "이미 사용 중인 브랜드명입니다.");
        }
    }

    // ---- 차량 관리 (IF-ADM-003~005) ----

    public static async Task<ResponseBase> CarListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminCarListRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        string sql = "SELECT c.car_id, c.brand_id, b.brand_name, c.car_name, c.image_url, c.car_price, " +
                     "c.fuel_min, c.fuel_max, c.release_date, c.is_active " +
                     "FROM cars c JOIN brands b ON b.brand_id = c.brand_id";
        if (req.BrandId is int)
        {
            sql += " WHERE c.brand_id = @brandId";
        }
        sql += " ORDER BY c.car_id";

        await using var cmd = new MySqlCommand(sql, conn);
        if (req.BrandId is int brandId)
        {
            cmd.Parameters.AddWithValue("@brandId", brandId);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        var cars = new List<AdminCarDto>();
        while (await reader.ReadAsync())
        {
            cars.Add(new AdminCarDto
            {
                CarId = reader.GetInt32("car_id"),
                BrandId = reader.GetInt32("brand_id"),
                BrandName = reader.GetString("brand_name"),
                CarName = reader.GetString("car_name"),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString("image_url"),
                CarPrice = reader.GetInt32("car_price"),
                FuelMin = reader.IsDBNull(reader.GetOrdinal("fuel_min")) ? null : (double?)reader.GetDecimal("fuel_min"),
                FuelMax = reader.IsDBNull(reader.GetOrdinal("fuel_max")) ? null : (double?)reader.GetDecimal("fuel_max"),
                ReleaseDate = reader.IsDBNull(reader.GetOrdinal("release_date"))
                    ? null
                    : reader.GetDateTime("release_date").ToString("yyyy-MM-dd"),
                IsActive = reader.GetBoolean("is_active")
            });
        }

        return new AdminCarListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Cars = cars };
    }

    public static async Task<ResponseBase> CarSaveAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminCarSaveRequest>(json)!;

        if (req.CarPrice < 0)
        {
            return Error(req, ResultCode.Validation, "가격은 0 이상이어야 합니다.");
        }

        if (req.FuelMin is double fmin && req.FuelMax is double fmax && fmin > fmax)
        {
            return Error(req, ResultCode.Validation, "최소 연비가 최대 연비보다 클 수 없습니다.");
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        try
        {
            if (req.CarId is int carId)
            {
                await using var cmd = new MySqlCommand(
                    "UPDATE cars SET brand_id = @brandId, car_name = @name, image_url = @img, car_price = @price, " +
                    "fuel_min = @fmin, fuel_max = @fmax, release_date = @releaseDate WHERE car_id = @id", conn);
                cmd.Parameters.AddWithValue("@brandId", req.BrandId);
                cmd.Parameters.AddWithValue("@name", req.CarName);
                cmd.Parameters.AddWithValue("@img", (object?)req.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@price", req.CarPrice);
                cmd.Parameters.AddWithValue("@fmin", (object?)req.FuelMin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fmax", (object?)req.FuelMax ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@releaseDate", (object?)req.ReleaseDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", carId);
                await cmd.ExecuteNonQueryAsync();

                return new AdminCarSaveResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, CarId = carId };
            }
            else
            {
                await using var cmd = new MySqlCommand(
                    "INSERT INTO cars (brand_id, car_name, image_url, car_price, fuel_min, fuel_max, release_date) " +
                    "VALUES (@brandId, @name, @img, @price, @fmin, @fmax, @releaseDate)", conn);
                cmd.Parameters.AddWithValue("@brandId", req.BrandId);
                cmd.Parameters.AddWithValue("@name", req.CarName);
                cmd.Parameters.AddWithValue("@img", (object?)req.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@price", req.CarPrice);
                cmd.Parameters.AddWithValue("@fmin", (object?)req.FuelMin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fmax", (object?)req.FuelMax ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@releaseDate", (object?)req.ReleaseDate ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();

                return new AdminCarSaveResponse
                {
                    Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, CarId = (int)cmd.LastInsertedId
                };
            }
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(req, ResultCode.Conflict, "같은 브랜드에 이미 같은 차량명이 있습니다.");
        }
    }

    public static async Task<ResponseBase> CarDeleteAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminCarDeleteRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE cars SET is_active = 0 WHERE car_id = @carId", conn);
        cmd.Parameters.AddWithValue("@carId", req.CarId);
        await cmd.ExecuteNonQueryAsync();

        return new ResponseBase { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok };
    }

    // ---- 옵션 관리 (IF-ADM-006~008) ----

    public static async Task<ResponseBase> OptionListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminOptionListRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT option_id, option_name, price, is_active FROM car_options WHERE car_id = @carId ORDER BY option_id", conn);
        cmd.Parameters.AddWithValue("@carId", req.CarId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var options = new List<AdminOptionDto>();
        while (await reader.ReadAsync())
        {
            options.Add(new AdminOptionDto
            {
                OptionId = reader.GetInt32("option_id"),
                OptionName = reader.GetString("option_name"),
                Price = reader.GetInt32("price"),
                IsActive = reader.GetBoolean("is_active")
            });
        }

        return new AdminOptionListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Options = options };
    }

    public static async Task<ResponseBase> OptionSaveAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminOptionSaveRequest>(json)!;

        if (req.Price < 0)
        {
            return Error(req, ResultCode.Validation, "가격은 0 이상이어야 합니다.");
        }

        bool isActive = req.IsActive ?? true;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        try
        {
            if (req.OptionId is int optionId)
            {
                await using var cmd = new MySqlCommand(
                    "UPDATE car_options SET option_name = @name, price = @price, is_active = @active " +
                    "WHERE option_id = @id AND car_id = @carId", conn);
                cmd.Parameters.AddWithValue("@name", req.OptionName);
                cmd.Parameters.AddWithValue("@price", req.Price);
                cmd.Parameters.AddWithValue("@active", isActive);
                cmd.Parameters.AddWithValue("@id", optionId);
                cmd.Parameters.AddWithValue("@carId", req.CarId);
                await cmd.ExecuteNonQueryAsync();

                return new AdminOptionSaveResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, OptionId = optionId };
            }
            else
            {
                await using var cmd = new MySqlCommand(
                    "INSERT INTO car_options (car_id, option_name, price, is_active) VALUES (@carId, @name, @price, @active)", conn);
                cmd.Parameters.AddWithValue("@carId", req.CarId);
                cmd.Parameters.AddWithValue("@name", req.OptionName);
                cmd.Parameters.AddWithValue("@price", req.Price);
                cmd.Parameters.AddWithValue("@active", isActive);
                await cmd.ExecuteNonQueryAsync();

                return new AdminOptionSaveResponse
                {
                    Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, OptionId = (int)cmd.LastInsertedId
                };
            }
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(req, ResultCode.Conflict, "같은 차량에 이미 같은 옵션명이 있습니다.");
        }
    }

    public static async Task<ResponseBase> OptionDeleteAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminOptionDeleteRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE car_options SET is_active = 0 WHERE option_id = @optionId", conn);
        cmd.Parameters.AddWithValue("@optionId", req.OptionId);
        await cmd.ExecuteNonQueryAsync();

        return new ResponseBase { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok };
    }

    // ---- 통계 (IF-ADM-009~011) ----

    public static async Task<ResponseBase> StatsSummaryAsync(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        int totalQuotes;
        await using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM quotes WHERE is_deleted = 0", conn))
        {
            totalQuotes = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        DailyTopSummaryDto? dailyTop = null;
        await using (var cmd = new MySqlCommand(
            "SELECT c.car_id, c.car_name, b.brand_name, COUNT(*) AS quote_count " +
            "FROM quotes q JOIN cars c ON c.car_id = q.car_id JOIN brands b ON b.brand_id = c.brand_id " +
            "WHERE q.created_at >= CURDATE() AND q.created_at < CURDATE() + INTERVAL 1 DAY AND q.is_deleted = 0 " +
            "GROUP BY c.car_id ORDER BY quote_count DESC LIMIT 1", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dailyTop = new DailyTopSummaryDto
                {
                    CarId = reader.GetInt32("car_id"),
                    CarName = reader.GetString("car_name"),
                    BrandName = reader.GetString("brand_name"),
                    QuoteCount = reader.GetInt32("quote_count")
                };
            }
        }

        return new AdminStatsSummaryResponse
        {
            Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, TotalQuotes = totalQuotes, DailyTop = dailyTop
        };
    }

    public static async Task<ResponseBase> StatsWeeklyTop5Async(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        List<WeeklyTopCarDto> domestic = await QueryWeeklyTop5Async(conn, "DOMESTIC");
        List<WeeklyTopCarDto> imported = await QueryWeeklyTop5Async(conn, "IMPORT");

        return new AdminStatsWeeklyTop5Response
        {
            Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Domestic = domestic, Imported = imported
        };
    }

    private static async Task<List<WeeklyTopCarDto>> QueryWeeklyTop5Async(MySqlConnection conn, string originType)
    {
        await using var cmd = new MySqlCommand(
            "SELECT c.car_id, c.car_name, b.brand_name, COUNT(*) AS quote_count " +
            "FROM quotes q JOIN cars c ON c.car_id = q.car_id JOIN brands b ON b.brand_id = c.brand_id " +
            "WHERE q.created_at >= (CURDATE() - INTERVAL 6 DAY) AND q.created_at < CURDATE() + INTERVAL 1 DAY " +
            "  AND q.is_deleted = 0 AND b.origin_type = @originType " +
            "GROUP BY c.car_id ORDER BY quote_count DESC LIMIT 5", conn);
        cmd.Parameters.AddWithValue("@originType", originType);
        await using var reader = await cmd.ExecuteReaderAsync();

        var result = new List<WeeklyTopCarDto>();
        while (await reader.ReadAsync())
        {
            result.Add(new WeeklyTopCarDto
            {
                CarId = reader.GetInt32("car_id"),
                CarName = reader.GetString("car_name"),
                BrandName = reader.GetString("brand_name"),
                QuoteCount = reader.GetInt32("quote_count")
            });
        }

        return result;
    }

    public static async Task<ResponseBase> StatsDailyAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminStatsDailyRequest>(json)!;

        if (!DateOnly.TryParse(req.DateFrom, out DateOnly dateFrom) || !DateOnly.TryParse(req.DateTo, out DateOnly dateTo))
        {
            return Error(req, ResultCode.Validation, "날짜 형식이 올바르지 않습니다.");
        }

        if (dateFrom > dateTo)
        {
            return Error(req, ResultCode.Validation, "시작일이 종료일보다 늦을 수 없습니다.");
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT DATE(created_at) AS day, COUNT(*) AS cnt FROM quotes " +
            "WHERE created_at >= @dateFrom AND created_at < @dateToExclusive AND is_deleted = 0 " +
            "GROUP BY DATE(created_at) ORDER BY day", conn);
        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@dateToExclusive", dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue));
        await using var reader = await cmd.ExecuteReaderAsync();

        var days = new List<DailyCountDto>();
        while (await reader.ReadAsync())
        {
            days.Add(new DailyCountDto
            {
                Day = reader.GetDateTime("day").ToString("yyyy-MM-dd"),
                Count = reader.GetInt32("cnt")
            });
        }

        return new AdminStatsDailyResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Days = days };
    }

    // ---- 로그 조회 (IF-ADM-012) ----

    public static async Task<ResponseBase> LogListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<AdminLogListRequest>(json)!;

        bool hasRange = !string.IsNullOrEmpty(req.DateFrom) && !string.IsNullOrEmpty(req.DateTo);

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT u.login_id, u.role, l.action, l.ip_addr, l.created_at " +
                     "FROM access_log l JOIN users u ON u.user_id = l.user_id";
        if (hasRange)
        {
            sql += " WHERE l.created_at >= @dateFrom AND l.created_at < @dateToExclusive";
        }
        sql += " ORDER BY l.created_at DESC";
        if (!hasRange)
        {
            sql += " LIMIT 200";
        }

        await using var cmd = new MySqlCommand(sql, conn);
        if (hasRange)
        {
            DateOnly dateFrom = DateOnly.Parse(req.DateFrom!);
            DateOnly dateTo = DateOnly.Parse(req.DateTo!);
            cmd.Parameters.AddWithValue("@dateFrom", dateFrom.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@dateToExclusive", dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        var logs = new List<AdminLogDto>();
        while (await reader.ReadAsync())
        {
            logs.Add(new AdminLogDto
            {
                LoginId = reader.GetString("login_id"),
                Role = reader.GetString("role"),
                Action = reader.GetString("action"),
                IpAddr = reader.GetString("ip_addr"),
                CreatedAt = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        return new AdminLogListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Logs = logs };
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
