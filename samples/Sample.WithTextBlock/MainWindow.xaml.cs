using System.Windows;
using Sample.WithTextBlock.ViewModels;

namespace Sample.WithTextBlock;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
