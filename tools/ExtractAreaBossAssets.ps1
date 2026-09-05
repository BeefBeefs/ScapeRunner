param(
    [Parameter(Mandatory = $true)]
    [string]$BossSheet,

    [Parameter(Mandatory = $true)]
    [string]$ItemSheet,

    [switch]$BossesOnly
)

Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$enemyOutput = Join-Path $projectRoot 'OSRSIdle\Resources\Images\Enemies'
$itemOutput = Join-Path $projectRoot 'OSRSIdle\Resources\Images\Items'

function Copy-Region {
    param(
        [System.Drawing.Bitmap]$Source,
        [System.Drawing.Rectangle]$Region
    )

    $copy = New-Object System.Drawing.Bitmap $Region.Width, $Region.Height,
        ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
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

function Remove-ConnectedDarkBackground {
    param([System.Drawing.Bitmap]$Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $visited = New-Object 'bool[]' ($width * $height)
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
        $index = ($point.Y * $width) + $point.X
        if ($visited[$index]) { continue }
        $visited[$index] = $true

        $color = $Bitmap.GetPixel($point.X, $point.Y)
        $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
        if ($maximum -gt 62 -or ($maximum - $minimum) -gt 18) { continue }

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
        throw 'No opaque artwork remained after background removal.'
    }

    return [System.Drawing.Rectangle]::FromLTRB($left, $top, $right + 1, $bottom + 1)
}

function Remove-GoldEdgeArtifacts {
    param([System.Drawing.Bitmap]$Bitmap)

    # The source cards use gold corner ornaments. The crop intentionally omits
    # the frame, but a few antialiased ornament pixels can extend inward.
    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($x -ge 22 -and $x -lt ($Bitmap.Width - 22) -and
                $y -ge 22 -and $y -lt ($Bitmap.Height - 22)) {
                continue
            }

            $color = $Bitmap.GetPixel($x, $y)
            if ($color.R -gt 85 -and $color.G -gt 55 -and
                $color.B -lt 90 -and $color.R -gt ($color.B * 1.5)) {
                $Bitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
        }
    }
}

function Clear-OuterCropPixels {
    param([System.Drawing.Bitmap]$Bitmap)

    # Artwork is deliberately cropped with ample breathing room. Clearing this
    # narrow perimeter removes disconnected frame antialiasing without touching
    # any portrait or item silhouette.
    $inset = 10
    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($x -lt $inset -or $x -ge ($Bitmap.Width - $inset) -or
                $y -lt $inset -or $y -ge ($Bitmap.Height - $inset)) {
                $Bitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
        }
    }
}

function Export-FittedAsset {
    param(
        [System.Drawing.Bitmap]$Source,
        [System.Drawing.Rectangle]$Region,
        [int]$Width,
        [int]$Height,
        [int]$Padding,
        [string]$OutputPath,
        [System.Drawing.Drawing2D.InterpolationMode]$InterpolationMode
    )

    $crop = Copy-Region -Source $Source -Region $Region
    try {
        Remove-ConnectedDarkBackground -Bitmap $crop
        Remove-GoldEdgeArtifacts -Bitmap $crop
        Clear-OuterCropPixels -Bitmap $crop
        $bounds = Get-OpaqueBounds -Bitmap $crop

        $scale = [Math]::Min(
            ($Width - 2 * $Padding) / $bounds.Width,
            ($Height - 2 * $Padding) / $bounds.Height)
        $drawWidth = [Math]::Max(1, [int][Math]::Round($bounds.Width * $scale))
        $drawHeight = [Math]::Max(1, [int][Math]::Round($bounds.Height * $scale))
        $destination = [System.Drawing.Rectangle]::new(
            [int](($Width - $drawWidth) / 2),
            [int](($Height - $drawHeight) / 2),
            $drawWidth,
            $drawHeight)

        $canvas = New-Object System.Drawing.Bitmap $Width, $Height,
            ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = $InterpolationMode
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
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

$bosses = @(
    @{ Slug = 'goblin_warlord'; Region = [System.Drawing.Rectangle]::new(25, 25, 325, 375) },
    @{ Slug = 'swamp_king'; Region = [System.Drawing.Rectangle]::new(395, 25, 325, 375) },
    @{ Slug = 'sandsoul_pharaoh'; Region = [System.Drawing.Rectangle]::new(765, 25, 325, 375) },
    @{ Slug = 'the_molten_colossus'; Region = [System.Drawing.Rectangle]::new(1135, 25, 325, 375) },
    @{ Slug = 'frostmaw_leviathan'; Region = [System.Drawing.Rectangle]::new(25, 510, 410, 410) },
    @{ Slug = 'the_voidcaller'; Region = [System.Drawing.Rectangle]::new(480, 510, 435, 410) },
    @{ Slug = 'aethereal_sovereign'; Region = [System.Drawing.Rectangle]::new(960, 510, 500, 410) }
)

$items = @(
    'goblin_warclub', 'swamp_cloak', 'frostbite_staff', 'magma_core',
    'sandswept_tablet', 'astral_essence', 'umbral_shard',
    'goblin_helm', 'swamp_king', 'frost_guard', 'ember_cape',
    'scarab_amulet', 'void_reaver', 'eclipse_crown'
)

$bossBitmap = [System.Drawing.Bitmap]::FromFile($BossSheet)
try {
    foreach ($boss in $bosses) {
        $largePath = Join-Path $enemyOutput "enemy_$($boss.Slug)_64.png"
        $smallPath = Join-Path $enemyOutput "enemy_$($boss.Slug).png"
        Export-FittedAsset -Source $bossBitmap -Region $boss.Region -Width 265 -Height 255 -Padding 5 `
            -OutputPath $largePath -InterpolationMode HighQualityBicubic
        Export-FittedAsset -Source $bossBitmap -Region $boss.Region -Width 32 -Height 32 -Padding 1 `
            -OutputPath $smallPath -InterpolationMode HighQualityBicubic
    }
}
finally {
    $bossBitmap.Dispose()
}

if (-not $BossesOnly) {
    $itemBitmap = [System.Drawing.Bitmap]::FromFile($ItemSheet)
    try {
        for ($index = 0; $index -lt $items.Count; $index++) {
            $column = $index % 7
            $row = [Math]::Floor($index / 7)
            $region = [System.Drawing.Rectangle]::new(
                43 + 232 * $column,
                $(if ($row -eq 0) { 185 } else { 495 }),
                190,
                205)
            $outputPath = Join-Path $itemOutput "item_$($items[$index]).png"
            Export-FittedAsset -Source $itemBitmap -Region $region -Width 32 -Height 32 -Padding 1 `
                -OutputPath $outputPath -InterpolationMode HighQualityBicubic
        }
    }
    finally {
        $itemBitmap.Dispose()
    }
}

$itemCount = if ($BossesOnly) { 0 } else { $items.Count }
Write-Host "Extracted $($bosses.Count) boss portraits (large + compact) and $itemCount item icons."
