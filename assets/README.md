# FlowRate Brand & Website Assets

Graphics assets for the flowrate.tech website. The icon PNGs are extracted
directly from the application's icon (`src/FlowRate/Assets/AppIcon.ico`) so
the web branding matches the app exactly.

## Contents

### `website/`
| File | Description |
|---|---|
| `flowrate-icon-1024x768.png` | App icon on a 1024x768 transparent canvas (icon 768px, centered) |
| `flowrate-icon-1024.png` | App icon at 1024x1024 (square, transparent) — good for social/OG images |
| `flowrate-icon-256.png` | Exact 256px frame extracted from `AppIcon.ico` (no scaling) |
| `logo-text-v1-solid-teal.svg` | Text logo — solid Brand Teal, bold |
| `logo-text-v2-gradient.svg` | Text logo — deep-teal-to-cyan horizontal gradient |
| `logo-text-v3-weight-split.svg` | Text logo — "Flow" light / "Rate" bold, two-tone |
| `logo-text-v4-dark-badge.svg` | Text logo — dark badge with speed-line accents (dark backgrounds) |
| `logo-text-v5-flow-underline.svg` | Text logo — ink text with gradient flow underline sweep |
| `logo-text-v6-lowercase-tech.svg` | Text logo — lowercase "flowrate • tech" (domain-friendly) |
| `color-swatch.svg` | Full brand palette swatch with names and hex values |

### `tools/`
PowerShell scripts used to extract/generate the icon PNGs. Re-run
`export-icon.ps1` if `AppIcon.ico` ever changes.

## Brand Palette
Sampled from the application icon:

| Name | Hex | Use |
|---|---|---|
| Deep Teal | `#009090` | Gradient start, accents |
| Brand Teal | `#00A0AB` | Primary brand color |
| Teal Cyan | `#00A0B0` | Icon body |
| Flow Cyan | `#00B4C8` | Highlights, gradient end |
| Ice White | `#F0F4F4` | Icon foreground / light text |
| Ink | `#0B2B31` | Dark backgrounds, headings |
| Deep Sea | `#104650` | Dark gradient partner |
| Mid Teal | `#0090A0` | Secondary text accents |
| Slate Gray | `#5A6E72` | Muted/supporting text |
| White | `#FFFFFF` | Page background |

Typeface used in the SVG mock-ups: **Segoe UI** (falls back to Helvetica/Arial).
