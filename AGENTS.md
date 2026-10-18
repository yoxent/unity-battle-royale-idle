# Battle royale idle

## Directories

This Unity project holds everything except docs: scenes, assets, code, and project settings. All docs live in the external docs directory below. Update that directory in the same session whenever something needs to be remembered. Do not add docs to this Unity project.

`E:\Projects\Docs\project-docs-unity-battle-royale-idle\Unity\unity-battle-royale-idle`

That folder is inside the shared docs repo at `E:\Projects\Docs\project-docs-unity-battle-royale-idle`. Do not `git commit` or `git push` unless the user explicitly says to commit or to push. Writing or syncing a note is not that request. When the user does ask for a commit, commit this project's docs only on branch `docs/unity-battle-royale-idle`.

| Path | Contents |
| --- | --- |
| `HOME.md` | Docs map |
| `PROJECT_CONTEXT.md` | Current state and next step |
| `GDD.md` | Gameplay rules |
| `TECHNICAL_DESIGN.md` | System owners and existing boundaries |
| `ROADMAP.md` | What to build next |
| `TEST_STRATEGY.md` | Done criteria and what a report must admit |
| `FEATURE_SPECS/` | A spec only when that feature is about to be built |
| `DECISIONS/` | A new architectural choice. Do not copy `notes/` into here |
| `notes/README.md` | Detail the living docs do not copy. Archive is not a work list |

Read the docs repo before assuming a decision is missing. For other references and best practices, search SCE for `unity-battle-royale-idle` after the docs repo.

## Project instructions

### Before working

- Inspect the existing Unity project and the relevant code before proposing changes.
- Read `PROJECT_CONTEXT.md` for the current state.
- Read `GDD.md` when a task affects gameplay.
- Read `TECHNICAL_DESIGN.md` when a task affects architecture or system boundaries.
- Read `FEATURE_SPECS/` when that feature has a spec. Otherwise use `notes/README.md`. Do not invent a spec. If a note disagrees with a living doc, the living doc wins.

### Implementation

- Follow the applicable rules in `.cursor/rules/`. Use `feature-implementation.mdc` for a feature or other substantial change.
- Prefer incremental changes over broad rewrites.
- Do not invent gameplay requirements or silently change an approved decision.
- Do not modify an unrelated system or asset without a reason.
- Ask when a missing decision would change the result. A large feature stops after the plan until it is agreed.
- Stay inside the current `ROADMAP.md` gate. Do not start the next gate until its exit line has been played. A class or a passing EditMode suite is not that exit.

### Verification

- Follow `TEST_STRATEGY.md`.
- Report what changed, what was tested, and what was not run.
- Never claim Unity runtime behavior was verified unless it was actually tested.

### Documentation

- Update `GDD.md` when a gameplay rule changes.
- Record a new architectural decision in `DECISIONS/` and in `TECHNICAL_DESIGN.md`.
- Keep `ROADMAP.md` and `PROJECT_CONTEXT.md` aligned with actual progress.
- Do not add a note that only repeats the work report.

## Working agreements

1. **Docs repo** — write notes there, not in this game repo.
2. **No screenshots** — do not capture or save screenshots. Check scenes by inspecting objects, camera settings, and the Unity console.
3. **No auto commit/push** — never `git commit` or `git push` unless the user explicitly says to commit or to push. Syncing docs is not a commit request.
