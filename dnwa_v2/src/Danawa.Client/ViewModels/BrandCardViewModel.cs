using System.Windows.Media.Imaging;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

public sealed class BrandCardViewModel : ViewModelBase
{
    public BrandDto Brand { get; }

    private BitmapImage? _icon;
    public BitmapImage? Icon
    {
        get => _icon;
        private set => SetProperty(ref _icon, value);
    }

    public BrandCardViewModel(BrandDto brand)
    {
        Brand = brand;
    }

    public async Task LoadIconAsync(ServerConnection connection)
    {
        Icon = await ImageLoader.LoadAsync(connection, Brand.BrandIcon);
    }
}
