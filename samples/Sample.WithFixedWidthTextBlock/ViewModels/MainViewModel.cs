using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Sample.WithFixedWidthTextBlock.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private static readonly (string Pair, double BasePrice)[] Data =
    [
        ("EUR/USD", 1.08500), ("GBP/USD", 1.26800), ("USD/JPY", 149.500), ("AUD/USD", 0.65200),
        ("USD/CAD", 1.36400), ("USD/CHF", 0.89700), ("NZD/USD", 0.60800), ("EUR/GBP", 0.85400),
        ("EUR/JPY", 161.200), ("GBP/JPY", 188.500), ("EUR/AUD", 1.65800), ("AUD/JPY", 97.8000)
    ];

    private string _statusText = "Updates: 0";
    private readonly Random _rng = new();
    private int _updateCount;

    public ObservableCollection<PriceTileViewModel> Tiles { get; } = new(
        Data.Select(d => new PriceTileViewModel { Pair = d.Pair, Price = d.BasePrice.ToString("F5") }));

    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += OnTick;
        timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        for (int i = 0; i < Tiles.Count; i++)
        {
            double basePrice = Data[i].BasePrice;
            double spread = (_rng.NextDouble() - 0.5) * 0.002 * basePrice;
            Tiles[i].Price = (basePrice + spread).ToString("F5");
        }
        _updateCount++;
        StatusText = $"Control: FixedWidthTextBlock  |  Updates: {_updateCount}  |  Rate: 10/sec";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
