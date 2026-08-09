param(
    [Parameter(Mandatory = $true)]
    [string]$Source,

    [Parameter(Mandatory = $true)]
    [string]$Destination
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Application icon source not found: $Source"
}

$destinationDirectory = Split-Path -Parent $Destination
if ($destinationDirectory) {
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
}

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$bitmap = New-Object System.Windows.Media.Imaging.BitmapImage
$bitmap.BeginInit()
$bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$bitmap.UriSource = [Uri]::new((Resolve-Path -LiteralPath $Source).Path)
$bitmap.EndInit()
$bitmap.Freeze()

# The user-provided artwork is square. Keep its full composition and generate a
# high-enough 128 px icon source; Windows will scale this embedded PNG entry for
# smaller shell/title-bar sizes.
$targetSize = 128.0
$scale = New-Object System.Windows.Media.ScaleTransform(
    ($targetSize / $bitmap.PixelWidth),
    ($targetSize / $bitmap.PixelHeight))
$scaled = New-Object System.Windows.Media.Imaging.TransformedBitmap($bitmap, $scale)
$scaled.Freeze()

$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($scaled))
$pngStream = New-Object System.IO.MemoryStream
$encoder.Save($pngStream)
$pngBytes = $pngStream.ToArray()
$pngStream.Dispose()

# ICO header + one PNG-compressed 128x128 image entry.
$fileStream = [System.IO.File]::Open($Destination, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter($fileStream)
try {
    $writer.Write([UInt16]0)           # reserved
    $writer.Write([UInt16]1)           # image type = icon
    $writer.Write([UInt16]1)           # image count

    $writer.Write([Byte]128)           # width
    $writer.Write([Byte]128)           # height
    $writer.Write([Byte]0)             # color count
    $writer.Write([Byte]0)             # reserved
    $writer.Write([UInt16]1)           # color planes
    $writer.Write([UInt16]32)          # bits per pixel
    $writer.Write([UInt32]$pngBytes.Length)
    $writer.Write([UInt32]22)          # 6-byte header + 16-byte directory entry
    $writer.Write($pngBytes)
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}
