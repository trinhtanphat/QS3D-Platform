# CI and integration policy

This file is the repository-level source of truth for CI ownership after multi-agent work.

## Per-agent task CI

`.github/workflows/ci.yml` is the canonical automatic validation workflow. It runs for implementation-relevant changes on `agent/**`, `recovery/**`, `integration/**`, pull requests targeting `main`, pushes to `main`, and manual dispatch.

Multiple agents share the workflow definition but do **not** share evidence: every required branch/PR run validates its own exact head SHA. Ten implementation task branches therefore produce independent CI runs for their own commits.

A GitHub Issue is coordination only; it has no source tree to build. The Issue must point to the branch/PR and exact commit SHA whose CI run is the evidence when CI is required.

## CI-neutral-only exemption

The full build/test CI is intentionally skipped when **all** changed files are limited to documentation or non-executable housekeeping paths configured in `.github/workflows/ci.yml`: `docs/**`, `**/*.md`, `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE*`, `NOTICE*`, and GitHub Issue/PR templates.

Such docs/Markdown/housekeeping-only tasks may complete without an artificial CI run after relevant lightweight validation. The exemption is based on changed paths, not commit wording. A commit prefixed `chore:` still requires normal CI if it changes source, tests, project/build files, dependencies, scripts, workflows, packaging, runtime-affecting configuration, or any other non-ignored path. Mixed changes always run CI.

`.github/workflows/**` is deliberately not ignored, so changes to CI itself exercise CI.

## Mandatory completion gate

For any task that is not CI-neutral-only, an implementation agent must not report a task completed or stop as completed until the required CI run for the **exact current branch/PR head SHA** has conclusion `success`.

A green run for an older SHA, another branch, another PR, or current `main` does not count. If a new implementation-relevant commit is pushed, the previous green result becomes stale for task completion.

If CI fails, the task remains active: diagnose the exact failing run, fix the real defect on `agent/<agent>/<scope>` or `recovery/<agent>/<scope>`, push a new SHA, and repeat until the exact current head is green. Never weaken architecture, persistence, compatibility, security or test guards merely to obtain green status.

If a task requires evidence unavailable to repository-safe CI, keep that boundary `BLOCKED`/handed off rather than claiming completion.

## Final-tree rule

CI evidence is meaningful only for the exact tree it tested. For implementation-relevant work, the multi-agent progression is:

```text
CLAIM_VISIBLE
  -> AGENT_BRANCH
  -> EXACT-HEAD CI
  -> CI_GREEN
  -> PR_READY
  -> INTEGRATION_BRANCH
  -> EXACT-INTEGRATION CI
  -> CI_GREEN
  -> ONE_AUTHORIZED_MERGE_TO_MAIN
  -> EXACT-MAIN CI
  -> CI_GREEN
  -> ALL_DONE
```

CI-neutral-only changes use the same branch/PR authorization path but do not manufacture a build-CI requirement. Normal implementation agents do not land implementation directly to `main`. CI success is a completion/quality gate, not merge authorization.

## Integration

For a multi-agent batch, combine participating implementation work on `integration/<batch-id>`. The coordinator must require a green automatic CI run for the exact integration head when implementation-relevant paths changed before an authorized landing, then require green CI again for the exact resulting `main` SHA when applicable before reporting the batch fully integrated.

## Evidence boundaries

- Platform CI proves only the exact host-neutral source tree it tested.
- Native BricsCAD/AutoCAD/DWG runtime qualification belongs in consuming adapter repositories.
- Repository-safe CI must never be promoted to native/runtime PASS.

## GitHub protection

Repository settings should require the stable `QS3D Platform CI / validate` status for implementation PRs to `main`, require the intended PR/integration path, block force-push and branch deletion, and keep bypass narrow. If a future ruleset requires a status on docs-only PRs, use a lightweight status/ruleset condition instead of forcing the full build CI for CI-neutral changes.
