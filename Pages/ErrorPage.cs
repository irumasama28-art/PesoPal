using CashLedgerAndroid.Services;
using CashLedgerAndroid;
using CashLedgerAndroid.Views;

namespace CashLedgerAndroid.Pages;

public class ErrorPage : ContentPage
{
    public ErrorPage(LedgerDatabase db, string message)
    {
        Title = "Error";
        BackgroundColor = Pal.Bg;
        var label = new Label { Text = "There was a problem starting the app.", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center };
        var details = new Label { Text = message, FontSize = 12, TextColor = Pal.Muted, HorizontalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
        var retry = new Button { Text = "Retry", HorizontalOptions = LayoutOptions.Center };
        retry.Clicked += async (_, _) =>
        {
            try
            {
                await db.InitializeAsync();
                if (Application.Current?.Windows?.Count > 0)
                    Application.Current.Windows[0].Page = new AppShell(db);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Startup error", ex.Message, "OK");
            }
        };

        var uninstall = new Button { Text = "Uninstall and Reinstall (adb)", HorizontalOptions = LayoutOptions.Center };
        uninstall.Clicked += async (_, _) =>
        {
            // Provide instructions; actual adb can't be run from app.
            await DisplayAlertAsync("Manual step", "From your PC run:\nadb uninstall com.francis.cashledger\nadb install -r <path-to-apk>", "OK");
        };

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            Children = { label, details, retry, uninstall }
        };
    }
}
