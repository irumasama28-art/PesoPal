using Microsoft.Maui.Controls;

namespace CashLedgerAndroid.Services;

public class ThemeService
{
    private const string LightTheme = "light";

    public void InitializeTheme()
    {
        var application = Application.Current;
        if (application is null) return;

        application.UserAppTheme = AppTheme.Light;
        ApplyLightTheme(application);

        var settings = App.AppSettings;
        if (settings is not null && settings.ThemeMode != LightTheme)
        {
            settings.ThemeMode = LightTheme;
            _ = SaveSettingsAsync(settings);
        }
    }

    private static void ApplyLightTheme(Application application)
    {
        var resources = application.Resources;
        var mergedDictionaries = resources.MergedDictionaries;

        foreach (var existingTheme in mergedDictionaries
                     .Where(dictionary => dictionary is Resources.Styles.Theme_Light)
                     .ToList())
            mergedDictionaries.Remove(existingTheme);

        var lightTheme = new Resources.Styles.Theme_Light();
        mergedDictionaries.Add(lightTheme);

        foreach (var key in lightTheme.Keys)
            resources[key] = lightTheme[key];
    }

    private static async Task SaveSettingsAsync(Models.AppSettings settings)
    {
        try
        {
            if (App.Database is not null)
                await App.Database.SaveSettingsAsync(settings);
        }
        catch
        {
            // Light mode remains active even if updating a legacy preference fails.
        }
    }
}