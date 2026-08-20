Add-Type -AssemblyName System.Drawing
$icoPath = 'C:\Users\gsper\source\repos\FlowRate\src\FlowRate\Assets\AppIcon.ico'

# Enumerate frames in the ICO
$bytes = [IO.File]::ReadAllBytes($icoPath)
$count = [BitConverter]::ToUInt16($bytes, 4)
Write-Host "ICO frames: $count"
for ($i = 0; $i -lt $count; $i++) {
	$o = 6 + ($i * 16)
	$w = $bytes[$o]; $h = $bytes[$o+1]
	if ($w -eq 0) { $w = 256 }; if ($h -eq 0) { $h = 256 }
	Write-Host "  Frame $i : ${w}x${h}"
}

# Load largest frame as bitmap and sample colors
$icon = New-Object System.Drawing.Icon($icoPath, 256, 256)
$bmp = $icon.ToBitmap()
Write-Host "Loaded bitmap: $($bmp.Width)x$($bmp.Height)"

$colors = @{}
for ($y = 0; $y -lt $bmp.Height; $y += 2) {
	for ($x = 0; $x -lt $bmp.Width; $x += 2) {
		$p = $bmp.GetPixel($x, $y)
		if ($p.A -lt 128) { continue }
		# quantize to reduce noise
		$key = ('{0:X2}{1:X2}{2:X2}' -f (($p.R -band 0xF0)), (($p.G -band 0xF0)), (($p.B -band 0xF0)))
		$colors[$key] = [int]$colors[$key] + 1
	}
}
Write-Host "Top quantized colors (RGB, count):"
$colors.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 12 | ForEach-Object { Write-Host "  #$($_.Key)  $($_.Value)" }
$bmp.Dispose(); $icon.Dispose()
