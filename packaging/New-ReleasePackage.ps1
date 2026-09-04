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
$stableZipPath = Join-Path $output 'Junhyun-Helper.zip'
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
