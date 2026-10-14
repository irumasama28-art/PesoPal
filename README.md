# Cash Ledger (Android, .NET 8 MAUI)

Offline cash-in / cash-out ledger. Records stay on the phone (SQLite). No internet permission, no accounts, no PIN.

## Build
1. Install Visual Studio Latest 2026 17.8+ with the ".NET Multi-platform App UI development" workload (or `dotnet workload install maui-android`).
2. Open `CashLedgerAndroid.csproj`, pick an Android device/emulator, press Run.
3. Release APK: `dotnet publish -f net10.0-android -c Release`.

## Features
Home totals (cash-in, cash-out, fees today), quick Cash-in/Cash-out entry with suggested fee, History with search, filters and CSV export, edit / void / delete, HTML receipt sharing, fee table, close-the-day check, backup and restore.

## Notes
- First build downloads the sqlite-net-pcl package (needs internet once).
- Back up regularly: uninstalling the app erases the records.
