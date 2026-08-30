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

# Architecture/reference documents are evergreen implementation maps. They are not
# release authority, but an old "PUBLIC STABLE" header is easy to misread. Keep
# surfacing this debt until the large documents are naturally edited/normalized.
foreach ($evergreenPath in @('docs/ARCHITECTURE.md', 'docs/DEVELOPER_REFERENCE.md')) {
    $evergreen = Read-RequiredText $evergreenPath
    $marker = [regex]::Match($evergreen, 'v(?<version>\d+\.\d+\.\d+)\s+PUBLIC STABLE')
    if ($marker.Success -and $marker.Groups['version'].Value -ne $stableVersion) {
        Write-Warning "$evergreenPath contains historical PUBLIC STABLE marker v$($marker.Groups['version'].Value). Current release authority is PROJECT_STATE/STATE ($stableTag). Normalize this evergreen header when that document is next materially edited."
    }
}

Write-Host 'Documentation consistency passed.'
Write-Host "Desktop version: $desktopVersion"
Write-Host "Public stable: $stableTag"
Write-Host "Exact product source: $productSource"
Write-Host "ACTIVE_WORK status: $activeStatus"
