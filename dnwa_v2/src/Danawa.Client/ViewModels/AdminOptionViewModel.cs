using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>10 관리자 — 옵션 관리, 차량을 먼저 고르면 그 차량의 옵션만 조회</summary>
public sealed class AdminOptionViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    public ObservableCollection<AdminCarDto> Cars { get; } = new();
    public ObservableCollection<AdminOptionDto> Options { get; } = new();

    private AdminCarDto? _selectedCar;
    public AdminCarDto? SelectedCar
    {
        get => _selectedCar;
        set
        {
            if (SetProperty(ref _selectedCar, value))
            {
                ClearOptionForm();
                _ = LoadOptionsAsync();
            }
        }
    }

    private int? _optionFormOptionId;
    public string OptionFormTitle => _optionFormOptionId is int id ? $"옵션 수정  (option_id = {id})" : "옵션 등록";
    private string _optionFormName = string.Empty;
    public string OptionFormName { get => _optionFormName; set => SetProperty(ref _optionFormName, value); }

    private string _optionFormPrice = string.Empty;
    public string OptionFormPrice { get => _optionFormPrice; set => SetProperty(ref _optionFormPrice, value); }

    private bool _optionFormIsActive = true;
    public bool OptionFormIsActive { get => _optionFormIsActive; set => SetProperty(ref _optionFormIsActive, value); }

    private string? _optionFormError;
    public string? OptionFormError { get => _optionFormError; set => SetProperty(ref _optionFormError, value); }

    private AdminOptionDto? _selectedOptionForList;
    public AdminOptionDto? SelectedOptionForList
    {
        get => _selectedOptionForList;
        set
        {
            SetProperty(ref _selectedOptionForList, value);
            if (value is not null)
            {
                LoadOptionForm(value);
            }
        }
    }

    public ICommand NewOptionCommand { get; }
    public ICommand SaveOptionCommand { get; }
    public ICommand DeleteOptionCommand { get; }

    public AdminOptionViewModel(ServerConnection connection)
    {
        _connection = connection;
        NewOptionCommand = new RelayCommand(_ => ClearOptionForm());
        // 요청 중에는 버튼 비활성화 (중복 클릭 방지)
        SaveOptionCommand = new RelayCommand(async _ => await SaveOptionAsync(), _ => !IsBusy);
        DeleteOptionCommand = new RelayCommand(async _ => await DeleteOptionAsync(), _ => !IsBusy && _optionFormOptionId is not null);

        _ = LoadCarsAsync();
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

    private async Task LoadOptionsAsync()
    {
        Options.Clear();
        if (SelectedCar is null)
        {
            return;
        }

        try
        {
            var resp = await _connection.SendAsync<AdminOptionListResponse>(
                new AdminOptionListRequest { Type = RequestType.AdminOptionList, CarId = SelectedCar.CarId });
            foreach (var o in resp.Options)
            {
                Options.Add(o);
            }
        }
        catch
        {
            // 연결 끊김 등: 빈 목록 유지
        }
    }

    private void LoadOptionForm(AdminOptionDto option)
    {
        _optionFormOptionId = option.OptionId;
        OnPropertyChanged(nameof(OptionFormTitle));
        RelayCommand.RaiseCanExecuteChanged();
        OptionFormName = option.OptionName;
        OptionFormPrice = option.Price.ToString(CultureInfo.InvariantCulture);
        OptionFormIsActive = option.IsActive;
        OptionFormError = null;
    }

    private void ClearOptionForm()
    {
        _optionFormOptionId = null;
        OnPropertyChanged(nameof(OptionFormTitle));
        RelayCommand.RaiseCanExecuteChanged();
        SelectedOptionForList = null;
        OptionFormName = string.Empty;
        OptionFormPrice = string.Empty;
        OptionFormIsActive = true;
        OptionFormError = null;
    }

    private async Task SaveOptionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        OptionFormError = null;

        if (SelectedCar is null)
        {
            OptionFormError = "차량을 먼저 선택하세요.";
            return;
        }

        if (!int.TryParse(OptionFormPrice, out int price))
        {
            OptionFormError = "가격을 숫자로 입력하세요.";
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<AdminOptionSaveResponse>(new AdminOptionSaveRequest
            {
                Type = RequestType.AdminOptionSave,
                OptionId = _optionFormOptionId,
                CarId = SelectedCar.CarId,
                OptionName = OptionFormName,
                Price = price,
                IsActive = OptionFormIsActive
            });

            if (resp.Result != "OK")
            {
                OptionFormError = resp.Message;
                return;
            }

            await LoadOptionsAsync();
        }
        catch (Exception ex)
        {
            OptionFormError = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>폼에 채워진 옵션을 소프트 삭제(is_active=0), 저장된 견적은 스냅샷이라 영향 없음 (QUO-05)</summary>
    private async Task DeleteOptionAsync()
    {
        if (IsBusy || _optionFormOptionId is not int optionId)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _connection.SendAsync(new AdminOptionDeleteRequest { Type = RequestType.AdminOptionDelete, OptionId = optionId });
            await LoadOptionsAsync();
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
}
