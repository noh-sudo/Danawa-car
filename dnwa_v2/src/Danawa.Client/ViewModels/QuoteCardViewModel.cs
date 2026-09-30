using System.Windows.Input;
using System.Windows.Media.Imaging;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>07 견적함의 카드 1개, 견적 1건 = 카드 1개, 옵션은 이 카드 안에 나열 (QUO-06)</summary>
public sealed class QuoteCardViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly Action<QuoteCardViewModel> _onDeleted;

    public QuoteDto Quote { get; }

    public string CarPriceText => $"{Quote.CarPrice / 10_000:N0} 만원";
    public string TotalPriceText => $"{Quote.TotalPrice / 10_000:N0} 만원";

    /// <summary>"2026-09-22 14:56:26" → "저장 2026-09-22 14:56"</summary>
    public string CreatedAtText => $"저장 {Quote.CreatedAt[..Math.Min(16, Quote.CreatedAt.Length)]}";

    /// <summary>카드 안 옵션 줄: 옵션명 / "+ 90 만원" (스냅샷 값, QUO-05)</summary>
    public IReadOnlyList<QuoteOptionLine> OptionLines { get; }

    private BitmapImage? _image;
    public BitmapImage? Image
    {
        get => _image;
        private set => SetProperty(ref _image, value);
    }


    public ICommand DeleteCommand { get; }

    public QuoteCardViewModel(QuoteDto quote, ServerConnection connection, Action<QuoteCardViewModel> onDeleted)
    {
        Quote = quote;
        _connection = connection;
        _onDeleted = onDeleted;
        OptionLines = quote.Options.Select(o => new QuoteOptionLine(o.OptionName, $"+ {o.Price / 10_000:N0} 만원")).ToList();
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !IsBusy);
    }

    public async Task LoadImageAsync()
    {
        Image = await ImageLoader.LoadAsync(_connection, Quote.ImageUrl);
    }

    private async Task DeleteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync(
                new QuoteDeleteRequest { Type = RequestType.QuoteDelete, QuoteId = Quote.QuoteId });

            if (resp.Result == "OK")
            {
                _onDeleted(this);
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

public sealed record QuoteOptionLine(string Name, string PriceText);
