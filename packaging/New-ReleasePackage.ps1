param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$publish = (Resolve-Path $PublishDirectory).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$productFolderName = '준현 헬퍼'
$stablePackageRoot = Join-Path $output 'stable-package'
$productFolder = Join-Path $stablePackageRoot $productFolderName
$stableZipPath = Join-Path $output '준현 헬퍼.zip'
$checksumPath = Join-Path $output 'SHA256SUMS.txt'

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}
New-Item -ItemType Directory -Path $productFolder -Force | Out-Null

Get-ChildItem $publish -Force | ForEach-Object {
    Copy-Item $_.FullName -Destination $productFolder -Recurse -Force
}

$required = @(
    '준현 헬퍼.exe',
    'FIRST_RUN_KO.txt',
    'Assets/tarkov_data.db'
)
foreach ($relative in $required) {
    if (-not (Test-Path (Join-Path $productFolder $relative))) {
        throw "Release package is missing required file: $relative"
    }
}

$exePath = Join-Path $publish '준현 헬퍼.exe'
$productVersion = (Get-Item $exePath).VersionInfo.ProductVersion
if ([string]::IsNullOrWhiteSpace($productVersion) -or $productVersion -notmatch '^(\d+\.\d+\.\d+)(?:$|[+-])') {
    throw "Could not derive a stable three-part version from ProductVersion '$productVersion'."
}
$releaseVersion = $Matches[1]

Compress-Archive -Path $productFolder -DestinationPath $stableZipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Assert-StablePackage {
    param([string]$Path)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\\', '/') })
        foreach ($relative in $required) {
            $expected = "$productFolderName/$($relative.Replace('\\', '/'))"
            if ($expected -notin $names) {
                throw "Release ZIP does not contain expected top-level product path: $expected"
            }
        }

        $outsideProductFolder = @($names | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_) -and
            -not $_.StartsWith("$productFolderName/", [System.StringComparison]::Ordinal)
        })
        if ($outsideProductFolder.Count -gt 0) {
            $outsideProductFolder | ForEach-Object { Write-Host "Unexpected stable ZIP entry: $_" }
            throw "Release ZIP contains entries outside the stable '$productFolderName' folder."
        }
    }
    finally {
        $archive.Dispose()
    }
}

Assert-StablePackage -Path $stableZipPath

$releaseFiles = [System.Collections.Generic.List[string]]::new()
$releaseFiles.Add($stableZipPath)

# v1.5.0 was already publicly released with an updater that can only discover a
# versioned ASCII package name and only accepts program files at the archive root.
# v1.6.0 is the one-time bridge release: publish a machine-compatibility package so
# existing v1.5.0 installations can update automatically. v1.6.0 itself prefers the
# stable Korean package above, so later releases do not need this compatibility asset.
if ($releaseVersion -eq '1.6.0') {
    $legacyZipName = "Junhyun-Helper-v$releaseVersion-win-x64.zip"
    $legacyZipPath = Join-Path $output $legacyZipName
    $legacyPackageRoot = Join-Path $output 'legacy-update-package'
    New-Item -ItemType Directory -Path $legacyPackageRoot -Force | Out-Null

    Get-ChildItem $publish -Force | ForEach-Object {
        Copy-Item $_.FullName -Destination $legacyPackageRoot -Recurse -Force
    }

    Compress-Archive -Path (Join-Path $legacyPackageRoot '*') -DestinationPath $legacyZipPath -CompressionLevel Optimal

    $legacyArchive = [System.IO.Compression.ZipFile]::OpenRead($legacyZipPath)
    try {
        $legacyNames = @($legacyArchive.Entries | ForEach-Object { $_.FullName.Replace('\\', '/') })
        foreach ($relative in $required) {
            $expected = $relative.Replace('\\', '/')
            if ($expected -notin $legacyNames) {
                throw "Legacy updater bridge ZIP is missing required root path: $expected"
            }
        }

        $allowedLegacyRoots = @('준현 헬퍼.exe', 'FIRST_RUN_KO.txt', 'Assets/')
        $unexpectedLegacy = @($legacyNames | Where-Object {
            if ([string]::IsNullOrWhiteSpace($_)) { return $false }
            foreach ($allowed in $allowedLegacyRoots) {
                if ($_.Equals($allowed.TrimEnd('/'), [System.StringComparison]::Ordinal) -or
                    $_.StartsWith($allowed, [System.StringComparison]::Ordinal)) {
                    return $false
                }
            }
            return $true
        })
        if ($unexpectedLegacy.Count -gt 0) {
            $unexpectedLegacy | ForEach-Object { Write-Host "Unexpected legacy ZIP entry: $_" }
            throw "Legacy updater bridge ZIP contains unexpected root entries."
        }
    }
    finally {
        $legacyArchive.Dispose()
    }

    $releaseFiles.Add($legacyZipPath)
    Write-Host "v1.5 updater bridge created: $legacyZipPath"
}

$checksumLines = foreach ($file in $releaseFiles) {
    $hash = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([System.IO.Path]::GetFileName($file))"
}
[System.IO.File]::WriteAllLines(
    $checksumPath,
    $checksumLines,
    [System.Text.UTF8Encoding]::new($false))

foreach ($line in $checksumLines) {
    Write-Host "SHA256 $line"
}

Write-Host "Stable release package: $stableZipPath"
Write-Host "Stable extracted product folder: $productFolderName"
Write-Host "Checksum file: $checksumPath"
