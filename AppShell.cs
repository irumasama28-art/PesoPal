using CashLedgerAndroid.Pages;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;

namespace CashLedgerAndroid;

public class AppShell : Shell
{
    private readonly LedgerDatabase _db;
    private readonly CashLedgerAndroid.Services.IUpdateService? _updater;
    private readonly ShellContent _homeContent;
    private readonly ShellContent _historyContent;
    private readonly ShellContent _settingsContent;

    public AppShell(LedgerDatabase db)
    {
        _db = db;
        _updater = MauiProgram.Services?.GetService(typeof(CashLedgerAndroid.Services.IUpdateService)) as CashLedgerAndroid.Services.IUpdateService;

        FlyoutBehavior = FlyoutBehavior.Disabled;
        SetBackgroundColor(this, Pal.BlueDark);
        SetForegroundColor(this, Colors.White);
        SetTitleColor(this, Colors.White);
        SetTabBarBackgroundColor(this, Colors.White);
        SetTabBarForegroundColor(this, Pal.Blue);
        SetTabBarTitleColor(this, Pal.Blue);
        SetTabBarUnselectedColor(this, Pal.Muted);
        SetDynamicResource(Shell.TabBarBackgroundColorProperty, "CardBackgroundColor");
        SetDynamicResource(Shell.TabBarForegroundColorProperty, "PrimaryColor");
        SetDynamicResource(Shell.TabBarTitleColorProperty, "PrimaryColor");
        SetDynamicResource(Shell.TabBarUnselectedColorProperty, "SecondaryTextColor");

        var bar = new TabBar();
        // Use custom images in Resources/Images for tab icons. New assets added with _new suffix.
        _homeContent = new ShellContent { Title = "Home", Icon = "home_new.svg", Content = new HomePage(db) };
        _historyContent = new ShellContent { Title = "History", Icon = "history_new.svg", Content = new HistoryPage(db) };
        _settingsContent = new ShellContent { Title = "Settings", Icon = "settings_new.svg", Content = new SettingsPage(db, _updater) };
        bar.Items.Add(_homeContent);
        bar.Items.Add(_historyContent);
        bar.Items.Add(_settingsContent);
        Items.Add(bar);
    }

    public void RecreatePages()
    {
        _homeContent.Content = new HomePage(_db);
        _historyContent.Content = new HistoryPage(_db);
        _settingsContent.Content = new SettingsPage(_db, _updater);
    }

    public static void ShowTab(int index)
    {
        try
        {
            if (Current is Shell shell && shell.Items.Count > 0)
            {
                var tabs = shell.Items[0];
                if (index >= 0 && index < tabs.Items.Count)
                    shell.CurrentItem = tabs.Items[index];
            }
        }
        catch
        {
            // Switching tabs is a convenience; ignore if the shell is not ready.
        }
    }
}
