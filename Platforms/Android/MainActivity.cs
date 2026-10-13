using Android.App;
using Android.Content.PM;
using Android.OS;

namespace CashLedgerAndroid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return;
#pragma warning disable CA1422
        Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0A47BE"));
#pragma warning restore CA1422
    }
}
