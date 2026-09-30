using System.Windows.Media.Imaging;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>01/04/05 화면의 차량 카드가 공유하는 표시용 래퍼, 가격 범위 표기(기본가~기본가+옵션합)와 이미지 지연 로딩(image_get) 담당 — car_list/car_search/main_daily_top/main_favorite_top5가 전부 이 모양 사용, 8단계: 카드 하트(관심 상태)도 여기서 보유, 상태는 favorite_list로 대조(목록 API에 필드 추가 안 함)</summary>
public sealed class CarCardViewModel : ViewModelBase
{
    public CarSummaryDto Car { get; }

    private BitmapImage? _image;
    public BitmapImage? Image
    {
        get => _image;
        private set => SetProperty(ref _image, value);
    }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        private set => SetProperty(ref _isFavorite, value);
    }

    /// <summary>같은 차량을 보여주는 다른 카드의 하트 상태를 따라감 — 메인 화면은 오늘의 인기 차량과 TOP5에 같은 차가 서로 다른 카드로 나올 수 있어, 한쪽을 누르면 나머지를 이것으로 맞춤</summary>
    public void SyncFavoriteFrom(CarCardViewModel source)
    {
        IsFavorite = source.IsFavorite;
    }

    /// <summary>메인 TOP5의 순위(1~5). 다른 화면에서는 0.</summary>
    public int Rank { get; init; }

    public int FavoriteCount => (Car as FavoriteTopCarDto)?.FavoriteCount ?? 0;
    public int QuoteCount => (Car as DailyTopCarDto)?.QuoteCount ?? 0;

    public string PriceRangeText => FormatPriceRange(Car.CarPrice, Car.OptionTotal);

    public string FuelText => Car.FuelMin is double min && Car.FuelMax is double max
        ? $"{min:0.0} ~ {max:0.0} km/L"
        : "-";

    /// <summary>"2025-03-10" → "2025.03 출시"</summary>
    public string ReleaseText => Car.ReleaseDate is { Length: >= 7 } d ? $"{d[..4]}.{d[5..7]} 출시" : string.Empty;

    public CarCardViewModel(CarSummaryDto car)
    {
        Car = car;
    }

    public async Task LoadImageAsync(ServerConnection connection)
    {
        Image = await ImageLoader.LoadAsync(connection, Car.ImageUrl);
    }

    /// <summary>카드 하트 클릭, 비로그인이면 로그인 화면을 거쳐 returning 화면으로 복귀 (USR-09)</summary>
    public async Task ToggleFavoriteAsync(ServerConnection connection, ShellViewModel shell, ViewModelBase returning)
    {
        if (IsBusy)
        {
            return;
        }

        if (!shell.IsLoggedIn)
        {
            shell.RequireLogin(returning);
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await connection.SendAsync<FavoriteSetResponse>(
                new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = Car.CarId, On = !IsFavorite });

            if (resp.Result == "OK")
            {
                IsFavorite = resp.IsFavorite;
            }
        }
        catch
        {
            // 연결 끊김 등: 헤더의 연결 표시등이 안내, 하트는 그대로 유지
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>로그인 상태면 favorite_list를 한 번 불러 카드들의 하트를 채운다. 비로그인이면 전부 빈 하트.</summary>
    public static async Task MarkFavoritesAsync(ServerConnection connection, ShellViewModel shell, IEnumerable<CarCardViewModel> cards)
    {
        HashSet<int> ids = new();
        if (shell.IsLoggedIn)
        {
            var resp = await connection.SendAsync<FavoriteListResponse>(new RequestBase { Type = RequestType.FavoriteList });
            if (resp.Result == "OK")
            {
                ids = resp.Favorites.Select(f => f.CarId).ToHashSet();
            }
        }

        foreach (CarCardViewModel card in cards)
        {
            card.IsFavorite = ids.Contains(card.Car.CarId);
        }
    }

    public static string FormatPriceRange(int carPrice, long optionTotal)
    {
        long min = carPrice / 10_000;
        long max = (carPrice + optionTotal) / 10_000;
        return min == max ? $"{min:N0} 만원" : $"{min:N0} ~ {max:N0} 만원";
    }
}
