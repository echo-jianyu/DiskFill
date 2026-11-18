$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = [Math]::Max(0.01, $Radius * 2)
    $arc = New-Object System.Drawing.RectangleF($Rectangle.X, $Rectangle.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rectangle.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rectangle.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rectangle.Left
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $margin = [float]($Size * 0.035)
    $square = New-Object System.Drawing.RectangleF(
        $margin,
        $margin,
        [float]($Size - $margin * 2),
        [float]($Size - $margin * 2))
    $squarePath = New-RoundedPath $square ([float]($Size * 0.2))
    $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(22, 33, 43))
    $graphics.FillPath($darkBrush, $squarePath)

    $lineWidth = [float][Math]::Max(1, $Size * 0.045)
    $body = New-Object System.Drawing.RectangleF(
        [float]($Size * 0.22),
        [float]($Size * 0.3),
        [float]($Size * 0.56),
        [float]($Size * 0.4))
    $bodyPath = New-RoundedPath $body ([float]($Size * 0.055))
    $outlinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 248, 249), $lineWidth)
    $outlinePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($outlinePen, $bodyPath)

    $slotY = [float]($Size * 0.575)
    $slotPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 248, 249), [float]([Math]::Max(1, $Size * 0.035)))
    $graphics.DrawLine($slotPen, [float]($Size * 0.28), $slotY, [float]($Size * 0.72), $slotY)

    $fillRect = New-Object System.Drawing.RectangleF(
        [float]($Size * 0.29),
        [float]($Size * 0.55),
        [float]($Size * 0.25),
        [float]($Size * 0.05))
    $fillPath = New-RoundedPath $fillRect ([float]($Size * 0.025))
    $tealBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(19, 160, 130))
    $graphics.FillPath($tealBrush, $fillPath)

    $dotSize = [float]($Size * 0.06)
    $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(232, 166, 72))
    $graphics.FillEllipse(
        $dotBrush,
        [float]($Size * 0.68),
        [float]($Size * 0.65),
        $dotSize,
        $dotSize)

    $graphics.Dispose()
    $squarePath.Dispose()
    $bodyPath.Dispose()
    $fillPath.Dispose()
    $darkBrush.Dispose()
    $outlinePen.Dispose()
    $slotPen.Dispose()
    $tealBrush.Dispose()
    $dotBrush.Dispose()
    return $bitmap
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetDirectory = Join-Path $projectRoot "DiskFill\Assets"
[System.IO.Directory]::CreateDirectory($assetDirectory) | Out-Null

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $bitmap = New-IconBitmap $size
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,$stream.ToArray()
    $stream.Dispose()
    $bitmap.Dispose()
}

$iconPath = Join-Path $assetDirectory "DiskFill.ico"
$file = [System.IO.File]::Open($iconPath, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
for ($index = 0; $index -lt $sizes.Count; $index++) {
    $size = $sizes[$index]
    $data = $images[$index]
    $writer.Write([Byte]$(if ($size -ge 256) { 0 } else { $size }))
    $writer.Write([Byte]$(if ($size -ge 256) { 0 } else { $size }))
    $writer.Write([Byte]0)
    $writer.Write([Byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$data.Length)
    $writer.Write([UInt32]$offset)
    $offset += $data.Length
}

foreach ($data in $images) {
    $writer.Write($data)
}

$writer.Flush()
$writer.Dispose()
$file.Dispose()

$preview = New-IconBitmap 512
$preview.Save(
    (Join-Path $assetDirectory "DiskFill.png"),
    [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Write-Host "Generated $iconPath"
