using System.Text;
using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls.Shapes;

namespace CashLedgerAndroid.Pages;

public class HistoryPage : ContentPage
{
    private readonly LedgerDatabase _db;
    private readonly Action _dataChangedHandler;
    private bool _isSubscribed;
    private List<TransactionRecord> _all = new List<TransactionRecord>();
    private string _filter = "All";
    private readonly Entry _search;
    private readonly HorizontalStackLayout _chips = new HorizontalStackLayout { Spacing = 8 };
    private readonly VerticalStackLayout _list = new VerticalStackLayout { Spacing = 10 };

    public HistoryPage(LedgerDatabase db)
    {
        _db = db;
        _dataChangedHandler = () => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
        Title = "History";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");
        Shell.SetNavBarIsVisible(this, false);

        _search = new Entry
        {
            Placeholder = "Search name, receipt, reference…",
            PlaceholderColor = Color.FromArgb("#8896AD"),
            TextColor = Pal.Navy,
            FontSize = 14,
            BackgroundColor = Colors.Transparent
        };
        _search.SetDynamicResource(Entry.TextColorProperty, "PrimaryTextColor");
        _search.SetDynamicResource(Entry.PlaceholderColorProperty, "HintTextColor");
        _search.TextChanged += (_, _) => Render();

        Content = BuildLayout();
        RenderChips();
    }

    private View BuildLayout()
    {
        var root = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }
        };

        var header = new Grid
        {
            BackgroundColor = Pal.BlueDark,
            Padding = new Thickness(18, 22, 18, 22),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        var title = Ui.Text("History", 24, true, Colors.White);
        title.VerticalOptions = LayoutOptions.Center;
        var export = new Button
        {
            Text = "Export CSV",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromRgba(255, 255, 255, 50),
            CornerRadius = 20,
            HeightRequest = 40,
            Padding = new Thickness(16, 0)
        };
        export.Clicked += async (_, _) => await ExportAsync();
        header.Add(title, 0, 0);
        header.Add(export, 1, 0);
        root.Add(header, 0, 0);

        var controls = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 6) };
        controls.Add(Ui.Card(_search, 16, new Thickness(12, 0)));
        controls.Add(_chips);
        root.Add(controls, 0, 1);

        var listHost = new VerticalStackLayout { Padding = new Thickness(16, 4, 16, 20), Spacing = 10 };
        listHost.Add(_list);
        listHost.Add(Ui.Text("Tap a transaction for receipt, edit, void or delete.", 12, false, Pal.Muted, TextAlignment.Center));
        root.Add(new ScrollView { Content = listHost }, 0, 2);
        return root;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_isSubscribed)
        {
            _db.DataChanged += _dataChangedHandler;
            _isSubscribed = true;
        }

        await RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        if (_isSubscribed)
        {
            _db.DataChanged -= _dataChangedHandler;
            _isSubscribed = false;
        }

        base.OnDisappearing();
    }

    public async Task RefreshAsync()
    {
        try
        {
            _all = await _db.GetTransactionsAsync();
            Render();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not load history", ex.Message, "OK");
        }
    }

    private void RenderChips()
    {
        _chips.Clear();
        foreach (var name in new[] { "All", "Cash-In", "Cash-Out", "Voided" })
        {
            var value = name;
            _chips.Add(Ui.Chip(value, _filter == value, () =>
            {
                _filter = value;
                RenderChips();
                Render();
                return Task.CompletedTask;
            }));
        }
    }

    private IEnumerable<TransactionRecord> Filtered()
    {
        IEnumerable<TransactionRecord> results = _all;
        if (_filter == "Cash-In") results = results.Where(t => t.IsCashIn && !t.IsVoided);
        else if (_filter == "Cash-Out") results = results.Where(t => !t.IsCashIn && !t.IsVoided);
        else if (_filter == "Voided") results = results.Where(t => t.IsVoided);

        var query = _search.Text?.Trim() ?? "";
        if (query.Length > 0)
        {
            results = results.Where(t =>
                ("TX-" + t.Id.ToString("000000")).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Id.ToString().Equals(query, StringComparison.OrdinalIgnoreCase) ||
                t.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Type.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.ReferenceNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Notes.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                Fmt.Num(t.Amount).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm", Fmt.Inv).Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return results;
    }

    private static string DayLabel(DateTime day)
    {
        if (day.Date == DateTime.Today) return "TODAY · " + day.ToString("MMM d", Fmt.Inv).ToUpperInvariant();
        if (day.Date == DateTime.Today.AddDays(-1)) return "YESTERDAY · " + day.ToString("MMM d", Fmt.Inv).ToUpperInvariant();
        return day.ToString("MMM d, yyyy", Fmt.Inv).ToUpperInvariant();
    }

    private void Render()
    {
        if (_list is null) return;
        _list.Clear();
        var matches = Filtered().OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).ToList();

        if (matches.Count == 0)
        {
            var text = _all.Count == 0
                ? "No transactions yet. Tap Cash-in or Cash-out on Home to record your first one."
                : "No transactions match your search.";
            _list.Add(Ui.Text(text, 14, false, Pal.Muted, TextAlignment.Center));
            return;
        }

        foreach (var group in matches.Take(200).GroupBy(t => t.CreatedAt.Date))
        {
            var fees = group.Where(t => !t.IsVoided).Sum(t => t.Fee);
            var head = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            head.Add(Ui.Text(DayLabel(group.Key), 11, true, Pal.Muted), 0, 0);
            head.Add(Ui.Text("Fees " + Fmt.MoneyShort(fees), 11, true, Pal.Muted, TextAlignment.End), 1, 0);
            _list.Add(head);

            var rows = new VerticalStackLayout { Spacing = 0 };
            var items = group.ToList();
            for (var i = 0; i < items.Count; i++)
            {
                rows.Add(TransactionRow.Create(items[i], ShowMenuAsync));
                if (i < items.Count - 1) rows.Add(Ui.Divider());
            }
            _list.Add(Ui.Card(rows, 20, new Thickness(14, 0)));
        }

        if (matches.Count > 200)
            _list.Add(Ui.Text("Showing the latest 200 matching records. Refine your search to find older ones.", 11, false, Pal.Muted, TextAlignment.Center));
    }

    private Task ShowMenuAsync(TransactionRecord t) => TransactionActions.ShowMenuAsync(this, _db, t);

    // ---- CSV export ----

    private static string Csv(string? value)
    {
        var text = value ?? "";
        // Stop spreadsheet apps from running a customer name like "=1+1" as a formula.
        if (text.Length > 0 && (text[0] == '=' || text[0] == '+' || text[0] == '-' || text[0] == '@'))
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private async Task ExportAsync()
    {
        if (_all.Count == 0)
        {
            await DisplayAlertAsync("Nothing to export", "Record a transaction first.", "OK");
            return;
        }

        var range = await DisplayActionSheetAsync("Export to CSV: which dates?", "Cancel", null, "Today", "This week", "This month", "All time");
        if (string.IsNullOrEmpty(range) || range == "Cancel") return;
        var includeVoided = await DisplayAlertAsync("Voided transactions",
            "Include voided transactions in the file? They are marked in a Status column.", "Include", "Leave out");

        var now = DateTime.Now;
        var start = DateTime.MinValue;
        if (range == "Today") start = DateTime.Today;
        else if (range == "This week") start = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        else if (range == "This month") start = new DateTime(now.Year, now.Month, 1);

        var rows = _all.Where(t => t.CreatedAt >= start && (includeVoided || !t.IsVoided))
                       .OrderBy(t => t.CreatedAt).ToList();
        if (rows.Count == 0)
        {
            await DisplayAlertAsync("Nothing to export", "There are no transactions in that range.", "OK");
            return;
        }

        var sb = new StringBuilder("Id,DateTime,Customer,Type,Amount,Fee,ReferenceNumber,Notes,Status\r\n");
        foreach (var t in rows)
        {
            sb.Append(t.Id).Append(',')
              .Append(Csv(t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", Fmt.Inv))).Append(',')
              .Append(Csv(t.CustomerName)).Append(',')
              .Append(Csv(t.Type)).Append(',')
              .Append(t.Amount.ToString(Fmt.Inv)).Append(',')
              .Append(t.Fee.ToString(Fmt.Inv)).Append(',')
              .Append(Csv(t.ReferenceNumber)).Append(',')
              .Append(Csv(t.Notes)).Append(',')
              .Append(Csv(t.IsVoided ? "Voided" : "Active")).Append("\r\n");
        }

        try
        {
            var path = System.IO.Path.Combine(FileSystem.CacheDirectory, "cash-ledger-" + now.ToString("yyyyMMdd-HHmmss", Fmt.Inv) + ".csv");
            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true));
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Cash Ledger export", File = new ShareFile(path) });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export failed", ex.Message, "OK");
        }
    }
}
