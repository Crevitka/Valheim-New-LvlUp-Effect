$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$artDir = Join-Path $PSScriptRoot 'Artwork'
New-Item -ItemType Directory -Force -Path $artDir | Out-Null

function Draw-Art([int]$width, [int]$height, [string]$path, [bool]$cover) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::FromArgb(12, 19, 24))
    $gold = [System.Drawing.Color]::FromArgb(227, 186, 111)
    $cream = [System.Drawing.Color]::FromArgb(244, 235, 214)
    $cx = $width / 2
    $cy = if ($cover) { 272 } else { 480 }
    $radius = if ($cover) { 95 } else { 278 }
    for ($i = 28; $i -ge 1; $i--) {
        $r = $radius + $i * 4
        $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(2, 226, 169, 71))
        $g.FillEllipse($brush, [single]($cx-$r), [single]($cy-$r), [single]($r*2), [single]($r*2))
        $brush.Dispose()
    }
    $pen = [System.Drawing.Pen]::new($gold, $(if ($cover) { 2 } else { 6 }))
    $g.DrawEllipse($pen, [single]($cx-$radius), [single]($cy-$radius), [single]($radius*2), [single]($radius*2))
    $thin = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(85, $gold), $(if ($cover) { 1 } else { 3 }))
    $r2 = $radius - $(if ($cover) { 9 } else { 20 })
    $g.DrawEllipse($thin, [single]($cx-$r2), [single]($cy-$r2), [single]($r2*2), [single]($r2*2))
    $brushGold = [System.Drawing.SolidBrush]::new($gold)
    $brushCream = [System.Drawing.SolidBrush]::new($cream)
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    # Original geometric upward mark, not a game asset or an in-game screenshot.
    $mark = $radius * .5
    $g.DrawLine($pen, [single]$cx, [single]($cy+$mark*.65), [single]$cx, [single]($cy-$mark))
    $g.DrawLine($pen, [single]($cx-$mark*.64), [single]($cy-$mark*.3), [single]$cx, [single]($cy-$mark))
    $g.DrawLine($pen, [single]($cx+$mark*.64), [single]($cy-$mark*.3), [single]$cx, [single]($cy-$mark))
    foreach ($side in @(-1,1)) {
        $start = $cx + $side * ($radius + 20)
        $end = $cx + $side * ($radius + $(if ($cover) { 280 } else { 135 }))
        $g.DrawLine($thin, [single]$start, [single]$cy, [single]$end, [single]$cy)
    }
    if ($cover) {
        $titleFont = [System.Drawing.Font]::new('Georgia', 61, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $smallFont = [System.Drawing.Font]::new('Segoe UI', 22, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString('NEW LVLUP EFFECT', $titleFont, $brushCream, [System.Drawing.RectangleF]::new(0,425,$width,95),$format)
        $g.DrawString('Minimal Nordic skill notifications for Valheim', $smallFont, $brushGold, [System.Drawing.RectangleF]::new(0,540,$width,55),$format)
        $g.DrawString('ROLLING NUMBERS   /   SOFT LIGHT   /   CLIENT-SIDE', $smallFont, $brushCream, [System.Drawing.RectangleF]::new(0,645,$width,45),$format)
        $g.DrawString('CREVITKA   ·   1.2.1', $smallFont, $brushGold, [System.Drawing.RectangleF]::new(0,750,$width,45),$format)
        $smallFont.Dispose(); $titleFont.Dispose()
    } else {
        $labelFont = [System.Drawing.Font]::new('Segoe UI', 67, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString('LEVEL UP', $labelFont, $brushCream, [System.Drawing.RectangleF]::new(0,815,$width,100),$format)
        $labelFont.Dispose()
    }
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $format.Dispose(); $brushCream.Dispose(); $brushGold.Dispose(); $thin.Dispose(); $pen.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
Draw-Art 1024 1024 (Join-Path $artDir 'icon-master.png') $false
Draw-Art 1600 900 (Join-Path $artDir 'cover.png') $true
$original = [System.Drawing.Image]::FromFile((Join-Path $artDir 'icon-master.png'))
$icon = [System.Drawing.Bitmap]::new(256,256)
$graphics = [System.Drawing.Graphics]::FromImage($icon)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($original, 0, 0, 256, 256)
$icon.Save((Join-Path $PSScriptRoot 'icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $icon.Dispose(); $original.Dispose()
