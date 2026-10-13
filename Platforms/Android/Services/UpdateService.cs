using Android.Content;
using Android.Net;
using Android.Widget;
using Java.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace CashLedgerAndroid.Platforms.Android.Services;

public class UpdateService : CashLedgerAndroid.Services.IUpdateService
{
    readonly Context _context = global::Android.App.Application.Context;
    readonly HttpClient _http = new HttpClient();
    readonly CashLedgerAndroid.Services.UpdateState _state;
    private System.Threading.CancellationTokenSource? _cts;
    private string? _currentDestPath;

    public UpdateService(CashLedgerAndroid.Services.UpdateState state)
    {
        _state = state;
    }

    public async Task<CashLedgerAndroid.Services.UpdateInfo?> CheckForUpdateAsync(string metadataUrl)
    {
        try
        {
            var txt = await _http.GetStringAsync(metadataUrl);
            using var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;
            var versionCode = root.GetProperty("versionCode").GetInt32();
            var versionName = root.GetProperty("versionName").GetString() ?? string.Empty;
            var apkUrl = root.GetProperty("apkUrl").GetString() ?? string.Empty;
            string? sha = null;
            if (root.TryGetProperty("sha256", out var shaProp)) sha = shaProp.GetString();

            var pkg = _context.PackageManager?.GetPackageInfo(_context.PackageName ?? string.Empty, 0);
            if (pkg == null) return null;
            var current = pkg.LongVersionCode; // versionCode on Android
            if (versionCode > current)
            {
                var info = new CashLedgerAndroid.Services.UpdateInfo(versionCode, versionName, apkUrl, sha);
                // update shared state
                try { _state.SetLatest(info); } catch { }
                return info;
            }

        }
        catch { }
        return null;
    }

    public async Task StartUpdateAsync(CashLedgerAndroid.Services.UpdateInfo info)
    {
        try
        {
            _cts?.Cancel();
            _cts = new System.Threading.CancellationTokenSource();
            // Download APK into external files dir
            var fileName = $"cashledger-update-{info.VersionCode}.apk";
            var dest = new Java.IO.File(_context.GetExternalFilesDir(global::Android.OS.Environment.DirectoryDownloads), fileName);
            if (dest.Exists()) dest.Delete();

            using (var resp = await _http.GetAsync(info.ApkUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
            {
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var fs = new FileStream(dest.AbsolutePath, FileMode.CreateNew, FileAccess.Write);
                _currentDestPath = dest.AbsolutePath;
                // copy with progress
                var buffer = new byte[81920];
                long totalRead = 0;
                var contentLength = resp.Content.Headers.ContentLength ?? -1L;
                int read;
                _state.SetDownloading(true);
                try
                {
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        if (contentLength > 0)
                        {
                            var percent = (double)totalRead / contentLength * 100.0;
                            try { _state.SetProgress(percent); } catch { }
                        }
                    }
                }
                finally
                {
                    _state.SetDownloading(false);
                    _cts = null;
                    _currentDestPath = null;
                }
            }

            // Verify sha256 if provided
            if (!string.IsNullOrEmpty(info.Sha256))
            {
                using var sha = SHA256.Create();
                using var fs = System.IO.File.OpenRead(dest.AbsolutePath);
                var hash = sha.ComputeHash(fs);
                var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                if (!hex.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Toast.MakeText(_context, "Downloaded update failed integrity check.", ToastLength.Long).Show();
                    try { _state.NotifyResult("Integrity check failed", false); } catch { }
                    return;
                }
            }

            // Get content URI via FileProvider (AndroidX)
            var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(_context, _context.PackageName + ".fileprovider", dest);

            // Launch installer intent
#pragma warning disable CA1422
            var intent = new Intent(Intent.ActionInstallPackage);
#pragma warning restore CA1422
            intent.SetData(apkUri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
            _context.StartActivity(intent);
            try { _state.NotifyResult("Update downloaded", true); } catch { }
        }
        catch (Exception ex)
        {
            Toast.MakeText(_context, "Update failed: " + ex.Message, ToastLength.Long).Show();
            try { _state.SetDownloading(false); _state.NotifyResult("Update failed: " + ex.Message, false); } catch { }
        }
    }

    public Task CancelCurrentUpdateAsync()
    {
        try
        {
            _cts?.Cancel();
            // delete partial download if exists
            try
            {
                if (!string.IsNullOrEmpty(_currentDestPath) && System.IO.File.Exists(_currentDestPath))
                {
                    System.IO.File.Delete(_currentDestPath);
                    _currentDestPath = null;
                }
            }
            catch { }
            return Task.CompletedTask;
        }
        catch { return Task.CompletedTask; }
    }
}
