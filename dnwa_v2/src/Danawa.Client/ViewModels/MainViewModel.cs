using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>01 메인 화면.</summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;

    public ObservableCollection<BrandCardViewModel> Brands { get; } = new();
    public ObservableCollection<CarCardViewModel> FavoriteDomestic { get; } = new();
    public ObservableCollection<CarCardViewModel> FavoriteImported { get; } = new();

    private CarCardViewModel? _dailyTop;
    public CarCardViewModel? DailyTop
    {
        get => _dailyTop;
        private set
        {
            if (SetProperty(ref _dailyTop, value))
            {
                OnPropertyChanged(nameof(HasDailyTop));
            }
        }
    }

    public bool HasDailyTop => DailyTop is not null;

    private int _dailyTopQuoteCount;
    public int DailyTopQuoteCount
    {
        get => _dailyTopQuoteCount;
        private set => SetProperty(ref _dailyTopQuoteCount, value);
    }

    private int _todayQuotes;
    public int TodayQuotes { get => _todayQuotes; private set => SetProperty(ref _todayQuotes, value); }

    private int _totalQuotes;
    public int TotalQuotes { get => _totalQuotes; private set => SetProperty(ref _totalQuotes, value); }

    private int _myQuotes;
    public int MyQuotes { get => _myQuotes; private set => SetProperty(ref _myQuotes, value); }

    private int _myFavorites;
    public int MyFavorites { get => _myFavorites; private set => SetProperty(ref _myFavorites, value); }

    public bool ShowMyStats => _shell.IsLoggedIn;

    public ICommand SelectBrandCommand { get; }
    public ICommand SelectCarCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    public MainViewModel(ServerConnection connection, ShellViewModel shell)
    {
        _connection = connection;
        _shell = shell;
        SelectBrandCommand = new RelayCommand(p => OnSelectBrand((BrandCardViewModel)p!));
        SelectCarCommand = new RelayCommand(p =>
            _shell.NavigateTo(new CarDetailViewModel(_connection, _shell, ((CarCardViewModel)p!).Car.CarId)));
        ToggleFavoriteCommand = new RelayCommand(
            async p => await ToggleFavoriteAsync((CarCardViewModel)p!),
            p => p is CarCardViewModel { IsBusy: false });
        _ = LoadAsync();
    }

    /// <summary>오늘의 인기 차량과 국산/수입 TOP5에 같은 차가 다른 카드로 나올 수 있음 — 누른 카드의 결과를 같은 car_id 카드 전부에 맞춤</summary>
    private async Task ToggleFavoriteAsync(CarCardViewModel card)
    {
        await card.ToggleFavoriteAsync(_connection, _shell, this);

        IEnumerable<CarCardViewModel> all = FavoriteDomestic.Concat(FavoriteImported);
        if (DailyTop is not null)
        {
            all = all.Append(DailyTop);
        }

        foreach (CarCardViewModel other in all)
        {
            if (other != card && other.Car.CarId == card.Car.CarId)
            {
                other.SyncFavoriteFrom(card);
            }
        }
    }

    public override void OnReturnedAfterLogin()
    {
        // 로그인 후 돌아오면 내 견적함·관심 건수와 하트를 다시 채움
        OnPropertyChanged(nameof(ShowMyStats));
        _ = LoadAsync();
    }

    private void OnSelectBrand(BrandCardViewModel brand)
    {
        _shell.NavigateTo(CarListViewModel.ForBrand(_connection, _shell, brand.Brand.BrandId, brand.Brand.BrandName));
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await LoadCoreAsync();
        }
        catch
        {
            // 연결이 끊긴 상태 등: 헤더의 연결 표시등이 이미 안내하므로 여기서는 빈 화면 유지
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        Task<BrandListResponse> brandsTask = _connection.SendAsync<BrandListResponse>(
            new RequestBase { Type = RequestType.BrandList });
        Task<MainSummaryResponse> summaryTask = _connection.SendAsync<MainSummaryResponse>(
            new RequestBase { Type = RequestType.MainSummary });
        Task<MainDailyTopResponse> dailyTask = _connection.SendAsync<MainDailyTopResponse>(
            new RequestBase { Type = RequestType.MainDailyTop });
        Task<MainFavoriteTop5Response> favTask = _connection.SendAsync<MainFavoriteTop5Response>(
            new RequestBase { Type = RequestType.MainFavoriteTop5 });

        await Task.WhenAll(brandsTask, summaryTask, dailyTask, favTask);

        Brands.Clear();
        foreach (BrandDto b in brandsTask.Result.Brands)
        {
            var card = new BrandCardViewModel(b);
            Brands.Add(card);
            _ = card.LoadIconAsync(_connection);
        }

        MainSummaryResponse summary = summaryTask.Result;
        TodayQuotes = summary.TodayQuotes;
        TotalQuotes = summary.TotalQuotes;
        MyQuotes = summary.MyQuotes;
        MyFavorites = summary.MyFavorites;

        DailyTopCarDto? daily = dailyTask.Result.Car;
        if (daily is not null)
        {
            var card = new CarCardViewModel(daily);
            DailyTop = card;
            DailyTopQuoteCount = daily.QuoteCount;
            _ = card.LoadImageAsync(_connection);
        }

        MainFavoriteTop5Response fav = favTask.Result;
        FavoriteDomestic.Clear();
        foreach (FavoriteTopCarDto c in fav.Domestic)
        {
            var card = new CarCardViewModel(c) { Rank = FavoriteDomestic.Count + 1 };
            FavoriteDomestic.Add(card);
            _ = card.LoadImageAsync(_connection);
        }

        FavoriteImported.Clear();
        foreach (FavoriteTopCarDto c in fav.Imported)
        {
            var card = new CarCardViewModel(c) { Rank = FavoriteImported.Count + 1 };
            FavoriteImported.Add(card);
            _ = card.LoadImageAsync(_connection);
        }

        IEnumerable<CarCardViewModel> hearts = FavoriteDomestic.Concat(FavoriteImported);
        if (DailyTop is not null)
        {
            hearts = hearts.Append(DailyTop);
        }
        await CarCardViewModel.MarkFavoritesAsync(_connection, _shell, hearts.ToList());
    }
}
