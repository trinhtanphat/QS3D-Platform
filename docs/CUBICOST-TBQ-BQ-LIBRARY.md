# Cubicost TBQ — BQ Library hierarchy parity

Updated: 2026-08-16 (UTC+7)
Issue: #28
Dependency stack: #15 -> #19 -> #21 -> #23 -> #25 -> #27

## Official behavior modeled

The official Glodon Asia TBQ User Guide documents the **BQ Library** workflow:

1. open BQ Library in Project Manager;
2. create a **New BQ Library** and enter its name;
3. create categories, subcategories, headings and bills;
4. alternatively import bills from past projects with **Import from Project**.

The public guide excerpt names those hierarchy concepts but does not publish a mandatory parent-kind transition table. QS3D therefore models a safe generic container hierarchy and makes **no mandatory parent-kind** claim such as requiring Category -> Subcategory -> Heading in every library.

## Shared contract

`TbqBqLibraryWorkspace.Create(name)` creates a named immutable-style library snapshot.

`BqLibraryNodeKind` identifies Category, Subcategory, Heading and Bill nodes. Each node has a stable case-insensitive ID, name and optional parent. Bill nodes carry the canonical `BqLibraryItem`; this lane does not introduce another bill/item schema.

Container nodes may be nested under the library root or another existing non-Bill node. Parent references must already exist, so a newly added node cannot create a cycle. Bill nodes cannot contain children.

Every mutation-style operation returns a new workspace snapshot, leaving the previous snapshot unchanged so consuming hosts retain their own transaction/persistence boundary.

## Import from Project

`ImportFromProject(projectBills, destinationNodeId)` requires an explicit existing destination container and imports canonical `BqLibraryItem` values as deterministic Bill nodes.

The complete incoming payload is validated before a new snapshot is returned:

- null bills fail closed;
- incoming bill item codes must be case-insensitively unique;
- imported item codes must not already exist in the library;
- generated stable node IDs must not collide with existing nodes;
- destination must exist and cannot be a Bill node.

Imported bill nodes use deterministic IDs derived from their item codes and preserve the canonical item description, unit and category-path payload. No price, classification or hierarchy is guessed from private vendor rules.

## Repository / side-effect boundary

`QS3D-Platform` owns this vendor-neutral library/hierarchy contract. Native Project Manager UI, drag/drop, persistence, Excel/PDF/database adapters and CAD runtime integration belong to consuming repositories or service/format layers.

This source performs no file, database, network, native CAD or vendor UI operation.

## Validation

`TbqBqLibraryParitySmoke` covers named-library creation, generic container nesting, manual Bill creation, deterministic project import, snapshot independence, duplicate node/item rejection, missing parent rejection and Bill-as-container rejection.

`scripts/check-cubicost-tbq-bq-library.py` guards source/smoke/docs/registration and rejects native/UI/file/report-package dependencies.

Green Platform CI proves this shared contract only; it is not a claim about private Glodon implementation or native runtime behavior.
