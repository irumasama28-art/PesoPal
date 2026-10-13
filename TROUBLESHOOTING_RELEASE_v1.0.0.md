# ❌ Release v1.0.0 - Troubleshooting

## What to Check

### Step 1: Verify Tag Was Pushed
```bash
git ls-remote origin | Select-String "v1.0.0"
```
Should show: `refs/tags/v1.0.0`

### Step 2: Check GitHub Actions
Visit: https://github.com/irumasama28-art/PesoPal/actions

Look for one of these scenarios:

---

## Scenario A: No Workflow Appears ⏳

**What you see:** Actions tab is empty or shows old workflows

**Causes:**
- GitHub takes 1-2 minutes to trigger
- Tag wasn't actually pushed
- Settings → Actions permissions disabled

**Fix:**
1. Refresh page: F5
2. Wait 2 minutes
3. Check tag was pushed: `git push origin v1.0.0`
4. Check Actions are enabled: Settings → Actions tab

---

## Scenario B: Yellow Circle (Running) 🟡

**What you see:** Workflow is currently running

**What to do:**
1. Click the workflow
2. Watch the steps progress
3. Wait for completion (5-10 min typical)
4. Look for green checkmark

**Common durations:**
- Validate: 1 min
- Setup: 2 min
- Build: 3-4 min
- Sign: 1 min
- Release: 2 min
- **Total: 5-10 min**

---

## Scenario C: Green Checkmark ✅

**What you see:** Workflow completed successfully

**Next steps:**
1. Go to Releases: https://github.com/irumasama28-art/PesoPal/releases/tag/v1.0.0
2. Should see APK file downloadable
3. Check latest.json: https://raw.githubusercontent.com/irumasama28-art/PesoPal/gh-pages/latest.json
4. **RELEASE SUCCESS!** 🎉

---

## Scenario D: Red X (Failed) ❌

**What you see:** Workflow has a red X

**What to do:**
1. Click the workflow name
2. Look for which step failed (red)
3. Click the failed step
4. Read the error message

**Common failure points:**

### D1: "Validate Release Tag" Failed
**Error:** "Invalid tag format: v1.0.0"
**Cause:** Tag doesn't match vX.Y.Z format
**Fix:** This shouldn't happen with v1.0.0
- Try: `git push origin --delete v1.0.0; git push origin v1.0.0`

### D2: "Validate secrets" Failed
**Error:** "ANDROID_KEYSTORE_PATH secret not configured"
**Cause:** Missing GitHub Actions secrets
**Fix:**
1. Go to: Settings → Secrets and variables → Actions
2. Add these 4 secrets:
   - `ANDROID_KEYSTORE_PATH` (base64 encoded keystore)
   - `ANDROID_KEY_ALIAS` (e.g., pesopa_key)
   - `ANDROID_KEY_PASSWORD` (key password)
   - `ANDROID_KEYSTORE_PASSWORD` (keystore password)
3. Retry: Push tag again

### D3: "Build Android APK" Failed
**Error:** "Error MSB..." or compilation error
**Cause:** Build error in code
**Fix:**
1. Look at error message in logs
2. Test locally: `dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release`
3. Fix the error
4. Commit fix
5. Re-push tag

### D4: "Verify APK was created" Failed
**Error:** "No APK file found in artifacts/publish"
**Cause:** Build created no APK (compilation silently failed)
**Fix:**
1. Scroll up in logs to "Build Android APK" step
2. Find the actual error
3. Fix it locally
4. Re-push tag

### D5: "Generate latest.json" Failed
**Error:** PowerShell script failed
**Cause:** File not found or permission issue
**Fix:**
1. Check gh-pages branch exists: `git branch -a | Select-String gh-pages`
2. If missing, create it:
   ```bash
   git checkout --orphan gh-pages
   git rm -rf .
   echo "# GitHub Pages" > README.md
   git add README.md
   git commit -m "Initial commit"
   git push -u origin gh-pages
   git checkout master
   ```
3. Re-push tag

---

## Command to Check Current Status

```bash
# Show all tags
git tag -l

# Show tag details
git show v1.0.0

# Verify it's pushed
git ls-remote origin | Select-String "v1.0.0"

# Check recent commits
git log --oneline -10
```

---

## How to Manually Fix Most Issues

```bash
cd C:\Dev\CashLedgerAndroid

# If you think tag is bad, delete and recreate
git tag -d v1.0.0
git push origin --delete v1.0.0
git tag -a v1.0.0 -m "Release v1.0.0 - Initial production release"
git push origin v1.0.0

# Then wait 2 minutes and check Actions tab
```

---

## If All Else Fails: Manual Release

Build and release manually:

```powershell
# 1. Build
dotnet publish CashLedgerAndroid.slnx -f net10.0-android -c Release `
  -o artifacts/publish `
  /p:AndroidSigningKeyStore="C:\path\to\cashledger.keystore" `
  /p:AndroidSigningKeyAlias="pesopa_key" `
  /p:AndroidSigningKeyPass="YOUR_PASSWORD" `
  /p:AndroidSigningStorePass="YOUR_PASSWORD"

# 2. Create release on GitHub web UI:
# https://github.com/irumasama28-art/PesoPal/releases/new

# 3. Upload APK using web UI (drag and drop)

# 4. Generate latest.json
pwsh -File tools/generate-latest.ps1 `
  -Tag v1.0.0 `
  -AssetPath "artifacts/publish/*.apk" `
  -Repo irumasama28-art/PesoPal `
  -Branch gh-pages `
  -Push
```

---

## Quick Checklist

- [ ] Tag v1.0.0 exists locally: `git tag -l`
- [ ] Tag is on GitHub: `git ls-remote origin | Select-String v1.0.0`
- [ ] Workflow file exists: `.github/workflows/publish-release.yml`
- [ ] Actions tab accessible: https://github.com/irumasama28-art/PesoPal/actions
- [ ] Secrets configured (if needed): Settings → Secrets
- [ ] gh-pages branch exists: `git branch -a`

---

## What's the Exact Error You're Seeing?

Please tell me:
1. Did workflow appear in Actions tab? (Yes/No)
2. If yes, what color? (🟡 Waiting / 🟢 Done / 🔴 Failed)
3. If failed, what's the error message?
4. Which step failed? (Validate / Setup / Build / Release / JSON / Other)

I can help fix the specific issue!

---

**Status:** Investigating...
**Next:** Wait your response with error details
**Reference:** https://github.com/irumasama28-art/PesoPal/actions
