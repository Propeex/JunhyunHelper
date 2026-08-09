param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PublishDirectory)) {
    return
}

# JunhyunHelper is a Korean-only product. The self-contained .NET/WPF publish brings
# many framework/package satellite resource folders that are not used by the product.
# Delete a directory only when every file under it is a *.resources.dll satellite;
# this prevents accidental removal of Assets, runtimes, Logs or other functional data.
$keepCultures = @('ko', 'ko-KR')

Get-ChildItem -LiteralPath $PublishDirectory -Directory | ForEach-Object {
    $directory = $_
    if ($keepCultures -contains $directory.Name) {
        return
    }

    $files = @(Get-ChildItem -LiteralPath $directory.FullName -File -Recurse)
    if ($files.Count -eq 0) {
        return
    }

    $satelliteOnly = $true
    foreach ($file in $files) {
        if (-not $file.Name.EndsWith('.resources.dll', [StringComparison]::OrdinalIgnoreCase)) {
            $satelliteOnly = $false
            break
        }
    }

    if ($satelliteOnly) {
        Remove-Item -LiteralPath $directory.FullName -Recurse -Force
        Write-Host "Removed unused satellite resources: $($directory.Name)"
    }
}
