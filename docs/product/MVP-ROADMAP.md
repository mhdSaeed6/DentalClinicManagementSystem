# DentalClinic — MVP Execution Roadmap

| Field | Value |
|---|---|
| Document status | Draft for stakeholder review — **not approved for implementation** |
| Version | 1.0 |
| Date | 2026-10-12 |
| Companion | [`PRD.md`](./PRD.md) (requirements, IDs), [`OPEN-QUESTIONS.md`](./OPEN-QUESTIONS.md) (decisions) |
| Baseline | Repository `DentalClinic`, branch `main`, commit `1369f0a` |

> **Purpose.** This document turns the PRD into an ordered, executable plan. It defines **what ships
> in each phase, where the code changes, what DB changes are needed, which tests prove it, how to
> roll it back, and the definition of done**. It does **not** paraphrase the PRD — requirement IDs
> (`TEN-002`, `INV-004`, `TEST-006`, …) point back to `PRD.md`.
>
> **No work starts until** the PRD and Phase 0 scope are explicitly approved (PRD rule) and the
> blocking `OPEN-QUESTIONS` are answered.

---

## How to read this document

Each phase follows the same template:

1. **Goal** — one sentence.
2. **Requirements** — PRD IDs delivered.
3. **Prerequisites** — what must already be true.
4. **Code areas** — files/projects likely to change (evidence-based; will be confirmed at implementation time).
5. **DB changes** — migrations, indexes, constraints, backfill.
6. **Tests** — new/updated tests, mapped to `TEST-###`.
7. **Acceptance criteria** — objective, checkable.
8. **Rollback** — how to undo safely.
9. **Effort** — rough person-day **range** with assumptions.
10. **Definition of Done (DoD)** — the shared "done" bar below, plus phase-specific items.

> **Universal DoD** (applies to every phase unless stated otherwise):
> - Builds clean (`dotnet build`) with analyzers on, no new warnings.
> - All tests green in CI (Docker/SQL available — see `OPEN-QUESTIONS Q10`).
> - New behavior covered by at least one automated test.
> - Server-side authorization and tenant scope applied to every new/changed endpoint.
> - Arabic/RTL smoke-checked for every new/changed screen.
> - No secrets, no client authoring of money, no client-supplied tenant.
> - PR reviewed by a second engineer; change is reversible (feature-flagged or additive where practical).

Effort assumptions: **small senior team (1–2 devs) already fluent with the stack**; ranges, not commitments. Estimates exclude waiting on external decisions.

---

## Phase overview

| Phase | Goal | Primary requirement IDs | Exit criteria |
|---|---|---|---|
| 0 | Stabilise & secure baseline | SEC-001, AUTH-005, D-05, D-08, D-12 | clean build; migrations apply; no secrets; tests run |
| 1 | Tenant foundation | TEN-001…005, D-06 | tenant isolation tests green |
| 2 | Authorization | AUTH-001/004/006, SEC-002, PAT-005, D-01 | authz tests green |
| 3 | Clinical core | TRT-001…005, PAT-003/004 | treatment tests green |
| 4 | Scheduling integrity | APT-001…004, D-03 | overlap tests green |
| 5 | Billing core | INV-002/009/010, SVC-003 | line/total/number tests green |
| 6 | Draft → issue workflow | WFL-001…005, INV-003…007, INV-008(void) | workflow tests green |
| 7 | Payments hardening | PAY-001…005 | concurrency/idempotency tests green |
| 8 | Dashboard, settings, audit | DASH-001, SET-001/003, TEN-006 | metrics/audit tests green |
| 9 | Operations | REL-001, D-13, D-14, health | runbook + restore rehearsal green |
| 10 | Pilot onboarding | TEN-005, launch checklist | go/no-go recorded |

Phases 0–7 are the **critical path** for a functioning pilot. Phases 8–10 complete trust, operability and onboarding. Tenant (1) and Authorization (2) must precede any multi-user or multi-clinic operation.

---

# Phase 0 — Stabilise & secure baseline

**Goal.** Make the existing solution build, migrate, and run safely before adding features.

**Requirements.** SEC-001, AUTH-005 (partial), D-05, D-08, D-12, D-15; PRD Parts B.4, I.

**Prerequisites.** Repository access; Docker or a SQL Server instance for tests; decision on secret storage (`OPEN-QUESTIONS` — hosting/secrets).

**Code areas.**
- `src/DentalClinic.Api/appsettings.json` / `appsettings.Development.json` — remove committed JWT secret; CORS policy rename (`MechanicShop` → app name) and remove stray origin.
- `src/DentalClinic.Infrastructure/DependencyInjection.cs` — password policy (min length ≥ 10, complexity, lockout).
- `src/DentalClinic.Infrastructure/Data/ApplicationDbContextInitialiser.cs` — replace `EnsureCreatedAsync()` with `MigrateAsync()`; make seeding environment-gated (no demo passwords in production).
- `src/DentalClinic.Infrastructure/Data/Configurations/*` — add global query filters for `TreatmentRecord` and `Payment` (D-08).
- `docker-compose.yml` / `Dockerfile` — SA password via env; fix Seq host (`ops.seq` → `seq`); remove hardcoded `TZ`.
- Web layout/titles — remove hardcoded "Al Noor Dental" (D-15) or move to config.

**DB changes.**
- None structural. Verify `__EFMigrationsHistory` state against the actual DB (see `OPEN-QUESTIONS Q4`); if `EnsureCreatedAsync` already produced the schema, reconcile the initial migration before switching to `MigrateAsync()`.
- This is the point where the migration baseline is locked. **Do not add tenant columns yet.**

**Tests.**
- Existing unit/application tests must pass.
- Add a startup test asserting migrations apply to a clean DB and `__EFMigrationsHistory` is populated.
- Add a query-filter test asserting soft-deleted `TreatmentRecord`/`Payment` are excluded.

**Acceptance criteria.**
- No secret literal in tracked files; app reads JWT secret from configuration/env.
- `docker compose up` reaches a working API with migrations applied.
- Demo seed data only appears in Development.
- `TreatmentRecord`/`Payment` soft-deletes are filtered.

**Rollback.** Revert configuration and initialiser changes; the DB is unchanged structurally, so no data rollback is required. Restore prior appsettings from source.

**Effort.** 3–5 person-days.

**DoD (phase-specific).** Migration-baseline decision recorded; tests executable in CI (`Q10` resolved for Phase 0).

---

# Phase 1 — Tenant foundation

**Goal.** Introduce the `Clinic` tenant root and guarantee no query or write can cross clinics.

**Requirements.** TEN-001…005, D-06; PRD Part F.2/F.3, I.2.

**Prerequisites.** Phase 0 complete (migration baseline locked).

**Code areas.**
- `src/DentalClinic.Domain/` — new `Clinic`, `ClinicUser` entities; add `ClinicId` to `Patient`, `Doctor`, `DentalService`, `Appointment`, `TreatmentRecord`, `Invoice`, `Payment`; `AuditLog` introduced here or Phase 8 (schema-stable either way).
- `src/DentalClinic.Application/Common/` — `ICurrentTenant` abstraction.
- `src/DentalClinic.Infrastructure/` — `ICurrentTenant` implementation reading the claim; global query filter helper reading `ICurrentTenant` inside `ApplicationDbContext`.
- `src/DentalClinic.Api/` — platform clinic endpoints (create/activate/suspend) — **or** defer the HTTP surface to Phase 2 if authorization is not yet in place. Recommended: add data model + filters in Phase 1, expose endpoints in Phase 2 where they can be permissioned.
- `src/DentalClinic.Infrastructure/Data/Migrations/` — one migration adding tables/columns/indexes.

**DB changes.**
- New tables `Clinics`, `ClinicUsers`.
- `ClinicId` columns + non-clustered indexes on every tenant table; composite indexes leading with `ClinicId` where useful (e.g. `(ClinicId, ScheduledDateTime)`).
- Global query filters on all tenant + soft-deletable entities.
- FKs to `Clinics` with `Restrict`/`NoAction`.
- Backfill: existing rows assigned the pilot clinic's id (data migration). See `OPEN-QUESTIONS Q4` for legacy invoice policy.

**Tests.** `TEST-005` (cross-tenant read/write → 404/403) at handler and API level; a filter test proving a user in clinic A cannot see clinic B rows.

**Acceptance criteria.**
- Every tenant-owned table has `ClinicId`, an index, and a global filter.
- `ClinicId` is derived server-side from the token, never from a request body.
- Creating a clinic + admin yields an isolated workspace.
- Isolation tests green.

**Rollback.** Additive migration (new tables/columns, nullable then backfilled). Roll back by reverting the migration; keep a pre-migration DB backup. Because filters depend on `ICurrentTenant`, ship the filter change and the backfill together.

**Effort.** 6–10 person-days.

**DoD (phase-specific).** `Q4` (legacy backfill/migration history) answered before running the migration on any non-empty database.

---

# Phase 2 — Authorization

**Goal.** Enforce least-privilege and clinic scoping server-side on every endpoint.

**Requirements.** AUTH-001 (enhance), AUTH-003, AUTH-004, AUTH-005 (complete), AUTH-006, SEC-002, PAT-005, D-01.

**Prerequisites.** Phase 1 (tenant context) complete.

**Code areas.**
- `src/DentalClinic.Application/` — permission constants; authorization behaviour or per-handler checks; extend current-user claims to include clinic id, roles, effective permissions.
- `src/DentalClinic.Api/Controllers/*` — apply `[Authorize(Policy = …)]` / permission attributes on every action; wire the existing `AdminOnly`/`DoctorOnly`/`StaffOnly` policies (currently unused).
- Platform clinic endpoints from Phase 1 — now protected by `platform.manage`.
- Identity — logout/refresh revocation endpoint; user activation/deactivation; create-user endpoint (`AUTH-003`); generic auth error messages (no user enumeration).
- `src/DentalClinic.Web/Components/` — permission-aware visibility of actions (defense-in-depth only).

**DB changes.**
- `AppUser` / `ClinicUser` — `IsActive` (or reuse existing flag if present; note identity has none today) `[CODE]`.
- Possibly a permissions cache column/table — keep it simple; derive permissions from role unless a decision requires custom sets.

**Tests.** `TEST-014` (receptionist cannot issue invoice or edit doctor/service → 403), permission matrix per role, deactivated-user login rejected, refresh-token revocation.

**Acceptance criteria.**
- No endpoint trusts client role/tenant.
- Every mutating endpoint requires an explicit permission.
- Deactivating a user blocks login and revokes refresh tokens.
- Auth error messages do not reveal account existence.

**Rollback.** Authorization is behavior-changing. Roll out policy-by-policy; if a regression occurs, relax the specific policy (revert that attribute) while keeping the framework. Keep the old UI toggles available until authz is proven.

**Effort.** 4–7 person-days.

**DoD (phase-specific).** A permission matrix document/table exists and each row has a test.

---

# Phase 3 — Clinical core

**Goal.** Make treatment records a trustworthy clinical record that can drive billing.

**Requirements.** TRT-001…005, PAT-001…005 (duplicate warning, archival, clinical restriction).

**Prerequisites.** Phases 1–2.

**Code areas.**
- `src/DentalClinic.Domain/TreatmentRecords/TreatmentRecord.cs` — add `ClinicId`, `ServiceId?`, `ClinicalNotes?`, `Status` (Planned/Performed/Cancelled), `PerformedAtUtc`; make `ToothNumber` nullable; keep `Cost`/`Fee` semantics.
- `src/DentalClinic.Domain/` — status transitions (`Planned → Performed`, cancel), enforce "only Performed is billable".
- `src/DentalClinic.Application/Features/TreatmentRecords/` — commands/queries; `PATCH …/perform`, `…/cancel`.
- `src/DentalClinic.Application/Features/Patients/` — duplicate-warning query; archive rules preventing deletion with issued invoices.
- `src/DentalClinic.Web/Components/Pages/Treatments.razor` — optional tooth, service picker, clinical notes, status.

**DB changes.**
- `TreatmentRecords`: add columns (`ClinicId`, `ServiceId`, `ClinicalNotes`, `Status`, `PerformedAtUtc`); make `ToothNumber` nullable; add global filter (Phase 0 already added the filter; confirm it composes with tenant filter).
- Index `(ClinicId, PatientId)`, `(ClinicId, AppointmentId)`.

**Tests.** `TEST-009` (Planned cannot be billed without explicit perform); status-transition unit tests; soft-delete/archive behavior; duplicate-warning.

**Acceptance criteria.**
- Non-tooth procedures can be recorded.
- Planned items are never billable implicitly.
- Medical history/notes gated by permission (`PAT-005`).
- Patients with financial history cannot be hard-deleted.

**Rollback.** Additive columns (nullable with defaults). Revert migration; UI additions are non-breaking if left behind.

**Effort.** 3–5 person-days.

---

# Phase 4 — Scheduling integrity

**Goal.** Prevent double-booking, safely and concurrently.

**Requirements.** APT-001…004, D-03; PRD E.1.

**Prerequisites.** Phases 1–2 (tenant + authz). Phase 3 helpful but independent.

**Code areas.**
- `src/DentalClinic.Application/Features/Appointments/Commands/CreateAppointment/CreateAppointmentCommandHandler.cs` — overlap check inside a transaction with a re-check.
- Reschedule handler — same check.
- `src/DentalClinic.Application/Features/Appointments/` — make `ServiceId` optional (`APT-001`); tighten `Complete` to `Scheduled`-only.
- `src/DentalClinic.Web/Components/Pages/Appointments.razor` — optional service, conflict messaging.

**DB changes.**
- Support overlap detection via indexed range query `(ClinicId, DoctorId, ScheduledDateTime)`. Optional: an exclusion strategy/`RowVersion` if the re-check under transaction is insufficient on SQL Server without `sp_getapplock`.
- No destructive schema change.

**Tests.** `TEST-006` (overlapping book → 409; adjacent slots allowed; concurrent requests safe); `TEST-003` (complete ≠ invoice) — relevant once Phase 6 lands, assert here that completing creates no invoice/payment.

**Acceptance criteria.**
- Overlapping appointment for same doctor/clinic rejected with 409.
- Adjacent/serial appointments allowed.
- Concurrent overlapping requests: at most one succeeds.

**Rollback.** Revert handler to previous logic (feature-flag the overlap check). Schema change additive.

**Effort.** 2–4 person-days.

---

# Phase 5 — Billing core

**Goal.** Make invoice money server-authoritative via line items.

**Requirements.** INV-001, INV-002, INV-009, INV-010, SVC-003; PRD E.6.

**Prerequisites.** Phase 1 (tenant). Independent of Phase 4.

**Code areas.**
- `src/DentalClinic.Domain/Invoices/Invoice.cs` — remove client-driven `TotalAmount`; add `InvoiceLine` collection; server computes `TotalAmount = Σ LineTotal`; add `ClinicId`, `InvoiceNumber`, `Currency`, `RowVersion`.
- New `InvoiceLine` entity + configuration.
- `src/DentalClinic.Application/Features/Invoices/Commands/CreateInvoice/` — ignore/reject client totals; create with lines.
- `CreateInvoiceCommandValidator` — validate lines, not just `> 0`.
- `src/DentalClinic.Web/Components/Pages/Invoices.razor` — line-item editor; format via clinic currency (remove hardcoded `USD`).

**DB changes.**
- New `InvoiceLines` table (Id, ClinicId, InvoiceId, Description, Quantity, UnitPrice, DiscountAmount, LineTotal, SourceTreatmentRecordId?, ServiceId?).
- `Invoices`: add `ClinicId`, `InvoiceNumber`, `Currency`, `RowVersion`, `IssuedAtUtc`/`IssuedBy` (used in Phase 6).
- Unique `(ClinicId, InvoiceNumber)`; `RowVersion` concurrency token.
- Backfill: existing invoices get one `InvoiceLines` row ("Legacy balance") preserving the old total (`OPEN-QUESTIONS Q4`).

**Tests.** `TEST-001` (server totals), `TEST-002` (manual invoice + full payment), `TEST-008` (catalog price change does not mutate history).

**Acceptance criteria.**
- A request supplying its own total cannot change the persisted total.
- Invoice total equals the sum of its lines, always.
- Currency is stored per clinic; formatting is locale-driven.
- Invoice number is human-readable, per-clinic, monotonic.

**Rollback.** Additive tables; the invoice total refactor is the risky part — keep a read-only fallback for legacy invoices (single "Legacy balance" line) and revert the validator/handler if needed.

**Effort.** 6–9 person-days.

---

# Phase 6 — Draft → issue workflow

**Goal.** Separate draft creation, issuance, and payment into distinct, safe steps.

**Requirements.** WFL-001…005, INV-003…007, INV-008 (void only); PRD C.4, E.3, E.4.

**Prerequisites.** Phases 2, 3, 5.

**Code areas.**
- `src/DentalClinic.Domain/Invoices/Invoice.cs` — lifecycle `Status` {Draft, Issued, Cancelled/Voided} separate from `PaymentStatus`; issuance assigns number, locks lines, records issuer/time.
- `src/DentalClinic.Application/Features/Invoices/` — `POST /invoices/drafts`, `POST /invoices/drafts/from-appointment/{id}` (idempotent), `POST /invoices/{id}/lines`, line edit/delete, `POST /invoices/{id}/issue`, `POST /invoices/{id}/void`.
- Draft synchronization rule (E.4 rule 11): auto-sync while Draft; forbidden/controlled once Issued.
- Idempotency for draft generation (key or deterministic unique guard on `(ClinicId, SourceAppointmentId)`).
- `src/DentalClinic.Web/Components/Pages/` — new appointment-detail/visit screen, draft review, issued read-only view.

**DB changes.**
- `Invoices`: `Status` lifecycle column; `IssuedAtUtc`, `IssuedBy`; possibly `SourceAppointmentId`.
- Unique guard for generated drafts (index/idempotency table).
- Migration to set legacy invoices to `Issued` (policy: `Q4`).

**Tests.** `TEST-003` (completing a visit creates no invoice/payment), `TEST-004` (retry draft → no duplicates), `TEST-011` (silent edit of issued invoice/line blocked), plus sync-rule tests (add treatment after draft → auto-sync while Draft; blocked after Issued).

**Acceptance criteria.**
- Draft ≠ issued ≠ paid: three explicit actions, each permissioned and audited.
- Completing an appointment never creates or issues an invoice.
- Repeating draft creation produces no duplicates.
- Issued invoices are immutable except via void/correction.
- Only `Performed` items enter a draft.

**Rollback.** Lifecycle is the highest-risk change. Gate issuance behind a permission and feature flag; legacy invoices remain `Issued` and are unaffected. Revert handlers/migration if needed; no data loss because the migration is additive + status backfill.

**Effort.** 5–8 person-days (draft/issue) + 3–5 (void/correction). Treat as core.

---

# Phase 7 — Payments hardening

**Goal.** Make concurrent/duplicate payments impossible to get wrong.

**Requirements.** PAY-001…005, D-04; PRD E.5.

**Prerequisites.** Phases 5–6.

**Code areas.**
- `src/DentalClinic.Domain/Invoices/Invoice.cs` — `RowVersion` on `Invoice`; domain rules for `0 ≤ PaidAmount ≤ TotalAmount`.
- `src/DentalClinic.Application/Features/Payments/` (or invoice payment handlers) — transaction + optimistic concurrency; idempotency key; reversal as a compensating entry.
- `src/DentalClinic.Domain/Payments/Payment.cs` — add `Method?`, `Reference?`, reversal link; ensure soft-delete filter (Phase 0).
- `src/DentalClinic.Web/Components/Pages/Invoices.razor` — payment UI: amount/method/reference; block overpay; double-submit safe.

**DB changes.**
- `Payments`: add `Method`, `Reference`, optional reversal fields; index `(ClinicId, InvoiceId)`.
- `Invoices.RowVersion` already added in Phase 5 — confirm concurrency enforcement.
- Optionally idempotency-key table.

**Tests.** `TEST-007` (overpay rejected, balance unchanged), `TEST-012` (double-submit → single payment), `TEST-013` (concurrent payments → no overpay, no lost update).

**Acceptance criteria.**
- Two simultaneous payments cannot overpay or lose an update.
- Duplicate submissions are idempotent.
- Reversal is a new compensating entry with audit; no silent delete.
- No payment is auto-created by treatment/invoice events.

**Rollback.** Revert handler changes; `RowVersion` columns are additive. If concurrency handling causes regressions, fall back to a serialized transaction for payment registration.

**Effort.** 3–5 person-days.

---

# Phase 8 — Dashboard, settings, audit UI

**Goal.** Give trustworthy numbers, per-clinic settings, and an accountability trail.

**Requirements.** DASH-001, SET-001, SET-003, TEN-006, D-09, D-10, D-15.

**Prerequisites.** Phases 1–7 (data to aggregate).

**Code areas.**
- `src/DentalClinic.Application/Features/Dashboard/` (new) — `GET /dashboard/summary`; document definitions (drafts excluded from issued financials).
- `src/DentalClinic.Web/Components/Pages/Dashboard.razor` — consume server metrics, drop client-side page-1 sums.
- New `AuditLog` entity + write path on sensitive actions (invoice issue/void, payment register/reverse, price change, user/role change, clinic suspend, login).
- Clinic settings endpoints `GET/PUT /clinic/settings` + UI; locale/timezone/currency; remove hardcoded clinic name.
- `InvoiceController`/`GetInvoicesRequest` — wire the existing `Status` filter (D-10).

**DB changes.**
- New `AuditLogs` table (Id, ClinicId?, ActorUserId, Action, EntityName, EntityId, OccurredAtUtc, Summary).
- `Clinics`: settings columns if not already present from Phase 1.
- Index `(ClinicId, OccurredAtUtc)` on audit.

**Tests.** `TEST-015` (dashboard totals match server aggregation), audit-entry creation and tenant scoping, settings round-trip and effect on formatting.

**Acceptance criteria.**
- Dashboard numbers come from server aggregation with documented definitions.
- Changing clinic currency/timezone changes display without touching stored data.
- Sensitive actions produce tenant-scoped audit entries (no full clinical payloads).
- Invoice status filter works server-side.

**Rollback.** Additive table/endpoints; revert UI to previous client-side aggregation if the server endpoint is wrong. Audit writes are additive (safe to keep).

**Effort.** 4–6 person-days.

---

# Phase 9 — Operations

**Goal.** Make the system deployable, recoverable and observable.

**Requirements.** REL-001, D-13, D-14, D-12 (Seq fix lands in Phase 0; monitoring follows here).

**Prerequisites.** Phases 0–8.

**Code areas.**
- Docker/compose — add the **Web** container (D-13); parameterize config.
- `src/DentalClinic.Web/Components/ClinicSessionState.cs` — persist session across refresh (D-14).
- API — `/health` (live/ready), DB connectivity probe; fix Seq endpoint; error-rate alerting in Prometheus/Grafana.
- Ops — automated SQL Server backups + documented, rehearsed restore.

**DB changes.** None (unless adding a health/ops table — not expected).

**Tests.** `TEST-010` (restore backup → records/relationships intact); health-check smoke; session-persistence UI test.

**Acceptance criteria.**
- Full stack (API + Web + SQL + observability) runs from one documented compose/deploy path.
- `/health` reflects DB connectivity.
- Backups scheduled; restore rehearsal recorded.
- No leftover config (`MechanicShop`, stray origins, wrong Seq host).
- Web session survives a browser refresh.

**Rollback.** Deployment changes are infra-level; keep the previous compose/deploy artifact and restore from backup if needed.

**Effort.** 4–7 person-days.

---

# Phase 10 — Pilot onboarding

**Goal.** Onboard the first clinic and decide go/no-go from real usage.

**Requirements.** TEN-005 (honesty), Part N checklist.

**Prerequisites.** Phases 0–9 accepted.

**Activities.**
- Provision the first clinic + admin (manual, `TEN-001`).
- Verify tenant isolation in production (second dummy clinic used only for isolation testing, if policy allows).
- Staff training on visit → draft → issue → payment.
- Agree feedback cadence and success metrics (PRD Part N hypotheses).
- Run the full Part N launch checklist.

**DB changes.** None beyond provisioning data.

**Tests.** Run the acceptance-gate suite from PRD Part J.3 against the deployed build.

**Acceptance criteria.** Every Part N checklist item is checked or explicitly waived with a reason; go/no-go recorded.

**Rollback.** Pilot is reversible: suspend the clinic (`TEN-001`), restore from backup if data is compromised. Keep a pre-pilot backup.

**Effort.** 3–5 person-days (coordination-heavy, depends on clinic availability).

---

## Cross-cutting sequencing rules

- **Tenant before authorization before everything with data.** Tenancy (1) and permissions (2) frame all later phases.
- **Money correctness is a release blocker, not a nice-to-have.** Until Phase 5–7 land, no pilot may take real payments.
- **No silent edits.** Immutability/void rules (6) and payment reversal (7) must exist before financial history is used.
- **Additive, reversible migrations.** Every migration should be deployable without downtime and revertible; avoid destructive drops during MVP.
- **Feature-flag risky changes** (overlap check, issuance, concurrency) so a regression can be contained without a rollback deployment.

## Deferred (explicitly out of MVP)

Operating hours/capacity (APT-005), calendar/server filters (APT-006), reminders/SMS-WhatsApp-email (NOT-002), reports/export (DASH-002), refunds/credit notes/e-invoicing (INV-008 full — pending `Q7`), subscription/self-onboarding, inventory/labs/imaging/ortho. See PRD Part K.

## Missing evidence to gather during build

- Actual CI capability for SQL/Docker tests (`Q10`) — blocks Testcontainers-based tests.
- `__EFMigrationsHistory` state on any existing database (`Q4`) — blocks the Phase 0/1 migration.
- Target market/currency/timezone/legal rules (`Q7`, `Q8`, `Q9`) — blocks settings defaults and any compliance claim.
- Whether the existing test suite passes in the current environment (README claims 380/380; not executed during discovery).

---

*End of roadmap. Build order and Phase 0 scope require explicit approval before implementation begins. Decisions that block Phases 0, 1 and 5 are listed in `OPEN-QUESTIONS.md`.*
