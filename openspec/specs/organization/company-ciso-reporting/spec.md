# organization/company-ciso-reporting Specification

## Purpose
Define a read-only organizational governance report that derives Company/Subsidiary–CISO information from the authoritative `TCISO.idEmpresaSubsidiaria` relationship while preserving the legacy text column for a later, approved migration.

## Requirements

### Requirement: Read-only relationship prevalidation
The system SHALL provide a read-only prevalidation of `TEmpresaSubsidiaria` and `TCISO` that reports total companies, CISO cardinality buckets, representative inconsistencies, and comparisons between `contactoCiso` and related `nombreCISO` values.

#### Scenario: Company cardinality buckets
- **WHEN** prevalidation is executed
- **THEN** it reports companies with zero, exactly one, and more than one related CISO using the real foreign-key relationship.

#### Scenario: Representative consistency
- **WHEN** related CISO rows are grouped by company
- **THEN** the result identifies companies with multiple `Representante = 1` rows and companies without any representative.

#### Scenario: Legacy text comparison
- **WHEN** `contactoCiso` is compared with related CISO names
- **THEN** the result distinguishes matches, mismatches, missing text, and companies with multiple CISO names without modifying either table.

### Requirement: Companies and CISO report
The system SHALL expose an authorized Reportería view named “Empresas y CISO” that uses a left relationship from `TEmpresaSubsidiaria` to `TCISO`, includes companies without CISO, and does not require users to enter technical identifiers.

#### Scenario: Company view
- **WHEN** the user selects “Por empresa”
- **THEN** the report shows one row per company, preferring the representative CISO, and shows “Sin CISO representante” when none exists.

#### Scenario: All CISO view
- **WHEN** the user selects “Todos los CISO”
- **THEN** the report shows one row per company–CISO relationship and a company may appear multiple times.

#### Scenario: Missing representative
- **WHEN** a company has CISO rows but no `Representante = 1`
- **THEN** the company view shows a safe inconsistency/gap indicator and does not throw an exception.

### Requirement: Report filters and governance indicators
The report SHALL support optional AND-combined filters for free text, company, country, grouping, industry, CISO, and representative status, plus filters for companies without any CISO and without a representative.

#### Scenario: Empty filters
- **WHEN** a filter is empty or set to “Todos”
- **THEN** that filter is omitted from the parameterized query.

#### Scenario: Governance KPIs
- **WHEN** the report loads
- **THEN** it displays total companies, companies with CISO, companies without CISO, companies with a representative, and companies without a representative derived from current data.

### Requirement: Secure navigation and authorization
The report SHALL enforce the existing server-side reporting authorization and SHALL generate links to real Company/Subsidiary and CISO detail endpoints through backend routing metadata.

#### Scenario: Authorized navigation
- **WHEN** an authorized user selects “Ver Empresa” or “Ver CISO”
- **THEN** the application navigates to the corresponding existing detail route without a 404.

#### Scenario: Unauthorized access
- **WHEN** a user lacks the reporting or organizational scope permission
- **THEN** access is denied without exposing technical identifiers or bypassing server-side authorization.

### Requirement: Server-side performance and accessibility
The report SHALL use server-side filtering, projection, ordering, and pagination, avoid per-row queries, and provide responsive semantic tables meeting the application's WCAG 2.2 AA conventions.

#### Scenario: Paginated query
- **WHEN** a page is requested
- **THEN** only the requested page and required aggregate counts are retrieved from SQL Server.

#### Scenario: Empty result
- **WHEN** filters produce no rows
- **THEN** the report returns HTTP 200 and displays an accessible empty state while retaining the filters.

### Requirement: Deferred removal proposal for contactoCiso
The system SHALL not remove or alter `TEmpresaSubsidiaria.contactoCiso` in this change and SHALL produce a documented dependency, migration, risk, and rollback proposal for a later approval.

#### Scenario: No DDL in analysis phase
- **WHEN** prevalidation and dependency analysis run
- **THEN** no INSERT, UPDATE, DELETE, ALTER, DROP, migration, or constraint change is executed.
