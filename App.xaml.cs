using CashLedgerAndroid.Services;
using Microsoft.Maui.ApplicationModel;
using CashLedgerAndroid.Pages;

namespace CashLedgerAndroid;

public partial class App : Application
{
    private LedgerDatabase _database = null!;

    /// <summary>
    /// Static reference to the app instance for global access
    /// </summary>
    public static App? Current { get; private set; }

    /// <summary>
    /// Static reference to the database instance
    /// </summary>
    public static LedgerDatabase? Database { get; private set; }

    /// <summary>
    /// Static reference to app settings
    /// </summary>
    public static Models.AppSettings? AppSettings { get; private set; }

    /// <summary>
    /// Static reference to the theme service
    /// </summary>
    public static Services.ThemeService? ThemeService { get; private set; }

    public App(LedgerDatabase database)
    {
        InitializeComponent();
        _database = database;
        Current = this;
        Database = database;

        // Start initialization in background; CreateWindow will provide the initial loading window.
        _ = InitializeAppAsync(database);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Provide the animated loading page while the database initializes.
        var loading = new LoadingPage();
        return new Window(loading);
    }

    private async Task InitializeAppAsync(LedgerDatabase database)
    {
        LoadingPage? loading = null;
        try
        {
            // Start both the DB init and the intro animation in parallel and wait for both.
            if (Application.Current?.Windows?.Count > 0)
                loading = Application.Current.Windows[0].Page as LoadingPage;

            var introTask = loading?.PlayIntroAsync() ?? Task.CompletedTask;
            var dbTask = database.InitializeAsync();

            await Task.WhenAll(introTask, dbTask);

            // Get AppSettings and initialize theme
            AppSettings = await database.GetSettingsAsync();
            ThemeService = new Services.ThemeService();
            ThemeService.InitializeTheme();

            // Fade out and switch to main shell on main thread.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (loading is not null)
                    {
                        await loading.StopAndFadeOutAsync();
                        Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(loading);
                    }

                    if (Application.Current?.Windows?.Count > 0)
                        Application.Current.Windows[0].Page = new AppShell(database);
                }
                catch (Exception ex)
                {
                    if (Application.Current?.Windows?.Count > 0)
                        Application.Current.Windows[0].Page = new ErrorPage(database, ex.Message);
                }
            });
        }
        catch (Exception ex)
        {
            // On failure, ensure we stop animations and show ErrorPage.
            try { if (loading is not null) await MainThread.InvokeOnMainThreadAsync(() => loading.StopAndFadeOutAsync()); } catch { }
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Application.Current?.Windows?.Count > 0)
                    Application.Current.Windows[0].Page = new ErrorPage(database, ex.Message);
            });
        }
    }
}
