using CashLedgerAndroid.Models;
using CashLedgerAndroid.Services;
using CashLedgerAndroid.Views;
using Microsoft.Maui.Controls.Shapes;

namespace CashLedgerAndroid.Pages;

public class HomePage : ContentPage
{
    private readonly LedgerDatabase _db;
    private readonly Action _dataChangedHandler;
    private bool _isSubscribed;
    private List<TransactionRecord> _all = new List<TransactionRecord>();

    private readonly Label _initials = Ui.Text("AD", 18, true, Colors.White, TextAlignment.Center);
    private readonly Label _date = Ui.Text("", 14, false, Pal.Muted, TextAlignment.End);
    private readonly Label _fees = Ui.Text("₱0", 42, true);
    private readonly Label _inTotal = Ui.Text("₱0", 22, true);
    private readonly Label _inCount = Ui.Text("0 transactions", 14, false, Pal.Muted);
    private readonly Label _outTotal = Ui.Text("₱0", 22, true);
    private readonly Label _outCount = Ui.Text("0 transactions", 14, false, Pal.Muted);
    private readonly VerticalStackLayout _recent = new VerticalStackLayout { Spacing = 0 };
    private readonly VerticalStackLayout _recentSection = new VerticalStackLayout { Spacing = 10 };
    private readonly VerticalStackLayout _welcome = new VerticalStackLayout { Spacing = 10, IsVisible = false };

    public HomePage(LedgerDatabase db)
    {
        _db = db;
        _dataChangedHandler = () => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
        Title = "Home";
        SetDynamicResource(BackgroundColorProperty, "PageBackgroundColor");
        Shell.SetNavBarIsVisible(this, false);
        Content = BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_isSubscribed)
        {
            _db.DataChanged += _dataChangedHandler;
            _isSubscribed = true;
        }

        // Fade-in animation for a polished entrance
        try
        {
            if (Content is View root)
            {
                root.Opacity = 0;
                await RefreshAsync();
                // Use the async animation APIs which are the recommended modern replacements
                var fade = root.FadeToAsync(1, 350, Easing.CubicOut);
                var translate = root.TranslateToAsync(0, 0, 350, Easing.CubicOut);
                await Task.WhenAll(fade, translate);
                return;
            }
        }
        catch { }

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

    private View BuildLayout()
    {
        _initials.VerticalTextAlignment = TextAlignment.Center;

        // ---- Blue header ----
        var header = new Grid
        {
            Padding = new Thickness(18, 22, 18, 78),
            RowSpacing = 14,
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            Background = new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(Pal.BlueDark, 0f), new GradientStop(Pal.Blue, 1f) },
                new Point(0, 0), new Point(1, 1))
        };
        var top = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
        };
        var avatar = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            StrokeThickness = 3,
            Stroke = new SolidColorBrush(Color.FromRgba(255, 255, 255, 120)),
            BackgroundColor = Color.FromRgba(255, 255, 255, 45),
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Content = _initials
        };
        top.Add(avatar, 0, 0);
        var ownerLabel = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        ownerLabel.Add(Ui.Text("Administrator", 16, true, Colors.White));
        top.Add(ownerLabel, 1, 0);
        header.Add(top, 0, 0);

        // ---- Totals card that overlaps the header ----
        var dateRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        dateRow.Add(Ui.Text("Fees today", 17, true, Pal.Muted), 0, 0);
        dateRow.Add(_date, 1, 0);

        var left = new VerticalStackLayout { Spacing = 4 };
        left.Add(LabelWithBadge("↓", "Cash-in", Pal.GreenBg, Pal.Green));
        left.Add(_inTotal);
        left.Add(_inCount);
        var right = new VerticalStackLayout { Spacing = 4 };
        right.Add(LabelWithBadge("↑", "Cash-out", Pal.BlueBg, Pal.BlueDark));
        right.Add(_outTotal);
        right.Add(_outCount);
        var columns = new Grid
        {
            ColumnSpacing = 16,
            Margin = new Thickness(0, 14, 0, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(1)),
                new ColumnDefinition(GridLength.Star)
            }
        };
        columns.Add(left, 0, 0);
        columns.Add(new BoxView { Color = Pal.Line, WidthRequest = 1 }, 1, 0);
        columns.Add(right, 2, 0);

        var totalsStack = new VerticalStackLayout { Spacing = 6 };
        totalsStack.Add(dateRow);
        totalsStack.Add(_fees);
        totalsStack.Add(Ui.Divider());
        totalsStack.Add(columns);

        var totalsCard = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            Padding = new Thickness(20, 18, 20, 4),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
            Content = totalsStack
        };
        totalsCard.SetDynamicResource(VisualElement.BackgroundColorProperty, "CardBackgroundColor");

        // Torn-receipt edge under the card
        var points = new PointCollection();
        for (var i = 0; i < 20; i++)
        {
            points.Add(new Point(i * 20, 0));
            points.Add(new Point(i * 20 + 10, 10));
        }
        points.Add(new Point(400, 0));
        var zigzag = new Polygon
        {
            Points = points,
            Fill = new SolidColorBrush(Colors.White),
            Aspect = Stretch.Fill,
            HeightRequest = 10,
            HorizontalOptions = LayoutOptions.Fill
        };
        if (zigzag.Fill is SolidColorBrush zigzagBrush)
            zigzagBrush.SetDynamicResource(SolidColorBrush.ColorProperty, "CardBackgroundColor");

        var totalsBlock = new VerticalStackLayout { Spacing = 0, Margin = new Thickness(0, -62, 0, 0) };
        totalsBlock.Add(totalsCard);
        totalsBlock.Add(zigzag);

        // ---- Big action tiles ----
        var tiles = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
        };
        tiles.Add(Tile("↓", "Cash-in", "Get cash, send GCash", Color.FromArgb("#087F49"), Color.FromArgb("#0A9456"), Pal.Green,
            () => OpenEntryAsync("Cash-In")), 0, 0);
        tiles.Add(Tile("↑", "Cash-out", "Get GCash, give cash", Color.FromArgb("#0A47BE"), Color.FromArgb("#1A6BF0"), Pal.BlueDark,
            () => OpenEntryAsync("Cash-Out")), 1, 0);

        // ---- Recent ----
        var recentHeader = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        recentHeader.Add(Ui.Text("Recent", 21, true), 0, 0);
        var seeAll = Ui.Text("See all", 15, true, Pal.Blue, TextAlignment.End);
        seeAll.VerticalOptions = LayoutOptions.Center;
        var seeAllTap = new TapGestureRecognizer();
        seeAllTap.Tapped += (_, _) => AppShell.ShowTab(1);
        seeAll.GestureRecognizers.Add(seeAllTap);
        recentHeader.Add(seeAll, 1, 0);
        _recentSection.Add(recentHeader);
        _recentSection.Add(Ui.Card(_recent, 20, new Thickness(14, 0)));

        // ---- Welcome card, shown until the first transaction ----
        _welcome.Add(Ui.Text("Welcome to PesoPal.", 20, true));
        _welcome.Add(Ui.Text("Review your fee table before your first transaction.", 13, false, Pal.Muted));
        var steps = new VerticalStackLayout { Spacing = 0 };
        steps.Add(StepRow("1", "Check your fee table", "Review", OpenFeesAsync));
        _welcome.Add(Ui.Card(steps, 20, new Thickness(16, 2)));
        _welcome.Add(Ui.Text("Your records stay on this phone. No account or internet needed.", 12, false, Pal.Muted, TextAlignment.Center));

        var body = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 14, 16, 24) };
        body.Add(tiles);
        body.Add(_welcome);
        body.Add(_recentSection);

        var page = new VerticalStackLayout { Spacing = 0 };
        page.Add(header);
        page.Add(totalsBlock);
        page.Add(body);
        return new ScrollView { Content = page };
    }

    private static View LabelWithBadge(string arrow, string text, Color tint, Color ink)
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        var badge = new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            StrokeThickness = 0,
            BackgroundColor = tint,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = arrow,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = ink,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        var label = Ui.Text(text, 18, true, Color.FromArgb("#3B4B66"));
        label.VerticalOptions = LayoutOptions.Center;
        row.Add(badge);
        row.Add(label);
        return row;
    }

    private static Border Tile(string arrow, string title, string subtitle, Color from, Color to, Color arrowInk, Func<Task> onTap)
    {
        var icon = new Border
        {
            WidthRequest = 48,
            HeightRequest = 48,
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new Label
            {
                Text = arrow,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = arrowInk,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(Ui.Text(title, 16, true, Colors.White));
        text.Add(Ui.Text(subtitle, 12, true, Colors.White));

        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            HeightRequest = 100
        };
        grid.Add(icon, 0, 0);
        grid.Add(text, 0, 1);

        var tile = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(16),
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Background = new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(from, 0f), new GradientStop(to, 1f) },
                new Point(0, 0), new Point(1, 1)),
            Content = grid
        };
        SemanticProperties.SetDescription(tile, "Record a " + title + " transaction");
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            try
            {
                await tile.ScaleToAsync(0.97, 90, Easing.CubicIn);
                await tile.ScaleToAsync(1.0, 120, Easing.CubicOut);
            }
            catch { }
            await onTap();
        };
        tile.GestureRecognizers.Add(tap);
        return tile;
    }

    private static View StepRow(string number, string text, string action, Func<Task> onTap)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            Padding = new Thickness(0, 12)
        };
        var badge = new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            StrokeThickness = 0,
            BackgroundColor = Pal.Blue,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new Label
            {
                Text = number,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        var label = Ui.Text(text, 14, true);
        label.VerticalOptions = LayoutOptions.Center;
        var go = Ui.Text(action + " ›", 14, true, Pal.Blue, TextAlignment.End);
        go.VerticalOptions = LayoutOptions.Center;
        row.Add(badge, 0, 0);
        row.Add(label, 1, 0);
        row.Add(go, 2, 0);
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await onTap();
        row.GestureRecognizers.Add(tap);
        return row;
    }

    private async Task OpenEntryAsync(string type)
    {
        await Navigation.PushModalAsync(new EntryPage(_db, type));
    }

    private async Task OpenFeesAsync()
    {
        await Navigation.PushAsync(new FeeTablePage(_db));
    }

    public async Task RefreshAsync()
    {
        try
        {
            _all = await _db.GetTransactionsAsync();

            _initials.Text = "AD";
            _date.Text = DateTime.Now.ToString("MMM d, yyyy", Fmt.Inv);

            var businessDayStart = _db.GetBusinessDayStart();
            var today = _all.Where(t => !t.IsVoided && t.CreatedAt >= businessDayStart).ToList();
            var ins = today.Where(t => t.IsCashIn).ToList();
            var outs = today.Where(t => !t.IsCashIn).ToList();
            _fees.Text = Fmt.MoneyShort(today.Sum(t => t.Fee));
            _inTotal.Text = Fmt.MoneyShort(ins.Sum(t => t.Amount));
            _outTotal.Text = Fmt.MoneyShort(outs.Sum(t => t.Amount));
            _inCount.Text = ins.Count + (ins.Count == 1 ? " transaction" : " transactions");
            _outCount.Text = outs.Count + (outs.Count == 1 ? " transaction" : " transactions");

            _recent.Clear();
            var latest = _all.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Take(5).ToList();
            for (var i = 0; i < latest.Count; i++)
            {
                _recent.Add(TransactionRow.Create(latest[i], ShowMenuAsync));
                if (i < latest.Count - 1) _recent.Add(Ui.Divider());
            }
            _welcome.IsVisible = _all.Count == 0;
            _recentSection.IsVisible = _all.Count > 0;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not open your records", ex.Message, "OK");
        }
    }

    private Task ShowMenuAsync(TransactionRecord t) => TransactionActions.ShowMenuAsync(this, _db, t);
}
