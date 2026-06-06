# BaseOps Backend Enterprise Architecture

## Frontend audit summary

The existing React frontend is a behavioral specification, not an authority. It exposes mock/demo flows for authentication, management, organizations, dashboards, AUMS, post-mortem, material orders, SAFA, handover, leave, ACE, bulletins, audit, production, carry-over, reports, files, notifications, and search.

Frontend assumptions that backend must override:
- Role and capability checks are client-side and non-authoritative.
- Demo users encode hierarchy and production-planner access directly.
- Workspace assignment logic conflates organizational hierarchy with access.
- Several services swallow backend failures and return empty collections.
- Multiple endpoint naming styles exist; backend must standardize on kebab-case REST routes.

## Canonical backend model

Organization is context only:
- Section
- Hangar
- Shop

Users carry nullable organizational placement:
- SectionId
- HangarId
- ShopId
- ReportsToUserId

Operational entities must implement `IScopedEntity` so all modules consume one scope engine.

## Aggregate boundaries

- Identity aggregate: `ApplicationUser`, `RefreshToken`, `RevokedToken`, `UserSession`.
- Organization aggregate: `Section`, `Hangar`, `Shop`.
- Audit aggregate: `AuditLog`.
- Future operational aggregates: schedules, assignments, work packages, defects, handovers, inspections, material orders, leave requests.

## Centralized RBAC invariant

Hierarchy is not authorization truth. Controllers, handlers, repositories, and modules must not duplicate predicates. All access must flow through:
- `IUserScopeResolver`
- `IRbacScopeValidator`
- `IHierarchyAccessService`

Unassigned team leaders authenticate successfully but receive zero operational records.

## Production planner capability

`ProductionPlannerAccess` is a derived capability. It is not stored as a role and should remain dynamically resolved from organizational/capability rules. Current foundation computes it from section name `Technical Support-Base` and emits the JWT claim `hasProductionPlannerAccess=true`.

## Current implemented foundation

- ASP.NET Core 10 Web API
- Clean Architecture projects
- PostgreSQL EF Core DbContext
- JWT access tokens
- Refresh-token persistence and rotation
- BCrypt password hashing
- Centralized RBAC/scope services
- Audit logging service
- Correlation ID middleware
- Security headers middleware
- Global exception middleware
- Rate limiting
- Health checks
- Swagger
- Security tests for scope isolation

## Next module implementation strategy

For every module:
1. Model the aggregate in Domain.
2. Implement `IScopedEntity` for operational records.
3. Add CQRS commands/queries in Application.
4. Apply `IHierarchyAccessService` once in query handlers.
5. Use `IRbacScopeValidator` before mutations/state transitions.
6. Audit every critical state transition.
7. Keep controllers thin.
