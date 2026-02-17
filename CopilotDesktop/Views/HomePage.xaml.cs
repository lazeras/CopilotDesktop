using CopilotDesktop.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace CopilotDesktop.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel
    {
        get;
    }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }
}
