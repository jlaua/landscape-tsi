## Context

The authoritative relationship is `TCISO.idEmpresaSubsidiaria -> TEmpresaSubsidiaria.idEmpresaSubsidiaria`. The legacy `TEmpresaSubsidiaria.contactoCiso` text remains in place and must be treated as comparison data only. The change spans read-only data analysis, Reportería, routing metadata, authorization scope, and tests; it does not authorize schema or data writes.

## Goals / Non-Goals

**Goals:**

- Produce reproducible read-only cardinality and legacy-column reconciliation results.
- Add an efficient, paginated “Empresas y CISO” report with company and all-CISO views.
- Derive representative and governance KPIs from `TCISO` rather than duplicating state.
- Reuse existing reporting table, routing, authorization, and metadata patterns.
- Leave an explicit, reviewable plan for eventual `contactoCiso` removal and a filtered uniqueness constraint.

**Non-Goals:**

- Do not remove, rename, backfill, or update `contactoCiso`.
- Do not create a direct FK from `TEmpresaSubsidiaria` to `TCISO`.
- Do not create a new `TBuildingBlockVsFamilia`-style relation or persist report KPIs.
- Do not apply a unique index, migration, DDL, or production data change.

## Decisions

1. **Relationship source.** Use a parameterized left join from companies to CISO. The company view groups by company and selects the representative row when unique; the all-CISO view retains one row per relationship. This preserves companies with no CISO and avoids treating legacy text as authoritative.

2. **Prevalidation queries.** Run separate read-only aggregates for total/cardinality buckets, representative anomalies, and legacy text comparison. Persist results in a diagnostic report rather than changing source tables. The unique filtered-index proposal is generated only after duplicate representatives are enumerated.

3. **Query shape.** Build an `IQueryable` projection with `AsNoTracking`, optional parameterized predicates, SQL-side ordering and `Skip/Take`. Aggregate KPIs use grouped subqueries or one read-only query batch; no N+1 per company/CISO queries are allowed.

4. **Routing and authorization.** Use existing backend route metadata or `Url.Action`/`LinkGenerator`; do not concatenate URLs. Apply the existing Reportería policy and organization/subsidiary scope to both rows and KPI queries.

5. **Metadata and display.** Use functional names and nullable CISO fields. Technical IDs are only internal route values. A company with multiple representatives is marked as inconsistent, never silently selected as a canonical representative.

6. **Future schema change.** The later migration proposal will first remediate all code/report dependencies, optionally reconcile legacy values, then remove the column only after approval. A rollback requires restoring the column and its data from a pre-change backup or explicitly approved snapshot; this change does not execute either action.

### Alternatives considered

- **Use `contactoCiso` as the report source:** rejected because it cannot represent multiple CISO and can drift from the FK relationship.
- **Add a direct company-to-CISO FK:** rejected because it duplicates the existing one-to-many relationship and creates conflicting sources of truth.
- **Load CISO per company in application code:** rejected because it creates N+1 queries and inconsistent pagination.

## Risks / Trade-offs

- **[Legacy mismatches]** `contactoCiso` may disagree with related names → report every mismatch and defer any backfill to an approved change.
- **[Multiple representatives]** Existing data may violate the future uniqueness rule → show explicit inconsistency counts before proposing the filtered index.
- **[Large report]** Joining multiple CISO rows can grow quickly → server-side pagination, indexed FK predicates, and bounded projections.
- **[Authorization scope]** KPI totals could leak out-of-scope companies → apply the same organizational predicate to detail and aggregate queries.
- **[Routing drift]** Existing detail endpoints may differ by catalog → resolve links through existing metadata and add non-404 routing tests.

## Migration Plan

This phase has no migration. Before any future DDL, produce and approve a separate migration containing only the removal of `contactoCiso` (and, if approved, the filtered unique representative index), with a pre-change backup, dependency checklist, and rollback script. No production database is touched by this change.

## Open Questions

None that change the agreed behavior. The exact final index name and migration ordering can be selected during the separately approved DDL change after prevalidation results are known.
