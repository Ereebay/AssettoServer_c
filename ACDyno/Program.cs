using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using ACDyno.Services;
using ACDyno.Components;

namespace ACDyno;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        
        // Register ACDyno services
        appBuilder.Services.AddSingleton<CarLoader>();
        appBuilder.Services.AddSingleton<UnrealisticDetector>();
        appBuilder.Services.AddSingleton<BalanceSuggester>();
        appBuilder.Services.AddSingleton<BackupService>();
        appBuilder.Services.AddSingleton<CarLibraryService>();
        appBuilder.Services.AddSingleton<WindowService>();
        appBuilder.Services.AddSingleton<GeminiService>();

        // Configure root component
        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();
        
        // Set window reference for native dialogs
        var windowService = app.Services.GetRequiredService<WindowService>();
        windowService.SetWindow(app.MainWindow);

        // Configure the main window
        app.MainWindow
            .SetTitle("ACDyno - Assetto Corsa Car Analyzer")
            .SetSize(1400, 900)
            .SetUseOsDefaultSize(false)
            .Center();

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal Error", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
