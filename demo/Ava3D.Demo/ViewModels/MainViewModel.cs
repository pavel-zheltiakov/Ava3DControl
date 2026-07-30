using CommunityToolkit.Mvvm.ComponentModel;

namespace Ava3D.Demo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}
