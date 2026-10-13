using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CashLedgerAndroid.Pages;

// Code-only animated loading/splash page used during app startup.
public class LoadingPage : ContentPage
{
    readonly Image _logo;
    readonly Label _nameLabel;
    readonly Label _cursorLabel;
    readonly Label _subtitleLabel;
    readonly ActivityIndicator _spinner;

    CancellationTokenSource? _breathingCts;
    Task? _breathingTask;
    CancellationTokenSource? _cursorCts;

    public LoadingPage()
    {
        BackgroundColor = Color.FromArgb("#0057E0");
        // Logo image
        _logo = new Image { Source = "pesopal_logo.svg", Opacity = 0, Scale = 0.6, HorizontalOptions = LayoutOptions.Center };

        // Name label (formatted spans)
        var fs = new FormattedString();
        var spanPeso = new Span { Text = string.Empty, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var spanPal = new Span { Text = string.Empty, TextColor = Color.FromArgb("#FFC21A"), FontAttributes = FontAttributes.Bold };
        fs.Spans.Add(spanPeso);
        fs.Spans.Add(spanPal);
        _nameLabel = new Label
        {
            FormattedText = fs,
            FontSize = 44,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1,
            HorizontalTextAlignment = TextAlignment.Center,
            Opacity = 1,
        };
        _cursorLabel = new Label { Text = "|", TextColor = Colors.White, FontSize = 44, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1, Opacity = 0 };

        var nameRow = new HorizontalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.Center,
            Children = { _nameLabel, _cursorLabel }
        };

        _subtitleLabel = new Label
        {
            Text = "CASH LEDGER",
            FontSize = 14,
            CharacterSpacing = 6,
            TextColor = Color.FromArgb("#CFE3FF"),
            HorizontalTextAlignment = TextAlignment.Center,
            Opacity = 0
        };

        _spinner = new ActivityIndicator { IsRunning = true, Color = Colors.White, IsVisible = true, Opacity = 0 };

        var stack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 12,
            Children = { _logo, nameRow, _subtitleLabel, _spinner }
        };

        Content = stack;
    }

    // Play the intro animations (logo entrance, typing, subtitle+spinner).
    // This starts the breathing pulse and cursor blink; breathing continues until StopAndFadeOutAsync is called.
    public async Task PlayIntroAsync()
    {
        try
        {
            // Logo entrance (fade + scale)
            await Task.WhenAll(
                _logo.FadeToAsync(1, 600, Easing.SpringOut),
                _logo.ScaleToAsync(1, 600, Easing.SpringOut)
            );

            // start breathing pulse in background (runs until canceled)
            _breathingCts = new CancellationTokenSource();
            _breathingTask = Task.Run(async () =>
            {
                var ct = _breathingCts.Token;
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await _logo.ScaleToAsync(1.05, 800, Easing.SinInOut);
                        if (ct.IsCancellationRequested) break;
                        await _logo.ScaleToAsync(1.0, 800, Easing.SinInOut);
                    }
                    catch { break; }
                }
            }, _breathingCts.Token);

            // Typewriter effect for "PesoPal"
            var text = "PesoPal";
            _cursorCts = new CancellationTokenSource();
            var cursorToken = _cursorCts.Token;
            // start blinking cursor
            _ = Task.Run(async () =>
            {
                while (!cursorToken.IsCancellationRequested)
                {
                    try { await _cursorLabel.FadeToAsync(1, 120); } catch { }
                    await Task.Delay(480, cursorToken).ContinueWith(_ => { });
                    try { await _cursorLabel.FadeToAsync(0, 120); } catch { }
                    await Task.Delay(480, cursorToken).ContinueWith(_ => { });
                }
            }, cursorToken);

            // Build spans progressively
            var fs = _nameLabel.FormattedText;
            var spanPeso = fs.Spans[0];
            var spanPal = fs.Spans[1];
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (i < 4)
                    spanPeso.Text += c;
                else
                    spanPal.Text += c;

                await Task.Delay(110);
            }

            // stop cursor blinking and hide cursor
            try { _cursorCts?.Cancel(); } catch { }
            await MainThread.InvokeOnMainThreadAsync(() => _cursorLabel.Opacity = 0);

            // Fade in subtitle and spinner
            await Task.WhenAll(
                _subtitleLabel.FadeToAsync(1, 400),
                _spinner.FadeToAsync(1, 400)
            );
        }
        catch
        {
            // Swallow animation errors to avoid blocking startup
        }
    }

    // Stop breathing, cancel any animations, fade out the page, and return when done.
    public async Task StopAndFadeOutAsync()
    {
        try
        {
            // cancel breathing and cursor
            try { _breathingCts?.Cancel(); } catch { }
            try { _cursorCts?.Cancel(); } catch { }

            // Cancel running view animations
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try { this.AbortAnimation("loading"); } catch { }
                try { Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_logo); } catch { }
                try { Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_nameLabel); } catch { }
                try { Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_subtitleLabel); } catch { }
                try { Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_spinner); } catch { }
            });

            // fade out whole page
            await this.FadeToAsync(0, 250);
        }
        catch
        {
            // ignore
        }
    }
}
