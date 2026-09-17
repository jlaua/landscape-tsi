## 1. Domain & Contracts Verification

- [x] 1.1 Verify physical database model and document non-existence of DDL/migration requirements (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`) via architecture test asserting existing foreign keys
- [x] 1.2 Define DTOs, command models, and impact summary models (`CapabilityReassignmentImpactDto`, `FunctionalityReassignmentImpactDto`, `AssignmentCommand`, `ReassignmentCommand`) with SHA-256 concurrency tokens in `Landscape.Tsi.Application`
- [x] 1.3 Define application contracts `IAssociationImpactService` and `IAssignmentService` in `Landscape.Tsi.Application` and verify compilation with `dotnet build`

## 2. Infrastructure & Application Implementation

- [x] 2.1 Implement `IAssociationImpactService` in `Landscape.Tsi.Infrastructure` to calculate child functionality count, affected parent building block, and associated technologies/use cases, and verify with unit tests
- [x] 2.2 Implement SHA-256 state token generation and optimistic concurrency verification for `TCapacidadDeSeguridad` and `TFuncionalidad`, and verify with unit tests
- [x] 2.3 Implement `IAssignmentService.AssignCapabilityAsync` and `ReassignCapabilityAsync` with transactional fail-closed audit log generation in `Landscape.Tsi.Infrastructure`, and verify with unit tests
- [x] 2.4 Implement `IAssignmentService.AssignFunctionalityAsync` and `ReassignFunctionalityAsync` with transactional fail-closed audit log generation in `Landscape.Tsi.Infrastructure`, and verify with unit tests
- [x] 2.5 Verify transactional rollback behavior (fail-closed) when audit log insertion fails during capability or functionality reassignment via isolated unit/integration tests

## 3. Web Presentation: Building Block Details View Reorganization

- [x] 3.1 Update `BuildingBlockDetailsViewModel` and `BuildingBlockRelatedService` to include Capacidad name, Funcionalidad name, and status label in the functionalities collection, verified by unit tests
- [x] 3.2 Implement server-side sorting logic with whitelist (`capacity`, `functionality`, `status`) and default compound sort (`Capacidad ASC, Funcionalidad ASC`), and verify with controller unit tests
- [x] 3.3 Update `src/Landscape.Tsi.Web/Views/Administration/BuildingBlockDetails.cshtml` (or corresponding view) to display columns `Capacidad | Funcionalidad | Estado de funcionalidad | Acciones` with accessible `aria-sort` headers and responsive styling
- [x] 3.4 Verify responsive behavior and empty states for the reorganized Functionalities table in Building Block details via UI/rendering tests

## 4. Web Presentation: Assignment & Reassignment Workflows

- [x] 4.1 Implement MVC/API controller actions to query orphan entities and impact preview for capabilities and functionalities, and verify with controller tests
- [x] 4.2 Implement MVC/API controller actions to process capability and functionality assignments and reassignments with concurrency token validation and anti-forgery protection
- [x] 4.3 Create accessible modal/drawer UI for associating orphan Capabilities to a Building Block with keyboard trap and focus restoration
- [x] 4.4 Create accessible modal/drawer UI for reassigning Capabilities with impact warning, child functionalities count, and explicit confirmation
- [x] 4.5 Create accessible modal/drawer UI for associating orphan Functionalities and reassigning Functionalities with cross-Building Block warning and confirmation
- [x] 4.6 Verify WCAG 2.2 AA accessibility compliance (keyboard navigation, Escape key closing, contrast, screen reader announcements) for all new dialogs

## 5. Verification & Regression Suite

- [x] 5.1 Run complete unit test suite `dotnet test --configuration Release` and verify all tests pass without regressions
- [x] 5.2 Execute integration tests on `db-landscape-tsi-dev-v2` validating Capability and Functionality reassignment, audit records, and referential integrity using `ITEST_<GUID>` prefixes
- [x] 5.3 Run `dotnet format --verify-no-changes` to ensure compliance with codebase style guidelines
