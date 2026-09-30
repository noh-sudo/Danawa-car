using System.Collections.ObjectModel;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>07 견적함, 사용자는 세션에서 꺼냄 — 요청에 아이디를 담지 않음 (R2)</summary>
public sealed class QuoteListViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    public ObservableCollection<QuoteCardViewModel> Quotes { get; } = new();

    private bool _hasNoResults;
    public bool HasNoResults
    {
        get => _hasNoResults;
        private set => SetProperty(ref _hasNoResults, value);
    }

    public QuoteListViewModel(ServerConnection connection)
    {
        _connection = connection;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<QuoteListResponse>(new RequestBase { Type = RequestType.QuoteList });

            Quotes.Clear();
            foreach (QuoteDto q in resp.Quotes)
            {
                var card = new QuoteCardViewModel(q, _connection, OnDeleted);
                Quotes.Add(card);
                _ = card.LoadImageAsync();
            }

            HasNoResults = resp.Result == "OK" && Quotes.Count == 0;
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

    private void OnDeleted(QuoteCardViewModel card)
    {
        Quotes.Remove(card);
        HasNoResults = Quotes.Count == 0;
    }
}
