$ErrorActionPreference = "Stop"

# ============================================================
# Android APK Builder
# Place this script in the same folder as your .slnx file.
# ============================================================

Set-Location $PSScriptRoot

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "       ANDROID APK BUILD SCRIPT"
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------
# Find solution
# ------------------------------------------------------------

$solution = Get-ChildItem `
    -Path $PSScriptRoot `
    -Filter "*.slnx" `
    -File |
    Select-Object -First 1

if (-not $solution) {
    Write-Host "ERROR: No .slnx file found next to this script." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Solution: $($solution.Name)" -ForegroundColor Gray


# ------------------------------------------------------------
# Find project
# ------------------------------------------------------------

$projects = Get-ChildItem `
    -Path $PSScriptRoot `
    -Filter "*.csproj" `
    -File `
    -Recurse

if (-not $projects) {
    Write-Host "ERROR: No .csproj files found." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

# Prefer a project containing an Android target framework
$project = $null

foreach ($candidate in $projects) {

    $content = Get-Content $candidate.FullName -Raw

    if ($content -match "net\d+\.\d+-android") {
        $project = $candidate
        break
    }
}

if (-not $project) {
    Write-Host "ERROR: Couldn't find an Android project." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Project:  $($project.FullName)" -ForegroundColor Gray


# ------------------------------------------------------------
# Detect Android target framework automatically
# ------------------------------------------------------------

$projectContent = Get-Content $project.FullName -Raw

$frameworkMatch = [regex]::Match(
    $projectContent,
    "net\d+\.\d+-android"
)

if (-not $frameworkMatch.Success) {
    Write-Host "ERROR: Couldn't determine Android target framework." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

$framework = $frameworkMatch.Value

Write-Host "Target:   $framework" -ForegroundColor Gray
Write-Host ""


# ------------------------------------------------------------
# Clean
# ------------------------------------------------------------

Write-Host "[1/3] Cleaning previous build..." -ForegroundColor Yellow

dotnet clean $project.FullName `
    -f $framework `
    -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "CLEAN FAILED." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit $LASTEXITCODE
}


# ------------------------------------------------------------
# Restore
# ------------------------------------------------------------

Write-Host ""
Write-Host "[2/3] Restoring packages..." -ForegroundColor Yellow

dotnet restore $project.FullName

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "RESTORE FAILED." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit $LASTEXITCODE
}


# ------------------------------------------------------------
# Publish APK
# ------------------------------------------------------------

Write-Host ""
Write-Host "[3/3] Building APK..." -ForegroundColor Yellow
Write-Host ""

dotnet publish $project.FullName `
    -f $framework `
    -c Release `
    -p:AndroidPackageFormat=apk

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "             BUILD FAILED"
    Write-Host "========================================" -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit $LASTEXITCODE
}


# ------------------------------------------------------------
# Locate newest APK
# ------------------------------------------------------------

$projectFolder = $project.DirectoryName

$apk = Get-ChildItem `
    -Path (Join-Path $projectFolder "bin") `
    -Filter "*.apk" `
    -File `
    -Recurse `
    -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1


Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "           BUILD SUCCESSFUL!"
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if ($apk) {

    $sizeMB = [math]::Round($apk.Length / 1MB, 2)

    Write-Host "APK:" -ForegroundColor Cyan
    Write-Host $apk.FullName
    Write-Host ""
    Write-Host "Size: $sizeMB MB"
    Write-Host ""

    # Open Explorer and select APK
    Start-Process explorer.exe "/select,`"$($apk.FullName)`""
}
else {
    Write-Host "WARNING: Build succeeded but APK could not be located." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to close"