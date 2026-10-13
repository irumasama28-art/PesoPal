# GitHub Secrets Setup for Release Pipeline

## Quick Setup (5 minutes)

### Step 1: Go to GitHub Settings
1. Navigate to: https://github.com/irumasama28-art/PesoPal/settings/secrets/actions
2. Click **"New repository secret"** button

### Step 2: Add Four Secrets

#### Secret #1: ANDROID_KEYSTORE_BASE64
- **Name:** `ANDROID_KEYSTORE_BASE64`
- **Value:** Copy the entire content from `keystore-base64.txt` in your workspace
- Click **Add secret**

#### Secret #2: ANDROID_KEY_ALIAS
- **Name:** `ANDROID_KEY_ALIAS`
- **Value:** `pesopal`
- Click **Add secret**

#### Secret #3: ANDROID_KEY_PASSWORD
- **Name:** `ANDROID_KEY_PASSWORD`
- **Value:** `PesoPal@2025`
- Click **Add secret**

#### Secret #4: ANDROID_KEYSTORE_PASSWORD
- **Name:** `ANDROID_KEYSTORE_PASSWORD`
- **Value:** `PesoPal@2025`
- Click **Add secret**

### Step 3: Verify Setup
After adding all four secrets, you should see:
```
✅ ANDROID_KEYSTORE_BASE64
✅ ANDROID_KEY_ALIAS
✅ ANDROID_KEY_PASSWORD
✅ ANDROID_KEYSTORE_PASSWORD
```

## Local Files Created

- **pesopal.keystore** - The actual keystore file (keep secure, don't commit)
- **keystore-base64.txt** - Base64 encoded keystore for GitHub secrets (don't commit to public repo)

## Security Notes

⚠️ **IMPORTANT:**
- Do NOT commit `keystore-base64.txt` or `pesopal.keystore` to the public repository
- The keystore credentials (PesoPal@2025) are only for development/testing
- For production, use a properly secured keystore with a strong password
- Rotate credentials regularly
- Never share keystore files or passwords in chat/email

## Testing the Release

Once all 4 secrets are added:

1. Go to: https://github.com/irumasama28-art/PesoPal/releases
2. This will automatically trigger the workflow since v1.0.0 tag exists
3. Monitor the workflow at: https://github.com/irumasama28-art/PesoPal/actions

The release should:
- ✅ Validate the tag
- ✅ Build the Android APK
- ✅ Sign with the keystore
- ✅ Create a GitHub Release
- ✅ Upload the APK as an artifact
- ✅ Generate latest.json
- ✅ Push to gh-pages branch
