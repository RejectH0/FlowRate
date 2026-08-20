Add-Type -AssemblyName System.Drawing
$srcIcon = 'C:\Users\gsper\source\repos\FlowRate\flowrate-assets-v2-extracted\flowrate_assets_v2\app\app-icon-1024.png'
$outDir  = 'C:\Users\gsper\source\repos\FlowRate\src\FlowRate\Assets'
$src = [System.Drawing.Image]::FromFile($srcIcon)

function New-Asset([string]$name, [int]$w, [int]$h) {
	$bmp = New-Object System.Drawing.Bitmap($w, $h)
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
	$g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
	$g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
	$g.Clear([System.Drawing.Color]::Transparent)
	# Fit icon into the shorter dimension, centered
	$size = [Math]::Min($w, $h)
	$x = [int](($w - $size) / 2); $y = [int](($h - $size) / 2)
	$g.DrawImage($src, [System.Drawing.Rectangle]::new($x, $y, $size, $size))
	$g.Dispose()
	$path = Join-Path $outDir $name
	$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
	$bmp.Dispose()
	Write-Host "Wrote $name ($w x $h)"
}

New-Asset 'LockScreenLogo.scale-200.png' 96 96
New-Asset 'SplashScreen.scale-200.png' 620 300
New-Asset 'Square150x150Logo.scale-100.png' 150 150
New-Asset 'Square150x150Logo.scale-200.png' 300 300
New-Asset 'Square44x44Logo.scale-100.png' 44 44
New-Asset 'Square44x44Logo.scale-200.png' 88 88
New-Asset 'Square44x44Logo.targetsize-24_altform-unplated.png' 24 24
New-Asset 'Square44x44Logo.targetsize-48_altform-lightunplated.png' 48 48
New-Asset 'StoreLogo.png' 50 50
New-Asset 'Wide310x150Logo.scale-200.png' 620 300

$src.Dispose()
Write-Host 'Done.'
