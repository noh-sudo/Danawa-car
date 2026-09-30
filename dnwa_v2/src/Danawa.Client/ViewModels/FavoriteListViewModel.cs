using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>06 관심 목록, 사용자는 세션에서 꺼냄 — 요청에 아이디를 담지 않음 (R2)</summary>
public sealed class FavoriteListViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;

    public ObservableCollection<FavoriteCardViewModel> Favorites { get; } = new();

    private bool _hasNoResults;
    public bool HasNoResults
    {
        get => _hasNoResults;
        private set => SetProperty(ref _hasNoResults, value);
    }

    public ICommand SelectCarCommand { get; }

    public FavoriteListViewModel(ServerConnection connection, ShellViewModel shell)
    {
        _connection = connection;
        _shell = shell;
        SelectCarCommand = new RelayCommand(p =>
            _shell.NavigateTo(new CarDetailViewModel(_connection, _shell, ((FavoriteCardViewModel)p!).Favorite.CarId)));
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<FavoriteListResponse>(
                new RequestBase { Type = RequestType.FavoriteList });

            Favorites.Clear();
            foreach (FavoriteDto fav in resp.Favorites)
            {
                var card = new FavoriteCardViewModel(fav, _connection, OnUnfavorited);
                Favorites.Add(card);
                _ = card.LoadImageAsync();
            }

            HasNoResults = resp.Result == "OK" && Favorites.Count == 0;
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

    private void OnUnfavorited(FavoriteCardViewModel card)
    {
        Favorites.Remove(card);
        HasNoResults = Favorites.Count == 0;
    }
}
