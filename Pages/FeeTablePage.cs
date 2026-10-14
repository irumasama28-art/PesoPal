using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.Controls.Shapes;

namespace CashLedgerAndroid.Pages;

public class FeeTablePage : ContentPage
{
    private readonly LedgerDatabase _db;
    private AppSettings _settings = new AppSettings();
    private readonly Entry[] _entries = new Entry[6];
    private readonly Border _warnBox;
    private readonly Label _warnLabel;

    private static readonly string[] Bands =
    {
        "₱1 – ₱500", "₱501 – ₱1,000", "₱1,001 – ₱1,500", "₱1,501 – ₱2,000", "₱2,001 – ₱3,000", "Above ₱3,000"
    };

    public FeeTablePage(LedgerDatabase db)
    {
        _db = db;
        Title = "Fee table";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");

        var rows = new VerticalStackLayout { Spacing = 0 };
        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Pal.Navy,
                HorizontalTextAlignment = TextAlignment.End,
                BackgroundColor = Colors.Transparent,
                WidthRequest = 110
            };
            entry.TextChanged += (_, _) => CheckWarning();
            _entries[i] = entry;

            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Padding = new Thickness(0, 4)
            };
            var label = Ui.Text(Bands[i], 14, true);
            label.VerticalOptions = LayoutOptions.Center;
            row.Add(label, 0, 0);
            row.Add(entry, 1, 0);
            rows.Add(row);
            if (i < _entries.Length - 1) rows.Add(Ui.Divider());
        }

        var note = Ui.Note("warn", "");
        _warnBox = note.Item1;
        _warnLabel = note.Item2;

        var page = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16, 14, 16, 24) };
        page.Add(Ui.Text("These fees are suggested on every new transaction. You can still change the fee on each one.", 13, false, Pal.Muted));
        page.Add(Ui.Card(rows, 20, new Thickness(16, 4)));
        page.Add(_warnBox);
        page.Add(Ui.Primary("Save fee table", Pal.Blue, SaveAsync));
        Content = new ScrollView { Content = page };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _settings = await _db.GetSettingsAsync();
            _entries[0].Text = Fmt.Num(_settings.FeeUpTo500);
            _entries[1].Text = Fmt.Num(_settings.FeeUpTo1000);
            _entries[2].Text = Fmt.Num(_settings.FeeUpTo1500);
            _entries[3].Text = Fmt.Num(_settings.FeeUpTo2000);
            _entries[4].Text = Fmt.Num(_settings.FeeUpTo3000);
            _entries[5].Text = Fmt.Num(_settings.FeeAbove3000);
            CheckWarning();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not load fees", ex.Message, "OK");
        }
    }

    private void CheckWarning()
    {
        var hasFifth = Fmt.TryParse(_entries[4].Text, out var fifth);
        var hasSixth = Fmt.TryParse(_entries[5].Text, out var sixth);
        if (hasFifth && hasSixth && fifth == sixth)
        {
            _warnBox.IsVisible = true;
            Ui.StyleNote(_warnBox, _warnLabel, "warn", "The fee above ₱3,000 matches the band below it. Check that this is right for large amounts.");
        }
        else
        {
            _warnBox.IsVisible = false;
        }
    }

    private async Task SaveAsync()
    {
        var values = new decimal[_entries.Length];
        for (var i = 0; i < _entries.Length; i++)
        {
            if (!Fmt.TryParse(_entries[i].Text, out var value) || value < 0)
            {
                await DisplayAlertAsync("Invalid fee", "Enter zero or a positive amount for \"" + Bands[i] + "\".", "OK");
                return;
            }
            values[i] = Math.Round(value, 2);
        }
        _settings.FeeUpTo500 = values[0];
        _settings.FeeUpTo1000 = values[1];
        _settings.FeeUpTo1500 = values[2];
        _settings.FeeUpTo2000 = values[3];
        _settings.FeeUpTo3000 = values[4];
        _settings.FeeAbove3000 = values[5];
        await _db.SaveSettingsAsync(_settings);
        await Navigation.PopAsync();
    }
}
