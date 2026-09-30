namespace Danawa.Shared.Contracts;

/// <summary>docs/03_API명세.md 2절 "인터페이스 목록"의 type 값, 총 29개</summary>
public static class RequestType
{
    // 통신 계층 검증용(3단계 전용, API 명세에 없음)
    public const string Ping = "ping";

    // IF-AUTH-001 ~ 004
    public const string CheckId = "check_id";
    public const string Signup = "signup";
    public const string Login = "login";
    public const string Logout = "logout";

    // IF-CAR-001 ~ 008
    public const string BrandList = "brand_list";
    public const string CarList = "car_list";
    public const string CarSearch = "car_search";
    public const string CarDetail = "car_detail";
    public const string MainDailyTop = "main_daily_top";
    public const string MainFavoriteTop5 = "main_favorite_top5";
    public const string ImageGet = "image_get";
    public const string MainSummary = "main_summary";

    // IF-FAV-001 ~ 002
    public const string FavoriteSet = "favorite_set";
    public const string FavoriteList = "favorite_list";

    // IF-QUO-001 ~ 003
    public const string QuoteAdd = "quote_add";
    public const string QuoteList = "quote_list";
    public const string QuoteDelete = "quote_delete";

    // IF-ADM-001 ~ 012
    public const string AdminBrandList = "admin_brand_list";
    public const string AdminBrandSave = "admin_brand_save";
    public const string AdminCarList = "admin_car_list";
    public const string AdminCarSave = "admin_car_save";
    public const string AdminCarDelete = "admin_car_delete";
    public const string AdminOptionList = "admin_option_list";
    public const string AdminOptionSave = "admin_option_save";
    public const string AdminOptionDelete = "admin_option_delete";
    public const string AdminStatsSummary = "admin_stats_summary";
    public const string AdminStatsWeeklyTop5 = "admin_stats_weekly_top5";
    public const string AdminStatsDaily = "admin_stats_daily";
    public const string AdminLogList = "admin_log_list";
}
