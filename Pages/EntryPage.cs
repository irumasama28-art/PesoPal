using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.Controls.Shapes;

namespace CashLedgerAndroid.Pages;

// One screen for adding a new Cash-In / Cash-Out and for editing an existing one.
public class EntryPage : ContentPage
{
    private readonly LedgerDatabase _db;
    private readonly TransactionRecord? _existing;
    private AppSettings _settings = new AppSettings();
    private string _type;
    private bool _settingFee;
    private bool _feeEdited;
    private bool _loaded;

    private readonly Entry _amount;
    private readonly Entry _customer;
    private readonly Entry _fee;
    private readonly Entry _reference;
    private readonly Entry _notes;
    private readonly Label _feeHint;
    private readonly Label _summaryTitle;
    private readonly Label _summaryValue;
    private readonly Label _summaryNote;
    private readonly VerticalStackLayout _more;
    private readonly Label _moreToggle;
    private readonly Border _inSegment;
    private readonly Border _outSegment;
    private readonly Label _inLabel;
    private readonly Label _outLabel;
    private readonly Button _save;

    public EntryPage(LedgerDatabase db, string type = "Cash-In", TransactionRecord? existing = null)
    {
        _db = db;
        _existing = existing;
        _type = existing?.Type ?? type;
        Title = existing is null ? "New transaction" : "Edit TX-" + existing.Id.ToString("000000");
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");

        _amount = new Entry
        {
            Placeholder = "0",
            PlaceholderColor = Color.FromArgb("#B8C4D6"),
            Keyboard = Keyboard.Numeric,
            FontSize = 44,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Pal.Navy,
            BackgroundColor = Colors.Transparent
        };
        _customer = Ui.PlainEntry("Customer name (optional)", Keyboard.Text, 80);
        _fee = Ui.PlainEntry("0", Keyboard.Numeric);
        _reference = Ui.PlainEntry("Reference number (optional)", Keyboard.Text, 100);
        _notes = Ui.PlainEntry("Notes (optional)", Keyboard.Text, 300);
        _feeHint = Ui.Text("Fee · suggested ₱0", 11, true, Pal.Muted);
        _summaryTitle = Ui.Text("Customer hands you", 14, true);
        _summaryValue = Ui.Text("₱0.00", 20, true, Pal.Green, TextAlignment.End);
        _summaryNote = Ui.Text("", 12, false, Pal.Muted);
        _more = new VerticalStackLayout { Spacing = 8, IsVisible = false };
        _moreToggle = Ui.Text("＋ More details  ·  Reference, Notes", 14, true, Pal.Blue);
        _inLabel = Ui.Text("Cash In", 14, true, Pal.Muted, TextAlignment.Center);
        _outLabel = Ui.Text("Cash Out", 14, true, Pal.Muted, TextAlignment.Center);
        _inSegment = SegmentBorder(_inLabel);
        _outSegment = SegmentBorder(_outLabel);
        _save = Ui.Primary("Save", Pal.Green, SaveAsync);

        Content = BuildLayout();
        RefreshType();

        _amount.TextChanged += (_, _) => OnAmountChanged();
        _fee.TextChanged += (_, _) =>
        {
            if (_loaded && !_settingFee) _feeEdited = true;
            UpdateSummary();
        };
    }

    private static Border SegmentBorder(Label label)
    {
        return new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(0, 11),
            StrokeShape = new RoundRectangle { CornerRadius = 11 },
            Content = label
        };
    }

    private View BuildLayout()
    {
        var root = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };

        // Header with a back button
        var header = new Grid
        {
            BackgroundColor = Pal.BlueDark,
            Padding = new Thickness(8, 14, 16, 14),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
        };
        var back = new Button
        {
            Text = "‹",
            FontSize = 30,
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 52,
            HeightRequest = 48,
            Padding = new Thickness(0)
        };
        SemanticProperties.SetDescription(back, "Back");
        back.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var heading = Ui.Text(Title ?? "", 20, true, Colors.White);
        heading.VerticalOptions = LayoutOptions.Center;
        header.Add(back, 0, 0);
        header.Add(heading, 1, 0);
        root.Add(header, 0, 0);

        // Form
        var form = new VerticalStackLayout { Padding = new Thickness(16, 14, 16, 20), Spacing = 12 };

        if (_existing is null)
        {
            var segment = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 4,
                Padding = new Thickness(4),
                BackgroundColor = Color.FromArgb("#DCE6F5")
            };
            segment.Add(_inSegment, 0, 0);
            segment.Add(_outSegment, 1, 0);
            var inTap = new TapGestureRecognizer();
            inTap.Tapped += (_, _) => { _type = "Cash-In"; RefreshType(); };
            _inSegment.GestureRecognizers.Add(inTap);
            var outTap = new TapGestureRecognizer();
            outTap.Tapped += (_, _) => { _type = "Cash-Out"; RefreshType(); };
            _outSegment.GestureRecognizers.Add(outTap);
            form.Add(Ui.Card(segment, 14, new Thickness(0), Color.FromArgb("#DCE6F5")));
        }
        else
        {
            form.Add(Ui.Field("Type · cannot be changed", Ui.Text(_type, 16, true)));
        }

        // Amount
        var amountBox = new VerticalStackLayout { Spacing = 0, HorizontalOptions = LayoutOptions.Fill };
        amountBox.Add(Ui.Text("₱ Amount", 12, true, Pal.Muted, TextAlignment.Center));
        amountBox.Add(_amount);
        form.Add(amountBox);

        var chips = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
        foreach (var quick in new[] { 500, 1000, 1500, 2000, 3000 })
        {
            var value = quick;
            chips.Add(Ui.Chip(value.ToString("#,##0", Fmt.Inv), false, () =>
            {
                _amount.Text = value.ToString(Fmt.Inv);
                return Task.CompletedTask;
            }));
        }
        form.Add(chips);

        form.Add(Ui.Field("Customer (optional)", _customer));

        var feeStack = new VerticalStackLayout { Spacing = 0 };
        feeStack.Add(_feeHint);
        feeStack.Add(_fee);
        form.Add(new Border
        {
            StrokeThickness = 1.5,
            Stroke = new SolidColorBrush(Pal.FieldLine),
            BackgroundColor = Colors.White,
            Padding = new Thickness(14, 6, 14, 2),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = feeStack
        });

        // More details (collapsed unless editing something that has them)
        _more.Add(Ui.Field("Reference number", _reference));
        _more.Add(Ui.Field("Notes", _notes));
        var toggleTap = new TapGestureRecognizer();
        toggleTap.Tapped += (_, _) => SetMoreVisible(!_more.IsVisible);
        _moreToggle.GestureRecognizers.Add(toggleTap);
        _moreToggle.Padding = new Thickness(2, 6);
        form.Add(_moreToggle);
        form.Add(_more);

        // Summary
        var summaryGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        _summaryTitle.VerticalOptions = LayoutOptions.Center;
        summaryGrid.Add(_summaryTitle, 0, 0);
        summaryGrid.Add(_summaryValue, 1, 0);
        var summaryStack = new VerticalStackLayout { Spacing = 4 };
        summaryStack.Add(summaryGrid);
        summaryStack.Add(_summaryNote);
        form.Add(Ui.Card(summaryStack, 14, new Thickness(14, 12)));

        root.Add(new ScrollView { Content = form }, 0, 1);

        // Save bar
        var bar = new VerticalStackLayout { Padding = new Thickness(16, 10, 16, 14), BackgroundColor = Colors.White };
        bar.SetDynamicResource(VisualElement.BackgroundColorProperty, "CardBackgroundColor");
        bar.Add(_save);
        root.Add(bar, 0, 2);
        return root;
    }

    private void SetMoreVisible(bool visible)
    {
        _more.IsVisible = visible;
        _moreToggle.Text = visible ? "－ Hide details" : "＋ More details  ·  Reference, Notes";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        try
        {
            _settings = await _db.GetSettingsAsync();
        }
        catch
        {
            _settings = new AppSettings();
        }

        if (_existing is not null)
        {
            _amount.Text = Fmt.Num(_existing.Amount);
            _customer.Text = _existing.CustomerName;
            SetFee(_existing.Fee);
            _reference.Text = _existing.ReferenceNumber;
            _notes.Text = _existing.Notes;
            _feeEdited = true;
            if (!string.IsNullOrWhiteSpace(_existing.ReferenceNumber) || !string.IsNullOrWhiteSpace(_existing.Notes))
                SetMoreVisible(true);
        }
        _loaded = true;
        UpdateSummary();
    }

    private void RefreshType()
    {
        var isIn = _type == "Cash-In";
        _inSegment.BackgroundColor = isIn ? Pal.Green : Colors.Transparent;
        _inLabel.TextColor = isIn ? Colors.White : Pal.Muted;
        _outSegment.BackgroundColor = isIn ? Colors.Transparent : Pal.BlueDark;
        _outLabel.TextColor = isIn ? Pal.Muted : Colors.White;
        _save.Text = _existing is null ? "Save " + _type : "Save changes";
        _save.BackgroundColor = isIn ? Pal.Green : Pal.BlueDark;
        _summaryValue.TextColor = isIn ? Pal.Green : Pal.BlueDark;
        UpdateSummary();
    }

    private void SetFee(decimal value)
    {
        _settingFee = true;
        _fee.Text = Fmt.Num(value);
        _settingFee = false;
    }

    private void OnAmountChanged()
    {
        if (!_feeEdited && Fmt.TryParse(_amount.Text, out var amount) && amount > 0)
            SetFee(_settings.SuggestFee(amount));
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        Fmt.TryParse(_amount.Text, out var amount);
        Fmt.TryParse(_fee.Text, out var fee);
        if (amount < 0) amount = 0;
        if (fee < 0) fee = 0;
        _feeHint.Text = "Fee · suggested " + Fmt.Money(_settings.SuggestFee(amount > 0 ? amount : 0));
        if (_type == "Cash-In")
        {
            _summaryTitle.Text = "Customer hands you";
            _summaryValue.Text = Fmt.Money(amount + fee);
            _summaryNote.Text = "You send " + Fmt.Money(amount) + " by GCash and keep the " + Fmt.Money(fee) + " fee.";
        }
        else
        {
            _summaryTitle.Text = "Hand customer";
            _summaryValue.Text = Fmt.Money(amount);
            _summaryNote.Text = "You receive " + Fmt.Money(amount) + " by GCash. Collect the " + Fmt.Money(fee) + " fee separately.";
        }
    }

    private async Task SaveAsync()
    {
        if (!Fmt.TryParse(_amount.Text, out var amount) || amount <= 0)
        {
            await DisplayAlertAsync("Invalid amount", "Enter an amount greater than zero, such as 200 or 3000.50.", "OK");
            return;
        }
        if (!Fmt.TryParse(_fee.Text, out var fee) || fee < 0)
        {
            await DisplayAlertAsync("Invalid fee", "Enter a fee of zero or more.", "OK");
            return;
        }
        amount = Math.Round(amount, 2);
        fee = Math.Round(fee, 2);

        _save.IsEnabled = false;
        try
        {
            if (_existing is null)
            {
                var record = new TransactionRecord
                {
                    CustomerName = (_customer.Text ?? "").Trim(),
                    Type = _type,
                    Amount = amount,
                    Fee = fee,
                    ReferenceNumber = (_reference.Text ?? "").Trim(),
                    Notes = (_notes.Text ?? "").Trim(),
                    CreatedAt = DateTime.Now
                };
                await _db.SaveTransactionAsync(record); // record.Id is set by SQLite here
                await Navigation.PopModalAsync();

                if (Shell.Current is not null)
                {
                    var share = await Shell.Current.DisplayAlertAsync(
                        record.Type + " saved",
                        "Receipt TX-" + record.Id.ToString("000000") + "\nAmount: " + Fmt.Money(record.Amount) + "\nFee: " + Fmt.Money(record.Fee) + "\n\nShare a receipt now?",
                        "Share receipt", "Done");
                    if (share)
                    {
                        var settings = await _db.GetSettingsAsync();
                        await ReceiptService.ShareAsync(settings, record);
                    }
                }
            }
            else
            {
                _existing.CustomerName = (_customer.Text ?? "").Trim();
                _existing.Amount = amount;
                _existing.Fee = fee;
                _existing.ReferenceNumber = (_reference.Text ?? "").Trim();
                _existing.Notes = (_notes.Text ?? "").Trim();
                _existing.EditedAt = DateTime.Now;
                await _db.UpdateTransactionAsync(_existing);
                await Navigation.PopModalAsync();
            }
        }
        finally
        {
            _save.IsEnabled = true;
        }
    }
}
