using Microsoft.Maui.Controls;

namespace CashLedgerAndroid.Services;

/// <summary>
/// Service to manage app theme (Light/Dark mode)
/// </summary>
public class ThemeService
{
    private const string LightTheme = "light";
    private const string DarkTheme = "dark";
    private const string SystemTheme = "system";

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Get current theme mode
    /// </summary>
    public string GetCurrentTheme()
    {
        var appSettings = App.AppSettings;
        return appSettings?.ThemeMode ?? LightTheme;
    }

    /// <summary>
    /// Apply theme to the application
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        if (Application.Current == null) return;

        bool isDarkMode = themeName == DarkTheme || 
                         (themeName == SystemTheme && Application.Current.UserAppTheme == AppTheme.Dark);

        ApplyThemeColors(isDarkMode);

        // Save preference
        if (App.AppSettings != null)
        {
            App.AppSettings.ThemeMode = themeName;
            Task.Run(() => App.Database?.SaveSettingsAsync(App.AppSettings));
        }

        // Trigger page refresh to update dynamic colors
        RefreshCurrentPage();

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { Theme = themeName, IsDarkMode = isDarkMode });
    }

    /// <summary>
    /// Toggle between light and dark themes
    /// </summary>
    public void ToggleTheme()
    {
        var current = GetCurrentTheme();
        var newTheme = current == LightTheme ? DarkTheme : LightTheme;
        ApplyTheme(newTheme);
    }

    /// <summary>
    /// Apply theme resources to the app
    /// </summary>
    private void ApplyThemeColors(bool isDarkMode)
    {
        if (Application.Current?.Resources == null) return;

        var resources = Application.Current.Resources;
        var mergedDictionaries = resources.MergedDictionaries;

        // Find and remove old theme
        var oldTheme = mergedDictionaries.FirstOrDefault(d => 
            d.Source?.ToString().Contains("Theme.") == true);
        if (oldTheme != null)
            mergedDictionaries.Remove(oldTheme);

        // Add new theme
        var themeFile = isDarkMode ? "Resources/Styles/Theme.Dark.xaml" : "Resources/Styles/Theme.Light.xaml";
        var newTheme = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };
        mergedDictionaries.Add(newTheme);
    }

    /// <summary>
    /// Initialize theme on app startup
    /// </summary>
    public void InitializeTheme()
    {
        var themeName = GetCurrentTheme();
        ApplyTheme(themeName);
    }

    /// <summary>
    /// Refresh the current page to apply new theme colors
    /// </summary>
    private void RefreshCurrentPage()
    {
        try
        {
            // Get the current active page and force a visual refresh
            if (Application.Current?.Windows?.Count > 0)
            {
                var window = Application.Current.Windows[0];
                var page = window.Page;

                if (page is Shell shell)
                {
                    // If it's a shell, refresh the current content
                    if (shell.CurrentPage is ContentPage contentPage)
                    {
                        // Trigger OnAppearing which many pages use to refresh
                        contentPage.SendAppearing();
                    }
                }
                else if (page is ContentPage contentPage)
                {
                    contentPage.SendAppearing();
                }
            }
        }
        catch { }
    }
}

/// <summary>
/// Event args for theme changed event
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public required string Theme { get; set; }
    public bool IsDarkMode { get; set; }
}
