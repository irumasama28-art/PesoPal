using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;

namespace CashLedgerAndroid.Pages;

public class SettingsPage : ContentPage
{
    private readonly LedgerDatabase _db;
    private readonly CashLedgerAndroid.Services.IUpdateService? _updater;
    private readonly Action _dataChangedHandler;
    private readonly Action? _stateChangedHandler;
    private bool _isSubscribed;
    private View? _updateRowView;
    private readonly string _metadataUrl = "https://raw.githubusercontent.com/Synonymz/PesoPal/gh-pages/latest.json";
    private readonly VerticalStackLayout _content = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 14, 16, 24) };
    private readonly UpdateState? _state;
    private readonly Border _progressOverlay = new Border { BackgroundColor = Color.FromArgb("#73000000"), IsVisible = false };

    public SettingsPage(LedgerDatabase db, CashLedgerAndroid.Services.IUpdateService? updater = null)
    {
        _db = db;
        _updater = updater;
        _state = MauiProgram.Services?.GetService(typeof(UpdateState)) as UpdateState;
        _dataChangedHandler = () => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
        _stateChangedHandler = _state is null
            ? null
            : () => MainThread.BeginInvokeOnMainThread(() => OnUpdateStateChanged());
        Title = "Settings";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");
        Shell.SetNavBarIsVisible(this, false);

        var header = new Grid { BackgroundColor = Pal.BlueDark, Padding = new Thickness(18, 22) };
        header.Add(Ui.Text("Settings", 24, true, Colors.White), 0, 0);

        var root = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        root.Add(header, 0, 0);
        root.Add(new ScrollView { Content = _content }, 0, 1);

        // prepare overlay progress UI
        var progressLabel = new Label { TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        var progressStack = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        progressStack.Add(new ActivityIndicator { IsRunning = false, Color = Colors.White, IsVisible = false });
        progressStack.Add(progressLabel);
        _progressOverlay.Content = progressStack;
        // add overlay to same cell as scrollview so it appears above content
        root.Add(_progressOverlay, 0, 1);

        Content = root;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_isSubscribed)
        {
            _db.DataChanged += _dataChangedHandler;
            if (_state is not null && _stateChangedHandler is not null)
                _state.StateChanged += _stateChangedHandler;
            _isSubscribed = true;
        }

        await RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        if (_isSubscribed)
        {
            _db.DataChanged -= _dataChangedHandler;
            if (_state is not null && _stateChangedHandler is not null)
                _state.StateChanged -= _stateChangedHandler;
            _isSubscribed = false;
        }

        base.OnDisappearing();
    }

    private static Label SectionLabel(string text)
    {
        var label = Ui.Text(text.ToUpperInvariant(), 11, true, Pal.Muted);
        label.Margin = new Thickness(4, 8, 0, 0);
        return label;
    }

    private static Border Group(params View[] rows)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        for (var i = 0; i < rows.Length; i++)
        {
            stack.Add(rows[i]);
            if (i < rows.Length - 1) stack.Add(Ui.Divider());
        }
        return Ui.Card(stack, 20, new Thickness(16, 2));
    }

    private async Task RefreshAsync()
    {
        try
        {
            var last = LedgerDatabase.LastBackupText();
            _content.Clear();
            _content.Add(SectionLabel("Store"));
            _content.Add(Group(
                Ui.SettingRow("Fee table", "6 bands", OpenFeesAsync)));

            _content.Add(SectionLabel("Daily work"));
            _content.Add(Group(
                Ui.SettingRow("Close the day", "Count cash", OpenClosingAsync)));

            _content.Add(SectionLabel("Safety"));
            _content.Add(Group(
                Ui.SettingRow("Backup and restore", last.Length == 0 ? "Never backed up" : "Last: " + last, OpenBackupAsync)));

            // About & updates
            _content.Add(SectionLabel("About"));
            var aboutGroup = Group(
                Ui.SettingRow("About", "", OpenAboutAsync));
            _updateRowView = Ui.SettingRow("Check for updates", "", CheckForUpdateAsync);
            // add both rows to the about group
            // Group currently builds its own card; to keep structure simple, add rows directly
            _content.Add(Group(
                Ui.SettingRow("About", "", OpenAboutAsync),
                _updateRowView));

            var about = Ui.Text("PesoPal 1.0 · records are saved only on this phone. Back up regularly.", 11, false, Pal.Muted, TextAlignment.Center);
            about.Margin = new Thickness(0, 16, 0, 0);
            _content.Add(about);


            // If updater is available, run a background check to show available version name on the row
            var updater = _updater ?? MauiProgram.Services?.GetService(typeof(CashLedgerAndroid.Services.IUpdateService)) as CashLedgerAndroid.Services.IUpdateService;
            if (updater is not null && _updateRowView is not null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var info = await updater.CheckForUpdateAsync(_metadataUrl);
                        var indicator = info is null ? "Up to date" : $"{info.VersionName} available";
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                if (_updateRowView is Grid g && g.Children.Count >= 2 && g.Children[1] is Label right)
                                {
                                    right.Text = indicator + "  ›";
                                }
                            }
                            catch { }
                        });
                    }
                    catch { }
                });
            }

            // initial state update
            OnUpdateStateChanged();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not load settings", ex.Message, "OK");
        }
    }

    private Task OpenFeesAsync() => Navigation.PushAsync(new FeeTablePage(_db));
    private Task OpenClosingAsync() => Navigation.PushAsync(new ClosingPage(_db));
    private Task OpenBackupAsync() => Navigation.PushAsync(new BackupPage(_db));

    private async Task CheckForUpdateAsync()
    {
        try
        {
            var updater = _updater ?? MauiProgram.Services?.GetService(typeof(CashLedgerAndroid.Services.IUpdateService)) as CashLedgerAndroid.Services.IUpdateService;
            if (updater is null)
            {
                await DisplayAlertAsync("Update", "Update service not available on this platform.", "OK");
                return;
            }

            var url = _metadataUrl;
            var info = await updater.CheckForUpdateAsync(url);
            if (info is null)
            {
                await DisplayAlertAsync("Update", "Your app is up to date.", "OK");
                return;
            }

            var ok = await DisplayAlertAsync("Update available", $"Version {info.VersionName} is available. Install now?", "Install", "Cancel");
            if (ok)
            {
                // start update and show progress via UpdateState
                var state = _state;
                if (state is not null)
                {
                    state.SetProgress(0);
                    state.SetDownloading(true);
                }
                // show cancel option
                var task = updater.StartUpdateAsync(info);
                // display a cancel option while downloading
                var cancel = await DisplayActionSheetAsync("Downloading update", "Dismiss", null, "Cancel download");
                if (cancel == "Cancel download")
                {
                    try { await updater.CancelCurrentUpdateAsync(); } catch { }
                }
                await task;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Update check failed", ex.Message, "OK");
        }
    }

    private async Task OpenAboutAsync()
    {
        var msg = "PesoPal 1.0\n\nRecords are saved only on this phone. Back up regularly.\n\n";
        var choice = await DisplayAlertAsync("About", msg, "Check for updates", "OK");
        if (choice)
        {
            await CheckForUpdateAsync();
        }
    }

    private void OnUpdateStateChanged()
    {
        if (_state is null) return;
        // update the check row label
        if (_updateRowView is Grid g && g.Children.Count >= 2 && g.Children[1] is Label right)
        {
            if (_state.Latest is not null)
                right.Text = $"{_state.Latest.VersionName} available  ›";
            else
                right.Text = _state.IsDownloading ? $"Downloading... {_state.Progress:F0}%  ›" : "Up to date  ›";
        }

        // update overlay
        if (_state.IsDownloading)
        {
            _progressOverlay.IsVisible = true;
            if (_progressOverlay.Content is VerticalStackLayout stack && stack.Children.Count > 0)
            {
                if (stack.Children[0] is ActivityIndicator ai) { ai.IsRunning = true; ai.IsVisible = true; }
                if (stack.Children[1] is Label lbl) lbl.Text = $"Downloading... {_state.Progress:F0}%";
            }
        }
        else
        {
            _progressOverlay.IsVisible = false;
            if (_progressOverlay.Content is VerticalStackLayout stack && stack.Children.Count > 0)
            {
                if (stack.Children[0] is ActivityIndicator ai) { ai.IsRunning = false; ai.IsVisible = false; }
                if (stack.Children[1] is Label lbl) lbl.Text = string.Empty;
            }
        }

        // show a brief toast/alert for result message
        if (!string.IsNullOrEmpty(_state.LastResultMessage))
        {
            try
            {
                var msg = _state.LastResultMessage;
                var succ = _state.LastResultSuccess;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (succ)
                            await DisplayAlertAsync("Update", msg, "OK");
                        else
                            await DisplayAlertAsync("Update failed", msg, "OK");
                    }
                    catch { }
                });
                // reset after showing
                _state.NotifyResult(string.Empty, false);
            }
            catch { }
        }
    }

}
