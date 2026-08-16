# Cubicost TBQ 360-Degree Price Check parity

Updated: 2026-08-16 (UTC+7)
Issue: #18
Dependency: shared Cubicost parity PR #15

## Public product behavior being modeled

The public Glodon Asia TBQ User Guide documents **360-Degree Price Check in Build-up unit rate** as a Build-up checking workflow for locating and validating price references.

The documented scenarios are:

1. identify rates that are or are not adopted in BQ;
2. reverse-check which bill items adopt a selected rate;
3. identify which basic rates are used to compose unit rates;
4. reverse-check which unit rates adopt a selected basic rate.

The documented UI exposes reference marks and actions named `Check Linking Rate` and `Check BQ Reversely`. Unit rates/basic rates may show `BQ` when adopted in bill items and basic rates may show `UR` when adopted in unit rates.

This repository implements the host-neutral data/validation contract only. It does not copy proprietary UI, assets, binaries, private behavior or vendor source.

## QS3D shared contract

`CostRateReferenceGraph` provides an immutable deterministic snapshot over:

- `CostRateNode` entries for unit rates and the public basic-rate families: Composite Material/Labor, Material, Labor, Equipment and Other;
- `CostRateCompositionLink` edges from a unit rate to a basic rate;
- `BqRateAdoption` edges from a BQ item to a rate.

The graph validates all references before publication:

- rate IDs are unique case-insensitively;
- every composition/adoption target must exist;
- a composition parent must be a unit rate;
- a composition component must be a basic rate;
- self-reference and duplicate edges are rejected.

## Reference marks

`GetReferenceState(rateId)` returns deterministic usage state:

- `BQ` — the rate is adopted directly by at least one BQ item;
- `UR` — the basic rate is adopted by at least one unit rate;
- `BQ+UR` — both relationships exist;
- empty mark — neither relationship exists.

This is data state, not UI visibility. A host can implement Show/Hide/Refresh reference-mark UX over a newly constructed graph without mutating the shared graph.

## Reverse checks

- `CheckLinkingRates(basicRateId)` returns the unit-rate IDs that adopt the selected basic rate.
- `CheckBqReversely(rateId)` returns the BQ item codes that directly adopt the selected rate.
- `FindRatesNotAdoptedInBq()` returns the deterministic set of rates without a direct BQ adoption, supporting missed-use review.

All output ordering is deterministic and comparisons are case-insensitive for stable cross-host behavior.

## Repository ownership

`QS3D-Platform` owns this graph, validation and reverse-query contract because it is cost-estimation domain logic independent of CAD APIs.

Consuming hosts may later add their own presentation surfaces:

- `QS3D-BricsCAD`: BricsCAD-native/modeless TBQ review UI only if needed;
- `QS3D-AutoCAD`: Autodesk-native presentation only;
- `QS3D-CAD`: standalone presentation only.

No host should duplicate the reference-graph rules.

## Validation

Deterministic smoke coverage checks:

- BQ-only, UR-only, BQ+UR and unreferenced marks;
- Check Linking Rate;
- Check BQ Reversely;
- rates not adopted in BQ;
- case-insensitive lookup;
- duplicate IDs, missing references and invalid composition roles failing closed.

`scripts/check-cubicost-tbq-360-price-check.py` guards the shared contract and vendor-neutral boundary and is wired into `scripts/validate.sh`.

## Evidence boundary

A green Platform validation proves the host-neutral source only. It is not evidence of Glodon internal implementation and is not licensed BricsCAD/AutoCAD runtime evidence.
