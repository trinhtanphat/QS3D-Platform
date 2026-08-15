# Agent policy

## Mandatory multi-agent integration

Before substantive repository work, read `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`, refresh the latest `origin/main`, and inspect every `ACTIVE` / `BLOCKED` claim under `docs/agent-work-claims/`.

1. AI agents and chat sessions must not push source, tests, scripts, workflows, packaging or release implementation directly to `main`.
2. Permission to dispatch, diagnose, or repair CI does **not** grant a direct-to-`main` implementation exception. CI recovery uses `recovery/<agent>/<scope>` or `agent/<agent>/<scope>`, then the normal integration path.
3. Publish a visible claim before implementation and implement only the reserved lane on a dedicated `agent/<agent>/<scope>` branch.
4. Push the final intended task head and open/update its PR before claiming remote completion.
5. `.github/workflows/ci.yml` runs automatically for `agent/**`, `recovery/**`, `integration/**`, PRs to `main`, and `main`.
6. An agent **must not report a task completed or stop as completed until CI is `success` for the exact current branch/PR head SHA**. Old green runs, another branch, another PR or `main` do not count.
7. If CI fails, keep the task active, fix the real defect on the task branch, push a new SHA and repeat the exact-SHA CI gate. Do not weaken guards/tests merely to obtain green status.
8. For a multi-agent batch, combine participating work on `integration/<batch-id>`, require green CI for the exact integration head, resolve semantic conflicts deliberately, and perform one final authorized PR/landing to `main`.
9. Require green CI again for the exact resulting `main` SHA before reporting `ALL MERGED TO MAIN`.
10. Never force-push `main`, reset it backwards, or overwrite concurrent work.

A GitHub Issue is a reservation/coordination surface, not a build target; it must reference the branch/PR SHA whose CI proves the task.

CI success is a quality/completion gate, not merge authorization.

## QS3D-Platform product rules

1. Keep this repository vendor-neutral and clean-room.
2. Never commit BricsCAD/AutoCAD/ODA/proprietary SDK binaries, private drawings, credentials or license material.
3. Public APIs must not expose vendor-specific types.
4. Add deterministic regression coverage with behavioral changes.
5. Prefer coherent request-scoped commits; never force-push over concurrent work.
6. Treat `PLANNING.md` as the architecture baseline. Changes to repository ownership, identity/persistence authority or vendor boundaries require explicit documentation.
7. Native/runtime qualification belongs in consuming adapter repositories; do not manufacture native-CAD evidence from in-memory or Platform CI.
