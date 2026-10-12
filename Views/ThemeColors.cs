using Microsoft.Maui.Controls;

namespace CashLedgerAndroid.Views;

/// <summary>
/// Dynamic theme-aware color resolver that adapts to light/dark mode
/// </summary>
public static class ThemeColors
{
    /// <summary>
    /// Get a resource color from the app's current theme, with fallback
    /// </summary>
    public static Color GetThemeColor(string resourceKey, Color fallback)
    {
        try
        {
            if (Application.Current?.Resources != null && 
                Application.Current.Resources.TryGetValue(resourceKey, out var value) &&
                value is Color color)
            {
                return color;
            }
        }
        catch { }

        return fallback;
    }

    /// <summary>
    /// Get the current page background color based on theme
    /// </summary>
    public static Color PageBackground => 
        GetThemeColor("PageBackgroundColor", Pal.Bg);

    /// <summary>
    /// Get the current surface/card background color
    /// </summary>
    public static Color SurfaceBackground => 
        GetThemeColor("SurfaceColor", Colors.White);

    /// <summary>
    /// Get the current primary text color
    /// </summary>
    public static Color PrimaryText => 
        GetThemeColor("PrimaryTextColor", Pal.Navy);

    /// <summary>
    /// Get the current secondary text color
    /// </summary>
    public static Color SecondaryText => 
        GetThemeColor("SecondaryTextColor", Pal.Muted);

    /// <summary>
    /// Get the current primary/accent color
    /// </summary>
    public static Color Primary => 
        GetThemeColor("PrimaryColor", Pal.Blue);

    /// <summary>
    /// Get the current border/divider color
    /// </summary>
    public static Color Border => 
        GetThemeColor("BorderColor", Pal.Line);

    /// <summary>
    /// Get the current card background color
    /// </summary>
    public static Color CardBackground => 
        GetThemeColor("CardBackgroundColor", Colors.White);

    /// <summary>
    /// Get the current input field background color
    /// </summary>
    public static Color InputBackground => 
        GetThemeColor("InputBackgroundColor", Color.FromArgb("#F5F5F5"));
}
