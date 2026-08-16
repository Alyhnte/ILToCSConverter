using System.ComponentModel;

namespace ILToCSConverter.App.Localization;

public sealed class UiText : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => UiCatalog.Get(key);

    public void Refresh() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}
