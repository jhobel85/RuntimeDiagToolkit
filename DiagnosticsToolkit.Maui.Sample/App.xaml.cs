namespace DiagnosticsToolkit.Maui.Sample;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

/* // Alternative constructor injection approach for .NET10+ (already working on android)
    public App(MainPage page)
    {
        InitializeComponent();
        MainPage = new NavigationPage(page);
    }
*/
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = Handler.MauiContext?.Services.GetService<MainPage>()
            ?? throw new InvalidOperationException("MainPage not registered in DI.");
        return new Window(mainPage);
    }

}
