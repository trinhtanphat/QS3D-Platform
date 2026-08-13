# Agent policy

1. Keep this repository vendor-neutral and clean-room.
2. Never commit BricsCAD/AutoCAD/ODA/proprietary SDK binaries, private drawings, credentials or license material.
3. Public APIs must not expose vendor-specific types.
4. Add deterministic regression coverage with behavioral changes.
5. Prefer coherent request-scoped commits; never force-push over concurrent work.
6. Treat `PLANNING.md` as the architecture baseline. Changes to repository ownership, identity/persistence authority or vendor boundaries require explicit documentation.
7. Native/runtime qualification belongs in consuming adapter repositories; do not manufacture native-CAD evidence from in-memory tests.
