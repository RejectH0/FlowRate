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

## Packaging & Deployment (v0.7.1)
- **Package identity**: `src/FlowRate/Package.appxmanifest` — `RejectH0.FlowRate`, publisher `CN=flowrate.tech`. Keep the manifest `Version` in sync with the csproj `<Version>`.
- **MSIX (single-project)**: `EnableMsixTooling` is on; signing is **off by default** (`AppxPackageSigningEnabled=false`) so plain builds never fail. To produce a signed sideload package, pass `-p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true -p:PackageCertificateThumbprint=<thumbprint>` (or use VS __Package and Publish__). Output goes to `artifacts\msix\`.
- **Signing certificate (temporary)**: currently a **self-signed** code-signing cert, subject `CN=flowrate.tech` (must match the manifest Publisher), thumbprint `2D2058E79079BFA646967B0E7B2EC622323F8F5A` in `Cert:\CurrentUser\My`, expires 2027-08-19. To sideload, export the cert (`.cer`) and install it to **LocalMachine \ Trusted People** on the target machine, then install the `.msix`. `Get-AuthenticodeSignature` reports `UnknownError` until the cert is trusted — expected for self-signed. **TODO**: replace with the public flowrate.tech certificate once Azure Artifact Signing validation completes (see TODO.md).
- **Trimming**: `PublishTrimmed=False` globally in the csproj — reflection-based System.Text.Json (parser/history/settings/export) is not trim-safe (IL2026).
- **Standalone (unpackaged)**: `dotnet publish src/FlowRate/FlowRate.csproj -c Release -p:PublishProfile=win-x64-unpackaged` → self-contained folder at `bin\Release\...\win-x64\publish-unpackaged\` (~267 MB). Uses `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true`; no `dotnet run` identity registration needed. **Trimming stays disabled** in this profile — WinUI 3 XAML reflection breaks under trimming.
- **Launch profiles**: `FlowRate (Package)` runs with MSIX identity; `FlowRate (Unpackaged)` runs unpackaged via debug identity.

## Iteration Workflow
Every iteration ends with: build + full test run, meaningful updates to all four docs
(README, CHANGELOG, TODO, DEVELOPMENT), then `git add -A; git commit; git push origin main`.

## Services (FlowRate.Core)
- `Iperf3Locator` — resolves iperf3.exe (app base dir wins over PATH) and reads `--version`; used by the startup gate, the Info dialog, and `Iperf3Service`.
- `UpdateService` — GitHub `releases/latest` checks for `RejectH0/FlowRate` and `ar51an/iperf3-win-builds` (unauthenticated, 60 req/hr; manual checks only).

## Documentation Policy
Every iteration must meaningfully update (no null updates):
1. **README.md** — user-facing state: version, features, roadmap.
2. **CHANGELOG.md** — Keep a Changelog format; newest entry on top.
3. **TODO.md** — current iteration tasks and backlog.
4. **docs/DEVELOPMENT.md** — this file; workflows, architecture, conventions.
