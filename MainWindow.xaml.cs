using PUBGFlagChanger.ViewModels;
using System.Windows;

namespace PUBGFlagChanger;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext =
            new MainViewModel();
    }
}