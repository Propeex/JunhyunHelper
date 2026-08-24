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
$packageRoot = Join-Path $output 'package'
$productFolder = Join-Path $packageRoot $productFolderName
$zipPath = Join-Path $output '준현 헬퍼.zip'

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

Compress-Archive -Path $productFolder -DestinationPath $zipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
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
        $outsideProductFolder | ForEach-Object { Write-Host "Unexpected ZIP entry: $_" }
        throw "Release ZIP contains entries outside the stable '$productFolderName' folder."
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Release package created: $zipPath"
Write-Host "Extracted product folder: $productFolderName"
