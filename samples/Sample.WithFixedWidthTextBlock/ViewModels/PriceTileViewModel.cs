using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sample.WithFixedWidthTextBlock.ViewModels;

public class PriceTileViewModel : INotifyPropertyChanged
{
    private string _price = string.Empty;

    public required string Pair { get; init; }

    public string Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
