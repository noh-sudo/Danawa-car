using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>09 관리자 — 차량·브랜드 관리. 탭 2개, 좌측 목록 + 우측 폼.</summary>
public sealed class AdminCarBrandViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    public ObservableCollection<AdminBrandDto> Brands { get; } = new();
    public ObservableCollection<AdminCarDto> Cars { get; } = new();

    private bool _isCarTab = true;
    public bool IsCarTab
    {
        get => _isCarTab;
        set
        {
            if (SetProperty(ref _isCarTab, value))
            {
                OnPropertyChanged(nameof(IsBrandTab));
            }
        }
    }

    public bool IsBrandTab => !IsCarTab;

    // ---- 차량 폼 ----
    private int? _carFormCarId;
    public string CarFormTitle => _carFormCarId is int id ? $"차량 수정  (car_id = {id})" : "차량 등록";
    private AdminBrandDto? _carFormBrand;
    public AdminBrandDto? CarFormBrand { get => _carFormBrand; set => SetProperty(ref _carFormBrand, value); }

    private string _carFormName = string.Empty;
    public string CarFormName { get => _carFormName; set => SetProperty(ref _carFormName, value); }

    private string _carFormImageUrl = string.Empty;
    public string CarFormImageUrl { get => _carFormImageUrl; set => SetProperty(ref _carFormImageUrl, value); }

    private string _carFormPrice = string.Empty;
    public string CarFormPrice { get => _carFormPrice; set => SetProperty(ref _carFormPrice, value); }

    private string _carFormFuelMin = string.Empty;
    public string CarFormFuelMin { get => _carFormFuelMin; set => SetProperty(ref _carFormFuelMin, value); }

    private string _carFormFuelMax = string.Empty;
    public string CarFormFuelMax { get => _carFormFuelMax; set => SetProperty(ref _carFormFuelMax, value); }

    private string _carFormReleaseDate = string.Empty;
    public string CarFormReleaseDate { get => _carFormReleaseDate; set => SetProperty(ref _carFormReleaseDate, value); }

    private string? _carFormError;
    public string? CarFormError { get => _carFormError; set => SetProperty(ref _carFormError, value); }

    // ---- 브랜드 폼 ----
    private int? _brandFormBrandId;
    public string BrandFormTitle => _brandFormBrandId is int id ? $"브랜드 수정  (brand_id = {id})" : "브랜드 등록";

    private string _brandFormName = string.Empty;
    public string BrandFormName { get => _brandFormName; set => SetProperty(ref _brandFormName, value); }

    private string _brandFormIcon = string.Empty;
    public string BrandFormIcon { get => _brandFormIcon; set => SetProperty(ref _brandFormIcon, value); }

    private string _brandFormOriginType = "DOMESTIC";
    public string BrandFormOriginType { get => _brandFormOriginType; set => SetProperty(ref _brandFormOriginType, value); }

    private bool _brandFormIsActive = true;
    public bool BrandFormIsActive { get => _brandFormIsActive; set => SetProperty(ref _brandFormIsActive, value); }

    private string? _brandFormError;
    public string? BrandFormError { get => _brandFormError; set => SetProperty(ref _brandFormError, value); }

    private AdminCarDto? _selectedCarForList;
    public AdminCarDto? SelectedCarForList
    {
        get => _selectedCarForList;
        set
        {
            SetProperty(ref _selectedCarForList, value);
            if (value is not null)
            {
                LoadCarForm(value);
            }
        }
    }

    private AdminBrandDto? _selectedBrandForList;
    public AdminBrandDto? SelectedBrandForList
    {
        get => _selectedBrandForList;
        set
        {
            SetProperty(ref _selectedBrandForList, value);
            if (value is not null)
            {
                LoadBrandForm(value);
            }
        }
    }

    public ICommand SwitchToCarTabCommand { get; }
    public ICommand SwitchToBrandTabCommand { get; }
    public ICommand NewCarCommand { get; }
    public ICommand SaveCarCommand { get; }
    public ICommand DeleteCarCommand { get; }
    public ICommand NewBrandCommand { get; }
    public ICommand SaveBrandCommand { get; }

    public AdminCarBrandViewModel(ServerConnection connection)
    {
        _connection = connection;

        SwitchToCarTabCommand = new RelayCommand(_ => IsCarTab = true);
        SwitchToBrandTabCommand = new RelayCommand(_ => IsCarTab = false);
        NewCarCommand = new RelayCommand(_ => ClearCarForm());
        // 요청 중에는 버튼 비활성화 (중복 클릭 방지)
        SaveCarCommand = new RelayCommand(async _ => await SaveCarAsync(), _ => !IsBusy);
        DeleteCarCommand = new RelayCommand(async _ => await DeleteCarAsync(), _ => !IsBusy && _carFormCarId is not null);
        NewBrandCommand = new RelayCommand(_ => ClearBrandForm());
        SaveBrandCommand = new RelayCommand(async _ => await SaveBrandAsync(), _ => !IsBusy);

        _ = LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        IsBusy = true;
        try
        {
            await LoadBrandsAsync();
            await LoadCarsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBrandsAsync()
    {
        try
        {
            var resp = await _connection.SendAsync<AdminBrandListResponse>(new RequestBase { Type = RequestType.AdminBrandList });
            Brands.Clear();
            foreach (var b in resp.Brands)
            {
                Brands.Add(b);
            }
        }
        catch
        {
            // 연결 끊김 등: 빈 목록 유지
        }
    }

    private async Task LoadCarsAsync()
    {
        try
        {
            var resp = await _connection.SendAsync<AdminCarListResponse>(new RequestBase { Type = RequestType.AdminCarList });
            Cars.Clear();
            foreach (var c in resp.Cars)
            {
                Cars.Add(c);
            }
        }
        catch
        {
            // 연결 끊김 등: 빈 목록 유지
        }
    }

    private void LoadCarForm(AdminCarDto car)
    {
        _carFormCarId = car.CarId;
        OnPropertyChanged(nameof(CarFormTitle));
        RelayCommand.RaiseCanExecuteChanged();
        CarFormBrand = Brands.FirstOrDefault(b => b.BrandId == car.BrandId);
        CarFormName = car.CarName;
        CarFormImageUrl = car.ImageUrl ?? string.Empty;
        CarFormPrice = car.CarPrice.ToString(CultureInfo.InvariantCulture);
        CarFormFuelMin = car.FuelMin?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        CarFormFuelMax = car.FuelMax?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        CarFormReleaseDate = car.ReleaseDate ?? string.Empty;
        CarFormError = null;
    }

    private void ClearCarForm()
    {
        _carFormCarId = null;
        OnPropertyChanged(nameof(CarFormTitle));
        RelayCommand.RaiseCanExecuteChanged();
        SelectedCarForList = null;
        CarFormBrand = null;
        CarFormName = string.Empty;
        CarFormImageUrl = string.Empty;
        CarFormPrice = string.Empty;
        CarFormFuelMin = string.Empty;
        CarFormFuelMax = string.Empty;
        CarFormReleaseDate = string.Empty;
        CarFormError = null;
    }

    private async Task SaveCarAsync()
    {
        if (IsBusy)
        {
            return;
        }

        CarFormError = null;

        if (CarFormBrand is null)
        {
            CarFormError = "브랜드를 선택하세요.";
            return;
        }

        if (!int.TryParse(CarFormPrice, out int price))
        {
            CarFormError = "가격을 숫자로 입력하세요.";
            return;
        }

        double? fuelMin = ParseNullableDouble(CarFormFuelMin);
        double? fuelMax = ParseNullableDouble(CarFormFuelMax);

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<AdminCarSaveResponse>(new AdminCarSaveRequest
            {
                Type = RequestType.AdminCarSave,
                CarId = _carFormCarId,
                BrandId = CarFormBrand.BrandId,
                CarName = CarFormName,
                ImageUrl = string.IsNullOrWhiteSpace(CarFormImageUrl) ? null : CarFormImageUrl,
                CarPrice = price,
                FuelMin = fuelMin,
                FuelMax = fuelMax,
                ReleaseDate = string.IsNullOrWhiteSpace(CarFormReleaseDate) ? null : CarFormReleaseDate
            });

            if (resp.Result != "OK")
            {
                CarFormError = resp.Message;
                return;
            }

            await LoadCarsAsync();
        }
        catch (Exception ex)
        {
            CarFormError = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>폼에 채워진 차량을 소프트 삭제(is_active=0), 목록에는 "삭제됨"으로 표시</summary>
    private async Task DeleteCarAsync()
    {
        if (IsBusy || _carFormCarId is not int carId)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _connection.SendAsync(new AdminCarDeleteRequest { Type = RequestType.AdminCarDelete, CarId = carId });
            await LoadCarsAsync();
        }
        catch
        {
            // 연결 끊김 등: 목록 그대로 유지
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadBrandForm(AdminBrandDto brand)
    {
        _brandFormBrandId = brand.BrandId;
        OnPropertyChanged(nameof(BrandFormTitle));
        BrandFormName = brand.BrandName;
        BrandFormIcon = brand.BrandIcon ?? string.Empty;
        BrandFormOriginType = brand.OriginType;
        BrandFormIsActive = brand.IsActive;
        BrandFormError = null;
    }

    private void ClearBrandForm()
    {
        _brandFormBrandId = null;
        OnPropertyChanged(nameof(BrandFormTitle));
        SelectedBrandForList = null;
        BrandFormName = string.Empty;
        BrandFormIcon = string.Empty;
        BrandFormOriginType = "DOMESTIC";
        BrandFormIsActive = true;
        BrandFormError = null;
    }

    private async Task SaveBrandAsync()
    {
        if (IsBusy)
        {
            return;
        }

        BrandFormError = null;

        if (string.IsNullOrWhiteSpace(BrandFormName))
        {
            BrandFormError = "브랜드명을 입력하세요.";
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<AdminBrandSaveResponse>(new AdminBrandSaveRequest
            {
                Type = RequestType.AdminBrandSave,
                BrandId = _brandFormBrandId,
                BrandName = BrandFormName,
                BrandIcon = string.IsNullOrWhiteSpace(BrandFormIcon) ? null : BrandFormIcon,
                OriginType = BrandFormOriginType,
                IsActive = BrandFormIsActive
            });

            if (resp.Result != "OK")
            {
                BrandFormError = resp.Message;
                return;
            }

            await LoadBrandsAsync();
        }
        catch (Exception ex)
        {
            BrandFormError = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static double? ParseNullableDouble(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;
}
