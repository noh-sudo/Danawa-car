using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>05 차량 상세, 옵션 체크는 화면 표시용 합계일 뿐 저장되는 금액 아님(R4) — [견적함에 담기]는 car_id/option_ids만 서버에 전송, 서버가 다시 계산한 total_price를 응답으로 수신</summary>
public sealed class CarDetailViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;
    private readonly int _carId;

    private CarCardViewModel? _car;
    public CarCardViewModel? Car
    {
        get => _car;
        private set
        {
            if (SetProperty(ref _car, value))
            {
                OnPropertyChanged(nameof(Breadcrumb));
                OnPropertyChanged(nameof(BasePriceText));
                OnPropertyChanged(nameof(ReleaseDateText));
            }
        }
    }

    public string Breadcrumb => Car is null ? string.Empty : $"{_shell.ActiveTabLabel}  ›  {Car.Car.BrandName}  ›  {Car.Car.CarName}";
    public string BasePriceText => $"{(Car?.Car.CarPrice ?? 0) / 10_000:N0} 만원";
    public string ReleaseDateText => Car?.Car.ReleaseDate?.Replace('-', '.') ?? "-";
    public string OptionCountText => $"{Options.Count}개 선택 가능";

    public ObservableCollection<OptionCheckViewModel> Options { get; } = new();

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        private set => SetProperty(ref _isFavorite, value);
    }

    private bool _notFound;
    public bool NotFound
    {
        get => _notFound;
        private set => SetProperty(ref _notFound, value);
    }

    private string? _notice;
    public string? Notice
    {
        get => _notice;
        private set => SetProperty(ref _notice, value);
    }


    public long EstimatedTotal => (Car?.Car.CarPrice ?? 0) + Options.Where(o => o.IsChecked).Sum(o => (long)o.Option.Price);
    public string EstimatedTotalText => $"{EstimatedTotal / 10_000:N0} 만원";

    // 예상 견적 패널의 내역 줄 (화면 표시용, R4)
    public int SelectedOptionCount => Options.Count(o => o.IsChecked);
    public string SelectedOptionTotalText => $"+ {Options.Where(o => o.IsChecked).Sum(o => (long)o.Option.Price) / 10_000:N0} 만원";

    public ICommand ToggleFavoriteCommand { get; }
    public ICommand AddToQuoteCommand { get; }

    public CarDetailViewModel(ServerConnection connection, ShellViewModel shell, int carId)
    {
        _connection = connection;
        _shell = shell;
        _carId = carId;
        ToggleFavoriteCommand = new RelayCommand(async _ => await ToggleFavoriteAsync(), _ => !IsBusy);
        AddToQuoteCommand = new RelayCommand(async _ => await AddToQuoteAsync(), _ => !IsBusy);
        _ = LoadAsync();
    }

    public override void OnReturnedAfterLogin()
    {
        // 로그인 전에 받은 is_favorite는 항상 false — 옵션 체크는 그대로 두고 하트만 다시 수신
        _ = RefreshFavoriteAsync();
    }

    private async Task RefreshFavoriteAsync()
    {
        try
        {
            var resp = await _connection.SendAsync<CarDetailResponse>(
                new CarDetailRequest { Type = RequestType.CarDetail, CarId = _carId });
            if (resp.Result == "OK")
            {
                IsFavorite = resp.IsFavorite;
            }
        }
        catch
        {
            // 연결 끊김 등: 헤더의 연결 표시등이 안내
        }
    }

    private async Task LoadAsync()
    {
        CarDetailResponse resp;
        IsBusy = true;
        try
        {
            resp = await _connection.SendAsync<CarDetailResponse>(
                new CarDetailRequest { Type = RequestType.CarDetail, CarId = _carId });
        }
        catch
        {
            // 연결 끊김 등: 헤더의 연결 표시등이 안내하므로 여기서는 NOT_FOUND와 같은 빈 화면 유지
            NotFound = true;
            return;
        }
        finally
        {
            IsBusy = false;
        }

        if (resp.Result != "OK" || resp.Car is null)
        {
            NotFound = true;
            return;
        }

        Car = new CarCardViewModel(resp.Car);
        _ = Car.LoadImageAsync(_connection);
        IsFavorite = resp.IsFavorite;

        Options.Clear();
        foreach (CarOptionDto opt in resp.Options)
        {
            var optionVm = new OptionCheckViewModel(opt);
            optionVm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(OptionCheckViewModel.IsChecked))
                {
                    OnPropertyChanged(nameof(EstimatedTotal));
                    OnPropertyChanged(nameof(EstimatedTotalText));
                    OnPropertyChanged(nameof(SelectedOptionCount));
                    OnPropertyChanged(nameof(SelectedOptionTotalText));
                }
            };
            Options.Add(optionVm);
        }

        OnPropertyChanged(nameof(EstimatedTotal));
        OnPropertyChanged(nameof(EstimatedTotalText));
        OnPropertyChanged(nameof(SelectedOptionCount));
        OnPropertyChanged(nameof(SelectedOptionTotalText));
        OnPropertyChanged(nameof(OptionCountText));
    }

    private async Task ToggleFavoriteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!_shell.IsLoggedIn)
        {
            _shell.RequireLogin(this);
            return;
        }

        Notice = null;
        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<FavoriteSetResponse>(
                new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = _carId, On = !IsFavorite });

            if (resp.Result != "OK")
            {
                Notice = resp.Message;
                return;
            }

            IsFavorite = resp.IsFavorite;
        }
        catch (Exception ex)
        {
            Notice = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddToQuoteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!_shell.IsLoggedIn)
        {
            _shell.RequireLogin(this);
            return;
        }

        Notice = null;
        IsBusy = true;
        try
        {
            List<int> optionIds = Options.Where(o => o.IsChecked).Select(o => o.Option.OptionId).ToList();

            var resp = await _connection.SendAsync<QuoteAddResponse>(new QuoteAddRequest
            {
                Type = RequestType.QuoteAdd,
                CarId = _carId,
                OptionIds = optionIds
            });

            Notice = resp.Result == "OK"
                ? $"견적함에 담았습니다. (총액 {resp.TotalPrice / 10_000:N0} 만원)"
                : resp.Message;
        }
        catch (Exception ex)
        {
            Notice = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
