using System.Text;
using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace CashLedgerAndroid.Pages;

// End-of-day check: compare what the app expects with the cash and GCash you actually count.
public class ClosingPage : ContentPage
{
    private readonly LedgerDatabase _db;

    private int _inCount;
    private int _outCount;
    private decimal _inAmount;
    private decimal _outAmount;
    private decimal _fees;
    private decimal _cashChange;
    private decimal _walletChange;

    private readonly Label _summary = Ui.Text("", 14, false, Pal.Navy);
    private readonly Entry _openCash = Ui.PlainEntry("0", Keyboard.Numeric);
    private readonly Entry _countedCash = Ui.PlainEntry("0", Keyboard.Numeric);
    private readonly Entry _openWallet = Ui.PlainEntry("0", Keyboard.Numeric);
    private readonly Entry _countedWallet = Ui.PlainEntry("0", Keyboard.Numeric);
    private readonly Label _expectedCash = Ui.Text("", 18, true);
    private readonly Label _expectedWallet = Ui.Text("", 18, true);
    private readonly Border _cashNote;
    private readonly Label _cashNoteLabel;
    private readonly Border _walletNote;
    private readonly Label _walletNoteLabel;

    public ClosingPage(LedgerDatabase db)
    {
        _db = db;
        Title = "Close the day";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");

        var cashNote = Ui.Note("info", "Enter what you counted.");
        _cashNote = cashNote.Item1;
        _cashNoteLabel = cashNote.Item2;
        var walletNote = Ui.Note("info", "Enter what your wallet shows.");
        _walletNote = walletNote.Item1;
        _walletNoteLabel = walletNote.Item2;

        foreach (var entry in new[] { _openCash, _countedCash, _openWallet, _countedWallet })
            entry.TextChanged += (_, _) => Recalculate();

        var page = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16, 14, 16, 24) };
        page.Add(Ui.Text(DateTime.Now.ToString("dddd, MMM d, yyyy", Fmt.Inv), 13, true, Pal.Muted));
        page.Add(Ui.Card(_summary, 20, new Thickness(16)));

        page.Add(Ui.Text("COUNT YOUR MONEY", 11, true, Pal.Muted));
        page.Add(Ui.Text("Opening is what you had at the start of today. The app adds today's transactions to work out what you should have now.", 12, false, Pal.Muted));

        var cash = new VerticalStackLayout { Spacing = 10 };
        cash.Add(Ui.Text("Cash", 16, true));
        cash.Add(Ui.Field("Opening cash (start of day)", _openCash));
        cash.Add(Ui.Field("Cash you counted now", _countedCash));
        cash.Add(Row("The app expects", _expectedCash));
        cash.Add(_cashNote);
        page.Add(Ui.Card(cash, 20, new Thickness(16)));

        var wallet = new VerticalStackLayout { Spacing = 10 };
        wallet.Add(Ui.Text("GCash", 16, true));
        wallet.Add(Ui.Field("Opening GCash (start of day)", _openWallet));
        wallet.Add(Ui.Field("GCash your wallet shows now", _countedWallet));
        wallet.Add(Row("The app expects", _expectedWallet));
        wallet.Add(_walletNote);
        page.Add(Ui.Card(wallet, 20, new Thickness(16)));

        page.Add(Ui.Primary("Close the day", Pal.Blue, CloseDayAsync));
        Content = new ScrollView { Content = page };
    }

    private static View Row(string label, Label value)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        var left = Ui.Text(label, 13, false, Pal.Muted);
        left.VerticalOptions = LayoutOptions.Center;
        value.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(left, 0, 0);
        grid.Add(value, 1, 0);
        return grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var all = await _db.GetTransactionsAsync();
            var businessDayStart = _db.GetBusinessDayStart();
            var today = all.Where(t => !t.IsVoided && t.CreatedAt >= businessDayStart).ToList();
            _inCount = 0; _outCount = 0; _inAmount = 0; _outAmount = 0; _fees = 0; _cashChange = 0; _walletChange = 0;
            foreach (var t in today)
            {
                _fees += t.Fee;
                if (t.IsCashIn)
                {
                    _inCount++;
                    _inAmount += t.Amount;
                    _cashChange += t.Amount + t.Fee; // customer hands you amount + fee
                    _walletChange -= t.Amount;       // you send the amount by GCash
                }
                else
                {
                    _outCount++;
                    _outAmount += t.Amount;
                    _cashChange += t.Fee - t.Amount; // you hand out the amount, then collect the fee
                    _walletChange += t.Amount;       // customer sends you the amount by GCash
                }
            }
            _summary.Text =
                "Cash-in: " + _inCount + " · " + Fmt.Money(_inAmount) + "\n" +
                "Cash-out: " + _outCount + " · " + Fmt.Money(_outAmount) + "\n" +
                "Fees earned: " + Fmt.Money(_fees);
            Recalculate();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not load today's transactions", ex.Message, "OK");
        }
    }

    private static bool Has(Entry entry, out decimal value)
    {
        return Fmt.TryParse(entry.Text, out value);
    }

    private void Recalculate()
    {
        Has(_openCash, out var openCash);
        Has(_openWallet, out var openWallet);
        var expectedCash = openCash + _cashChange;
        var expectedWallet = openWallet + _walletChange;
        _expectedCash.Text = Fmt.Money(expectedCash);
        _expectedWallet.Text = Fmt.Money(expectedWallet);

        Compare(_countedCash, expectedCash, _cashNote, _cashNoteLabel, "counted");
        Compare(_countedWallet, expectedWallet, _walletNote, _walletNoteLabel, "shown");
    }

    private static void Compare(Entry counted, decimal expected, Border note, Label label, string verb)
    {
        if (!Has(counted, out var actual))
        {
            Ui.StyleNote(note, label, "info", "Enter what you " + (verb == "counted" ? "counted." : "see in your wallet."));
            return;
        }
        var diff = Math.Round(actual - expected, 2);
        if (diff == 0)
            Ui.StyleNote(note, label, "ok", "Matches. Nothing to check.");
        else if (diff < 0)
            Ui.StyleNote(note, label, "warn", "Short by " + Fmt.Money(-diff) + ". Check for a missed transaction or an unrecorded fee.");
        else
            Ui.StyleNote(note, label, "warn", "Over by " + Fmt.Money(diff) + ". Check for a transaction recorded with the wrong amount.");
    }

    private async Task CloseDayAsync()
    {
        var closeConfirmed = await DisplayAlertAsync(
            "Close the day",
            "Today's dashboard totals will reset to zero. Your transactions will remain in History.",
            "Close day",
            "Cancel");
        if (!closeConfirmed) return;

        _db.CloseBusinessDay();
        await DisplayAlertAsync("Day closed", "Today's totals have been reset. Your transaction history is preserved.", "OK");
        await Navigation.PopAsync();
    }
}
