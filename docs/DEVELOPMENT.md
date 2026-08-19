# FlowRate Development Guide

Developer-facing reference: environment, workflows, architecture, and conventions.
Updated every iteration alongside README.md, CHANGELOG.md, and TODO.md.

## Environment
- **OS**: Windows 11 (build 22000+)
- **IDE**: Visual Studio 2026 (Community or higher) with Windows App SDK workload
- **SDK**: .NET 10
- **Runtime dependency**: `iperf3.exe` in PATH or next to FlowRate.exe

## Solution Layout
| Project | Target | Purpose |
|---|---|---|
| `src/FlowRate` | `net10.0-windows10.0.26100.0`, x64 | WinUI 3 app: views, view models, converters, custom controls (`SpeedometerGauge`, `ThroughputChart`) |
| `src/FlowRate.Core` | `net10.0`, x64 | Domain models, iperf3 transport/parser, `Iperf3Service`, `RunHistoryService`, `SettingsService` |
| `tests/FlowRate.Core.Tests` | `net10.0`, x64 | xUnit, fixture-based tests against real iperf3 JSON output in `fixtures/` |

## Build, Test, Run
```powershell
dotnet restore FlowRate.slnx
dotnet build FlowRate.slnx
dotnet test FlowRate.slnx
cd src/FlowRate; dotnet run   # registers Windows App SDK debug identity
```

## Solution Configuration Notes (important)
- The solution defines a single platform, **x64**. All projects declare `<Platforms>x64</Platforms>`.
- `FlowRate.slnx` carries an explicit mapping `<Platform Solution="*|x64" Project="x64" />` for
  `FlowRate.csproj`. **Do not remove it** — without it, Visual Studio's default mapping targets
  `Any CPU`, which the project doesn't define, producing a persistent
  "project configuration does not exist" warning (fixed in v0.5.2).

## Architecture
Layered, one-way dependencies (UI → Services → Domain ← Transport):
- **Transport** (`FlowRate.Core.Iperf3.Transport`): `System.Text.Json` DTOs mirroring iperf3 JSON.
- **Domain** (`FlowRate.Core.Domain`): normalized models (`BenchmarkSummary`, `ThroughputMetrics`, …).
- **Services** (`FlowRate.Core.Services`): `Iperf3Service` process execution with `--json-stream`, cancellation, UDP `-b 0` default.
- **UI** (`FlowRate`): MVVM via CommunityToolkit.Mvvm; custom-drawn controls, no charting dependencies.

## Persistence Paths
- Settings/profiles: `%LOCALAPPDATA%\FlowRate\settings.json`
- Run history: `%LOCALAPPDATA%\FlowRate\history\` (`index.json` capped at 100 runs)

## Packaging & Deployment (v0.6.0)
- **Package identity**: `src/FlowRate/Package.appxmanifest` — `RejectH0.FlowRate`, publisher `CN=RejectH0`. Keep the manifest `Version` in sync with the csproj `<Version>`.
- **MSIX (single-project)**: `EnableMsixTooling` is on; signing is **off by default** (`AppxPackageSigningEnabled=false`) so plain builds never fail. To produce a signed sideload package, create a self-signed cert with subject `CN=RejectH0`, then pass `-p:AppxPackageSigningEnabled=true -p:PackageCertificateThumbprint=<thumbprint>` (or use VS __Package and Publish__). Output goes to `artifacts\msix\`.
- **Standalone (unpackaged)**: `dotnet publish src/FlowRate/FlowRate.csproj -c Release -p:PublishProfile=win-x64-unpackaged` → self-contained folder at `bin\Release\...\win-x64\publish-unpackaged\` (~267 MB). Uses `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true`; no `dotnet run` identity registration needed. **Trimming stays disabled** in this profile — WinUI 3 XAML reflection breaks under trimming.
- **Launch profiles**: `FlowRate (Package)` runs with MSIX identity; `FlowRate (Unpackaged)` runs unpackaged via debug identity.

## Documentation Policy
Every iteration must meaningfully update (no null updates):
1. **README.md** — user-facing state: version, features, roadmap.
2. **CHANGELOG.md** — Keep a Changelog format; newest entry on top.
3. **TODO.md** — current iteration tasks and backlog.
4. **docs/DEVELOPMENT.md** — this file; workflows, architecture, conventions.
