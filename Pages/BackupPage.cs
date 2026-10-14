using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace CashLedgerAndroid.Pages;

public class BackupPage : ContentPage
{
    private readonly LedgerDatabase _db;
    private readonly Border _statusBox;
    private readonly Label _statusLabel;
    private readonly Label _count = Ui.Text("", 14, true, Pal.Navy, TextAlignment.End);
    private readonly Label _last = Ui.Text("", 14, true, Pal.Navy, TextAlignment.End);

    public BackupPage(LedgerDatabase db)
    {
        _db = db;
        Title = "Backup and restore";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");

        var status = Ui.Note("bad", "No backup yet. If this phone is lost, reset or the app is uninstalled, every record is gone.");
        _statusBox = status.Item1;
        _statusLabel = status.Item2;

        var page = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16, 14, 16, 24) };
        page.Add(_statusBox);
        page.Add(Ui.Card(InfoRows(), 20, new Thickness(16, 4)));
        page.Add(Ui.Primary("Back up now", Pal.Blue, BackupAsync));
        page.Add(Ui.Text("Creates one file you can save to your drive, email to yourself or send to another phone.", 12, false, Pal.Muted, TextAlignment.Center));
        page.Add(Ui.Text("RESTORE", 11, true, Pal.Muted));
        page.Add(Ui.Outline("Restore from a backup file", RestoreAsync));
        page.Add(Ui.Text("Restoring replaces everything now on this phone. You will be asked to confirm first.", 12, false, Pal.Muted, TextAlignment.Center));
        Content = new ScrollView { Content = page };
    }

    private View InfoRows()
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Add(InfoRow("Records on this phone", _count));
        stack.Add(Ui.Divider());
        stack.Add(InfoRow("Last backup", _last));
        return stack;
    }

    private static View InfoRow(string label, Label value)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(0, 14)
        };
        grid.Add(Ui.Text(label, 14, true, Pal.Muted), 0, 0);
        grid.Add(value, 1, 0);
        return grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var all = await _db.GetTransactionsAsync();
            _count.Text = all.Count + (all.Count == 1 ? " transaction" : " transactions");
            var last = LedgerDatabase.LastBackupText();
            if (last.Length == 0)
            {
                _last.Text = "Never";
                _last.TextColor = Pal.Red;
                _statusBox.IsVisible = true;
                Ui.StyleNote(_statusBox, _statusLabel, "bad", "No backup yet. If this phone is lost, reset or the app is uninstalled, every record is gone.");
            }
            else
            {
                _last.Text = last;
                _last.TextColor = Pal.Navy;
                Ui.StyleNote(_statusBox, _statusLabel, "ok", "Your records were last backed up on " + last + ".");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not load backup status", ex.Message, "OK");
        }
    }

    private async Task BackupAsync()
    {
        try
        {
            var path = await _db.CreateBackupFileAsync();
            // Offer both Share and optional upload to GitHub Releases via UpdateService metadata
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Cash Ledger backup", File = new ShareFile(path) });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Backup failed", ex.Message, "OK");
        }
    }

    private async Task RestoreAsync()
    {
        var go = await DisplayAlertAsync("Restore from a backup?",
            "This replaces ALL records on this phone with the ones in the backup file. Back up first if you are not sure.",
            "Choose file", "Cancel");
        if (!go) return;

        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a Cash Ledger backup file" });
            if (picked is null) return;

            var temp = Path.Combine(FileSystem.CacheDirectory, "restore-" + DateTime.Now.Ticks + ".db3");
            using (var input = await picked.OpenReadAsync())
            using (var output = File.Create(temp))
            {
                await input.CopyToAsync(output);
            }

            await _db.RestoreFromAsync(temp);
            try { File.Delete(temp); } catch { /* the cache folder is cleaned by Android anyway */ }
            await DisplayAlertAsync("Restored", "Your records were restored from the backup.", "OK");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Restore failed", ex.Message, "OK");
        }
    }
}
