using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfFastControls.Controls;

namespace Sample.WithFixedWidthTextBlock;

public partial class MainWindow : Window
{
    private static readonly string[] Pairs =
    [
        "EUR/USD", "GBP/USD", "USD/JPY", "AUD/USD",
        "USD/CAD", "USD/CHF", "NZD/USD", "EUR/GBP",
        "EUR/JPY", "GBP/JPY", "EUR/AUD", "AUD/JPY"
    ];

    private static readonly double[] BasePrices =
    [
        1.08500, 1.26800, 149.500, 0.65200,
        1.36400, 0.89700, 0.60800, 0.85400,
        161.200, 188.500, 1.65800, 97.8000
    ];

    private readonly FixedWidthTextBlock[] _priceBlocks = new FixedWidthTextBlock[12];
    private readonly Random _rng = new();
    private readonly DispatcherTimer _timer;
    private int _updateCount;

    public MainWindow()
    {
        InitializeComponent();
        BuildTiles();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void BuildTiles()
    {
        for (int i = 0; i < 12; i++)
        {
            var pairLabel = new TextBlock
            {
                Text = Pairs[i],
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 2)
            };

            // FixedWidthTextBlock — Text change calls InvalidateVisual() only.
            // MeasureOverride returns the fixed size every time, so no layout pass
            // propagates up the visual tree when the price updates.
            var priceBlock = new FixedWidthTextBlock
            {
                Text = BasePrices[i].ToString("F5"),
                FixedWidth = 140,
                FontSize = 26,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _priceBlocks[i] = priceBlock;

            var stack = new StackPanel();
            stack.Children.Add(pairLabel);
            stack.Children.Add(priceBlock);

            TileGrid.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5A)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44)),
                Child = stack,
                Margin = new Thickness(4)
            });
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        for (int i = 0; i < 12; i++)
        {
            double spread = (_rng.NextDouble() - 0.5) * 0.002 * BasePrices[i];
            _priceBlocks[i].Text = (BasePrices[i] + spread).ToString("F5");
        }
        _updateCount++;
        StatusText.Text = $"Control: FixedWidthTextBlock  |  Updates: {_updateCount}  |  Rate: 10/sec";
    }
}
