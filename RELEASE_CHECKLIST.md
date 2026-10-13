# Release Checklist - First-Time Setup & Publishing

## ✅ Pre-Release Setup (One-Time)

### GitHub Setup
- [ ] Repository created: https://github.com/irumasama28-art/PesoPal
- [ ] `gh-pages` branch created (for hosting `latest.json`)
  ```bash
  git checkout --orphan gh-pages
  touch .gitkeep
  git add .gitkeep
  git commit -m "Initial gh-pages commit"
  git push -u origin gh-pages
  git checkout main (or master)
  ```

### Android Keystore
- [ ] Keystore file generated: `cashledger.keystore` (or similar)
  ```bash
  keytool -genkey -v -keystore cashledger.keystore \
	-keyalg RSA -keysize 2048 -validity 10000 \
	-alias pesopa_key
  ```
- [ ] Keystore stored securely (NOT in git repo)
- [ ] Password written down and kept safe

### GitHub Actions Secrets
Navigate to: **Settings > Secrets and variables > Actions**

Add 4 secrets:
- [ ] `ANDROID_KEYSTORE_PATH` - base64 encoded keystore
  ```bash
  # Windows (PowerShell):
  $keystore = Get-Content -AsByteStream cashledger.keystore
  $encoded = [Convert]::ToBase64String($keystore)
  Set-Clipboard -Value $encoded
  # Paste into GitHub secret

  # Mac/Linux:
  base64 -i cashledger.keystore | pbcopy
  # Or:
  cat cashledger.keystore | base64 | xclip -selection clipboard
  ```

- [ ] `ANDROID_KEY_ALIAS` - e.g., `pesopa_key`
- [ ] `ANDROID_KEY_PASSWORD` - your private key password
- [ ] `ANDROID_KEYSTORE_PASSWORD` - your keystore password

### CashLedgerAndroid.csproj
- [ ] Verify `<AndroidVersionCode>` is set to Unix timestamp
  ```xml
  <AndroidVersionCode>1600000000</AndroidVersionCode>  <!-- Jan 2020 timestamp -->
  ```
- [ ] Verify `<ApplicationTitle>` is `PesoPal`
- [ ] Verify `<ApplicationId>` is `com.pesopal.app`

---

## 🚀 Publishing a Release

### Step 1: Prepare Code
```bash
cd C:\Dev\CashLedgerAndroid

# Update VersionCode for new release
# Edit CashLedgerAndroid.csproj
# OLD: <AndroidVersionCode>1600000000</AndroidVersionCode>
# NEW: <AndroidVersionCode>1700000000</AndroidVersionCode>
#      (use Unix timestamp: date +%s on Linux/Mac, or PowerShell: [int](Get-Date -UFormat %s))

git add CashLedgerAndroid.csproj
git commit -m "Bump version code for v1.2.3"
```

### Step 2: Create and Push Release Tag
```bash
# Tag with semantic version
git tag -a v1.2.3 -m "Release v1.2.3 - Add dashboard widget"

# Push tag (this triggers GitHub Actions automatically)
git push origin v1.2.3
```

### Step 3: Monitor GitHub Actions
1. Go to https://github.com/irumasama28-art/PesoPal/actions
2. Watch the "Build and Publish APK" workflow run
3. Should take 5-10 minutes to complete
4. Check for errors in logs if it fails

### Step 4: Verify Release
1. Go to https://github.com/irumasama28-art/PesoPal/releases/tag/v1.2.3
2. Confirm APK is uploaded as release asset
3. Go to https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
4. Should show:
   ```json
   {
	 "versionCode": 1700000000,
	 "versionName": "v1.2.3",
	 "apkUrl": "https://github.com/irumasama28-art/PesoPal/releases/download/v1.2.3/...-Signed.apk",
	 "sha256": "abc123def456..."
   }
   ```

### Step 5: Test Update
1. Install old version on device/emulator
2. Open app → Settings
3. Tap "Check for updates"
4. Should see UPDATE badge within seconds
5. Tap "Check for updates" again
6. Download should start with progress bar
7. System installer should prompt to upgrade
8. Tap "Install"
9. App should uninstall old version, install new, relaunch
10. Verify data is still there (transactions, settings)

---

## 🔄 Workflow Status

### GitHub Actions Workflow (`publish-release.yml`)

**What it does automatically when you push a tag:**

1. ✅ Checks out code
2. ✅ Sets up .NET SDK
3. ✅ Sets up Java 17 (Android build requirement)
4. ✅ Sets up Android SDK API 34
5. ✅ Restores NuGet packages
6. ✅ Builds and signs APK (using your keystore from secrets)
7. ✅ Creates GitHub Release
8. ✅ Uploads APK to release
9. ✅ Runs PowerShell script to:
   - Compute SHA-256 of APK
   - Generate `latest.json`
   - Push `latest.json` to `gh-pages` branch

**Result:** `latest.json` available at public URL within seconds

---

## 📋 Detailed Workflow Instructions

### GitHub Actions Workflow File Location
```
.github/workflows/publish-release.yml
```

### Key Sections Explained

**Trigger (runs when you push a tag like v1.2.3):**
```yaml
on:
  push:
	tags:
	  - 'v*'  # Matches v1.2.3, v1.0.0, etc.
```

**Build APK with Signing:**
```yaml
- name: Build Android APK
  run: dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release \
	-o artifacts/publish \
	/p:AndroidSigningKeyStore="${{ secrets.ANDROID_KEYSTORE_PATH }}" \
	/p:AndroidSigningKeyAlias="${{ secrets.ANDROID_KEY_ALIAS }}" \
	/p:AndroidSigningKeyPass="${{ secrets.ANDROID_KEY_PASSWORD }}" \
	/p:AndroidSigningStorePass="${{ secrets.ANDROID_KEYSTORE_PASSWORD }}"
```

**Upload to GitHub Release:**
```yaml
- name: Upload APK to Release
  uses: softprops/action-gh-release@v1
  with:
	files: artifacts/publish/*.apk
  env:
	GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Generate & Push latest.json:**
```yaml
- name: Generate latest.json and push to gh-pages
  run: pwsh -File tools/generate-latest.ps1 \
	-Tag ${{ github.ref_name }} \
	-AssetPath artifacts/publish/*.apk \
	-Repo Synonymz/PesoPal \
	-Branch gh-pages \
	-Push \
	-UploadRelease
  env:
	GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## ⚠️ Troubleshooting

### Issue: Workflow fails at "Build Android APK"

**Symptom:** Error like "AndroidSigningKeyStore not found"

**Cause:** Secret not properly decoded or path incorrect

**Fix:**
1. Verify secret is set: Settings > Secrets > check `ANDROID_KEYSTORE_PATH` exists
2. Workflow needs to **decode** base64 secret before using
3. **Current workflow bug:** It's passing base64 string directly instead of decoding

**Temporary fix** (add step to decode):
```yaml
- name: Decode Keystore
  run: |
	$bytes = [Convert]::FromBase64String("${{ secrets.ANDROID_KEYSTORE_PATH }}")
	[IO.File]::WriteAllBytes("cashledger.keystore", $bytes)
```

Then update build step to use local `cashledger.keystore`:
```yaml
/p:AndroidSigningKeyStore="$(pwd)/cashledger.keystore"
```

---

### Issue: latest.json not created / pushed

**Symptom:** 
```
⚠️ File latest.json not found on gh-pages
```

**Cause:** PowerShell script failed or gh-pages branch not set up

**Fix:**
1. Ensure gh-pages branch exists
2. Check GitHub Actions logs for PS script errors
3. Verify GITHUB_TOKEN has proper permissions
4. Manually test script locally:
   ```powershell
   pwsh -File tools/generate-latest.ps1 \
	 -Tag v1.2.3 \
	 -AssetPath "C:\path\to\app-Signed.apk" \
	 -Repo irumasama28-art/PesoPal \
	 -Branch gh-pages \
	 -Push  # Only if git is configured
   ```

---

### Issue: Users say "App not installed" during update

**Symptom:** Android installer shows error, doesn't install

**Cause #1:** Different signing certificate
- Old APK signed with Certificate A
- New APK signed with Certificate B
- Android requires same cert for upgrade

**Fix:** Ensure same keystore/alias used for all builds

**Cause #2:** Corrupt APK or wrong URL in latest.json

**Fix:** Check:
- APK file size is reasonable (>50MB for MAUI)
- SHA-256 matches
- Direct download link works in browser

---

## 🎯 Quick Command Reference

### Local Build & Install
```bash
# Build
dotnet publish -f net10.0-android -c Release -o bin/Release/android

# Get APK path
ls bin/Release/android/*.apk

# Install on device
adb install -r bin/Release/android/*.apk

# Launch app
adb shell am start -n com.pesopal.app/.MainActivity

# View logs
adb logcat | grep pesopal
```

### Manual Release (if GitHub Actions fails)
```bash
# 1. Build APK locally
dotnet publish -f net10.0-android -c Release \
  /p:AndroidPackageFormat=apk \
  -o artifacts/publish \
  /p:AndroidSigningKeyStore="$(pwd)/cashledger.keystore" \
  /p:AndroidSigningKeyAlias="pesopa_key" \
  /p:AndroidSigningKeyPass="YOUR_PASS" \
  /p:AndroidSigningStorePass="YOUR_PASS"

# 2. Upload APK to GitHub Release
---

## 🚀 Quick-Start Release Process (5 min)

### Prerequisites
- ✅ GitHub secrets configured (4 secrets)
- ✅ gh-pages branch exists
- ✅ Android Keystore file stored locally

### Release Steps

**1. Update Version Code**
```powershell
# Generate new Unix timestamp
[int](Get-Date -UFormat %s)  # e.g., 1791820575

# Edit CashLedgerAndroid.csproj and update:
# <AndroidVersionCode>NEW_TIMESTAMP</AndroidVersionCode>
```

**2. Commit & Tag**
```bash
cd C:\Dev\CashLedgerAndroid
git add CashLedgerAndroid.csproj
git commit -m "Bump version for vX.Y.Z"
git tag -a vX.Y.Z -m "Release vX.Y.Z - Description"
git push origin vX.Y.Z
```

**3. Monitor Workflow**
- Go to: https://github.com/irumasama28-art/PesoPal/actions
- Click the "Build and Publish APK" workflow
- Watch progress (5-10 minutes typically)

**4. Verify Release**
- ✅ GitHub Release created: https://github.com/irumasama28-art/PesoPal/releases/tag/vX.Y.Z
- ✅ APK uploaded to release assets
- ✅ latest.json updated: https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json

**5. Test on Device**
- Install old version on test device/emulator
- Settings → Check for updates
- Should show UPDATE available within seconds
- Test install flow end-to-end

---

## 📋 Troubleshooting Release Issues

### ❌ Workflow Failed: "ANDROID_KEYSTORE_PATH secret not configured"
**Solution:**
1. Settings → Secrets and variables → Actions → Check all 4 secrets exist:
   - `ANDROID_KEYSTORE_PATH` (base64 encoded keystore file)
   - `ANDROID_KEY_ALIAS` (e.g., `pesopa_key`)
   - `ANDROID_KEY_PASSWORD` (private key password)
   - `ANDROID_KEYSTORE_PASSWORD` (keystore password)

2. If missing, delete old release, add secrets, and push tag again

### ❌ APK Not Found in Build Step
**Symptom:** "No APK file found in artifacts/publish"

**Solution:**
1. Check build logs for .NET/Android compilation errors
2. Verify `CashLedgerAndroid.csproj` has correct configuration
3. Ensure keystore parameters are not causing silent fail
4. Re-push tag to retry: `git push origin --delete vX.Y.Z && git push origin vX.Y.Z`

### ❌ latest.json Not Created
**Symptom:** https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json returns 404

**Solution:**
1. Verify gh-pages branch exists: `git branch -a | grep gh-pages`
2. Check if initial commit is on gh-pages
3. Manually push to trigger script:
   ```bash
   git clone --branch gh-pages https://github.com/irumasama28-art/PesoPal.git gh-test
   cd gh-test
   # gh-pages should have latest.json file
   ```

### ❌ Wrong Repository URL in Workflow
**Symptom:** Release goes to Synonymz/PesoPal instead of irumasama28-art/PesoPal

**Solution:**
- Workflow already configured to use `irumasama28-art/PesoPal`
- If you changed fork location, update `.github/workflows/publish-release.yml`:
  ```yaml
  -Repo irumasama28-art/PesoPal
  ```

---

## 📝 Release Changelog Template

When creating a release tag, use this format:

```
Release vX.Y.Z - Your App Name

What's New:
- Feature 1 description
- Feature 2 description
- Bug fix 1 description

Improvements:
- Performance improvement 1
- UI improvement 1

Known Issues:
- (If any, list here)

Installation:
1. Settings → Check for updates
2. Tap "Install" when prompted
3. App will restart automatically
```

---

## 💡 Release Workflow Enhanced Features (v2.0)

The GitHub Actions workflow now includes:

✅ **Tag Format Validation**
- Ensures tags match semantic versioning (vX.Y.Z)
- Fails early if format is invalid

✅ **Secret Pre-Flight Check**
- Validates all 4 required secrets before build
- Clear error messages if any are missing

✅ **Better Logging**
- Timestamps on all operations
- Emoji indicators (✅, ❌, 📝, etc.)
- Clearer progress messages

✅ **APK Verification**
- Confirms APK file actually created
- Lists file size for verification
- Fails with clear error if APK missing

✅ **Release Summary**
- Automatic summary on success/failure
- Direct links to release and latest.json
- Useful for CI/CD dashboards

✅ **Better Error Handling**
- Graceful failure messages
- Exit codes for automation
- PowerShell script with improved logging

---

## 🎯 Best Practices

1. **Always test locally first**
   - Build locally to catch errors before CI
   - `dotnet publish -f net10.0-android -c Release`

2. **Use meaningful commit messages**
   - `git commit -m "Bump version for v1.2.3"`
   - `git tag -a v1.2.3 -m "Release v1.2.3 - Added transaction filters"`

3. **Monitor first release carefully**
   - Allow time for workflow to complete
   - Check latest.json is valid JSON
   - Test update on actual device

4. **Keep secrets secure**
   - Never commit keystore file
   - Never output secrets in logs
   - Rotate passwords periodically

5. **Document breaking changes**
   - If database schema changes, note in Release notes
   - If API format changes, update update system docs

---

## 🔗 Useful Workflow Links

- **Workflow File:** `.github/workflows/publish-release.yml`
- **PowerShell Script:** `tools/generate-latest.ps1`
- **Release History:** https://github.com/irumasama28-art/PesoPal/releases
- **Workflow Runs:** https://github.com/irumasama28-art/PesoPal/actions/workflows/publish-release.yml
- **latest.json:** https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json

---

## 🐛 Manual Release (If Workflow Fails)

If GitHub Actions workflow is broken, you can manually release:

```powershell
# 1. Build locally
dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release `
  -o artifacts/publish `
  /p:AndroidSigningKeyStore="path/to/cashledger.keystore" `
  /p:AndroidSigningKeyAlias="pesopa_key" `
  /p:AndroidSigningKeyPass="****" `
  /p:AndroidSigningStorePass="****"

# 2. Create release on GitHub (use web UI or gh CLI)
gh release create v1.2.3 artifacts/publish/*.apk --latest --title "v1.2.3"

# 3. Generate and push latest.json
pwsh -File tools/generate-latest.ps1 `
  -Tag v1.2.3 `
  -AssetPath artifacts/publish/*.apk `
  -Repo irumasama28-art/PesoPal `
  -Branch gh-pages `
  -Push
```

---

## 🎓 Release Workflow Basics

### What Happens When You Push a Tag

1. **GitHub detects tag:** v1.2.3 pushed
2. **Workflow triggers:** `publish-release.yml` starts
3. **Validation:** Tag format checked (vX.Y.Z)
4. **Secrets check:** All 4 secrets verified
5. **Build:** Android APK built and signed
6. **APK validation:** Confirms APK created and has reasonable size
7. **Release created:** GitHub Release created with tag details
8. **APK uploaded:** Signed APK uploaded to Release assets
9. **latest.json generated:**
   - SHA-256 hash computed
   - Download URL constructed
   - JSON file created with version info and build timestamp
10. **pushed to gh-pages:** latest.json pushed to gh-pages branch
11. **Summary:** Clear success/failure message with links

### Download URL Format

```
https://github.com/irumasama28-art/PesoPal/releases/download/v1.2.3/CashLedgerAndroid-Signed.apk
```

App uses this in update system to download new versions.

---

## 📊 Release Workflow Diagram

```
User pushes tag
      ↓
GitHub Actions triggered
      ↓
[Validate] Format check, Secret check
      ↓
[Build] .NET publish with signing
      ↓
[Verify] APK exists and valid
      ↓
[Create] GitHub Release
      ↓
[Upload] APK to Release assets
      ↓
[Generate] latest.json with hash + download URL
      ↓
[Push] latest.json to gh-pages branch
      ↓
✅ Release Complete
      ↓
App checks latest.json URLs, downloads APK automatically
```

---

## 📞 Support / Next Steps

- **First release not working?** Check `.github/workflows/publish-release.yml` logs
- **Need to rollback?** Delete release or edit `latest.json` on gh-pages
- **Want periodic checks?** App already checks on app launch
- **Want WiFi-only updates?** Modify `UpdateService.cs` to check network type

---

**Ready to release?** 🚀

1. Update versionCode in `.csproj`
2. Commit changes
3. `git tag -a v1.2.3 -m "Description"`
4. `git push origin v1.2.3`
5. Watch Actions tab
6. Verify `latest.json` updated
7. Test on device
8. Done! ✅
