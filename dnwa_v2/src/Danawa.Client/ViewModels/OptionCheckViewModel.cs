using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

public sealed class OptionCheckViewModel : ViewModelBase
{
    public CarOptionDto Option { get; }

    public string PriceText => $"{Option.Price / 10_000:N0} 만원";

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public OptionCheckViewModel(CarOptionDto option)
    {
        Option = option;
    }
}
