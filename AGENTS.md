# Agent policy

## Mandatory multi-agent integration

Before substantive repository work, read `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`, refresh the latest `origin/main`, and inspect every `ACTIVE` / `BLOCKED` claim under `docs/agent-work-claims/`.

1. AI agents and chat sessions must not push source, tests, scripts, workflows, packaging or release implementation directly to `main`.
2. Permission to dispatch, diagnose, or repair CI does **not** grant a direct-to-`main` implementation exception. CI recovery uses `recovery/<agent>/<scope>` or `agent/<agent>/<scope>`, then the normal integration path.
3. Publish a visible claim before implementation. Prefer a tiny `claim/<agent>/<scope>` PR to `main`; a claim-only Markdown landing is coordination, not implementation.
4. Implement only the reserved lane on a dedicated `agent/<agent>/<scope>` branch.
5. For a multi-agent batch, combine participating work on `integration/<batch-id>`, resolve semantic conflicts deliberately, run combined validation, and perform one final PR/landing to `main`.
6. Never force-push `main`, reset it backwards, or overwrite concurrent work. Refresh immediately before integration and verify the resulting commit/tree is reachable from current `main`.
7. A branch, issue, PR, or old green CI run is not proof that all required work is merged. See `docs/AGENT-WORK-REGISTRATION.md` for the `ALL MERGED TO MAIN` gate.

## QS3D-Platform product rules

1. Keep this repository vendor-neutral and clean-room.
2. Never commit BricsCAD/AutoCAD/ODA/proprietary SDK binaries, private drawings, credentials or license material.
3. Public APIs must not expose vendor-specific types.
4. Add deterministic regression coverage with behavioral changes.
5. Prefer coherent request-scoped commits; never force-push over concurrent work.
6. Treat `PLANNING.md` as the architecture baseline. Changes to repository ownership, identity/persistence authority or vendor boundaries require explicit documentation.
7. Native/runtime qualification belongs in consuming adapter repositories; do not manufacture native-CAD evidence from in-memory tests.
