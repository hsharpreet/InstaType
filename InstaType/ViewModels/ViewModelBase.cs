using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InstaType.ViewModels;

/// <summary>
/// Base class for all ViewModels. Implements <see cref="INotifyPropertyChanged"/>
/// and provides <see cref="SetProperty{T}"/> for concise property setters.
/// No Win32 or infrastructure code belongs here.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Sets <paramref name="field"/> to <paramref name="value"/> and raises PropertyChanged if changed.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>Raises PropertyChanged for <paramref name="propertyName"/>.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
