Add-Type -AssemblyName System.Drawing

function New-FlowRateBitmap {
	param([int]$Size)
	$bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
	$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
	$g.Clear([System.Drawing.Color]::Transparent)
	$s = [double]$Size
	$inset = $s * 0.06
	$rectF = New-Object System.Drawing.RectangleF($inset, $inset, ($s - 2*$inset), ($s - 2*$inset))
	$radius = $s * 0.22
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$d = $radius * 2.0
	$path.AddArc($rectF.X, $rectF.Y, $d, $d, 180, 90)
	$path.AddArc($rectF.Right - $d, $rectF.Y, $d, $d, 270, 90)
	$path.AddArc($rectF.Right - $d, $rectF.Bottom - $d, $d, $d, 0, 90)
	$path.AddArc($rectF.X, $rectF.Bottom - $d, $d, $d, 90, 90)
	$path.CloseFigure()
	$c1 = [System.Drawing.Color]::FromArgb(255, 6, 182, 212)
	$c2 = [System.Drawing.Color]::FromArgb(255, 13, 148, 136)
	$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectF, $c1, $c2, 45.0)
	$g.FillPath($brush, $path)
	$cx = $s * 0.5
	$cy = $s * 0.56
	$r  = $s * 0.28
	$penW = [Math]::Max(2.0, $s * 0.055)
	$arcRect = New-Object System.Drawing.RectangleF(($cx - $r), ($cy - $r), ($r*2), ($r*2))
	$arcPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, 255, 255, 255), $penW)
	$arcPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
	$arcPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
	$g.DrawArc($arcPen, $arcRect, 150, 240)
	$tickPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200, 255, 255, 255), [Math]::Max(1.0, $s*0.018))
	for ($i = 0; $i -le 6; $i++) {
		$ang = (150 + $i * 40) * [Math]::PI / 180.0
		$ro = $r + $penW * 0.75
		$ri = $r + $penW * 0.75 - [Math]::Max(2.0, $s*0.05)
		$x1 = $cx + $ro * [Math]::Cos($ang); $y1 = $cy + $ro * [Math]::Sin($ang)
		$x2 = $cx + $ri * [Math]::Cos($ang); $y2 = $cy + $ri * [Math]::Sin($ang)
		$g.DrawLine($tickPen, [single]$x1, [single]$y1, [single]$x2, [single]$y2)
	}
	$needleAng = 315 * [Math]::PI / 180.0
	$nx = $cx + ($r * 0.86) * [Math]::Cos($needleAng)
	$ny = $cy + ($r * 0.86) * [Math]::Sin($needleAng)
	$needlePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(2.0, $s*0.05))
	$needlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
	$needlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
	$g.DrawLine($needlePen, [single]$cx, [single]$cy, [single]$nx, [single]$ny)
	$hubR = [Math]::Max(2.0, $s * 0.06)
	$hubBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
	$g.FillEllipse($hubBrush, [single]($cx-$hubR), [single]($cy-$hubR), [single]($hubR*2), [single]($hubR*2))
	$g.Dispose()
	return $bmp
}

function Get-PngBytes {
	param([int]$Size)
	$bmp = New-FlowRateBitmap -Size $Size
	$ms = New-Object System.IO.MemoryStream
	$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
	$bytes = $ms.ToArray()
	$ms.Dispose(); $bmp.Dispose()
	return ,$bytes
}

function Save-Png {
	param([int]$Size, [string]$Path)
	[System.IO.File]::WriteAllBytes($Path, (Get-PngBytes -Size $Size))
	Write-Host "wrote $Path ($Size x $Size)"
}

function Save-Wide {
	param([int]$W, [int]$H, [string]$Path)
	$canvas = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$gc = [System.Drawing.Graphics]::FromImage($canvas)
	$gc.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
	$gc.Clear([System.Drawing.Color]::Transparent)
	$glyph = [Math]::Min($H, [int]($W*0.4))
	$icon = New-FlowRateBitmap -Size $glyph
	$gc.DrawImage($icon, [int](($W-$glyph)/2), [int](($H-$glyph)/2), $glyph, $glyph)
	$canvas.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
	$icon.Dispose(); $gc.Dispose(); $canvas.Dispose()
	Write-Host "wrote $Path ($W x $H)"
}

$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets'

Save-Png -Size 88  -Path (Join-Path $assets 'Square44x44Logo.scale-200.png')
Save-Png -Size 44  -Path (Join-Path $assets 'Square44x44Logo.scale-100.png')
Save-Png -Size 24  -Path (Join-Path $assets 'Square44x44Logo.targetsize-24_altform-unplated.png')
Save-Png -Size 48  -Path (Join-Path $assets 'Square44x44Logo.targetsize-48_altform-lightunplated.png')
Save-Png -Size 300 -Path (Join-Path $assets 'Square150x150Logo.scale-200.png')
Save-Png -Size 150 -Path (Join-Path $assets 'Square150x150Logo.scale-100.png')
Save-Png -Size 96  -Path (Join-Path $assets 'LockScreenLogo.scale-200.png')
Save-Png -Size 50  -Path (Join-Path $assets 'StoreLogo.png')
Save-Wide -W 620 -H 300 -Path (Join-Path $assets 'Wide310x150Logo.scale-200.png')
Save-Wide -W 620 -H 300 -Path (Join-Path $assets 'SplashScreen.scale-200.png')

$icoPath = Join-Path $assets 'AppIcon.ico'
$sizes = @(16,24,32,48,64,128,256)
$frames = @()
foreach ($sz in $sizes) { $frames += ,(Get-PngBytes -Size $sz) }

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)
$offset = 6 + (16 * $sizes.Count)
for ($i=0; $i -lt $sizes.Count; $i++) {
	$sz = $sizes[$i]
	$len = $frames[$i].Length
	$dim = [byte]($(if ($sz -ge 256) { 0 } else { $sz }))
	$bw.Write($dim)
	$bw.Write($dim)
	$bw.Write([byte]0)
	$bw.Write([byte]0)
	$bw.Write([UInt16]1)
	$bw.Write([UInt16]32)
	$bw.Write([UInt32]$len)
	$bw.Write([UInt32]$offset)
	$offset += $len
}
foreach ($f in $frames) { $bw.Write($f) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
Write-Host "wrote $icoPath (multi-res, $($sizes.Count) frames)"
