# SVG Icons Guide

This document explains how SVG icons are imported, rasterized, and used in the app.

## Overview

SVG icons are converted to PNG at build time via **Uno.Resizetizer**. PNGs are used with `BitmapIcon.ShowAsMonochrome(true)` for runtime tinting via the `Foreground` brush.

## Folder Structure

```
Assets/Svg/
├── Badges/      # Grammar badges (case, gender, number, person, tense, voice)
├── Menu/        # Start page buttons (nouns, verbs, settings, stats, help)
└── Practice/    # Practice page buttons (reveal, easy, hard, chevrons)
```

## How Resizetizer Works

### BaseSize and Scales

`BaseSize` in the csproj defines the **scale-100** (1x) PNG dimensions. Resizetizer generates multiple scales:

| Scale | Multiplier | Use Case |
|-------|------------|----------|
| scale-100 | 1x | Standard displays |
| scale-125 | 1.25x | 125% Windows scaling |
| scale-150 | 1.5x | 150% Windows scaling |
| scale-200 | 2x | Retina Mac, high-DPI Windows |
| scale-300 | 3x | iPhone Pro, high-DPI Android |
| scale-400 | 4x | Ultra high-DPI displays |

### Avoiding Jagged Edges

**Problem**: If BaseSize is much larger than display size, downscaling causes jagged edges.

**Solution**: Set BaseSize close to the actual display size (at scale-100).

| Icon Category | viewBox | BaseSize | Divisor | Display Height |
|---------------|---------|----------|---------|----------------|
| Badges | ~54x54 | ~18x18 | ÷3 | 16px |
| Menu | 70x70 | 35x35 | ÷2 | 24-30px |
| Practice | 60-90 | 15-23 | ÷4 | 12-16px |

**Rule of thumb**: BaseSize height should be close to display height (within ~2px) for pixel-perfect rendering.

## Adding New Icons

### 1. Export SVG from Figma

- Export at 1x scale
- Note the viewBox dimensions (e.g., `viewBox="0 0 70 70"`)
- Note the desired display height from Figma

### 2. Place SVG in Appropriate Folder

```
Assets/Svg/{Category}/{icon_name}.svg
```

### 3. Add UnoImage Entry to csproj

```xml
<UnoImage Include="Assets\Svg\{Category}\{icon_name}.svg" BaseSize="{width},{height}" />
```

Calculate BaseSize: divide viewBox so height ≈ display height
- 16px display, 54px viewBox: ÷3 → 18px (close match)
- 30px display, 70px viewBox: ÷2 → 35px (close match)
- 13px display, 60px viewBox: ÷4 → 15px (close match)

### 4. Add to Helper Class

Each category has a helper class in `Themes/`:

```csharp
// Themes/MenuIcons.cs
public static class MenuIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Menu/";
    public static string NewIcon => $"{BasePath}new_icon.png";  // Note: .png not .svg
}
```

### 5. Use in UI

```csharp
new BitmapIcon()
    .UriSource(new Uri(MenuIcons.NewIcon))
    .ShowAsMonochrome(true)
    .Height(24)  // Figma @1x height
    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
```

## Helper Classes

| Class | Path | Purpose |
|-------|------|---------|
| `BadgeIcons` | `Themes/BadgeIcons.cs` | Grammar badge icons |
| `MenuIcons` | `Themes/MenuIcons.cs` | Start page menu icons |
| `PracticeIcons` | `Themes/PracticeIcons.cs` | Practice page action icons |

## Current Icon Inventory

### Badges (16px display)
- Cases: nominative, accusative, instrumental, dative, ablative, genitive, locative, vocative
- Genders: male, female, neutral
- Numbers: singular, plural
- Persons: 1st, 2nd, 3rd
- Tenses: present, future, imperative, optative
- Voice: reflexive

### Menu (24-30px display)
- nouns, verbs, settings, stats, help

### Practice (12-16px display)
- reveal (eye icon)
- easy (checkmark)
- hard (X)
- chevron_left, chevron_right (carousel navigation)

## Troubleshooting

### Icons appear blurry
- BaseSize too small → increase it
- Check that scale-200/300 PNGs exist in build output

### Icons appear jagged
- BaseSize too large relative to display size → decrease it
- Rule: BaseSize ≤ 2x display height

### Icons not tinting
- Ensure `ShowAsMonochrome(true)` is set
- Ensure `Foreground` brush is set
- Verify SVG uses single color (black recommended)

### Icons not found
- Check path uses `.png` extension (not `.svg`)
- Verify UnoImage entry exists in csproj
- Clean and rebuild to regenerate PNGs
