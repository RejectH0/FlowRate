# FlowRate Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Fixed
- **Missing icons in unpackaged deployment** (`src/FlowRate/FlowRate.csproj`): `Assets\` content wasn't copied to the publish output, so `AppWindow.SetIcon` silently failed (generic Alt+Tab/title-bar icon) and the title-bar image had nothing to load. `AppIcon.ico`, `Square44x44Logo.scale-100.png`, and `HeaderIcon.png` now use `CopyToOutputDirectory=PreserveNewest`.
- **Header badge showed generic glyph** (`src/FlowRate/MainWindow.xaml`): the branded header used a drawn gradient square with a font glyph instead of the app icon. Replaced with an `<Image>` of `Assets/HeaderIcon.png` (the v2 `app-icon-128.png`).

### Changed
- **New brand assets v2 ingested** (`assets/`, `src/FlowRate/Assets/`): the approved flowrate-assets-v2 set (app icons, brand palette, logos, wordmarks, gauge hero, website favicons/OG images, concept board) replaced the interim generated assets. The application's `AppIcon.ico` was replaced with the new artwork and all ten MSIX tile/logo PNGs (Square44x44/150x150, Wide310x150, SplashScreen, StoreLogo, LockScreenLogo) were regenerated from `app-icon-1024.png` via `assets/tools/generate-msix-assets.ps1`.

### Added
- **Website/brand assets** (`assets/`): icon exports extracted from the app's `AppIcon.ico` (`flowrate-icon-1024x768.png`, `flowrate-icon-1024.png`, exact `flowrate-icon-256.png`), six SVG text-logo variations, a brand color swatch (palette sampled from the icon), and a README documenting the palette. Extraction scripts live in `assets/tools/`. *(Superseded in the same cycle by the approved v2 asset set.)*

---

## [0.7.1] - Self-Signed MSIX Sideload Pipeline

### Added
Output: `artifacts\msix\FlowRate_0.7.1.0_x64_Test\FlowRate_0.7.1.0_x64.msix`.

### Changed
- **Trimming disabled globally** (`src/FlowRate/FlowRate.csproj`): `PublishTrimmed` is now `False` for all configurations. The app's reflection-based `System.Text.Json` usage (parser, history, settings, export) generated IL2026/IL2104 trim warnings and is not trim-safe. Rebuild after the change produced zero trim warnings.

---

## [0.7.0] - UI Fixes, iperf3 Detection, Info Dialog & Update Checks

### Fixed
- **Title bar covering content** (`src/FlowRate/MainWindow.xaml`(+`.cs`)): `ExtendsContentIntoTitleBar` was enabled without a registered drag region, so the caption area overlaid the header. Added a dedicated 36px `AppTitleBar` row (app icon + caption text) registered via `SetTitleBar`; content now starts below it.
- **Default taskbar/Start icon** (`src/FlowRate/FlowRate.csproj`): the exe carried no embedded icon, so the shell fell back to the default glyph in unpackaged builds. Added `<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>`.

### Added
- **iperf3 startup detection** (`src/FlowRate.Core/Services/Iperf3Locator.cs`, `src/FlowRate/App.xaml.cs`): FlowRate now resolves iperf3.exe (app folder first, then PATH) at launch. If missing, a dialog directs the user to the official Windows builds (https://github.com/ar51an/iperf3-win-builds) with an Open Download Page button, then exits.
- **Information dialog** (`src/FlowRate/MainWindow.xaml`(+`.cs`)): new (i) button left of the Settings gear shows the detected iperf3 executable path and version, the FlowRate version, and a "Check for updates" action.
- **Update checks via GitHub** (`src/FlowRate.Core/Services/UpdateService.cs`): queries the GitHub releases/latest API for both `RejectH0/FlowRate` and `ar51an/iperf3-win-builds`, compares versions, and opens the release page when an update is available. Full silent self-update deferred until signing/hosting (flowrate.tech) is finalized.
- **Resolved iperf3 path in benchmarks** (`src/FlowRate.Core/Services/Iperf3Service.cs`): the service now launches the located absolute iperf3 path (bundled copy wins over PATH).

### Changed
- **Package publisher identity** (`src/FlowRate/Package.appxmanifest`): `Publisher` changed from `CN=RejectH0` to `CN=flowrate.tech` (matching the newly registered project domain); manifest version 0.7.0.0.
- **README acknowledgements**: proper sourced credits for iperf3 (ESnet / Lawrence Berkeley National Laboratory, BSD-3) and the Windows builds maintainer (ar51an), with hyperlinks.
- **Iteration workflow**: every iteration now ends with `git add/commit/push` — pending 0.5.1–0.6.0 work was pushed to GitHub at the start of this iteration.

---

## [0.6.0] - MSIX Packaging & Standalone Deployment

### Added
- **Real package identity** (`src/FlowRate/Package.appxmanifest`): replaced template placeholders — identity is now `RejectH0.FlowRate` / `CN=RejectH0` at version `0.6.0.0`; removed the unused `systemAIModels` capability (only `runFullTrust` remains).
- **MSIX packaging properties** (`src/FlowRate/FlowRate.csproj`): signing disabled by default so builds/CI never fail without a certificate; no bundling; package output routed to `artifacts\msix\`. To sign a sideload package, enable `AppxPackageSigningEnabled` with a certificate whose subject matches `CN=RejectH0`.
- **Standalone unpackaged publish profile** (`src/FlowRate/Properties/PublishProfiles/win-x64-unpackaged.pubxml`): `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true` + self-contained runtime produces a folder-deployable `FlowRate.exe` that runs without MSIX identity, an installed Windows App SDK runtime, or `dotnet run` debug-identity registration. Trimming is disabled in this profile (unsafe with WinUI 3 XAML reflection). Verified: 516 files / ~267 MB, exe and Windows App Runtime bootstrap present.

### Verified
- Solution builds clean (0 warnings / 0 errors) and all 16 tests pass with the packaging changes.

---

## [0.5.2] - Solution Platform Fix & Documentation Overhaul

### Fixed
- **Persistent "project configuration does not exist" warning** (`FlowRate.slnx`): the solution declared only an `x64` platform but had no per-project platform mapping, so Visual Studio's default mapping targeted `Debug|Any CPU` / `Release|Any CPU` for `FlowRate.csproj`, which declares `<Platforms>x64</Platforms>` only. Added an explicit `<Platform Solution="*|x64" Project="x64" />` mapping to `src/FlowRate/FlowRate.csproj` in the solution file. Deleting the `.vs` cache had no effect because the mapping is derived, not cached.

### Added
- **`TODO.md`**: living task list tracking the active milestone (v0.6.0 MSIX packaging) and backlog items.
- **`docs/DEVELOPMENT.md`**: developer guide covering environment setup, build/test/run workflows, architecture, solution configuration notes, and the documentation-maintenance policy (README, CHANGELOG, TODO, and DEVELOPMENT are updated every iteration).

### Changed
- **`README.md`**: synced stale version/feature information (was still describing v0.3.0) to reflect the current 0.5.x feature set — run history, throughput chart, UDP mode, profiles, export, preferences — and updated the roadmap checkboxes.

---

## [0.5.1] - Fullscreen Layout, UDP Fix & Lifecycle Logging

### Fixed
- **Fullscreen centering** (`src/FlowRate/MainWindow.xaml`): the content root now uses `HorizontalAlignment="Center"`, so maximizing or going fullscreen keeps the layout centered instead of shifting the whole view to the right edge of the screen.
- **UDP throughput** (`src/FlowRate.Core/Services/Iperf3Service.cs`): iperf3 defaults UDP to a 1 Mbit/sec target bitrate when `-b` is omitted, which produced abysmal UDP numbers. FlowRate now sends `-b 0` (unlimited) for UDP when no explicit target bitrate is set, so UDP is measured comparably to TCP. To verify UDP is working: run a UDP test and confirm the reported throughput is in the expected range along with jitter and 0%–low packet loss in the UDP Quality section.

### Changed
- **Definite process lifecycle logging** (`src/FlowRate/App.xaml.cs`): startup now logs a clear "launched successfully" marker after the main window is activated, a "closed by user" marker when the window is closed, and a "process terminated normally" marker via `AppDomain.ProcessExit`. This removes ambiguity between a normal user-initiated shutdown and an actual crash. The noisy first-chance exception handler was removed.

---

## [0.5.0] - History, Chart, UDP, Profiles & UX

### Added
- **Persistent run history** (`src/FlowRate.Core/History/RunHistoryService.cs`, `HistoryEntry.cs`): every successful run is saved as full JSON under `%LOCALAPPDATA%\FlowRate\history\` with a capped (100-run) `index.json`. A Run History panel lists past runs; selecting one re-views its results and rebuilds its chart so it can be re-exported.
- **Throughput-over-time chart** (`src/FlowRate/Controls/ThroughputChart.xaml`): a dependency-free, custom-drawn line/area chart plots per-interval Mbps live during a run and for any reviewed history entry, revealing ramp-up, dips, and jitter the gauge cannot show.
- **UDP mode** (`src/FlowRate.Core/Services/Iperf3Service.cs`, transport/domain/parser): a TCP/UDP toggle runs iperf3 with `-u`; jitter and packet-loss are parsed (`Iperf3Stream`/`Iperf3End` UDP fields, `ThroughputMetrics`/`BenchmarkSummary.Udp`) and shown in a new "UDP Quality" section of the results.
- **Preset profiles** (`src/FlowRate.Core/Settings/AppSettings.cs` `BenchmarkProfile`): named, savable configurations (server, port, duration, streams, reverse, UDP, bitrate, window) selectable from a dropdown, in addition to the existing global defaults.
- **Bitrate and window options**: target bitrate (`-b`, in Mbps) and TCP window / socket buffer (`-w`, in KB) are exposed in the configuration card; blank/zero means the iperf3 default.
- **Cancel / Stop button**: an in-progress benchmark can be stopped; the iperf3 child process tree is killed cleanly via a `CancellationToken`.
- **Copy to clipboard**: a Copy button on the results card copies the formatted results text.
- **Recent servers**: recently used server addresses are remembered (capped, de-duplicated) and selectable from a dropdown.

### Changed
- `Iperf3Service.RunBenchmarkAsync` now accepts protocol, target bitrate, window size, and a cancellation token.
- `Iperf3Parser` maps UDP jitter/packet-loss and falls back to the UDP aggregate `sum` when `sum_sent`/`sum_received` are absent.

### Notes
- Delivered with reasonable defaults where interactive confirmation was unavailable: full-JSON history capped at 100 runs, a custom-drawn chart (no new dependencies), non-destructive profiles alongside the global default, and optional (blank = iperf3 default) bitrate/window fields.

---

## [0.4.5] - Interactive Export

### Fixed
- **Export buttons had no visible effect** (`src/FlowRate/ViewModels/MainViewModel.cs`, `src/FlowRate/MainWindow.xaml.cs`): the export commands previously wrote a timestamped file silently into `Documents\FlowRate` with no prompt, so clicking Export JSON / Export CSV appeared to do nothing. Both commands now open a native **Save As** dialog where the user chooses the location and file name, write the file there, and report the full saved path in the status line. The picker is initialized against the window HWND as required for WinUI desktop file pickers.

---

## [0.4.4] - Export, Results Formatting & Preferences

### Added
- **Persistent preferences** (`src/FlowRate.Core/Settings/AppSettings.cs`, `SettingsService.cs`): server address, port, duration, parallel streams, reverse mode, and the show-all-intervals option are now stored as JSON in `%LOCALAPPDATA%\FlowRate\settings.json` and restored on every launch.
- **Preferences dialog** (`src/FlowRate/SettingsDialogContent.xaml`): a gear button in the header opens a dialog where the current configuration can be saved as the defaults for future sessions, so common values like the iperf3 server address no longer need to be re-typed each run.

### Fixed
- **Export buttons stayed disabled** (`src/FlowRate/ViewModels/MainViewModel.cs`): `ExportJsonCommand` and `ExportCsvCommand` now re-evaluate when `IsRunning` changes, so the Export JSON / Export CSV buttons enable correctly once a benchmark finishes.
- **Garbled results dividers** (`src/FlowRate/ViewModels/MainViewModel.cs`): the results summary previously rendered double-encoded box-drawing characters (e.g., `â•`, `â”€`). The divider rules are now portable ASCII (`=` and `-`) that render cleanly.

---

## [0.4.3] - Custom FlowRate Icon

### Added
- **Custom brand icon set** (`src/FlowRate/tools/GenIcons.ps1`): a reproducible generator that renders the FlowRate mark â€” a teal-to-cyan rounded square with a white speedometer arc, tick marks, and an angled needle â€” at every required resolution. Replaces the default WinUI template placeholder art.

### Changed
- Regenerated all Windows tile and icon assets (`Square44x44`, `Square150x150`, `Wide310x150`, `StoreLogo`, `LockScreenLogo`, `SplashScreen`) plus a multi-resolution `AppIcon.ico` (16/24/32/48/64/128/256) so the title bar, taskbar, Start menu, and splash all use the branded icon.
- Registered the added `scale-100` tile variants in the project so they deploy with the package.

### Notes
- Windows caches shell icons aggressively; if the taskbar still shows the old glyph after updating, it is a shell cache artifact (clearing the icon cache or re-pinning refreshes it).

---

## [0.4.2] - Gauge Marker Fix, Smoothing & Windows Icon

### Fixed
- **Average marker misplacement** (`src/FlowRate/Controls/SpeedometerGauge.xaml.cs`): the rim marker triangle was rotated using the wrong reference angle (subtracting the 135-degree start angle instead of the 270-degree geometry base), which placed the marker ~135 degrees off from its true value (e.g., a ~8000 Mbps reading appeared near the 18000 tick). The marker now sits at the correct position on the dial.
- **Missing taskbar/title-bar icon** (`src/FlowRate/MainWindow.xaml.cs`): the window now applies the bundled `Assets/AppIcon.ico` via an absolute path resolved from the app base directory, so FlowRate shows a proper icon in the title bar and on the Windows taskbar. (The prior relative-path attempt had been disabled because it crashed at startup.)

### Changed
- **Exponential needle smoothing** (`src/FlowRate/Controls/SpeedometerGauge.xaml.cs`): the needle position is now passed through an exponential moving average (smoothing factor 0.35) before animating, layering additional fluidity on top of the average-tracking value for convincing, trustworthy motion.
- **Manifest metadata** (`src/FlowRate/Package.appxmanifest`): gave the application a meaningful `Description` for a more complete, compliant Windows app presentation.

---

## [0.4.1] - Responsive Window & Smoother Gauge

### Changed
- **Window sizing** (`src/FlowRate/MainWindow.xaml.cs`): the window now launches at a sensible default size (1040x920) and is centered on the primary work area, so it no longer opens cramped or biased to one side. The layout remains fully responsive as the window is resized or maximized.
- **Gauge always visible** (`src/FlowRate/MainWindow.xaml`): the live throughput card (and its speedometer gauge) is now shown from launch instead of appearing only after the first interval arrives, giving a stable, non-shifting layout.
- **Smoother needle** (`src/FlowRate/Controls/SpeedometerGauge.xaml.cs`): the needle now tracks the running average (a naturally smoother signal) rather than the jittery per-interval current value, and its sweep animation was lengthened to ~950ms with an ease-in-out curve so motion reads as fluid and trustworthy rather than herky-jerky. The instantaneous current value is now shown by the rim marker.
- Legend and readouts updated so the needle is labelled AVERAGE and the marker is labelled CURRENT, matching the new gauge behavior.

---

## [0.4.0] - Branded Visual Overhaul

### Added
- **Brand design system** (`src/FlowRate/Themes/Brand.xaml`): a centralized resource dictionary with an accent palette (blue/cyan/teal/green/amber/coral), signature gradient brushes (`BrandAccentGradient`, `BrandSpeedGradient`, `BrandCardGradient`), a typography scale (`BrandDisplayText`/`BrandTitleText`/`BrandSubtitleText`/`BrandCaptionText`), and reusable card styles (`BrandCard`, `BrandAccentCard`). Merged into `App.xaml` so every view can consume it.
- **Animated numeric readouts** (`src/FlowRate/Behaviors/NumberAnimator.cs`): an attached-property helper that eases a `TextBlock` toward its target value, giving live throughput numbers a lively counting feel. Exposes `Value`, `Format`, and `Suffix` attached properties.

### Changed
- **Main window redesign** (`src/FlowRate/MainWindow.xaml`): rebuilt as a scrollable, centered layout of branded glass cards over the Mica backdrop, pairing the speedometer gauge with large animated Current/Average readouts and a results card that appears only when a summary is present.
- Results-card visibility is now driven by a `MainViewModel.HasResult` boolean instead of a value converter, avoiding converter usage on the `Window` root.

---

## [0.3.2] - Gauge Fix & Hardening Pass

### Fixed
- **Speedometer gauge crash**: the gauge threw `System.ArgumentException: Value does not fall within the expected range` at `Path.set_Data` because a `Geometry` built for one `Path` was reassigned to another. The control now builds a fresh, unparented arc `Geometry` on every update, eliminating the crash during live runs.
- **Missing dial numbers**: the gauge now renders numeric tick labels at each major tick, scaled to the auto-scaling dial maximum (whole numbers for coarse scales, decimals for fine ones).
- **Erratic needle**: replaced the unstable needle with a tapered triangle that snaps into place on the first frame and animates smoothly (CubicEase) thereafter, so it tracks throughput instead of jumping.
- **Average marker**: the running average is now a clearly rendered amber rim marker rotated to the average angle, replacing the stray floating dot.
- **Center readout**: the value/unit readout is written first each update so the number always renders even if a later draw step fails.

### Changed
- Converted all `MainViewModel` `[ObservableProperty]` fields to `public partial` properties, clearing all 8 `MVVMTK0045` warnings for correct CsWinRT/WinUI marshalling. Build is now warning-free.

### Added
- **Diagnostics logging**: a lightweight file logger (`FlowRate.Core.Diagnostics.Logger`) plus global exception handlers in `App.xaml.cs` capture startup and crash context to a per-day log file.

### Removed
- Deleted unused `MainPage.xaml` / `MainPage.xaml.cs` template stubs (no references anywhere in the solution).

---

## [0.3.0] - Animated Speedometer Gauge

### Added
- **Custom-drawn speedometer gauge** (`src/FlowRate/Controls/SpeedometerGauge.xaml[.cs]`): a bespoke, speedtest.net-style radial dial rendered from geometry (no third-party control). Features a 270-degree sweep with tick marks, a colored track, and a progress arc.
  - **Animated needle**: rotates to the current interval throughput with a 400ms CubicEase animation for smooth sweeps.
  - **Speed-based color grading**: needle, progress arc, and value readout grade from blue (slow) through cyan/teal to lime-green (fast) based on `Value/Maximum`.
  - **Average marker**: the running average is shown as a distinct amber radial marker, clearly delineated from the current-throughput needle. A color legend under the gauge labels both.
  - **Dependency properties**: `Value`, `Average`, `Maximum`, `Unit`; the control redraws on resize.
- **Auto-scaling dial**: `MainViewModel.GaugeMaximumMbps` tracks the observed peak and snaps up to the next "nice" ceiling (1/2/2.5/5 x 10^n) with ~25% headroom via `NiceCeiling()`. The scale only grows during a run (never shrinks) to avoid needle whiplash. Manual override deferred to a later revision.

### Design Decisions
- User has no design background and requested Copilot design the gauge; implemented as a reusable `UserControl` for future restyling.
- Gauge scale uses Mbps for natural readability; the numeric readouts continue to show Gbps.

---

## [0.2.1] - Live Display Refinements

### Fixed
- **Throughput precision**: Current and Average Gbps now display with exactly three decimal places (`xx.yyy`) via new formatted `CurrentThroughputGbpsText` / `AverageThroughputGbpsText` view-model properties bound in `MainWindow.xaml`.
- **Interval feed order**: The live interval feed beneath Current/Average now shows newest-first (each new interval is inserted at the top rather than appended to the bottom).

---

## [0.2.0] - Live Interval Updates

### Real-Time Throughput Streaming

**User Request**:
> "Let's get those real time updates working... Let's see what a 'Current Throughput' display area which shows the throughput update appending lines as they come."
> "Let's have a preference for either keeping all intervals visible as a scrolling list, or for showing just the most recent + running average. For now this can be a checkbox on the direct UI."

**Discovery**:
- Standard `iperf3 -J` buffers all output and only emits the full JSON blob at completion, so it cannot drive live interval updates.
- Switched to `iperf3 --json-stream`, which emits newline-delimited JSON events (`start`, `interval`, `end`, `error`) as the test runs (iperf 3.17+).

**Changes**:
- `Iperf3Service`: rewrote execution to parse NDJSON events line-by-line, raise a new `IntervalProgress` event (`IntervalProgressEventArgs` with `IntervalSnapshot`, running average Gbps/Mbps), and reassemble a standard `Iperf3Result` blob at completion so the existing (tested) parser handles final mapping unchanged.
- `MainViewModel`: subscribes to `IntervalProgress`, marshals updates onto the UI thread via `DispatcherQueue`, and exposes live state: `LiveThroughputFeed`, `CurrentThroughputGbps/Mbps`, `AverageThroughputGbps/Mbps`, `HasLiveData`, and `ShowAllIntervals` toggle.
- `MainWindow.xaml`: added a "Current Throughput" card showing current + running-average throughput, a scrolling live interval feed, and a "Show all intervals" checkbox toggling between full scrolling history and most-recent + running-average.
- Version bumped to `0.2.0`.

---

## [0.1.0] - 2026/08/10.141500

### Project Inception

**Initial User Prompt**:
> "I want to design and develop a polished, modern Windows desktop network benchmarking and diagnostics application. The immediate objective is to create a substantially better graphical front end for `iperf3` on Windows."

**Key Constraints Established**:
- Windows 11 / .NET 10 / WinUI 3 target platform
- Strict Milestone 1 focus: iperf3 benchmarking only (no scope creep)
- Privacy/OPSEC conscious: no telemetry, no cloud dependencies
- Fixture-driven development: use real iperf3 JSON output for parser design

### Repository Setup - 2026/08/10.080000

**Created**:
- Git repository initialized at `C:\Users\gsper\source\repos\FlowRate`
- `.gitignore` configured for .NET / WinUI / Visual Studio
- `README.md` created with project vision and Milestone 1 objectives
- `fixtures/README.md` documenting real iperf3 test scenarios
- GitHub remote configured (HTTPS): `https://github.com/RejectH0/FlowRate.git`

**Real iperf3 Fixtures Collected** (9 total):
1. `tcp-forward-10s.json` - Basic TCP forward test
2. `tcp-reverse-10s.json` - TCP reverse mode
3. `tcp-parallel-4stream-10s.json` - Parallel streams test
4. `udp-forward-10s.json` - UDP test
5. `tcp-forward-bidir-10s.json` - Bidirectional test
6. `tcp-fail-refused.json` - Connection refused failure
7. `tcp-fail-timeout.json` - Connection timeout failure
8. `tcp-fail-unreachable.json` - Network unreachable failure
9. `tcp-fail-invalid-json.txt` - Malformed output

### Project Structure - 2026/08/10.083000

**Created Solution**: `FlowRate.slnx`

**Projects**:
1. `src/FlowRate.Core/FlowRate.Core.csproj`
   - Target: `net10.0`
   - Purpose: Core business logic, iperf3 integration, domain models

2. `src/FlowRate/FlowRate.csproj`
   - Target: `net10.0-windows10.0.26100.0`
   - WinUI 3 desktop application
   - Dependencies: `CommunityToolkit.Mvvm` (8.4.2), `Microsoft.WindowsAppSDK` (2.3.1)

3. `tests/FlowRate.Core.Tests/FlowRate.Core.Tests.csproj`
   - Target: `net10.0`
   - xUnit test project with fixture-based validation

**Commit**: Initial commit - repository structure and fixtures

### Transport Models - 2026/08/10.092000

**Created** (`src/FlowRate.Core/Iperf3/Transport/`):
- `Iperf3Result.cs` - Root JSON deserialization model
- `Iperf3Start.cs` - Test configuration and metadata
- `Iperf3Interval.cs` - Per-interval streaming statistics
- `Iperf3End.cs` - Final summary statistics
- `Iperf3Stream.cs` - Per-stream detailed data
- `Iperf3Sum.cs` - Aggregated metrics
- `Iperf3CpuUtilization.cs` - CPU usage data
- Supporting types: `Connected`, `TestStart`, `TcpInfo`, etc.

**Key Design Decisions**:
- Nullable properties to handle both success and error scenarios
- `IsSuccess` computed property based on `Error` field presence
- JSON property name mapping via `[JsonPropertyName]` attributes
- All numeric fields support null to handle sparse JSON

**Commit**: `320acf3` - "Add iperf3 transport models for JSON deserialization"

### Domain Models - 2026/08/10.095000

**Created** (`src/FlowRate.Core/Domain/`):
- `BenchmarkResult.cs` - Normalized top-level result
- `BenchmarkConfiguration.cs` - Test parameters
- `ConnectionInfo.cs` - Endpoint details
- `IntervalResult.cs` - Per-interval measurements
- `SummaryResult.cs` - Final aggregated results
- `CpuUtilization.cs` - CPU metrics
- Supporting enums: `Protocol`, `Direction`

**Design Philosophy**:
- Clean domain layer separate from transport/JSON concerns
- Convenience properties: `ThroughputGbps`, `ThroughputMbps`, `DataTransferredGb`
- Strongly-typed enums instead of strings
- Comprehensive coverage of all iperf3 metrics needed for UI

**Commit**: `6519fce` - "Add domain models for normalized benchmark results"

### Parser Implementation - 2026/08/10.102000

**Created**: `src/FlowRate.Core/Iperf3/Iperf3Parser.cs`

**Responsibilities**:
- Deserialize iperf3 JSON output to transport models
- Map transport models to normalized domain models
- Handle both success and error scenarios
- Provide clean API: `BenchmarkResult Parse(string json)`

**Key Mapping Logic**:
- `MapConfiguration()` - Extract test parameters from `start` section
- `MapDirection()` - Infer forward/reverse from test configuration
- `MapIntervals()` - Convert streaming intervals to domain results
- `MapSummary()` - Aggregate final statistics from `end` section
- `MapCpuUtilization()` - Extract local/remote CPU usage
- `MapTimestamp()` - Convert Unix timestamps to `DateTimeOffset`

**Commit**: `7fe877a` - "Add Iperf3Parser and comprehensive fixture-based tests"

### Parser Tests - 2026/08/10.104000

**Created**: `tests/FlowRate.Core.Tests/Iperf3/Iperf3ParserTests.cs`

**Test Coverage**:
- Parse TCP forward test successfully
- Parse TCP reverse test successfully
- Parse parallel streams test successfully
- Parse UDP test successfully
- Parse bidirectional test successfully
- Parse connection refused error
- Parse connection timeout error
- Parse network unreachable error
- Handle null/empty/invalid JSON gracefully
- Validate convenience properties (Gbps, MB calculations)

**Test Infrastructure**:
- Copied all fixtures to `tests/FlowRate.Core.Tests/Fixtures/`
- Configured fixtures as embedded resources
- All 10 tests passing (validated via `dotnet test`)

**Commit**: Included in `7fe877a`

### UI Shell - First Visual Milestone - 2026/08/10.120000

**Created Service Layer**: `src/FlowRate.Core/Services/Iperf3Service.cs`

**Responsibilities**:
- Execute `iperf3.exe` with configurable parameters
- Capture stdout/stderr asynchronously via `Process` API
- Parse JSON output using `Iperf3Parser`
- Return `BenchmarkResult` to UI layer

**Key Methods**:
- `RunBenchmarkAsync()` - Main entry point with cancellation support
- `BuildArguments()` - Construct iperf3 CLI arguments
- `ExecuteIperf3Async()` - Launch process and capture output

**Created ViewModel**: `src/FlowRate/ViewModels/MainViewModel.cs`

**Properties** (using `CommunityToolkit.Mvvm`):
- Configuration: `ServerAddress`, `Port`, `DurationSeconds`, `ParallelStreams`, `ReverseMode`
- State: `IsRunning`, `StatusMessage`, `LastResult`, `ResultSummary`
- Command: `RunBenchmarkCommand` (async, with `CanExecute`)

**Result Formatting**:
- `FormatSuccessResult()` - Beautiful ASCII-art style output with box drawing characters
- Sections: Server info, protocol details, throughput metrics, CPU utilization
- Human-readable units: Gbps/Mbps, GB/MB, percentage formatting

**Created UI**: `src/FlowRate/MainWindow.xaml`

**Layout**:
- Header: App title and subtitle with Fluent typography
- Configuration: Server address, port, duration, parallel streams, reverse mode checkbox
- Action: Run benchmark button with status message
- Results: Scrollable monospace text block for formatted output

**Visual Design**:
- `<MicaBackdrop />` for modern Windows 11 material
- Fluent spacing and padding (24px margins, 12px internal spacing)
- `NumberBox` controls for numeric inputs with min/max validation
- Monospace font (`Consolas`) for results display

**Created Converters**: `src/FlowRate/Converters/ValueConverters.cs`
- `InverseBoolConverter` - For enabled state inversions
- `InverseBoolToVisibilityConverter` - For visibility toggling

**Updated**: `src/FlowRate/App.xaml`
- Registered converters in resource dictionary

**Updated**: `src/FlowRate/MainWindow.xaml.cs`
- Exposed `ViewModel` property for `x:Bind` support
- Set `ExtendsContentIntoTitleBar = true` for modern chrome
- Icon configuration (initially attempted, later fixed)

**Commit**: `a6c18b2` - "Add UI shell: service, view model, and main window layout"

**Known Issues**:
- MVVMTK0045 warnings: `[ObservableProperty]` fields not AOT-compatible in WinRT (deferred for later refactor)

### Icon Crash Fix - 2026/08/10.134000

**Issue Discovered**:
- App crashed immediately on launch with `0x80040154 (REGDB_E_CLASSNOTREG)`
- Root cause: `AppWindow.SetIcon("Assets/AppIcon.ico")` failed because icon not deployed to output
- Secondary issue: WinUI 3 unpackaged apps require Windows App SDK runtime registration

**Resolution**:
- Commented out `SetIcon()` call with TODO note for proper asset deployment
- Documented correct launch method: `dotnet run` (registers debug package identity via WinApp build tools)
- Direct `.exe` launch fails without runtime registration

**Commit**: `3cc6ca1` - "Fix app icon crash - comment out SetIcon until asset deployment configured"

### First Successful Run - 2026/08/10.141000

**Validation**:
- Launched via `dotnet run` from `src/FlowRate/` directory
- App window displayed successfully with Mica backdrop
- Executed real iperf3 benchmark against `10.20.65.160:5201`
- Test parameters: 16 parallel streams, 10 second duration, TCP forward
- **Results**: 9.26 Gbps sustained throughput (9259 Mbps)
- Formatted output confirmed beautiful and readable

**User Feedback**:
> "Aw, hell-yes!! We've got a UI and we've got a basic functioning application."

**Current State**:
- Fixture-based parser fully tested
- Clean domain model architecture
- WinUI 3 functional UI shell
- Real iperf3 integration working
- Professional formatted output
- Static results (no real-time updates yet)
- No visual gauges/charts yet

---

## [Unreleased]

### Next Planned Features

**Stage 1: Real-time Interval Updates** (Target: v0.2.0)
- Modify `Iperf3Service` to expose progress events
- Stream interval results as they arrive from iperf3
- Update UI during test execution (not just at completion)
- Live throughput display

**Stage 2: Animated Throughput Gauge** (Target: v0.3.0)
- Circular speedometer visualization
- Real-time animation during test
- Color-coded performance zones
- WinUI composition API integration

**Stage 3: Enhanced Results Cards** (Target: v0.4.0)
- Acrylic/Mica material cards for metric sections
- Icon badges and visual indicators
- Animated number counters
- Color-coded progress bars for CPU utilization

---

## Development Guidelines

Project process, versioning, commit discipline, documentation rules, and session-recovery
strategy are maintained in [`WORKFLOW.md`](WORKFLOW.md).

---

**Current Version**: 0.1.0
**Last Updated**: 2026/08/10.141500
**Status**: Functional UI shell with working iperf3 integration; ready for real-time updates implementation
