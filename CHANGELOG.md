# FlowRate Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
