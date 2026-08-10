# FlowRate Development Workflow

This document defines the **process and working agreements** for developing FlowRate.
It is intentionally kept separate from:

- [`README.md`](README.md) — describes the **application** only (what it is, how to install, use, and build).
- [`CHANGELOG.md`](CHANGELOG.md) — a running **project journal** of features, fixes, and design decisions only.

Nothing about how we work (commit cadence, tooling policy, doc rules, session handoff) belongs in those two files. It belongs here.

---

## Versioning Strategy

- **0.x.yy** — Pre-release development (not production-ready)
- **+0.0.1** — Minor changes, bug fixes, small features
- **+0.1.0** — Major milestones (real-time updates, gauge, results redesign)
- **1.0.0** — First public release (when Milestone 1 is feature-complete and polished)

Timestamp format for dated entries: `YYYY/MM/DD.HHMMSS`.

---

## Commit & Push Discipline

- Commit and push immediately after a successful build with tests passing, **before** launching the app for the user to test.
- Every substantial change must be committed immediately — never batch unrelated work into one commit.
- Commit messages must be descriptive and reference the functionality changed.
- Push to GitHub after each commit for recovery safety.

---

## Documentation Requirements

- **`README.md`** — Keep current with each iteration; product-facing only. No workflow/process content.
- **`CHANGELOG.md`** — **APPEND ONLY**. Never purge or remove content. Project-pertinent entries only (features, fixes, design decisions, commit references). No workflow/process content.
- **`WORKFLOW.md`** (this file) — The single home for process, policy, and working-agreement changes.
- Document every iteration with context and rationale in the appropriate file.

---

## Session Recovery Strategy

- `CHANGELOG.md` serves as the project-history source of truth for session handoffs.
- `WORKFLOW.md` preserves the working agreements so a new session can follow the same process.
- Together they let the next contributor (human or agent) pick up exactly where work left off, with all architectural decisions and context preserved.

---

## Scope Control

- **Milestone 1 ONLY** during initial development: iperf3 benchmarking frontend. No feature creep.
- Longer-term product vision must not create scope creep in the current milestone.

---

## Provenance

This file was created to separate process/working-agreement content from the product docs.
Its content was relocated from the following sources (no rules were changed, only moved):

- **`CHANGELOG.md`** — the trailing "Development Guidelines" section (Versioning Strategy,
  Commit Discipline, Documentation Requirements, Session Recovery Strategy), the `### Workflow`
  note under the 0.3.0 entry, and the "Commit-and-push discipline" constraint bullet under 0.1.0.
- **`README.md`** — the "Scope Control" statement (Milestone 1 focus / no feature creep).

