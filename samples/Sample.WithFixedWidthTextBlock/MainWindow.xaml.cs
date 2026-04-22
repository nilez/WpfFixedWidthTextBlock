using System.Windows;
using Sample.WithFixedWidthTextBlock.ViewModels;

namespace Sample.WithFixedWidthTextBlock;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
