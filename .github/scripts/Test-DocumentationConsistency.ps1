$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-RequiredText {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "Required project-memory file is missing: $Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

$requiredFiles = @(
    'AGENTS.md',
    'README.md',
    'docs/PROJECT_STATE.json',
    'docs/ACTIVE_WORK.md',
    'docs/DOCUMENTATION_POLICY.md',
    'docs/CURRENT_STATE.md',
    'docs/STATE.md',
    'docs/PRODUCT.md',
    'docs/DECISIONS.md',
    'docs/ARCHITECTURE.md',
    'docs/DEVELOPER_REFERENCE.md',
    'docs/MAINTENANCE_CONTRACTS.md',
    'docs/CONTENT_STORAGE.md',
    'docs/DATA_MODEL.md',
    'docs/SCANNER.md',
    'docs/PROGRAM_UPDATE.md',
    'docs/DEPLOYMENT.md',
    'src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj',
    'packaging/FIRST_RUN_KO.txt'
)

foreach ($path in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Required documentation/recovery file is missing: $path"
}

$projectState = Get-Content 'docs/PROJECT_STATE.json' -Raw -Encoding UTF8 | ConvertFrom-Json
Assert-True ($projectState.schemaVersion -eq 1) "Unsupported docs/PROJECT_STATE.json schemaVersion '$($projectState.schemaVersion)'."

[xml]$desktopProject = Get-Content 'src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj' -Raw -Encoding UTF8
$desktopVersion = [string]($desktopProject.Project.PropertyGroup.Version | Select-Object -First 1)
Assert-True (-not [string]::IsNullOrWhiteSpace($desktopVersion)) 'Desktop project <Version> is missing.'
Assert-True ($projectState.product.desktopVersion -eq $desktopVersion) "PROJECT_STATE product.desktopVersion '$($projectState.product.desktopVersion)' does not match desktop project version '$desktopVersion'."

$stableVersion = [string]$projectState.publicStable.version
$stableTag = [string]$projectState.publicStable.tag
$productSource = [string]$projectState.publicStable.exactProductSource
Assert-True (-not [string]::IsNullOrWhiteSpace($stableVersion)) 'PROJECT_STATE publicStable.version is missing.'
Assert-True ($stableTag -eq "v$stableVersion") "PROJECT_STATE publicStable.tag '$stableTag' does not match version '$stableVersion'."
Assert-True ($productSource -match '^[0-9a-f]{40}$') "PROJECT_STATE exactProductSource '$productSource' is not a full commit SHA."

$readme = Read-RequiredText 'README.md'
$currentState = Read-RequiredText 'docs/CURRENT_STATE.md'
$state = Read-RequiredText 'docs/STATE.md'
foreach ($entry in @(
    @{ Name = 'README.md'; Text = $readme },
    @{ Name = 'docs/CURRENT_STATE.md'; Text = $currentState },
    @{ Name = 'docs/STATE.md'; Text = $state }
)) {
    Assert-True ($entry.Text.Contains($stableTag, [System.StringComparison]::Ordinal)) "$($entry.Name) does not contain canonical public stable tag '$stableTag'."
    Assert-True ($entry.Text.Contains($productSource, [System.StringComparison]::Ordinal)) "$($entry.Name) does not contain canonical exact public product source '$productSource'."
}

$firstRunFirstLine = Get-Content 'packaging/FIRST_RUN_KO.txt' -TotalCount 1 -Encoding UTF8
$expectedFirstRun = "준현 헬퍼 v$desktopVersion — Windows x64"
Assert-True ([string]::Equals($firstRunFirstLine, $expectedFirstRun, [System.StringComparison]::Ordinal)) "FIRST_RUN_KO.txt first line '$firstRunFirstLine' does not match '$expectedFirstRun'."

$agents = Read-RequiredText 'AGENTS.md'
foreach ($requiredReference in @(
    'docs/PROJECT_STATE.json',
    'docs/ACTIVE_WORK.md',
    'docs/DOCUMENTATION_POLICY.md'
)) {
    Assert-True ($agents.Contains($requiredReference, [System.StringComparison]::Ordinal)) "AGENTS.md does not reference required recovery source '$requiredReference'."
}

$activeWork = Read-RequiredText 'docs/ACTIVE_WORK.md'
$statusMatch = [regex]::Match($activeWork, 'Status:\s*\*\*(NONE|ACTIVE)\*\*')
Assert-True $statusMatch.Success 'docs/ACTIVE_WORK.md must contain exactly Status: **NONE** or Status: **ACTIVE**.'
$activeStatus = $statusMatch.Groups[1].Value

if ($activeStatus -eq 'ACTIVE') {
    foreach ($heading in @(
        '## Goal',
        '## Base',
        '## Confirmed scope',
        '## Completed',
        '## Current step',
        '## Remaining'
    )) {
        Assert-True ($activeWork.Contains($heading, [System.StringComparison]::Ordinal)) "ACTIVE_WORK is ACTIVE but required section '$heading' is missing."
    }
    Assert-True ($activeWork -match 'branch:\s*\S+') 'ACTIVE_WORK is ACTIVE but branch information is missing.'
}

# Current implementation/reference documents must not duplicate a historical release
# identity as if it were current. Release identity belongs to PROJECT_STATE/STATE.
foreach ($evergreenPath in @(
    'docs/ARCHITECTURE.md',
    'docs/DEVELOPER_REFERENCE.md',
    'docs/SCANNER.md',
    'docs/PROGRAM_UPDATE.md',
    'docs/DEPLOYMENT.md'
)) {
    $evergreen = Read-RequiredText $evergreenPath
    Assert-True (-not ($evergreen -match 'v\d+\.\d+\.\d+\s+PUBLIC STABLE')) "$evergreenPath contains a release-specific PUBLIC STABLE marker. Current release authority is PROJECT_STATE/STATE."
    Assert-True (-not ($evergreen -match 'Current v\d+\.\d+\.\d+ proof:')) "$evergreenPath embeds a historical current-release proof block."
}

# Active-work and canonical decision records replace duplicate current-looking task docs.
Assert-True (-not (Test-Path -LiteralPath 'docs/NEXT.md')) 'docs/NEXT.md must not compete with docs/ACTIVE_WORK.md.'
Assert-True (-not (Test-Path -LiteralPath 'docs/FARMING_GUIDE.md')) 'Retired Farming Guide must not regain a current-looking specialist document.'

$contentWrite = [int]$projectState.schemas.contentWrite
$contentReadable = @($projectState.schemas.contentReadable | ForEach-Object { [int]$_ })
$contentReadableMin = ($contentReadable | Measure-Object -Minimum).Minimum
$contentReadableMax = ($contentReadable | Measure-Object -Maximum).Maximum
$contentStorage = Read-RequiredText 'docs/CONTENT_STORAGE.md'
$dataModel = Read-RequiredText 'docs/DATA_MODEL.md'
foreach ($entry in @(
    @{ Name = 'docs/CONTENT_STORAGE.md'; Text = $contentStorage },
    @{ Name = 'docs/DATA_MODEL.md'; Text = $dataModel }
)) {
    Assert-True ($entry.Text.Contains("v$contentWrite", [System.StringComparison]::Ordinal)) "$($entry.Name) does not contain canonical Content write schema v$contentWrite."
    Assert-True ($entry.Text.Contains("v$contentReadableMin~v$contentReadableMax", [System.StringComparison]::Ordinal)) "$($entry.Name) does not contain canonical Content readable range v$contentReadableMin~v$contentReadableMax."
}

$developerReference = Read-RequiredText 'docs/DEVELOPER_REFERENCE.md'
$scannerDisplaySchema = [int]$projectState.schemas.scannerDisplaySettings
$scannerCatalogWrite = [int]$projectState.schemas.scannerCatalogWrite
$scannerCatalogReadable = @($projectState.schemas.scannerCatalogReadable | ForEach-Object { [int]$_ })
$scannerCatalogMin = ($scannerCatalogReadable | Measure-Object -Minimum).Minimum
$scannerCatalogMax = ($scannerCatalogReadable | Measure-Object -Maximum).Maximum
Assert-True ($developerReference.Contains("Scanner display settings: v$scannerDisplaySchema", [System.StringComparison]::Ordinal)) 'DEVELOPER_REFERENCE Scanner display schema is stale.'
Assert-True ($developerReference.Contains("Scanner catalog write: v$scannerCatalogWrite / readable v$scannerCatalogMin~v$scannerCatalogMax", [System.StringComparison]::Ordinal)) 'DEVELOPER_REFERENCE Scanner catalog schema is stale.'

Write-Host 'Documentation consistency passed.'
Write-Host "Desktop version: $desktopVersion"
Write-Host "Public stable: $stableTag"
Write-Host "Exact product source: $productSource"
Write-Host "ACTIVE_WORK status: $activeStatus"
