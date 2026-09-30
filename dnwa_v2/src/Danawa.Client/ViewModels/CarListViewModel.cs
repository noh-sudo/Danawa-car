using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>04 차량 목록 · 검색 결과, 브랜드 클릭 / GNB 필터(전체·국산·수입) / 검색이 전부 이 화면 사용(진입 경로만 다름) — 정렬은 서버가 처리(IF-CAR-002/003 sort), 기본은 인기순(최근 7일 관심 수)</summary>
public sealed class CarListViewModel : ViewModelBase
{
    private enum Mode { All, Brand, Origin, Search }

    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;
    private readonly Mode _mode;
    private readonly int? _brandId;
    private readonly string? _originType;
    private readonly string? _keyword;

    public string Title { get; }
    public ObservableCollection<CarCardViewModel> Cars { get; } = new();

    private bool _hasNoResults;
    public bool HasNoResults
    {
        get => _hasNoResults;
        private set => SetProperty(ref _hasNoResults, value);
    }

    private string _sort = "POPULAR";
    public bool IsPopularSort => _sort == "POPULAR";
    public bool IsPriceSort => _sort == "PRICE_ASC";

    public ICommand SelectCarCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SortPopularCommand { get; }
    public ICommand SortPriceCommand { get; }

    private CarListViewModel(
        ServerConnection connection, ShellViewModel shell, Mode mode, string title,
        int? brandId, string? originType, string? keyword)
    {
        _connection = connection;
        _shell = shell;
        _mode = mode;
        Title = title;
        _brandId = brandId;
        _originType = originType;
        _keyword = keyword;

        SelectCarCommand = new RelayCommand(p =>
            _shell.NavigateTo(new CarDetailViewModel(_connection, _shell, ((CarCardViewModel)p!).Car.CarId)));
        ToggleFavoriteCommand = new RelayCommand(
            async p => await ((CarCardViewModel)p!).ToggleFavoriteAsync(_connection, _shell, this),
            p => p is CarCardViewModel { IsBusy: false });
        SortPopularCommand = new RelayCommand(async _ => await ChangeSortAsync("POPULAR"), _ => !IsBusy);
        SortPriceCommand = new RelayCommand(async _ => await ChangeSortAsync("PRICE_ASC"), _ => !IsBusy);

        _ = LoadAsync();
    }

    public static CarListViewModel ForAll(ServerConnection connection, ShellViewModel shell) =>
        new(connection, shell, Mode.All, "전체차량", null, null, null);

    public static CarListViewModel ForOrigin(ServerConnection connection, ShellViewModel shell, string originType, string title) =>
        new(connection, shell, Mode.Origin, title, null, originType, null);

    public static CarListViewModel ForBrand(ServerConnection connection, ShellViewModel shell, int brandId, string brandName) =>
        new(connection, shell, Mode.Brand, brandName, brandId, null, null);

    public static CarListViewModel ForSearch(ServerConnection connection, ShellViewModel shell, string keyword) =>
        new(connection, shell, Mode.Search, $"'{keyword}' 검색 결과", null, null, keyword);

    public override void OnReturnedAfterLogin()
    {
        _ = CarCardViewModel.MarkFavoritesAsync(_connection, _shell, Cars);
    }

    private async Task ChangeSortAsync(string sort)
    {
        if (IsBusy || _sort == sort)
        {
            return;
        }

        _sort = sort;
        OnPropertyChanged(nameof(IsPopularSort));
        OnPropertyChanged(nameof(IsPriceSort));
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            CarListResponse resp = _mode == Mode.Search
                ? await _connection.SendAsync<CarListResponse>(
                    new CarSearchRequest { Type = RequestType.CarSearch, Keyword = _keyword!, Sort = _sort })
                : await _connection.SendAsync<CarListResponse>(
                    new CarListRequest { Type = RequestType.CarList, BrandId = _brandId, OriginType = _originType, Sort = _sort });

            Cars.Clear();
            foreach (CarSummaryDto car in resp.Cars)
            {
                var card = new CarCardViewModel(car);
                Cars.Add(card);
                _ = card.LoadImageAsync(_connection);
            }

            HasNoResults = resp.Result == "OK" && Cars.Count == 0;
            await CarCardViewModel.MarkFavoritesAsync(_connection, _shell, Cars);
        }
        catch
        {
            // 연결 끊김 등: 헤더의 연결 표시등이 안내하므로 여기서는 빈 목록 유지
        }
        finally
        {
            IsBusy = false;
        }
    }
}
