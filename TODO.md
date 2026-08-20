# FlowRate TODO

Living task list. Update every iteration. Completed items move to CHANGELOG.md.

## Current Iteration (v0.7.1 — Self-Signed MSIX Sideload Pipeline) — DONE
- [x] Self-signed code-signing certificate `CN=flowrate.tech` in `Cert:\CurrentUser\My` (thumbprint `2D2058E79079BFA646967B0E7B2EC622323F8F5A`, expires 2027-08-19)
- [x] Signed MSIX build via MSBuild (`GenerateAppxPackageOnBuild` + `PackageCertificateThumbprint`) → `artifacts\msix\FlowRate_0.7.1.0_x64_Test\FlowRate_0.7.1.0_x64.msix`
- [x] Trimming disabled globally (`PublishTrimmed=False`) — reflection-based System.Text.Json is not trim-safe; IL2026/IL2104 warnings eliminated
- [ ] User sideload validation: install cert to Trusted People (LocalMachine), double-click MSIX to install, smoke-test

## Certificate Migration (undetermined — pending Azure validation)
- [ ] **Switch from self-signed to the public flowrate.tech certificate** once Azure Artifact Signing identity validation (id `3b0145a8-52e4-49f9-bfff-ae7b5498fce0`, org `flowrate.tech`, status: In Progress) completes. Re-sign MSIX with the public cert (subject must remain `CN=flowrate.tech` to match the manifest Publisher), remove the self-signed cert from user stores, and update docs/DEVELOPMENT.md signing instructions.

## Previous Iteration (v0.7.0 — UI Fixes, Detection & Updates) — DONE
- [x] Fix title-bar overlap (AppTitleBar drag region + SetTitleBar)
- [x] Embed exe icon (`ApplicationIcon`) so taskbar/Start shows the FlowRate icon
- [x] iperf3 startup detection with exit dialog linking to ar51an/iperf3-win-builds
- [x] Info (i) dialog: iperf3 path/version, FlowRate version, GitHub update checks
- [x] UpdateService (GitHub releases/latest) for FlowRate and iperf3 Windows builds
- [x] Publisher CN changed to `CN=flowrate.tech`
- [x] README acknowledgements for the iperf3 team (sourced, hyperlinked)
- [ ] User smoke-test: title bar, taskbar icon, Info dialog, missing-iperf3 path

## Next Up
- [ ] Publish first GitHub Release (v0.7.0) so the FlowRate update check has data
- [ ] Full auto-update (download + install) once signing/hosting via flowrate.tech is decided
- [ ] Smoke-test packaged app: settings, history, and export paths under package identity

## Previous Iteration (v0.6.0 — MSIX Packaging & Standalone Deployment)
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
