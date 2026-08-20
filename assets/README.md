# FlowRate Visual Assets v2

Approved visual direction for **flowrate.tech** and the FlowRate Windows application.

The design evolves the existing cyan/teal identity into a dark Windows 11-inspired system with a distinctive segmented throughput gauge, a white needle, and flowing data-stream ribbons. The gauge + stream combination is the primary FlowRate brand mark.

## Quick handoff to Copilot

- Windows executable icon: `app/AppIcon.ico`
- High-resolution application icon: `app/app-icon-1024.png`
- Vector app icon source: `source/flowrate-app-icon.svg`
- Gauge artwork/reference: `source/flowrate-gauge-hero.svg`
- Website horizontal logo: `website/flowrate-logo-horizontal-dark.svg` for dark backgrounds, `...-light.svg` for light backgrounds
- Website hero gauge: `website/flowrate-gauge-hero.svg` or `flowrate-gauge-hero-1600.png`
- Browser favicon: `website/favicon.ico`, `website/favicon.svg`, and PNG fallbacks (small sizes use a deliberately simplified master for legibility)
- Current application screenshot prepared for the website: `website/flowrate-app-screenshot.webp`
- Social/OpenGraph image: `website/flowrate-og-1200x630.png`
- Palette: `brand/flowrate-brand-palette.svg` and `.png`

## Folders

### `app/`
Windows icon assets at common sizes. `AppIcon.ico` contains multiple resolutions for Windows shell scaling.

### `website/`
SVG/PNG logo artwork, hero illustration, favicons, responsive icons, web-ready application screenshot, and OpenGraph artwork.

### `source/`
Editable SVG masters. Prefer these as canonical sources when implementing native UI or generating future sizes. `flowrate-app-icon-small.svg` is the taskbar-optimized master. `flowrate-app-icon-micro.svg` is the deliberately simplified 16/24px favicon master so the gauge remains recognizable at tiny sizes.

### `brand/`
Palette reference and implementation notes.

### `concept/`
The approved concept board that established this visual direction. This is reference material, not a production UI screenshot.

## Important implementation note

The new gauge is intentionally not a generic speedometer. The segmented arc plus layered flowing data ribbons are meant to visually connect **measurement** with **network flow**. When translating this into native C# controls, preserve that relationship rather than replacing the data ribbons with ordinary automotive gauge decoration.

The supplied application screenshot is not cosmetically altered; it is only optimized into website-friendly formats.
