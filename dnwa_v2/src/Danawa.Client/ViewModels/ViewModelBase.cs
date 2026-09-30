using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Danawa.Client.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isBusy;

    /// <summary>서버 응답 대기 중 — 화면은 이 값으로 로딩 표시, 명령은 canExecute에서 !IsBusy로 중복 클릭 방지</summary>
    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>비로그인 상태에서 로그인이 필요한 동작을 눌러 로그인 화면을 거친 뒤 이 화면으로 돌아왔을 때 호출 (USR-09) — 로그인 전에 불러둔 관심 상태 등을 다시 채울 화면만 재정의</summary>
    public virtual void OnReturnedAfterLogin()
    {
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
