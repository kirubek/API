# Frontend Integration Audit

## Audited frontend modules

Routes and services show these modules:
- Authentication and session management
- Dashboard and role dashboards
- Management: users, sections, hangars, shops
- Admin legacy user generation
- Employee profiles
- AUMS / production planning
- Post-mortem reports
- Carry-over reports
- Material order status reports
- SAFA inspections, templates, analytics
- ACE activities: 5S+1, QCPC, EH&S, reviews, reports
- Annual leave
- Bulletins
- Daily assignments
- Handover logbooks
- Monthly schedules
- De-hangaring reports and defect logs
- Reports/history/export
- Audit logs

## Integration result

DB-backed authoritative endpoints now exist for:
- `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/me`
- `/api/users/current`
- `/api/v1/users`, `/api/management/users`
- `/api/v1/organizations/sections`, `/api/management/sections`
- `/api/v1/organizations/hangars`, `/api/management/hangars`
- `/api/v1/organizations/shops`, `/api/management/shops`
- `/api/employee-profile/me`, `/api/employee-profile`, `/api/user/profile`
- `/api/audit`, `/api/audit/logs`, `/api/audit/suspicious`
- `/api/dashboard/stats`, `/api/v1/dashboard/metrics`, `/api/v1/dashboard/summary`

Compatibility endpoints now return valid empty contracts for operational modules so the frontend can remove mock fallbacks while backend aggregates are implemented incrementally.

## Backend authority decisions

- Frontend role `SystemAdministrator` maps to backend `SystemAdmin`.
- Frontend role `SafaInspector` maps to backend `SafetyInspector`.
- Frontend role `Production` must not become a primary backend role.
- Production planner access remains a dynamic capability through `ProductionPlannerAccess`.
- Management workspace assumptions are normalized into Section/Hangar/Shop. Future temporary/cross-functional assignment support should be modeled as operational assignments, not as RBAC truth.

## Routes requiring full domain persistence next

1. Monthly schedules and daily assignments
2. Handover logbooks
3. Annual leave workflow
4. AUMS projects and progress logs
5. Material order status reports
6. SAFA inspections and defects
7. ACE activity workflow
8. Carry-over and post-mortem workflows

Each future module must implement `IScopedEntity` and consume centralized scope/RBAC services only.
