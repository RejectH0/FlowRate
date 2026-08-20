Add-Type -AssemblyName System.Drawing
$icoPath = 'C:\Users\gsper\source\repos\FlowRate\src\FlowRate\Assets\AppIcon.ico'
$outDir  = 'C:\Users\gsper\source\repos\FlowRate\assets\website'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$bytes = [IO.File]::ReadAllBytes($icoPath)
$count = [BitConverter]::ToUInt16($bytes, 4)

# Find largest frame
$best = $null
for ($i = 0; $i -lt $count; $i++) {
	$o = 6 + ($i * 16)
	$w = $bytes[$o]; if ($w -eq 0) { $w = 256 }
	$size   = [BitConverter]::ToUInt32($bytes, $o + 8)
	$offset = [BitConverter]::ToUInt32($bytes, $o + 12)
	if ($null -eq $best -or $w -gt $best.W) { $best = @{ W = $w; Size = $size; Offset = $offset } }
}
Write-Host "Largest frame: $($best.W)px, $($best.Size) bytes at offset $($best.Offset)"

$frame = New-Object byte[] $best.Size
[Array]::Copy($bytes, [int]$best.Offset, $frame, 0, [int]$best.Size)

# Is it PNG? (89 50 4E 47)
$isPng = ($frame[0] -eq 0x89 -and $frame[1] -eq 0x50)
Write-Host "PNG-compressed: $isPng"

if ($isPng) {
	$ms = New-Object IO.MemoryStream(,$frame)
	$src = [System.Drawing.Image]::FromStream($ms)
} else {
	$icon = New-Object System.Drawing.Icon($icoPath, $best.W, $best.W)
	$src = $icon.ToBitmap()
}
Write-Host "Source: $($src.Width)x$($src.Height)"

# Save exact 256px extraction
$src.Save((Join-Path $outDir 'flowrate-icon-256.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# 1024x768 canvas (transparent) with the icon upscaled to 768x768, centered
$canvas = New-Object System.Drawing.Bitmap(1024, 768)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)
$g.DrawImage($src, [System.Drawing.Rectangle]::new(128, 0, 768, 768))
$g.Dispose()
$canvas.Save((Join-Path $outDir 'flowrate-icon-1024x768.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()

# Also a square 1024 version (common for web/social)
$sq = New-Object System.Drawing.Bitmap(1024, 1024)
$g = [System.Drawing.Graphics]::FromImage($sq)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([System.Drawing.Color]::Transparent)
$g.DrawImage($src, [System.Drawing.Rectangle]::new(0, 0, 1024, 1024))
$g.Dispose()
$sq.Save((Join-Path $outDir 'flowrate-icon-1024.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$sq.Dispose()

# Sample dominant colors from source for the palette
$bmp = New-Object System.Drawing.Bitmap($src)
$colors = @{}
for ($y = 0; $y -lt $bmp.Height; $y += 2) {
	for ($x = 0; $x -lt $bmp.Width; $x += 2) {
		$p = $bmp.GetPixel($x, $y)
		if ($p.A -lt 200) { continue }
		$key = ('{0:X2}{1:X2}{2:X2}' -f ($p.R -band 0xF0), ($p.G -band 0xF0), ($p.B -band 0xF0))
		$colors[$key] = [int]$colors[$key] + 1
	}
}
Write-Host "Top colors:"
$colors.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 10 | ForEach-Object { Write-Host "  #$($_.Key)  $($_.Value)" }
$bmp.Dispose(); $src.Dispose()
Write-Host "Done."
