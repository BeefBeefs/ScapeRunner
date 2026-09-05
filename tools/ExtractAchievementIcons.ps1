param(
    [Parameter(Mandatory = $true)]
    [string]$SourceSheet
)

Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot 'OSRSIdle\Resources\Images\Achievements'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$columnLefts = @(25, 166, 304, 444, 584, 723, 864, 1003, 1140)
$columnRights = @(132, 271, 410, 551, 689, 829, 967, 1103, 1241)
$rowTops = @(15, 103, 191, 279, 371, 463, 553, 644, 736, 826)
$rowBottoms = @(92, 180, 269, 358, 450, 540, 630, 722, 814, 903)

function Copy-Region {
    param(
        [System.Drawing.Bitmap]$Source,
        [System.Drawing.Rectangle]$Region
    )

    $copy = [System.Drawing.Bitmap]::new(
        $Region.Width,
        $Region.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($copy)
    try {
        $graphics.DrawImage(
            $Source,
            [System.Drawing.Rectangle]::new(0, 0, $Region.Width, $Region.Height),
            $Region,
            [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $graphics.Dispose()
    }

    return $copy
}

function Remove-ConnectedBackground {
    param([System.Drawing.Bitmap]$Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $visited = [bool[]]::new($width * $height)
    $queue = [System.Collections.Generic.Queue[System.Drawing.Point]]::new()

    for ($x = 0; $x -lt $width; $x++) {
        $queue.Enqueue([System.Drawing.Point]::new($x, 0))
        $queue.Enqueue([System.Drawing.Point]::new($x, $height - 1))
    }
    for ($y = 1; $y -lt ($height - 1); $y++) {
        $queue.Enqueue([System.Drawing.Point]::new(0, $y))
        $queue.Enqueue([System.Drawing.Point]::new($width - 1, $y))
    }

    while ($queue.Count -gt 0) {
        $point = $queue.Dequeue()
        $index = $point.Y * $width + $point.X
        if ($visited[$index]) { continue }
        $visited[$index] = $true

        $color = $Bitmap.GetPixel($point.X, $point.Y)
        $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
        if ($maximum -gt 58 -or ($maximum - $minimum) -gt 20) { continue }

        $Bitmap.SetPixel($point.X, $point.Y, [System.Drawing.Color]::Transparent)

        if ($point.X -gt 0) { $queue.Enqueue([System.Drawing.Point]::new($point.X - 1, $point.Y)) }
        if ($point.X + 1 -lt $width) { $queue.Enqueue([System.Drawing.Point]::new($point.X + 1, $point.Y)) }
        if ($point.Y -gt 0) { $queue.Enqueue([System.Drawing.Point]::new($point.X, $point.Y - 1)) }
        if ($point.Y + 1 -lt $height) { $queue.Enqueue([System.Drawing.Point]::new($point.X, $point.Y + 1)) }
    }
}

function Get-OpaqueBounds {
    param([System.Drawing.Bitmap]$Bitmap)

    $left = $Bitmap.Width
    $top = $Bitmap.Height
    $right = -1
    $bottom = -1

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($Bitmap.GetPixel($x, $y).A -le 8) { continue }
            $left = [Math]::Min($left, $x)
            $top = [Math]::Min($top, $y)
            $right = [Math]::Max($right, $x)
            $bottom = [Math]::Max($bottom, $y)
        }
    }

    if ($right -lt $left -or $bottom -lt $top) {
        throw 'An achievement crop contained no artwork.'
    }

    return [System.Drawing.Rectangle]::FromLTRB($left, $top, $right + 1, $bottom + 1)
}

function Remove-CardLabelPixels {
    param([System.Drawing.Bitmap]$Bitmap)

    # Card numbers sit entirely in this upper-left corner, including their
    # dark antialiasing. The artwork starts below or to the right of this zone.
    $labelWidth = [Math]::Min(28, $Bitmap.Width)
    $labelHeight = [Math]::Min(20, $Bitmap.Height)
    for ($y = 0; $y -lt $labelHeight; $y++) {
        for ($x = 0; $x -lt $labelWidth; $x++) {
            $Bitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
        }
    }
}

function Export-AchievementIcon {
    param(
        [System.Drawing.Bitmap]$Source,
        [System.Drawing.Rectangle]$Region,
        [string]$OutputPath
    )

    $crop = Copy-Region -Source $Source -Region $Region
    try {
        Remove-ConnectedBackground -Bitmap $crop
        Remove-CardLabelPixels -Bitmap $crop
        $bounds = Get-OpaqueBounds -Bitmap $crop
        $canvasSize = 64
        $padding = 3
        $scale = [Math]::Min(
            ($canvasSize - 2 * $padding) / $bounds.Width,
            ($canvasSize - 2 * $padding) / $bounds.Height)
        $drawWidth = [Math]::Max(1, [int][Math]::Round($bounds.Width * $scale))
        $drawHeight = [Math]::Max(1, [int][Math]::Round($bounds.Height * $scale))
        $destination = [System.Drawing.Rectangle]::new(
            [int](($canvasSize - $drawWidth) / 2),
            [int](($canvasSize - $drawHeight) / 2),
            $drawWidth,
            $drawHeight)

        $canvas = [System.Drawing.Bitmap]::new(
            $canvasSize,
            $canvasSize,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            $graphics.DrawImage($crop, $destination, $bounds, [System.Drawing.GraphicsUnit]::Pixel)
            $canvas.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $graphics.Dispose()
            $canvas.Dispose()
        }
    }
    finally {
        $crop.Dispose()
    }
}

$sheet = [System.Drawing.Bitmap]::FromFile($SourceSheet)
try {
    $index = 1
    for ($row = 0; $row -lt $rowTops.Count; $row++) {
        for ($column = 0; $column -lt $columnLefts.Count; $column++) {
            # Start far enough inside the card to exclude its number and frame.
            $left = $columnLefts[$column] + 18
            $top = $rowTops[$row] + 7
            $right = $columnRights[$column] - 5
            $bottom = $rowBottoms[$row] - 5
            $region = [System.Drawing.Rectangle]::FromLTRB($left, $top, $right, $bottom)
            $outputPath = Join-Path $outputDirectory ('achievement_{0:D3}.png' -f $index)
            Export-AchievementIcon -Source $sheet -Region $region -OutputPath $outputPath
            $index++
        }
    }
}
finally {
    $sheet.Dispose()
}

Write-Host "Extracted $($index - 1) achievement icons to $outputDirectory"
