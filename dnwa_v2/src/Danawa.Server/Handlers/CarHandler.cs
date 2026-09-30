using System.Text.Json;
using Danawa.Server.Data;
using Danawa.Server.Net;
using Danawa.Shared.Contracts;
using MySqlConnector;

namespace Danawa.Server.Handlers;

/// <summary>
/// IF-CAR-001~008. docs/03_API명세.md 3절 그대로.
/// </summary>
public static class CarHandler
{
    private const string CarSummaryColumns = @"
        c.car_id, c.car_name, b.brand_name, c.image_url, c.car_price,
        COALESCE((SELECT SUM(o.price) FROM car_options o WHERE o.car_id = c.car_id AND o.is_active = 1), 0) AS option_total,
        c.fuel_min, c.fuel_max, c.release_date";

    /// <summary>car_list/car_search의 sort 값 → ORDER BY 절, 정해진 값만 받음(요청 문자열을 SQL에 붙이지 않음) — POPULAR는 메인 TOP5(IF-CAR-006)와 같은 지표: 최근 7일(INTERVAL 6 DAY) 관심 등록 수, 모르는 값이면 null</summary>
    private static string? OrderByFor(string? sort) => sort switch
    {
        null or "" => "c.car_id",
        "POPULAR" => @"(SELECT COUNT(*) FROM favorites f
                         WHERE f.car_id = c.car_id
                           AND f.created_at >= (CURDATE() - INTERVAL 6 DAY) AND f.created_at < CURDATE() + INTERVAL 1 DAY) DESC,
                       c.car_id",
        "PRICE_ASC" => "c.car_price, c.car_id",
        _ => null
    };

    private static ResponseBase InvalidSort(RequestBase req) => new()
    {
        Type = req.Type,
        ReqId = req.ReqId,
        Result = "ERROR",
        Code = ResultCode.Validation,
        Message = "정렬 기준이 올바르지 않습니다."
    };

    public static async Task<ResponseBase> BrandListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT brand_id, brand_name, brand_icon, origin_type FROM brands WHERE is_active = 1 ORDER BY brand_id", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var brands = new List<BrandDto>();
        while (await reader.ReadAsync())
        {
            brands.Add(new BrandDto
            {
                BrandId = reader.GetInt32("brand_id"),
                BrandName = reader.GetString("brand_name"),
                BrandIcon = reader.IsDBNull(reader.GetOrdinal("brand_icon")) ? null : reader.GetString("brand_icon"),
                OriginType = reader.GetString("origin_type")
            });
        }

        return new BrandListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Brands = brands };
    }

    public static async Task<ResponseBase> CarListAsync(string json)
    {
        var req = JsonSerializer.Deserialize<CarListRequest>(json)!;

        string? orderBy = OrderByFor(req.Sort);
        if (orderBy is null)
        {
            return InvalidSort(req);
        }

        var conditions = new List<string> { "c.is_active = 1" };
        if (req.BrandId is int brandId && brandId > 0)
        {
            conditions.Add("c.brand_id = @brandId");
        }
        if (!string.IsNullOrEmpty(req.OriginType))
        {
            conditions.Add("b.origin_type = @originType");
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand($@"
            SELECT {CarSummaryColumns}
            FROM cars c JOIN brands b ON b.brand_id = c.brand_id
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY {orderBy}", conn);
        if (req.BrandId is int brandIdParam && brandIdParam > 0)
        {
            cmd.Parameters.AddWithValue("@brandId", brandIdParam);
        }
        if (!string.IsNullOrEmpty(req.OriginType))
        {
            cmd.Parameters.AddWithValue("@originType", req.OriginType);
        }
        await using var reader = await cmd.ExecuteReaderAsync();

        var cars = new List<CarSummaryDto>();
        while (await reader.ReadAsync())
        {
            cars.Add(ReadCarSummary(reader));
        }

        return new CarListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Cars = cars };
    }

    public static async Task<ResponseBase> CarSearchAsync(string json)
    {
        var req = JsonSerializer.Deserialize<CarSearchRequest>(json)!;

        string? orderBy = OrderByFor(req.Sort);
        if (orderBy is null)
        {
            return InvalidSort(req);
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand($@"
            SELECT {CarSummaryColumns}
            FROM cars c JOIN brands b ON b.brand_id = c.brand_id
            WHERE c.is_active = 1 AND (c.car_name LIKE @kw OR b.brand_name LIKE @kw)
            ORDER BY {orderBy}", conn);
        cmd.Parameters.AddWithValue("@kw", $"%{req.Keyword}%");
        await using var reader = await cmd.ExecuteReaderAsync();

        var cars = new List<CarSummaryDto>();
        while (await reader.ReadAsync())
        {
            cars.Add(ReadCarSummary(reader));
        }

        return new CarListResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Cars = cars };
    }

    public static async Task<ResponseBase> CarDetailAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<CarDetailRequest>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        CarSummaryDto? car;
        await using (var cmd = new MySqlCommand($@"
            SELECT {CarSummaryColumns}
            FROM cars c JOIN brands b ON b.brand_id = c.brand_id
            WHERE c.car_id = @carId AND c.is_active = 1", conn))
        {
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            await using var reader = await cmd.ExecuteReaderAsync();
            car = await reader.ReadAsync() ? ReadCarSummary(reader) : null;
        }

        if (car is null)
        {
            return new ResponseBase
            {
                Type = req.Type,
                ReqId = req.ReqId,
                Result = "ERROR",
                Code = ResultCode.NotFound,
                Message = "차량을 찾을 수 없습니다."
            };
        }

        var options = new List<CarOptionDto>();
        await using (var cmd = new MySqlCommand(
            "SELECT option_id, option_name, price FROM car_options WHERE car_id = @carId AND is_active = 1 ORDER BY option_id", conn))
        {
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                options.Add(new CarOptionDto
                {
                    OptionId = reader.GetInt32("option_id"),
                    OptionName = reader.GetString("option_name"),
                    Price = reader.GetInt32("price")
                });
            }
        }

        bool isFavorite = false;
        if (session.UserId is int uid)
        {
            await using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM favorites WHERE user_id = @uid AND car_id = @carId", conn);
            cmd.Parameters.AddWithValue("@uid", uid);
            cmd.Parameters.AddWithValue("@carId", req.CarId);
            isFavorite = (long)(await cmd.ExecuteScalarAsync())! > 0;
        }

        return new CarDetailResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            Car = car,
            Options = options,
            IsFavorite = isFavorite
        };
    }

    public static async Task<ResponseBase> MainDailyTopAsync(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand($@"
            SELECT {CarSummaryColumns}, COUNT(*) AS quote_count
            FROM quotes q
            JOIN cars c ON c.car_id = q.car_id
            JOIN brands b ON b.brand_id = c.brand_id
            WHERE q.created_at >= CURDATE() AND q.created_at < CURDATE() + INTERVAL 1 DAY
              AND q.is_deleted = 0 AND c.is_active = 1
            GROUP BY c.car_id
            ORDER BY quote_count DESC
            LIMIT 1", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        DailyTopCarDto? car = null;
        if (await reader.ReadAsync())
        {
            var summary = ReadCarSummary(reader);
            car = new DailyTopCarDto
            {
                CarId = summary.CarId,
                CarName = summary.CarName,
                BrandName = summary.BrandName,
                ImageUrl = summary.ImageUrl,
                CarPrice = summary.CarPrice,
                OptionTotal = summary.OptionTotal,
                FuelMin = summary.FuelMin,
                FuelMax = summary.FuelMax,
                ReleaseDate = summary.ReleaseDate,
                QuoteCount = reader.GetInt32("quote_count")
            };
        }

        return new MainDailyTopResponse { Type = req.Type, ReqId = req.ReqId, Result = "OK", Code = ResultCode.Ok, Car = car };
    }

    public static async Task<ResponseBase> MainFavoriteTop5Async(string json)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        var domestic = await QueryFavoriteTop5Async(conn, "DOMESTIC");
        var imported = await QueryFavoriteTop5Async(conn, "IMPORT");

        return new MainFavoriteTop5Response
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            Domestic = domestic,
            Imported = imported
        };
    }

    public static async Task<ResponseBase> MainSummaryAsync(string json, ClientSession session)
    {
        var req = JsonSerializer.Deserialize<RequestBase>(json)!;

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();

        int todayQuotes = await ScalarIntAsync(conn,
            "SELECT COUNT(*) FROM quotes WHERE created_at >= CURDATE() AND created_at < CURDATE() + INTERVAL 1 DAY", null);
        int totalQuotes = await ScalarIntAsync(conn, "SELECT COUNT(*) FROM quotes WHERE is_deleted = 0", null);

        int myQuotes = 0;
        int myFavorites = 0;
        if (session.UserId is int uid)
        {
            myQuotes = await ScalarIntAsync(conn, "SELECT COUNT(*) FROM quotes WHERE user_id = @uid AND is_deleted = 0", uid);
            myFavorites = await ScalarIntAsync(conn, "SELECT COUNT(*) FROM favorites WHERE user_id = @uid", uid);
        }

        return new MainSummaryResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            TodayQuotes = todayQuotes,
            TotalQuotes = totalQuotes,
            MyQuotes = myQuotes,
            MyFavorites = myFavorites
        };
    }

    private static async Task<List<FavoriteTopCarDto>> QueryFavoriteTop5Async(MySqlConnection conn, string originType)
    {
        await using var cmd = new MySqlCommand($@"
            SELECT {CarSummaryColumns}, COUNT(*) AS favorite_count
            FROM favorites f
            JOIN cars c ON c.car_id = f.car_id
            JOIN brands b ON b.brand_id = c.brand_id
            WHERE f.created_at >= (CURDATE() - INTERVAL 6 DAY) AND f.created_at < CURDATE() + INTERVAL 1 DAY
              AND c.is_active = 1 AND b.origin_type = @originType
            GROUP BY c.car_id
            ORDER BY favorite_count DESC
            LIMIT 5", conn);
        cmd.Parameters.AddWithValue("@originType", originType);
        await using var reader = await cmd.ExecuteReaderAsync();

        var result = new List<FavoriteTopCarDto>();
        while (await reader.ReadAsync())
        {
            var summary = ReadCarSummary(reader);
            result.Add(new FavoriteTopCarDto
            {
                CarId = summary.CarId,
                CarName = summary.CarName,
                BrandName = summary.BrandName,
                ImageUrl = summary.ImageUrl,
                CarPrice = summary.CarPrice,
                OptionTotal = summary.OptionTotal,
                FuelMin = summary.FuelMin,
                FuelMax = summary.FuelMax,
                ReleaseDate = summary.ReleaseDate,
                FavoriteCount = reader.GetInt32("favorite_count")
            });
        }

        return result;
    }

    public static async Task<(ImageGetResponse Response, byte[]? Blob)> ImageGetAsync(string json)
    {
        var req = JsonSerializer.Deserialize<ImageGetRequest>(json)!;

        string baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "images")) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(baseDir, req.ImageUrl));

        if (!fullPath.StartsWith(baseDir, StringComparison.Ordinal) || !File.Exists(fullPath))
        {
            return (new ImageGetResponse
            {
                Type = req.Type,
                ReqId = req.ReqId,
                Result = "ERROR",
                Code = ResultCode.NotFound,
                Message = "이미지를 찾을 수 없습니다."
            }, null);
        }

        byte[] blob = await File.ReadAllBytesAsync(fullPath);
        string contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        return (new ImageGetResponse
        {
            Type = req.Type,
            ReqId = req.ReqId,
            Result = "OK",
            Code = ResultCode.Ok,
            ContentType = contentType
        }, blob);
    }

    private static async Task<int> ScalarIntAsync(MySqlConnection conn, string sql, int? userId)
    {
        await using var cmd = new MySqlCommand(sql, conn);
        if (userId is int uid)
        {
            cmd.Parameters.AddWithValue("@uid", uid);
        }

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static CarSummaryDto ReadCarSummary(MySqlDataReader reader)
    {
        return new CarSummaryDto
        {
            CarId = reader.GetInt32("car_id"),
            CarName = reader.GetString("car_name"),
            BrandName = reader.GetString("brand_name"),
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString("image_url"),
            CarPrice = reader.GetInt32("car_price"),
            OptionTotal = Convert.ToInt64(reader["option_total"]),
            FuelMin = reader.IsDBNull(reader.GetOrdinal("fuel_min")) ? null : (double?)reader.GetDecimal("fuel_min"),
            FuelMax = reader.IsDBNull(reader.GetOrdinal("fuel_max")) ? null : (double?)reader.GetDecimal("fuel_max"),
            ReleaseDate = reader.IsDBNull(reader.GetOrdinal("release_date"))
                ? null
                : reader.GetDateTime("release_date").ToString("yyyy-MM-dd")
        };
    }
}
