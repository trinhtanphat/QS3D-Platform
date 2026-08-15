# QS3D Cubicost-style feature master plan

Updated: 2026-08-15 (UTC+7)  
Tracking: #13  
Architecture authority: `QS3D-Platform` is vendor-neutral; native CAD behavior stays in the consuming host repositories.

## 1. Repository ownership decision

The full Cubicost-style feature family does **not** belong entirely in `QS3D-BricsCAD`.

| Concern | Canonical repository | Rule |
|---|---|---|
| Shared semantic BIM/QS domain, MEP classification, quantity math, clash contracts, cost/tender/progress logic, parity fixtures | `QS3D-Platform` | no BricsCAD/AutoCAD/ODA/UI vendor dependency |
| BricsCAD V25/V26 commands, selection, transactions, `Solid3d`, highlight/zoom/palette, DWG-native qualification | `QS3D-BricsCAD` | adapter only; consume shared Platform contracts over time |
| AutoCAD 2021/2025-2027 commands, palettes/ribbon, Autodesk geometry/runtime qualification | `QS3D-AutoCAD` | Autodesk adapter only; same shared contracts |
| Standalone Windows CAD/BIM/QS workspace and future licensed native DWG/rendering adapter | `QS3D-CAD` | desktop host consuming Platform |
| Product-family shared contracts are **not** duplicated into host repositories once migrated | `QS3D-Platform` | compatibility adapters may remain temporarily during migration |

`QS3D-BricsCAD/src/QS3D.Core` currently contains significant mature functionality. Migration must therefore be incremental and compatibility-first: shared contracts move to Platform, adapters are switched in small lanes, then duplicate host-neutral code can be retired only after parity tests prove equivalent behavior.

## 2. Status vocabulary

- `DONE_SHARED` — source-complete host-neutral contract exists in Platform.
- `DONE_BRX` — BricsCAD-native implementation exists.
- `DONE_QS3D_CORE` — currently implemented in legacy host-neutral `QS3D-BricsCAD/src/QS3D.Core`; candidate for Platform convergence.
- `PARTIAL` — useful foundation exists but advertised workflow is incomplete.
- `NEXT_SHARED` — belongs in Platform but is outside the current completed shared wave.
- `NEXT_BRX` — belongs in BricsCAD adapter/UI.
- `NEXT_ACAD` — AutoCAD adapter parity.
- `NEXT_DESKTOP` — standalone CAD parity.
- `FORMAT_SCOPE` — external-format/OCR/import lane requiring separate approval/evidence.
- `SERVICE_SCOPE` — server/team/service capability, outside vendor-neutral CAD libraries.
- `LOCAL_ONLY` — source may be remote-safe, but native host truth requires real licensed runtime evidence.

## 3. TAS-style architecture/structure takeoff

| Capability | Current QS3D status | Canonical direction |
|---|---|---|
| Semantic walls/columns/beams/slabs/openings/rooms | DONE_QS3D_CORE + Platform domain foundations | converge semantic identity into Platform |
| 3D quantity takeoff | DONE_QS3D_CORE | shared quantity contracts in Platform; host extraction per adapter |
| DWG recognition-assisted modelling | PARTIAL | recognition policy shared; native entity extraction in BricsCAD/AutoCAD |
| PDF recognition-assisted modelling | PARTIAL | FORMAT_SCOPE for OCR/text/geometry extraction |
| IFC import/interchange | PARTIAL | shared interchange contract; host/service lane for richer import |
| RVT/Revit import | FORMAT_SCOPE | separate lawful native/import lane |
| Localized measurement rules | DONE_QS3D_CORE/PARTIAL | shared rule packs/presets in Platform |
| one-click quantity calculation | DONE_QS3D_CORE | expose in each host UI |
| automatic deductions/opening relationships | DONE_QS3D_CORE | keep authoritative semantic/regeneration engine |
| recalculation after model changes | DONE_QS3D_CORE | converge dirty/dependency contracts |
| quantity trace to source objects | DONE_QS3D_CORE | shared trace identity + native Locate |
| calculation-expression explanation | DONE_QS3D_CORE | shared explanation model + host UI |
| 3D deduction review | PARTIAL | shared review contract; host graphics UI |
| reports / custom templates | DONE_QS3D_CORE/PARTIAL | shared projection contracts + host/export surfaces |
| custom classification / filtering | DONE_QS3D_CORE/PARTIAL | shared query/classification contracts |
| zones / segments / floors | DONE_QS3D_CORE + Platform domain | canonical shared IDs |
| revision compare / quantity change review | DONE_QS3D_CORE | shared diff contracts, native visualization per host |
| steel / earthwork / finish / precast workflows | PARTIAL | dedicated shared domain modules, adapter extraction |

## 4. TRB-style rebar takeoff

QS3D-BricsCAD already has broad rebar functionality: beam longitudinal/stirrups, columns, walls/slabs, mesh/layout, schedules, BBS, weight, stock demand, cutting optimization, procurement and health checks. The correct architecture is:

- rebar calculation/planning/identity/report contracts -> Platform convergence;
- BricsCAD object creation/edit/preview/highlight -> `QS3D-BricsCAD`;
- AutoCAD native equivalent -> `QS3D-AutoCAD`;
- country-code BS/ACI/Eurocode preset packs -> shared Platform rule packs;
- PDF/JPG intelligent recognition -> FORMAT_SCOPE;
- real-host interactive rebar review -> LOCAL_ONLY per adapter.

## 5. TME/TMEC-style MEP quantity and coordination

| Capability | Status | Ownership |
|---|---|---|
| MEP element kinds and semantic grouping | DONE_SHARED + DONE_QS3D_CORE | Platform canonical target |
| system/specification/region classification | DONE_SHARED + DONE_QS3D_CORE | Platform canonical target |
| configurable Layer/BlockName recognition profile | DONE_SHARED + DONE_QS3D_CORE | Platform canonical target |
| fail-closed unmatched/ambiguous recognition | DONE_SHARED + DONE_QS3D_CORE | Platform canonical target |
| deterministic count/length/area/volume aggregation | DONE_SHARED + DONE_QS3D_CORE | Platform canonical target |
| native BricsCAD selected-entity takeoff `QS3DMEPTAKEOFF` | DONE_BRX | BricsCAD |
| broad-phase hard/clearance clash `QS3DMEPCLASH` | DONE_SHARED + DONE_BRX | Platform math + BricsCAD extraction |
| clash Locate/select `QS3DMEPCLASHLOCATE` | DONE_BRX | BricsCAD |
| exact `Solid3d.CheckInterference` hard clash `QS3DMEPEXACTCLASH` | DONE_BRX / LOCAL_ONLY | BricsCAD |
| transient exact-clash highlight review | source-ready PR lane / LOCAL_ONLY | BricsCAD |
| zoom/camera to reviewed clash | NEXT_BRX / LOCAL_ONLY | BricsCAD |
| modeless clash review palette | NEXT_BRX | BricsCAD |
| persistent clash issues/status/assignee/comments | DONE_SHARED contract; NEXT_BRX/NEXT_ACAD UI/persistence bridge | Platform contract + hosts |
| recognition-profile catalog/persistence model | DONE_SHARED contract; NEXT_BRX/NEXT_ACAD editor | Platform schema + host UI |
| MEP rule authoring (duct/pipe/cable/fittings/equipment) | PARTIAL | Platform quantity/rule engine |
| MEP reports to BQ/cost | PARTIAL | Platform projection + host export |
| AutoCAD MEP adapter parity | NEXT_ACAD | AutoCAD |
| standalone MEP reference workflows | NEXT_DESKTOP | QS3D-CAD |

## 6. TBQ-style digital BQ / estimating / cost management

Shared clean-room contracts delivered by #13 cover:

1. BQ/item library with deterministic identity and duplicate rejection.
2. resource/rate build-up (labour/material/plant/subcontract/other components).
3. linked unit-rate analysis with overhead/profit.
4. historical BQ/rate catalog.
5. multi-dimensional historical benchmark statistics.
6. rate-reference marks and reverse BQ/rate lookup.
7. build-up analysis and applied-rate traceability foundation.
8. adjust-cost by ratio/target total.
9. trade analysis and CFA/cost-per-area projection.
10. smart/batch rate application with priority and fail-closed ambiguity.
11. tender BOQ requirements and bid lines.
12. completeness checking and missing-item evidence.
13. comparable total and deterministic ranking of complete bids.
14. reasonability/benchmark foundation through the shared benchmark service.
15. tender revision/addendum comparison contracts.
16. multi-round tender evaluation model.
17. progress contract items and claims.
18. certified quantity cap, overclaim rejection, retention and net certification.
19. stable identity surfaces for later model/BQ trace bridges.
20. time-phased baseline/actual/certified cost projection for 4D/5D monitoring.

Existing `QS3D-BricsCAD/src/QS3D.Core` already implements much of this production-oriented behavior. Platform is the long-term multi-host canonical contract, with migration performed by golden parity rather than bulk replacement.

Native BQ tables, XLSX/CSV/report rendering and palette interaction stay in the consuming product repository. Tender-PDF OCR/table recognition is FORMAT_SCOPE. Organization-wide shared libraries, multi-user supplier submission and online permissions are SERVICE_SCOPE.

## 7. 4D/5D lifecycle

Shared Platform responsibilities now include the deterministic time-phased cost baseline/actual/certified projection. Further shared lifecycle convergence should add semantic activity/work-package IDs and deeper quantity/model revision linkage.

Host responsibilities:

- native object selection/visualization;
- model highlighting by time/progress state;
- charts/palettes and export wiring.

Server/service responsibilities:

- multi-user collaboration, remote comments, organization RBAC, supplier tender portal, shared cloud libraries, notifications and cross-device synchronization.

## 8. Deep CAD-identification workflow parity

Vendor-neutral configuration delivered in Platform includes:

- import-hatch filtering policy;
- select-by-color classification rules;
- beam size read mode (`Width×Height` vs `Height×Width`);
- beam end-extension policy/tolerance;
- fail-closed recognition contracts;
- PDF text-recognition/restore **capability flags only**.

Actual DWG/PDF/JPG parsing and native entity mutation are adapter/FORMAT_SCOPE work.

## 9. Coordination/review architecture

Platform now defines a vendor-neutral coordination issue contract carrying:

- issue ID/type/severity/status;
- left/right semantic IDs and optional stable `CadReference` values;
- discipline/category/system/region context;
- hard/clearance/exact evidence kind;
- measured separation;
- title/assignee/comment history timestamps;
- deterministic state transition and monotonic timestamp validation.

No Platform API exposes `ObjectId`, `DBObject`, `Solid3d`, Autodesk/Bricsys/ODA types.

## 10. Cubicost-Manager-style workspace

Split deliberately:

- recent projects, local project discovery, learning/support links, update UI, product launcher -> product shell/desktop scope;
- package/update/signing policy -> each distributable repository;
- shared module compatibility/version contracts -> Platform;
- license portfolio/account/service state -> service/product-shell scope, not CAD domain.

## 11. Implementation waves

### Wave A — shared parity baseline (#13)

- [x] publish architecture/feature master plan;
- [x] shared MEP recognition + aggregation;
- [x] shared clash envelope contract;
- [x] shared BQ/rate/benchmark/tender/progress contracts;
- [x] smart rate, tender revision and multi-round evaluation foundations;
- [x] shared time-phased 4D/5D cost projection;
- [x] shared CAD-identification configuration and coordination issue contracts;
- [x] deterministic smoke coverage;
- [x] vendor-neutral/netstandard source guard;
- [x] migration map from BricsCAD Core to Platform.

Wave A is **SOURCE_READY** on the #13 implementation branch. It is not `main`-integrated until the repository integration process lands it, and Platform tests do not constitute native CAD qualification.

### Wave B — BricsCAD review UX

- integrate exact-clash transient highlight lane;
- zoom-to-clash with robust WCS/DCS behavior;
- modeless clash palette;
- persist/restore shared coordination issues through BricsCAD project storage;
- recognition profile editor/persistence bridge;
- MEP rule editor/report wiring;
- licensed V25 exact-SHA qualification.

### Wave C — cross-host reuse

- consume shared Platform contracts from `QS3D-AutoCAD`;
- implement AutoCAD MEP takeoff/clash/Locate/exact-review parity;
- expose the same host-neutral contracts in `QS3D-CAD` reference/standalone workflows;
- retire duplicate host-neutral implementations only after golden parity proves behavioral equivalence.

## 12. Definition of completion

`IMPLEMENTED` means source and deterministic tests exist in the correct repository. It does **not** imply `NATIVE_PASS`.

A BricsCAD/AutoCAD feature is only production-qualified when its exact integrated SHA is built and exercised in the licensed native host with evidence for selection, transactions, geometry, graphics, undo/cancellation, multi-document affinity and no unintended drawing/project mutation.

This master plan is clean-room workflow parity. It is not authorization to copy proprietary Cubicost source code, private formats, branding or undisclosed implementation details.
