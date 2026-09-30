using System.Windows.Input;
using System.Windows.Media.Imaging;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>06 관심 목록의 카드 1개, 해제하면 목록에서 즉시 사라짐 (FAV-04)</summary>
public sealed class FavoriteCardViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly Action<FavoriteCardViewModel> _onUnfavorited;

    public FavoriteDto Favorite { get; }

    public string PriceText => CarCardViewModel.FormatPriceRange(Favorite.CarPrice, Favorite.OptionTotal);

    /// <summary>"2026-09-22 14:10:26" → "등록 2026-09-22 14:10"</summary>
    public string CreatedAtText => $"등록 {Favorite.CreatedAt[..Math.Min(16, Favorite.CreatedAt.Length)]}";

    private BitmapImage? _image;
    public BitmapImage? Image
    {
        get => _image;
        private set => SetProperty(ref _image, value);
    }


    public ICommand UnfavoriteCommand { get; }

    public FavoriteCardViewModel(FavoriteDto favorite, ServerConnection connection, Action<FavoriteCardViewModel> onUnfavorited)
    {
        Favorite = favorite;
        _connection = connection;
        _onUnfavorited = onUnfavorited;
        UnfavoriteCommand = new RelayCommand(async _ => await UnfavoriteAsync(), _ => !IsBusy);
    }

    public async Task LoadImageAsync()
    {
        Image = await ImageLoader.LoadAsync(_connection, Favorite.ImageUrl);
    }

    private async Task UnfavoriteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<FavoriteSetResponse>(
                new FavoriteSetRequest { Type = RequestType.FavoriteSet, CarId = Favorite.CarId, On = false });

            if (resp.Result == "OK" && !resp.IsFavorite)
            {
                _onUnfavorited(this);
            }
        }
        catch
        {
            // 연결 끊김 등: 헤더의 연결 표시등이 안내, 카드는 그대로 유지
        }
        finally
        {
            IsBusy = false;
        }
    }
}
