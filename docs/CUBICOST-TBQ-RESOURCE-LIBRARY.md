# Cubicost TBQ — Resource Library batch import parity

Updated: 2026-08-16 (UTC+7)
Issue: #24
Dependency stack: #15 -> #19 -> #21 -> #23

## Public behavior being modeled

Glodon's public TBQ material lists **Batch Import from RL**. The current TBQ product page describes the **Resource library** as a store for build-up rate details and describes reuse of historical unit-rate data. Public TBQC training material shows the costing sequence more explicitly:

1. create a **Resource Library** by **Import from project** from schedule/rate data;
2. open **Build-up Unit Rates**;
3. use **Batch Import from RL** to import price/rate data from the Resource Library.

The available public material does not define a fuzzy or automatic matching algorithm. QS3D therefore models only the evidence-backed shared behavior and deliberately uses **explicit selection** of Resource Library rate IDs. It does not guess by description, similarity, vendor scoring, hidden ranking or private TBQ rules.

## Shared contract

`TbqResourceLibrary.ImportFromProject(...)` creates an immutable-style host-neutral library snapshot from canonical `CostRateBuildUp` values.

Construction requires:

- a stable library ID and source-project ID;
- non-null project rate payload;
- case-insensitively unique rate IDs;
- no null rate entries.

The library exposes rates in deterministic case-insensitive ID order. `CostRateBuildUp` already owns resource components, unit, currency, overhead, profit and derived unit rate, so this lane does not create a second pricing model.

## Batch Import from RL

`BatchImport(rateIds)` requires an explicit non-empty selection. Every requested ID must resolve exactly, case-insensitively, to a Resource Library rate. Duplicate request IDs and missing rates fail closed before a result is returned.

The result contains:

- the Resource Library ID;
- source-project provenance;
- selected canonical `CostRateBuildUp` snapshots;
- canonical source rate IDs;
- deterministic ordering by rate ID.

No rate is synthesized, rounded, converted, fuzzily matched or silently coerced. In particular, Platform does not silently change unit or currency. A consuming host can preview the returned rates and own its project persistence/transaction boundary.

## Immutability / side-effect boundary

Batch import here is a shared planning/result operation. It does not mutate the Resource Library, target project, BQ, Build-up Analysis workspace or CAD state. It performs no file, Excel/PDF, network or database I/O. That keeps Platform reusable by BricsCAD, AutoCAD and standalone hosts and lets each host apply accepted changes atomically.

## Repository ownership

`QS3D-Platform` owns this Resource Library/rate-selection contract because it is vendor-neutral cost-domain behavior. Native CAD UI, project persistence, interactive selection, file-format adapters and licensed runtime qualification belong to consuming repositories or format/service layers.

## Explicit evidence limit

This lane intentionally makes **no fuzzy matching claim**. If authoritative public documentation later establishes automatic matching keys or conflict-resolution behavior, that should be implemented as a separate evidence-backed follow-up rather than inferred here.

## Validation

`TbqResourceLibraryParitySmoke` covers project-to-library creation, deterministic ordering, explicit multi-rate selection, provenance, preservation of exact build-up values and fail-closed duplicate/missing/empty selection behavior.

`scripts/check-cubicost-tbq-resource-library.py` guards the source/smoke/docs/registration contract and rejects native CAD/UI/file/network dependencies.

Green Platform CI proves this host-neutral source only. It does not prove any private Glodon implementation or native CAD runtime behavior.
