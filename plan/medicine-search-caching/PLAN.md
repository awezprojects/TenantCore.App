# Feature Plan: Medicine Search Caching

**Repo:** TenantCore.App
**Date:** 2026-09-13
**Domain area:** Medicines (existing area — no new entity)
**Status:** Approved — ready for execution

---

## Overview

Doctors and reception search the medicine catalogue constantly — on the Medicines admin list, the Prescription form's medicine autocomplete, and the Medicine Bundle builder's medicine autocomplete. The catalogue is dominated by a system-wide reference set of roughly 2.5 lakh medicines shared by every clinic, plus a small per-clinic set (100–500 rows) of medicines each clinic has added itself. Every search currently re-queries SQL Server against the full system table on every keystroke, across every clinic, which is the dominant load on the database.

This feature adds an in-process cache in front of the read paths for `Medicine`, `MedicineType`, and `MedicineDosageForm`. The system-wide portion of each dataset is held in memory, shared by every clinic and every request, and kept warm by a background hosted service that refreshes it every 30 minutes — **never** by a live request. Each clinic's own medicines (never more than a few hundred rows) continue to be queried live and are merged with the cached system data in memory before filtering/paging/sorting. No API contract, DTO, controller, command/query, or UI code changes — the cache is inserted transparently at the repository layer, so MedicineList, PrescriptionForm, and MedicineBundleList all benefit automatically.

**Revision (2026-09-13):** the initial implementation used `IMemoryCache` with a 30-minute absolute expiration, populated lazily by whichever live request happened to find it empty. That caused exactly the failure it was meant to prevent — the very first request after a cold start (or after any 30-minute expiry) paid the full cost of a ~2.5 lakh-row query and timed out. The design below replaces that with a background warm-up service and a cache that never expires out from under a reader — see "Caching Design" for the corrected mechanics.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Infrastructure | New cache-decorator repositories for `Medicine`, `MedicineType`, `MedicineDosageForm`; `IMemoryCache` registration; DI registration swapped to the decorators |

Domain, Application, API, Shared, and Web.Client are all unchanged — every existing interface, DTO, controller, command, query, handler, and page keeps working exactly as it does today.

---

## How Existing Pages Reach the Cache (Call Chain)

No page, API client, controller, command/query, or handler changes. Every consumer already goes through `IMedicineRepository` / `IMedicineTypeRepository` / `IMedicineDosageFormRepository` — only the DI registration for those three interfaces changes, from the concrete repository to its caching decorator. The call chain below is **unchanged end-to-end except the last hop**, which is exactly why nothing else needs editing:

| Page | Client call | API endpoint | Query/Handler | Repository interface (unchanged) | Resolves to (after this feature) |
|------|------------|--------------|----------------|-----------------------------------|-------------------------------------|
| `MedicineList.razor` | `IMedicineApiClient.GetMedicinesAsync(...)` | `GET api/medicines` | `GetMedicinesQuery` → `GetMedicinesHandler` | `IMedicineRepository.GetPagedAsync(...)` | `CachedMedicineRepository` |
| `MedicineList.razor` | `IMedicineApiClient.SearchMedicinesByNameAsync(...)` | `GET api/medicines/autocomplete` | `GetMedicineAutocompleteQuery` → `GetMedicineAutocompleteHandler` | `IMedicineRepository.GetByNamePrefixAsync(...)` | `CachedMedicineRepository` |
| `PrescriptionForm.razor` (`SearchMedicines`, [PrescriptionForm.razor.cs:2315](src/TenantCore.Web.Client/Pages/Prescriptions/PrescriptionForm.razor#L2315)) | `IMedicineApiClient.SearchMedicinesByNameAsync(...)` | `GET api/medicines/autocomplete` | same as above | same as above | `CachedMedicineRepository` |
| `MedicineBundleList.razor` ([MedicineBundleList.razor:433](src/TenantCore.Web.Client/Pages/Medicines/MedicineBundleList.razor#L433)) | `IMedicineApiClient.SearchMedicinesByNameAsync(...)` | `GET api/medicines/autocomplete` | same as above | same as above | `CachedMedicineRepository` |
| Medicine detail / edit views | `IMedicineApiClient.GetByIdAsync(...)` | `GET api/medicines/{id}` | `GetMedicineByIdQuery` → `GetMedicineByIdHandler` | `IMedicineRepository.GetByIdWithTypeAsync(...)` | `CachedMedicineRepository` |
| Medicine Type filter dropdowns / admin list | typed client for `api/medicine-types` | `GET api/medicine-types` | `GetMedicineTypesQuery` → handler | `IMedicineTypeRepository.GetPagedAsync(...)` | `CachedMedicineTypeRepository` |
| Dosage Form filter dropdowns / admin list | typed client for `api/medicine-dosage-forms` | `GET api/medicine-dosage-forms` | `GetMedicineDosageFormsQuery` → handler | `IMedicineDosageFormRepository.GetPagedAsync(...)` | `CachedMedicineDosageFormRepository` |

**Why this is safe:** handlers only ever depend on the *interface* (`IMedicineRepository`, etc.), never the concrete class — that's already an existing rule (ADR-004: "always inject the interface"). Swapping what `AddInfrastructure()` binds that interface to is the only change needed for every one of these call sites to start benefiting from the cache automatically, with zero risk of missing a caller.

---

## Caching Design

### The cache never expires out from under a reader

Instead of an `IMemoryCache` entry with a TTL (which forces whichever request notices the expiry to pay for a synchronous rebuild), each dataset is held in a `RefreshableCache<T>` — a tiny singleton holder with no expiration of its own:

- `Current` is `null` only until the very first successful refresh after process start.
- A refresh replaces it with **one atomic reference swap**, once the entire new snapshot has been built — readers only ever see a fully-old or fully-new snapshot, never a partial or empty one.
- Refreshing is driven entirely by a background hosted service (`MedicineCacheWarmupService`), never by a live request.

### `MedicineCacheWarmupService` — the background refresh loop

- Runs for the lifetime of the process: refreshes immediately on startup, then every 30 minutes.
- Each cycle opens its own DI scope (a fresh `ClinicDbContext`), reads the system-medicine table, the medicine-type table, and the dosage-form table, and only calls `.Set()` on all three `RefreshableCache<T>` instances after all three reads succeed — so the caches never disagree with each other.
- If a refresh cycle throws (DB hiccup, timeout, etc.), the exception is caught and logged, the **previous snapshot is left untouched**, and the next cycle simply tries again 30 minutes later. A failed refresh degrades to "serving slightly stale data for longer," never to an outage.
- Uses a longer command timeout for this one background query (2 minutes) since it's no longer running on a request's clock.

### What gets cached, and why each is safe

| Repository | Cached scope | Mutable via this app? | Staleness handling |
|---|---|---|---|
| `IMedicineRepository` | System medicines only (`ApplicationId == null`) — one shared snapshot, ~2.5 lakh rows | No — `CreateMedicineHandler`/`UpdateMedicineHandler` only ever operate on the caller's own clinic's medicines (`ApplicationId` always set); a system record can never be created/edited/deleted through the API | Refreshed only by the background service every 30 minutes; no write-side invalidation needed since the cached data can't be changed via this app |
| `IMedicineTypeRepository` | Entire table (small lookup, no per-clinic split) | Yes — `CreateMedicineTypeCommand`/`UpdateMedicineTypeCommand` | Refreshed by the background service every 30 minutes, **plus** an immediate inline reload right after a successful write through the decorator — the table is small, so reloading it on the spot is cheap, and it means an admin sees their own change on the very next read |
| `IMedicineDosageFormRepository` | Entire table (small lookup, no per-clinic split) | Yes — Create/Update/Delete commands exist | Same as above: background refresh + immediate inline reload on write |

Each clinic's own medicines are **never** cached — they're a few hundred rows at most, so a live, tenant-filtered query on every request is already cheap and guarantees a clinic instantly sees medicines it just added or edited.

### Cold-start fallback (the one gap that's unavoidable, and how it's handled)

For the few seconds between process start and the background service's first completed refresh, `RefreshableCache<T>.Current` is `null`. Rather than blocking that request or trying to trigger a rebuild inline (the mistake in the original design), each decorator just queries the database directly for that one call and returns — no caching, no blocking, no special-casing at the call site. Once the background service's first refresh lands, every subsequent request is served from the warm snapshot.

### How search/paging/filtering works against the cache

For each of the three repositories, the existing `GetPagedAsync` / `GetByNamePrefixAsync` / `GetByIdAsync`-style methods keep their exact current signatures and behavior from the caller's point of view. Internally, the decorator:

1. Reads the current snapshot (`RefreshableCache<T>.Current`), falling back to a live query only in the cold-start case above.
2. For `Medicine` only: also runs the existing live, tenant-filtered query for the requesting clinic's own medicines (unchanged from today).
3. Applies the same search term, filter, active/inactive, and sort logic that the repository applies today — now over the in-memory combined set instead of a SQL query — then pages/limits exactly as before.
4. Returns the same shape (`(Items, Total)` tuples, entities, etc.) the interface already promises, so nothing downstream (handlers, translators, DTOs) needs to change.

`FindSimilarAsync` (the exact-duplicate check used on medicine create/update) and `GetByNameAsync`/`GetByIdAsync` on the two lookup repositories are **not** cached — they stay live SQL queries: `FindSimilarAsync` and `GetByNameAsync` are correctness-critical duplicate checks that must never miss a row created moments earlier, and `GetByIdAsync` on the lookups is used by Update/Delete handlers to fetch-then-mutate an entity in place, which requires a live, unshared instance rather than one pulled from a shared cached snapshot.

---

## Files to Create

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Caching/RefreshableCache.cs` | `RefreshableCache<T>` — a singleton snapshot holder with no expiration; `Current` is null only until the first successful refresh, and `Set()` performs one atomic reference swap |
| `Caching/MedicineCacheQueries.cs` | The three full-table reads (system medicines, medicine types, dosage forms) shared between the warm-up service and each decorator's cold-start fallback |
| `Caching/MedicineCacheWarmupService.cs` | `BackgroundService` — refreshes all three caches on startup and every 30 minutes after that; on failure, logs and keeps the previous snapshot, retrying next cycle |
| `Caching/CachedMedicineRepository.cs` | Implements `IMedicineRepository` — wraps `MedicineRepository`; serves the system-wide (`ApplicationId == null`) portion from `RefreshableCache<Medicine>` (falling back to a live query only if the cache hasn't warmed up yet), merges with a live per-clinic query for every read method; delegates writes straight through, since system rows are never written via this app |
| `Caching/CachedMedicineTypeRepository.cs` | Implements `IMedicineTypeRepository` — wraps `MedicineTypeRepository`; serves the whole table from `RefreshableCache<MedicineType>`; on a successful write, reloads the table inline and pushes the fresh snapshot immediately (no gap) |
| `Caching/CachedMedicineDosageFormRepository.cs` | Implements `IMedicineDosageFormRepository` — wraps `MedicineDosageFormRepository`; same pattern as the type-cache decorator |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `RefreshableCache<Medicine>`, `RefreshableCache<MedicineType>`, `RefreshableCache<MedicineDosageForm>` as singletons; register `MedicineCacheWarmupService` as a hosted service; change the three repository registrations so `IMedicineRepository`, `IMedicineTypeRepository`, `IMedicineDosageFormRepository` resolve to the new `Cached...Repository` decorators (each wrapping the existing concrete repository, which stays registered so the decorator can be constructed with it) |
| `.claude/context/current-state.md` | Note that these three repository interfaces now resolve to caching decorators in Infrastructure DI, so a future planning session doesn't mistake this for a new repository/entity |

---

## Business Rules

1. A clinic must never see another clinic's own medicines, even through the cache — enforced by never caching clinic-owned rows at all; only the shared system dataset and the two global lookup tables are cached.
2. The cache must never mask a clinic's own recent create/update/delete of its own medicine, medicine type, or dosage form:
   - Clinic-owned medicines: guaranteed by always querying them live (never cached).
   - Medicine types / dosage forms: guaranteed by an immediate inline reload right after a successful write, in addition to the background service's 30-minute refresh.
3. System-wide medicines are read-only through this application (existing rule, unchanged) — the cache relies on this to justify a background-only refresh with no write-side invalidation for that dataset.
4. A live request must never trigger, block on, or pay for a cache rebuild — that is exclusively the background service's job. A request that arrives before the first warm-up completes falls back to a live, uncached query for that single call instead.
5. A failed background refresh must never clear or replace a working snapshot — the previous snapshot stays in place, the failure is logged, and the next scheduled cycle retries.

---

## Multi-Tenancy Checklist

- [x] Cached data contains no clinic-owned (`ApplicationId`-set) medicine rows — only `ApplicationId == null` system rows (verified by `GetPagedAsync_NeverReturnsAnotherClinicsOwnMedicine`)
- [x] Clinic-owned medicines are still queried live, filtered by `applicationId`, on every request (unchanged from current behavior; verified by `GetPagedAsync_ReflectsNewClinicMedicineImmediately`)
- [x] `MedicineType`/`MedicineDosageForm` caches are global lookups with no tenant dimension, matching their existing non-tenant-scoped data shape
- [x] No cache key is ever derived from or includes `ApplicationId` — the whole point is one shared entry across all clinics

---

## EF Migration

None — no entity, column, or schema change.

---

## Implementation Order

1. `RefreshableCache<T>` (generic singleton snapshot holder)
2. `MedicineCacheQueries` (shared full-table read helpers)
3. `MedicineCacheWarmupService` (background refresh loop)
4. `CachedMedicineRepository` (system-medicine caching + per-clinic live merge + cold-start fallback + pass-through writes)
5. `CachedMedicineTypeRepository` (whole-table caching + cold-start fallback + immediate inline reload on write)
6. `CachedMedicineDosageFormRepository` (whole-table caching + cold-start fallback + immediate inline reload on write)
7. Register the three `RefreshableCache<T>` singletons, the hosted service, and swap the three repository DI registrations to the decorators
8. Update `.claude/context/current-state.md`

---

## Open Questions / Risks

- Memory footprint: caching ~2.5 lakh system medicines in-process will hold a non-trivial but bounded amount of memory per API instance — acceptable given the read-speed goal, but worth watching if the API ever scales to many instances (each instance builds and holds its own copy independently; there's no shared/distributed cache here).
- If the API ever runs as multiple instances behind a load balancer, each instance runs its own `MedicineCacheWarmupService` and does its own full-table reads on startup, independently. Acceptable for the current single-instance deployment; would need a shared/distributed cache (e.g. Redis via `IDistributedCache`) or a coordinated warm-up if that changes later.
- The medicine-type/dosage-form "immediate inline reload on write" re-reads the whole (small) table synchronously inside the write request. Fine while these tables stay small (tens to low hundreds of rows); would need revisiting if that assumption ever changes.
