param(
	[Parameter(Mandatory=$true)]
	[string]$Tag,
	[Parameter(Mandatory=$true)]
	[string]$AssetPath,
	[string]$Repo = "irumasama28-art/PesoPal",
	[string]$Branch = "gh-pages",
	[switch]$Push,
	[switch]$UploadRelease
)

$ErrorActionPreference = "Stop"

function Write-Log($msg) { 
	$timestamp = Get-Date -Format "HH:mm:ss"
	Write-Host "[$timestamp] [generate-latest] $msg" 
}

function Write-Error-Log($msg) {
	Write-Host "❌ ERROR: $msg" -ForegroundColor Red
	Write-Error $msg
}

Write-Log "Script started for release: $Tag"
Write-Log "Repository: $Repo"
Write-Log "Branch: $Branch"

# Resolve wildcard path
$resolvedPaths = @(Resolve-Path $AssetPath -ErrorAction SilentlyContinue)
if ($resolvedPaths.Count -eq 0) {
	Write-Error-Log "Asset file not found: $AssetPath"
	exit 2
}

$AssetPath = $resolvedPaths[0].Path
Write-Log "Using APK: $AssetPath"

if (-not (Test-Path $AssetPath -PathType Leaf)) {
	Write-Error-Log "Asset is not a file: $AssetPath"
	exit 2
}

$fileSize = (Get-Item $AssetPath).Length / 1MB
Write-Log "APK size: $([Math]::Round($fileSize, 2)) MB"

# Compute SHA256
Write-Log "Computing SHA256 hash..."
$hash = Get-FileHash -Path $AssetPath -Algorithm SHA256 | Select-Object -ExpandProperty Hash
$hash = $hash.ToLowerInvariant()
Write-Log "SHA256: $hash"

# Build filename
$fileName = Split-Path -Path $AssetPath -Leaf

# Build release download URL
$apkUrl = "https://github.com/$Repo/releases/download/$Tag/$fileName"
Write-Log "Download URL: $apkUrl"

# Create latest.json
$versionCode = [int](Get-Date -UFormat %s)
$versionName = $Tag

$obj = @{ 
	versionCode = $versionCode
	versionName = $versionName
	apkUrl = $apkUrl
	sha256 = $hash
	buildTime = (Get-Date -Format o)
}

$json = $obj | ConvertTo-Json -Depth 5
Write-Log "Generated latest.json:"
Write-Host $json

# Save to temp file
$tmp = [IO.Path]::GetTempFileName()
Set-Content -Path $tmp -Value $json -Encoding UTF8
Write-Log "Saved to temp: $tmp"

# Create gh-pages directory
if (-not (Test-Path "gh-pages")) {
	New-Item -ItemType Directory -Path "gh-pages" | Out-Null
	Write-Log "Created gh-pages directory"
}

# Copy to gh-pages
$ghPagesPath = Join-Path "gh-pages" "latest.json"
Copy-Item -Path $tmp -Destination $ghPagesPath -Force
Write-Log "Copied latest.json to: $ghPagesPath"

if ($Push) {
	Write-Log "Preparing to push latest.json to gh-pages branch of $Repo"

	# Configure git
	git config --global user.email "github-actions@github.com"
	git config --global user.name "GitHub Actions"
	Write-Log "Configured git user"

	$token = $env:GITHUB_TOKEN
        if ([string]::IsNullOrEmpty($token)) {
                Write-Error-Log "GITHUB_TOKEN env var not set - cannot authenticate git push"
                exit 2
        }
        $repoUrl = "https://x-access-token:$token@github.com/$Repo.git"
	$work = Join-Path -Path (Get-Location) -ChildPath "gh-pages-work"

	if (Test-Path $work) {
		Remove-Item $work -Recurse -Force | Out-Null
		Write-Log "Removed existing work directory"
	}

	Write-Log "Cloning gh-pages branch..."
	& git clone --branch $Branch $repoUrl $work 2>&1
	if ($LASTEXITCODE -ne 0) {
		Write-Error-Log "git clone failed with exit code $LASTEXITCODE"
		exit 3
	}
	Write-Log "Repository cloned successfully"

	Copy-Item -Path $tmp -Destination (Join-Path $work "latest.json") -Force
	Write-Log "Copied latest.json to work directory"

	Push-Location $work
	try {
		& git add latest.json 2>&1
		Write-Log "Staged latest.json"

		& git commit -m "Update latest.json for $Tag" 2>&1
		if ($LASTEXITCODE -ne 0) {
			Write-Error-Log "git commit failed"
			exit 4
		}
		Write-Log "Committed changes"

		& git push origin $Branch 2>&1
		if ($LASTEXITCODE -ne 0) {
			Write-Error-Log "git push failed"
			exit 4
		}
		Write-Log "Pushed to gh-pages branch successfully"
	}
	finally {
		Pop-Location
		Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
		Write-Log "Cleanup completed"
	}
} else {
	Write-Log "latest.json written to: $tmp (Push flag not set)"
}

if ($UploadRelease) {
	Write-Log "Uploading to GitHub release for tag $Tag"

	$token = $env:GITHUB_TOKEN
	if ([string]::IsNullOrEmpty($token)) {
		Write-Error-Log "GITHUB_TOKEN not set in environment"
		exit 5
	}

	$apiBase = "https://api.github.com/repos/$Repo"

	# Find release by tag
	Write-Log "Querying GitHub API for release: $Tag"
	try {
		$release = Invoke-RestMethod `
			-Headers @{ 
				Authorization = "token $token"
				Accept = 'application/vnd.github.v3+json'
				'User-Agent' = 'PowerShell-Script'
			} `
			-Uri "$apiBase/releases/tags/$Tag" `
			-Method Get `
			-ErrorAction Stop
	} catch {
		Write-Error-Log "Failed to find release: $_"
		exit 6
	}

	if ($null -eq $release) {
		Write-Error-Log "Release for tag $Tag not found"
		exit 6
	}

	Write-Log "Found release: $($release.name)"

	$uploadUrl = $release.upload_url -replace '\{.*\}', ''
	$uploadUrlWithName = "$uploadUrl`?name=$fileName"

	Write-Log "Uploading $fileName..."
	try {
		$bytes = [System.IO.File]::ReadAllBytes($AssetPath)
		$uri = [System.Uri]::EscapeUriString($uploadUrlWithName)

		$resp = Invoke-RestMethod `
			-Headers @{ 
				Authorization = "token $token"
				Accept = 'application/vnd.github.v3+json'
				'User-Agent' = 'PowerShell-Script'
			} `
			-Uri $uri `
			-Method Post `
			-ContentType "application/octet-stream" `
			-Body $bytes `
			-ErrorAction Stop

		Write-Log "✅ Asset uploaded successfully"
		Write-Log "Asset URL: $($resp.browser_download_url)"
	} catch {
		Write-Error-Log "Asset upload failed: $_"
		exit 7
	}
}

Write-Log "✅ Script completed successfully"
exit 0
