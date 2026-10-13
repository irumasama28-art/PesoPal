# PesoPal Release Guide

## Overview

This guide covers the complete release process for PesoPal Android app, including automated CI/CD workflow, version management, and update distribution.

Repository: https://github.com/irumasama28-art/PesoPal

## Quick Release Checklist (5 minutes)

- [ ] Update `CashLedgerAndroid.csproj` with new `AndroidVersionCode` (Unix timestamp)
- [ ] Commit: `git add . && git commit -m "Bump version for vX.Y.Z"`
- [ ] Tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z - Description"`
- [ ] Push: `git push origin vX.Y.Z`
- [ ] Monitor: https://github.com/irumasama28-art/PesoPal/actions
- [ ] Verify: Check latest.json at https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
- [ ] Test: Install on device and test update flow

## System Architecture

### Components

1. **GitHub Repository**
   - Main repository: `irumasama28-art/PesoPal`
   - Branches: `main`, `gh-pages`
   - Tags: Semantic versioning (v1.0.0, v1.1.0, etc.)

2. **GitHub Actions Workflow**
   - File: `.github/workflows/publish-release.yml`
   - Triggers: When `v*` tag is pushed
   - Jobs:
	 - Validate tag format
	 - Build and sign APK
	 - Create GitHub Release
	 - Generate latest.json
	 - Push to gh-pages

3. **Update Server**
   - Hosted on GitHub gh-pages
   - File: `latest.json`
   - Format: Contains download URL, version info, and SHA-256 hash
   - Publicly accessible via raw.githubusercontent.com

4. **UpdateService (App-Side)**
   - Checks latest.json on app startup
   - Compares local version with remote version
   - Downloads and installs APK if newer version available

## Version Management

### AndroidVersionCode

Used by Android to determine if update is newer. Must be **strictly increasing**.

```
Current: [int](Get-Date -UFormat %s)  # Unix timestamp in seconds
Example: 1791820575
```

**IMPORTANT:** Always increment before release. Never reuse same code for different APKs.

### ApplicationVersion (Not Used)

Currently `1` in csproj. Can remain as is - AndroidVersionCode is what matters.

### ApplicationDisplayVersion

Shown to users. Should match tag name without 'v' prefix.

```xml
<ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>  <!-- For v1.0.0 tag -->
```

## Release Process

### Step 1: Pre-Release Checks

```powershell
# Verify you're on main branch
git branch
git status

# Run local build
dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release -o temp-out

# Verify no uncommitted changes
git status
```

### Step 2: Prepare Release

```powershell
# Generate new version code
$newCode = [int](Get-Date -UFormat %s)
Write-Host "New version code: $newCode"

# Edit CashLedgerAndroid.csproj
# Update: <AndroidVersionCode>NEW_CODE</AndroidVersionCode>

# Verify edit
$content = Get-Content CashLedgerAndroid.csproj | Select-String "AndroidVersionCode"
Write-Host $content.Line
```

### Step 3: Commit Changes

```powershell
git add CashLedgerAndroid.csproj
git commit -m "Bump version code for v1.2.3"
git log -1  # Verify commit
```

### Step 4: Create and Push Release Tag

```bash
# Create annotated tag with description
git tag -a v1.2.3 -m "Release v1.2.3 - Added transaction filters, improved performance"

# Verify tag created
git tag -l v1.2.3 -n

# Push tag (this triggers GitHub Actions)
git push origin v1.2.3

# Monitor progress
# Go to: https://github.com/irumasama28-art/PesoPal/actions
```

### Step 5: Monitor Build

Watch the GitHub Actions workflow:

1. Go to https://github.com/irumasama28-art/PesoPal/actions
2. Click the active "Build and Publish APK" workflow
3. Expected duration: 5-10 minutes
4. Check each step completes successfully

**Key steps in workflow:**
1. ✅ Validate tag format
2. ✅ Check secrets configured
3. ✅ Setup .NET 10, Java 17, Android SDK
4. ✅ Build APK
5. ✅ Verify APK file created
6. ✅ Create GitHub Release
7. ✅ Upload APK to release
8. ✅ Generate latest.json
9. ✅ Push latest.json to gh-pages

### Step 6: Verify Release

Verify all release artifacts are in place:

```bash
# Release page
https://github.com/irumasama28-art/PesoPal/releases/tag/v1.2.3
# Should see: APK file listed as asset

# latest.json
curl https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
# Should return valid JSON with:
# - versionCode: Unix timestamp (must be larger than previous)
# - versionName: "v1.2.3"
# - apkUrl: Download URL
# - sha256: Hash of APK
# - buildTime: ISO 8601 timestamp
```

### Step 7: Test on Device

```bash
# Install old version on test device/emulator
# Open app
# Go to Settings tab
# Tap "Check for updates"
# Should show UPDATE AVAILABLE notification within 2-3 seconds
# Tap notification or "Check for updates" again
# Download should start
# Android system installer shows
# Tap "Install"
# App uninstalls old, installs new
# App relaunches automatically
# Verify all data intact (transactions, settings)
```

## Deployment Architecture

### GitHub Pages (Distribution)

```
GitHub Repository
├── /
│   ├── CashLedgerAndroid.slnx
│   ├── CashLedgerAndroid.csproj
│   └── .github/workflows/publish-release.yml
│
├── gh-pages branch (SEPARATE)
│   └── latest.json
│       └── Public URL: https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
│
└── Releases
	└── v1.2.3
		└── CashLedgerAndroid-Signed.apk
```

### Update Flow

```
1. App Starts
   ↓
2. UpdateService.OnAppStarting() called
   ↓
3. Fetch: https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
   ↓
4. Parse JSON
   ↓
5. Compare: remote.versionCode > local.versionCode?
   ↓ YES
6. Show UPDATE AVAILABLE notification
   ↓
7. User taps "Check for updates"
   ↓
8. Download APK from apkUrl
   ↓
9. Verify SHA-256 matches
   ↓
10. Start Android installer
   ↓
11. User taps Install
   ↓
12. Old app uninstalled
   ↓
13. New app installed
   ↓
14. App launches automatically
```

## latest.json Format

```json
{
  "versionCode": 1791820575,
  "versionName": "v1.2.3",
  "apkUrl": "https://github.com/irumasama28-art/PesoPal/releases/download/v1.2.3/CashLedgerAndroid-Signed.apk",
  "sha256": "abc123def456789...",
  "buildTime": "2024-12-30T15:30:45.1234567+00:00"
}
```

**Fields:**
- `versionCode`: Unix timestamp (Android uses this to determine if newer)
- `versionName`: User-facing version (v1.2.3)
- `apkUrl`: Direct download link to signed APK
- `sha256`: SHA-256 hash for integrity verification
- `buildTime`: When APK was built (ISO 8601)

## Troubleshooting

### Issue: Workflow Failed - "Validate secrets are configured"

**Symptom:** Workflow stops at validation step

**Cause:** One or more GitHub Actions secrets are missing

**Solution:**
1. Go to: Settings → Secrets and variables → Actions
2. Verify all 4 secrets exist and are non-empty:
   - `ANDROID_KEYSTORE_PATH` - base64 encoded keystore file
   - `ANDROID_KEY_ALIAS` - e.g., `pesopa_key`
   - `ANDROID_KEY_PASSWORD` - private key password
   - `ANDROID_KEYSTORE_PASSWORD` - keystore password
3. If missing, add secret and re-push tag:
   ```bash
   git push origin --delete v1.2.3
   git push origin v1.2.3
   ```

### Issue: No APK Created - Build Step Failed

**Symptom:** Workflow fails at "Find APK" step

**Cause:** .NET/Android build failed, but error not clear

**Solution:**
1. Check workflow logs for actual error (scroll up in build step)
2. Common causes:
   - Project file syntax error
   - Missing dependencies
   - Android SDK issue
3. Test locally:
   ```bash
   dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release -o artifacts/publish
   ```
4. Fix error, re-push tag

### Issue: latest.json Not Updated

**Symptom:** Workflow succeeds but latest.json doesn't have new version

**Cause:** 
- gh-pages branch doesn't exist
- Push to gh-pages failed (auth issue)
- Script error

**Solution:**
1. Verify gh-pages branch exists:
   ```bash
   git branch -a | grep gh-pages
   ```
2. If missing, create it:
   ```bash
   git checkout --orphan gh-pages
   touch .gitkeep
   git add .gitkeep
   git commit -m "Initial gh-pages commit"
   git push -u origin gh-pages
   git checkout main
   ```
3. Check workflow logs for script errors
4. Re-push tag to retry

### Issue: Users Can't Update

**Symptom:** App shows "No updates available"

**Causes & Solutions:**

1. **latest.json not accessible**
   - Test: `curl https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json`
   - Fix: Republish to gh-pages

2. **versionCode not larger**
   - Check: Current APK versionCode < last release
   - Fix: Ensure you updated AndroidVersionCode before release

3. **Download URL invalid**
   - Test: Visit apkUrl in browser, should download APK
   - Fix: Verify APK uploaded to release

4. **SHA-256 mismatch**
   - Cause: APK corrupted during upload or hash computed incorrectly
   - Fix: Re-release

5. **Network issues**
   - Device offline or no connectivity
   - UpdateService only checks on app startup

## Advanced Topics

### Manual Release (If Workflow Broken)

```powershell
# Step 1: Build locally
dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release `
  -o artifacts/publish `
  /p:AndroidSigningKeyStore="path/to/cashledger.keystore" `
  /p:AndroidSigningKeyAlias="pesopa_key" `
  /p:AndroidSigningKeyPass="YOUR_PASSWORD" `
  /p:AndroidSigningStorePass="YOUR_PASSWORD"

# Step 2: Get APK path
$apkPath = (Get-ChildItem artifacts/publish/*.apk | Select-Object -First 1).FullName

# Step 3: Compute SHA-256
$hash = Get-FileHash $apkPath -Algorithm SHA256 | Select-Object -ExpandProperty Hash
Write-Host "SHA-256: $hash"

# Step 4: Create release on GitHub (use web UI)
# https://github.com/irumasama28-art/PesoPal/releases/new

# Step 5: Upload APK to release (drag & drop in web UI)

# Step 6: Generate latest.json
pwsh -File tools/generate-latest.ps1 `
  -Tag v1.2.3 `
  -AssetPath $apkPath `
  -Repo irumasama28-art/PesoPal `
  -Branch gh-pages `
  -Push
```

### Rollback Release

If you need to revert to previous version:

```bash
# Option 1: Delete release (users won't see new version)
# Go to: https://github.com/irumasama28-art/PesoPal/releases/tag/v1.2.3
# Click "Delete release"

# Option 2: Revert latest.json to previous version
git clone --branch gh-pages https://github.com/irumasama28-art/PesoPal.git gh-rollback
cd gh-rollback
git log --oneline latest.json  # Show history
git checkout PREVIOUS_COMMIT latest.json  # Revert to previous
git commit -m "Rollback to v1.2.2"
git push origin gh-pages
```

### Setting Update Check Frequency

Currently app checks for updates on every startup. To change:

Edit `App.xaml.cs` or wherever `UpdateService` initialized:

```csharp
// Check every 24 hours instead of every startup
var updateService = new UpdateService();
updateService.CheckFrequency = TimeSpan.FromHours(24);
```

### Signing APK Locally

If you need to sign APK locally (for testing):

```bash
jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 \
  -keystore cashledger.keystore \
  -storepass YOUR_KEYSTORE_PASSWORD \
  app-release.apk pesopa_key

# Or use Android tools (apksigner is preferred)
apksigner sign \
  --ks cashledger.keystore \
  --ks-key-alias pesopa_key \
  --ks-pass pass:YOUR_PASSWORD \
  --key-pass pass:YOUR_KEY_PASSWORD \
  --out app-signed.apk \
  app-release.apk
```

## FAQ

**Q: Can I delete and re-push the same tag?**
A: Yes, but rebuild everything. Use: `git push origin --delete v1.2.3 && git push origin v1.2.3`

**Q: What if versionCode overflows?**
A: Int32 max is 2,147,483,647. Unix timestamp won't overflow until year 2038. Plan accordingly or use different scheme after 2037.

**Q: Can multiple people release simultaneously?**
A: Workflow has concurrency control - only one release at a time to prevent conflicts.

**Q: Where are old APK files stored?**
A: On GitHub Releases page. They don't auto-cleanup. Delete old releases if storage is concern.

**Q: Can I test the workflow without releasing?**
A: Not easily. Workflow only triggers on `v*` tags. Create test tag `v0.0.0-test` to test workflow.

**Q: Is the APK signed automatically?**
A: Yes, GitHub Actions uses secrets to sign during build. Never push unsigned APK.

**Q: Can I have pre-releases?**
A: Create tags like `v1.2.3-beta1` or `v1.2.3-rc1`. they're handled normally but won't be downloaded by default (UpdateService can filter).

---

**Last Updated:** 2024-12-30
**Status:** ✅ Ready for Production
**Repository:** irumasama28-art/PesoPal
