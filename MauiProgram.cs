using CashLedgerAndroid.Services;
using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace CashLedgerAndroid;

public static class MauiProgram
{
    public static System.IServiceProvider? Services { get; private set; }
    public static MauiApp CreateMauiApp()
    {
        // Ensure native SQLite provider is initialized early to avoid DllNotFoundException
        Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddSingleton<LedgerDatabase>();
        // Register UpdateState singleton
        builder.Services.AddSingleton<UpdateState>();
        // Register ThemeService singleton
        builder.Services.AddSingleton<ThemeService>();
#if ANDROID
        // Register platform update service only on Android
        builder.Services.AddSingleton<CashLedgerAndroid.Services.IUpdateService, CashLedgerAndroid.Platforms.Android.Services.UpdateService>();
#endif
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        Services = app.Services;

        // Trigger a background update check after startup (Android only)
#if ANDROID
        try
        {
            var state = Services.GetService(typeof(UpdateState)) as UpdateState;
            var updater = Services.GetService(typeof(CashLedgerAndroid.Services.IUpdateService)) as CashLedgerAndroid.Services.IUpdateService;
            if (updater is not null && state is not null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var info = await updater.CheckForUpdateAsync("https://raw.githubusercontent.com/Synonymz/PesoPal/gh-pages/latest.json");
                        state.SetLatest(info);
                    }
                    catch { }
                });
            }
        }
        catch { }
#endif
        return app;
    }
}
