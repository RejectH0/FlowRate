# FlowRate TODO

Living task list. Update every iteration. Completed items move to CHANGELOG.md.

## Current Iteration (v0.6.0 — MSIX Packaging & Standalone Deployment)
- [x] Verify/create `Package.appxmanifest` and package identity configuration — identity set to `RejectH0.FlowRate` / `CN=RejectH0` / 0.6.0.0; unused `systemAIModels` capability removed
- [x] Create publish profiles — `win-x64.pubxml` (existing) + new `win-x64-unpackaged.pubxml` (standalone)
- [x] Self-contained Release publish — verified 516 files / ~267 MB; trimming disabled in unpackaged profile (unsafe with WinUI 3 XAML reflection)
- [x] Remove the `dotnet run` debug-identity requirement — unpackaged profile uses `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true`
- [ ] MSIX package build, signing (self-signed test certificate matching `CN=RejectH0`), and sideload install validation
- [ ] Smoke-test packaged app: settings, history, and export paths under package identity
- [ ] Smoke-test standalone `publish-unpackaged\FlowRate.exe` launch and benchmark run (user)

## Verification
- [x] Run full test suite (16 tests) and record results after solution platform fix — all 16 passed (2026/08/19, `dotnet test`, 0 failed / 0 skipped)

## Backlog (pre-v1.0.0)
- [ ] License selection before public release
- [ ] Final UI polish pass

## Out of Scope (Milestone 1 — no feature creep)
- Ping / traceroute / DNS tools (Milestone 2)
- Historical trends & advanced diagnostics (Milestone 3)
