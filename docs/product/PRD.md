# DentalClinic — Product Requirements Document (PRD)

| Field | Value |
|---|---|
| Product | DentalClinic (Dental Clinic Management System) |
| Document status | Draft for stakeholder review — **not approved for implementation** |
| Version | 1.0 |
| Date | 2026-10-12 |
| Author role | Product Manager / Solution Architect / Business Analyst / Technical Lead |
| Target release | Pilot MVP (manual provisioning, single shared deployment) |
| Repository | `DentalClinic` (branch `main`, commit `1369f0a`) |
| Scope of this document | Analysis + requirements + MVP scope + phased plan. **No code changes made.** |

> **How to read evidence tags.** Every material claim is tagged:
> `[CODE]` confirmed from source, `[TEST]` confirmed by an automated test that exists in the repo,
> `[UI]` observed in the current UI/Blazor components, `[README]` stated only in project documentation,
> `[INFERRED]` reasoned but needs validation, `[UNKNOWN]` evidence not available.
>
> Where this document proposes something not in the repo, it is labelled **PROPOSED**.
> Where it reports a problem, it is labelled **GAP** or **DEFECT**.
>
> Test execution note: the repository contains **273 `[Fact]` and 46 `[Theory]` attributes across 105 test files** `[CODE]`.
> The README claims **380/380 passing** `[README]`. **Tests were not executed during this discovery**, so no
> pass/fail result is asserted here.

---

## Table of Contents

- Part A — Executive Summary
- Part B — Current-State Assessment
- Part C — Personas, Permissions & Workflows
- Part D — Functional Requirements
- Part E — Business Rules & State Transitions
- Part F — Data Model
- Part G — API & Integration Requirements
- Part H — UX & Screen Requirements
- Part I — Security, Privacy & Reliability
- Part J — Test Plan
- Part K — MVP Scope
- Part L — Implementation Roadmap
- Part M — Risks & Open Questions
- Part N — MVP Launch Checklist
- Part O — Traceability Matrix

Companion documents:
- [`MVP-ROADMAP.md`](./MVP-ROADMAP.md) — phased execution plan
- [`OPEN-QUESTIONS.md`](./OPEN-QUESTIONS.md) — decisions required before/during build

---

# Part A — Executive Summary

## A.1 Product overview

DentalClinic is a dental clinic management system consisting of:

- a **clean-architecture, CQRS REST API** (.NET 10) `[CODE]`, and
- an **Arabic/English RTL Blazor Web App** front end `[CODE]` `[UI]`.

The repository already implements patient, doctor, service, appointment, treatment-record, invoice and
payment modules with domain-level invariants, JWT authentication, soft delete, row audit fields and an
automated test suite `[CODE]`.

## A.2 Product vision

> **A calm, reliable clinic workspace that lets a small dental clinic run its whole day — booking, treating,
> billing and collecting — accurately, safely and in Arabic — and that later becomes a trustworthy
> multi-clinic SaaS without a rewrite.**

## A.3 Target customers

1. Small independent dental clinics (1–5 dentists, 1–3 chairs) in Arabic-speaking markets `[INFERRED]`.
2. Pilot clinics onboarded **manually by the platform operator** (no self-service signup) — per master prompt.

## A.4 Main problems to solve

| # | Problem | Evidence |
|---|---|---|
| P1 | Appointments, treatments and invoices are **not connected** into one visit workflow. | `TreatmentRecord`/`Invoice` are independent; no service reference on treatments; no invoice lines `[CODE]` |
| P2 | Invoices have **no real lifecycle** (no draft/issued), **no line items**, and **trust a client-submitted total**. | `Invoice.Create(patientId, totalAmount, appointmentId)` `[CODE]` |
| P3 | **No tenant model at all.** All clinics would share one flat dataset. | grep for `Tenant/ClinicId/Organization` → 0 matches `[CODE]` |
| P4 | **Authorization is effectively "any logged-in user can do anything"** — role policies exist but are never applied. | all controllers use `[Authorize]` only; `AdminOnly/DoctorOnly/StaffOnly` unused `[CODE]` |
| P5 | **Double-booking prevention is claimed but not implemented.** | README "collision checks" `[README]` vs `CreateAppointmentCommandHandler` has none `[CODE]` |
| P6 | Financial integrity gaps: `TotalAmount` is client-supplied; no concurrency guard on invoice/payment. | `CreateInvoiceRequest.TotalAmount` `[CODE]`; `Invoice` has no rowversion `[CODE]` |
| P7 | Production-readiness gaps: migrations unused, secrets committed, weak password policy, misleading config. | `EnsureCreatedAsync` `[CODE]`; `appsettings.json` secret `[CODE]`; `ops.seq` mismatch `[CODE]` |

## A.5 MVP goals

1. **Connect the visit workflow**: appointment → record actual treatment → request **draft** invoice → review → **issue** → register payment.
2. **Make the money correct**: server-authoritative totals derived from invoice **line items**; no client-trusted totals; no overpayment; idempotent, concurrency-safe operations.
3. **Protect patient data**: real role/permission enforcement on the server, tenant isolation (even for a single pilot), and auditability of sensitive actions.
4. **Make pilot operation simple and safe**: manual clinic provisioning, backup/restore, basic monitoring.
5. **Keep everything that already works** and preserve Arabic RTL UX.

## A.6 Explicit non-goals (MVP)

- Public clinic self-registration and self-service onboarding.
- Automated subscription billing / payment gateways.
- SMS/WhatsApp/email automation, external calendars.
- Advanced accounting (tax engines, e-invoicing compliance, general ledger, refunds/credit notes) — deferred pending the target market's legal requirements (`OPEN-QUESTIONS Q7`).
- Database-per-tenant, microservices, Redis, SignalR-driven real-time collaboration, distributed caching tiers.
- Insurance claims, inventory/stock, laboratories, imaging, orthodontic case management.
- "Verified regulatory compliance" claims — no compliance is asserted until legal review (`OPEN-QUESTIONS Q8`).

## A.7 Headline recommendation

Do **not** rewrite. Extend the existing clean architecture with a small number of well-chosen
additions: a `Clinic` tenant root + tenant stamping, invoice **line items** + a real invoice lifecycle,
a billing-status on treatment records, server-side authorization/permissions, and reliability fixes.
Sequence is defined in Part L and `MVP-ROADMAP.md`.

---

# Part B — Current-State Assessment

## B.1 Technology stack `[CODE]` unless noted

| Layer | Technology |
|---|---|
| Runtime | .NET 10 (`net10.0`), ASP.NET Core Web API |
| Front end | Blazor Web App (interactive server render mode) — `src/DentalClinic.Web` |
| App pattern | Clean Architecture; CQRS via MediatR 14; Result/Error pattern; FluentValidation 12 pipeline |
| ORM | EF Core 10 (SQL Server provider; SQLite provider available for tests) |
| DB | Microsoft SQL Server 2022 (Docker image) |
| Identity | ASP.NET Core Identity (`IdentityDbContext<AppUser>`), JWT bearer + refresh tokens |
| Caching | `HybridCache` (in-process + tag-based invalidation) |
| Observability | OpenTelemetry (traces/metrics), Prometheus exporter, Serilog + Seq, Grafana |
| Docs | Scalar + Swagger UI (OpenAPI) |
| Tests | xUnit, `WebApplicationFactory`, Testcontainers.MsSql, Respawn, NSubstitute |
| Quality | StyleCop analyzers; central package management (`Directory.Packages.props`) |
| Delivery | Dockerfile (API only), docker-compose (API, SQL Server, Seq, Prometheus, Grafana), GitHub Actions build-and-test |

Projects: `Api`, `Application`, `Contracts`, `Domain`, `Infrastructure`, `Web` + 5 test projects `[CODE]`.

## B.2 Architecture overview `[CODE]`

```
Web (Blazor)  --HTTP/JSON-->  Api (controllers, v1)
                                  |  MediatR
                                  v
                            Application (handlers, validators, behaviours, DTOs)
                                  |  IAppDbContext
                                  v
                            Infrastructure (AppDbContext, configs, interceptors, Identity, TokenProvider)
                                  |
                                  v
                            Domain (entities, value objects, enums, Result)
```

- **Request pipeline behaviours** `[CODE]`: Logging, Validation, UnhandledException, Performance, Caching (via `ICachedQuery`).
- **Persistence patterns**: no classic repository; handlers use `IAppDbContext` DbSets; `SaveChangesAsync` acts as unit of work.
- **Cross-cutting**: `AuditableEntityInterceptor` stamps `CreatedBy/CreatedAt/LastModified*`; `SoftDeleteInterceptor` converts deletes to soft deletes.
- **API conventions**: route `api/v{version}/...`, `ApiController` maps domain `Error` → ProblemDetails (Validation → 400, NotFound → 404, Conflict → 409, Unauthorized-kind → **403**), global exception handler.

## B.3 Existing module inventory

Legend: **[F]** fully implemented, **[P]** partial, **[M]** missing, **[U]** unverified.

### B.3.1 Identity & authentication — `[F]` core / `[P]` authorization

| Item | Status | Evidence |
|---|---|---|
| Email+password login → JWT + refresh | [F] | `IdentityController`, `IdentityService.AuthenticateAsync`, `TokenProvider` |
| Refresh-token rotation | [F] | `TokenProvider.CreateAsync` deletes prior tokens |
| Current user claims | [F] | `GET identity/current-user/claims` |
| Roles (Admin, Doctor, Receptionist) | [F] | seeded `IdentityRole`s |
| Role/permission enforcement per endpoint | [M] | policies `AdminOnly/DoctorOnly/StaffOnly` defined, **never applied** `[CODE]` |
| Password reset / email confirmation flow | [M] | `RequireConfirmedAccount=false`, no recovery endpoint |
| User management (create/deactivate clinic users) | [M] | no API; only seeder |

### B.3.2 Patients — `[F]`

CRUD + search by first/last name or primary phone `[CODE]`; medical history toggles + allergies + notes `[CODE]`; soft delete `[F]` (`PatientConfiguration.HasQueryFilter`) `[CODE]`.

### B.3.3 Doctors — `[F]`

CRUD + search + activate/deactivate (`IsActive`) `[CODE]`; soft delete query filter `[CODE]`. No clinic membership, no availability/schedule, no link to the logged-in user's identity `[CODE]`/`[INFERRED]`.

### B.3.4 Service catalog — `[F]`

CRUD + activate/deactivate; `Name`, `Description`, `Price`, `IsActive` `[CODE]`. **No duration field** `[CODE]` (master prompt lists duration as optional).

### B.3.5 Appointments — `[F]` CRUD, `[P]` scheduling rules

Create / get / list / reschedule / complete / cancel / no-show `[CODE]`; background service auto-marks overdue as no-show `[CODE]`.
**Missing:** overlap/double-booking prevention `[CODE]`, operating-hours enforcement (`OpeningTime/ClosingTime/MaxSpots` configured but unused `[CODE]`), calendar view.

### B.3.6 Treatment records — `[F]` basic, `[P]` clinical depth

Create/update/get/list; `ToothNumber` validated against FDI (permanent 11–48, pediatric 51–85) `[CODE]`; `ProcedureDetails`, `Cost` `[CODE]`.
**Missing:** service/procedure link, procedure **status** (planned vs performed), clinical notes separate from procedure text, non-tooth procedures (tooth number is required) `[CODE]`, delete/cancel `[M]`, link to invoices `[M]`.

### B.3.7 Invoices — `[P]` (minimal)

Create (patient, optional appointment, **total amount from client**), get, list `[CODE]`. Payments registered separately `[CODE]`.
**Missing:** line items `[M]`, invoice number `[M]`, draft/issued lifecycle `[M]`, issuance action `[M]`, edit draft `[M]`, cancel/void/credit `[M]`, discount/tax/currency `[M]`, price snapshots `[M]`, tenant `[M]`.

### B.3.8 Payments — `[P]`

Add payment with partial-payment support; `PaidAmount`/`Status` recalculated in-domain; overpayment rejected in-domain `[CODE]`; payment history in UI `[UI]`.
**Missing:** payment method `[M]`, reference `[M]`, idempotency key `[M]`, reversal/refund `[M]`, concurrency protection `[M]`, recorded-by beyond audit fields `[M]`.

### B.3.9 Dashboard & reports — `[P]`

Client-side aggregation of first 100 rows for patients/appointments/invoices `[CODE]`. No server reports/export.

### B.3.10 Settings & localization — `[P]`

Arabic/English inline toggle (`Session.T`), RTL via `dir`, light/dark theme `[CODE]` `[UI]`. Currency hardcoded `USD` `[CODE]`. No clinic-level settings, no persisted user preferences, no timezone handling (a few UTC-vs-local mixes; see Part I).

## B.4 Confirmed defects and integrity risks `[CODE]`

| ID | Severity | Finding | Where |
|---|---|---|---|
| D-01 | High | **Authorization not enforced by role.** Every endpoint requires only authentication. A receptionist can create/edit/pay invoices, edit doctors/services, etc. | all controllers `[Authorize]`; policies unused |
| D-02 | High | **Invoice total trusted from client.** No server derivation from source items; `CreateInvoiceCommandValidator` only checks `> 0`. | `CreateInvoice*` |
| D-03 | High | **No double-booking/overlap check.** README claims collision checks; handler has none. | `CreateAppointmentCommandHandler` |
| D-04 | High | **No concurrency control on `Invoice`.** Two concurrent payments can both read the same `PaidAmount` and one update is lost. No rowversion/concurrency token. | `Invoice`, `InvoiceConfiguration` |
| D-05 | High | **Migrations are not applied at runtime.** Startup calls `EnsureCreatedAsync()`; migrations exist but are unused → schema drift risk in production. | `ApplicationDbContextInitialiser.InitialiseAsync` |
| D-06 | High | **No multi-tenancy.** One flat dataset; a second clinic would see the first clinic's data. | repo-wide |
| D-07 | Medium | **Secrets & weak credentials committed.** JWT secret in `appsettings.json`; SA password in `docker-compose.yml`; demo users/passwords in seeder; password policy min length 6, no complexity. | config/seed/`AddIdentityCore` |
| D-08 | Medium | **Soft delete filters incomplete.** `HasQueryFilter` on Patient/Doctor/Service/Appointment/Invoice, but **not** on `TreatmentRecord` or `Payment`. Deleting a patient/doctor does not remove their treatment records/payments from queries (and cascade FKs exist in schema). | configs + migration |
| D-09 | Medium | **Dashboard metrics unreliable at scale.** Sums/counts computed client-side from page 1 (100 rows). "Collected"/"Outstanding" are guesses beyond 100 invoices. | `Dashboard.razor` |
| D-10 | Medium | **Invoice status filter not wired.** `GetInvoicesQuery.Status` exists; controller request DTO has no `Status`; UI filters only the current page. | `InvoiceController`/`GetInvoicesRequest` |
| D-11 | Medium | **No invoice number.** UI shows `Id[..8]`, unbillable/ambiguous for real invoices. | `Invoice` |
| D-12 | Low | **Config leftovers.** CORS policy named `MechanicShop` with stray origin `http://mechanicshop-app`; Serilog targets `http://ops.seq:5341` while compose service is `seq`; Dockerfile pins `TZ=America/Montreal`. | appsettings/compose/Dockerfile |
| D-13 | Low | **Web container absent.** Dockerfile/compose build only the API; the Blazor front end has no container/deployment path. | Dockerfile |
| D-14 | Low | **Session not persisted.** JWT held only in scoped `ClinicSessionState`; a browser refresh drops the session (logout). | `ClinicSessionState` |
| D-15 | Low | **Hardcoded clinic identity.** "Al Noor Dental" appears in layout/login/titles. | Web components |
| D-16 | Low | **Non-functional notification bell.** No notifications backend. | `MainLayout.razor` |

## B.5 Unverified assumptions `[UNKNOWN]`/`[INFERRED]`

- That the full test suite passes in the current environment (needs Docker/SQL; not run here).
- That inbound claim mapping reliably populates `ClaimTypes.NameIdentifier` for `CurrentUser.Id` (works in tests `[INFERRED]`).
- Intended target market / currency / timezone / fiscal rules (`OPEN-QUESTIONS`).
- Whether integration tests depend on Testcontainers (Docker) or can run offline `[INFERRED]`.
- Actual production hosting model for the Blazor Web app `[UNKNOWN]`.

## B.6 Documentation vs reality

| Claim in README | Reality |
|---|---|
| "Advanced scheduling system with collision checks" | No overlap check `[CODE]` (D-03) |
| "Automated invoice generation" | No generation from treatments; manual totals only `[CODE]` |
| "fine-grained role & permission access control" | Roles exist; no per-endpoint enforcement `[CODE]` (D-01) |
| "380/380 Passed" | Counts differ (273 Fact + 46 Theory); not executed here `[README]` |

---

# Part C — Personas, Permissions & Workflows

## C.1 Personas

| Persona | Scope | Goals | Key permissions (PROPOSED baseline) |
|---|---|---|---|
| **Platform Administrator** | Platform (cross-clinic) | Create/suspend clinics, provision clinic admins, monitor platform health | Manage clinics & clinic admins; no clinical/financial edit by default |
| **Clinic Administrator** | One clinic | Run their clinic; manage users, services, prices; see all clinic financials | Full clinic scope: patients, appointments, treatments, invoices, payments, users, settings |
| **Receptionist** | One clinic | Book/reschedule appointments, register patients, take payments | Patients (create/edit), appointments, view treatments, **view invoices, register payments**; cannot edit prices, issue/void invoices, edit doctors/services |
| **Dentist** | One clinic | Record clinical treatment; review their visits | Patients (view/edit clinical), appointments (view), treatments (create/edit/perform), view invoices; cannot manage users/services/prices |

> Keep the existing three Identity roles (`Admin`, `Doctor`, `Receptionist`) `[CODE]` and add a platform-level
> distinction. **A clinic administrator must not gain platform-wide privileges** (master-prompt rule).

## C.2 Permission model — recommendation

Introduce an explicit **permission set** rather than scattered role checks (PROPOSED, `SEC-002`):

`patient.read`, `patient.write`, `appointment.read`, `appointment.write`, `treatment.read`, `treatment.write`,
`invoice.read`, `invoice.create`, `invoice.issue`, `invoice.void`, `payment.read`, `payment.register`,
`payment.reverse`, `catalog.manage`, `staff.manage`, `clinic.settings.manage`, `platform.manage`.

Roles map to permission bundles. Authorization must be enforced **server-side in the Application layer**
(handler/behaviour or policy), not only in controllers.

## C.3 User stories (representative)

| ID | As a… | I want… | So that… |
|---|---|---|---|
| US-01 | Receptionist | book an appointment (patient, doctor, service, time) | the visit is scheduled |
| US-02 | Receptionist | reschedule/cancel/mark no-show | the schedule stays accurate |
| US-03 | Dentist | open a completed visit and record each procedure (tooth, procedure, fee) | the clinical record is accurate |
| US-04 | Dentist | record a treatment without an appointment | walk-ins are captured |
| US-05 | Dentist/Receptionist | request a **draft** invoice from the visit's billable items | billing is fast and complete |
| US-06 | Clinic Admin | review/edit the draft (prices, discounts, manual lines) and **issue** it | the invoice is correct and immutable |
| US-07 | Receptionist | register a full or partial payment | collection is tracked |
| US-08 | Clinic Admin | correct a mistake without silently editing an issued invoice | history stays trustworthy |
| US-09 | Clinic Admin | see who did what and when | accountability |
| US-10 | Platform Admin | create a clinic and its admin | onboard a pilot |
| US-11 | Clinic Admin | isolate my clinic's data | privacy & trust |
| US-12 | Dentist (Arabic) | use the app fully in Arabic RTL | usability |

## C.4 Target end-to-end workflow (the core of the MVP)

```
Receptionist                    Dentist                         Authorized user
-----------                     -------                         ---------------
create appointment  ──────►  patient attends
(optional expected service)       │
                                  ▼
                          open appointment → record actual treatment(s)
                          (tooth, procedure, notes, fee) [status: performed]
                                  │
                                  ▼
                          "Create draft invoice from this visit"
                                  │
                                  ▼
                          ┌──────────────────────────┐
                          │ DRAFT invoice (editable) │  ← review / adjust / add manual line
                          └──────────────────────────┘
                                  │
                                  ▼
                          "Issue invoice"  (explicit, confirmed)
                                  │
                                  ▼
                          ISSUED invoice (immutable; corrections via controlled actions)
                                  │
                                  ▼
                          register payment (partial or full)  → balance recalculated on server
```

Non-negotiable rules (see Part E):
- Completing an appointment **never** issues an invoice or registers a payment.
- Creating a draft invoice ≠ issuing it ≠ registering a payment — three separate actions.
- Only **performed/billable** treatment items enter a draft.
- The server recalculates all totals.

## C.5 Critical edge cases

- Adding a treatment **after** a draft exists (sync rule — E.4).
- Double-click / retried "create draft" (idempotency — E.4, `INV-006`).
- Two receptionists pay the same invoice simultaneously (concurrency — E.5).
- Issuing an invoice with zero lines, or below remaining paid amount.
- Deleting/soft-deleting a patient who has issued invoices (must not orphan financial history).
- Changing a service's default price (must not mutate history — E.6).
- Appointment completed with no treatment recorded, then draft requested (empty draft; allowed with warning).
- Non-tooth procedures (e.g., consultation) where a tooth number makes no sense.

---

# Part D — Functional Requirements

Each requirement: **ID · Title · Priority (P0/P1/P2) · Type (New/Enhance/Fix)**.
"Existing implementation" notes what the repo already provides.

## D.1 Authentication & user management

### AUTH-001 — Secure login/logout (P0, Enhance)
- **Description:** Email+password login issuing JWT + refresh token; explicit logout revoking the refresh token.
- **Business justification:** Control access to patient/financial data; support session termination.
- **Preconditions:** User exists and is active.
- **Main flow:** POST credentials → validate → issue tokens → client stores session.
- **Alternative/error:** Bad credentials → 401/409 mapped consistently; inactive user → 403.
- **Acceptance criteria:** Bad password returns a generic error (no user enumeration); logout invalidates refresh token; access token TTL configurable.
- **Dependencies:** `AUTH-004` (active flag), `TEN-004` (clinic context in token).
- **Existing:** JWT login + refresh implemented `[CODE]`. **GAP:** no logout/revoke endpoint; `IdentityService.AuthenticateAsync` reveals whether the email exists ("User not found") `[CODE]`.

### AUTH-002 — Password recovery (P1, New)
- Optional; only if an email/SMS channel is chosen. Defer if no channel exists (`OPEN-QUESTIONS Q6`).

### AUTH-003 — Invitation / manual account creation (P0, New)
- Platform admin creates a clinic admin; clinic admin creates staff accounts.
- MVP approach: **manual creation via admin-only API + emailed one-time set-password link OR a temporary password with forced change**. Depends on channel decision.
- Acceptance: a created user is bound to exactly one clinic (MVP), has a role, and cannot access other clinics.

### AUTH-004 — Activation/deactivation (P0, New)
- Users can be deactivated (cannot log in) without deleting history. `IsActive` on identity or a profile table.
- Acceptance: deactivated user's existing refresh tokens are revoked; login returns 403.

### AUTH-005 — Session security (P0, Enhance)
- Password policy strengthened (min length ≥ 10, complexity, lockout after repeated failures).
- Secrets moved out of source (`SEC-001`).
- Acceptance: failed-login lockout works; no secret in repo.

### AUTH-006 — Current-user & permissions (P0, Enhance)
- `GET current-user/claims` must also return clinic id, role(s) and effective permissions.
- Acceptance: front end can hide/disable actions by permission (defense-in-depth only; server still enforces).

## D.2 Manual clinic provisioning & multi-tenancy

### TEN-001 — Clinic entity & provisioning (P0, New)
- Platform admin can **create a clinic** (name, contact, default currency/timezone), **activate/suspend** it, and view basic status.
- MVP does **not** include public self-registration.
- Acceptance: creating a clinic + its admin yields a working isolated workspace; suspended clinic users cannot log in.

### TEN-002 — Tenant identity & ownership (P0, New)
- Introduce a `Clinic` (tenant) root. Every business row that must be isolated carries a `ClinicId`.
- Entities: `Patient`, `Doctor`, `DentalService`, `Appointment`, `TreatmentRecord`, `Invoice`, `Payment`, `AuditLog`; plus clinic user membership.
- **PROPOSED decision:** **shared database, shared schema, `ClinicId` discriminator + global query filter** (not DB-per-tenant). Trade-off: simplest to operate for a small number of clinics; isolation enforced centrally; scale-out later by sharding if ever needed.

### TEN-003 — Tenant resolution & server-side enforcement (P0, New)
- Resolve `ClinicId` from authenticated context (claim), **never** from a client-supplied field alone.
- Enforce via EF Core global query filter + an `ICurrentTenant` service + server-side checks on every write.
- Acceptance: cross-tenant read/write attempts fail (404/403), covered by automated tests (`TEST-005`).

### TEN-004 — Clinic membership & roles (P0, New)
- A user belongs to one clinic (MVP) with a role. Platform admins are separate (no clinic membership).
- Acceptance: a clinic user's token/claims scope all queries to their clinic.

### TEN-005 — Single-pilot honesty (P0, Process)
- If the pilot runs with one clinic initially, still implement `TEN-001..004`. Do **not** advertise "multi-tenant SaaS" until isolation tests pass and a second clinic is onboarded.

### TEN-006 — Audit logging (P0, New)
- Append-only `AuditLog` for sensitive actions: invoice issue/void, payment register/reverse, price changes, user/role changes, clinic suspend, login.
- Fields: clinic, actor, action, entity, entityId, timestamp, before/after summary (no full clinical payload).
- Acceptance: `SEC-005` tests confirm entries are created and tenant-scoped.

## D.3 Patient management

### PAT-001 — Patient profile (P0, Enhance)
- Minimum: first/last name, primary phone, DOB, gender; optional secondary phone, WhatsApp flag; medical history (diabetes/hypertension/heart/allergies/notes).
- Acceptance: create/read/update validated server-side; Arabic names preserved.

### PAT-002 — Search / filter / pagination (P0, Enhance)
- Server-side search by name/phone with pagination (exists) `[CODE]`; add stable sort and null-safe search.
- Acceptance: search is case-insensitive and clinic-scoped.

### PAT-003 — Duplicate handling (P0, New)
- Warn on likely duplicates (same phone or same name+DOB) before creating; allow override with reason.
- Acceptance: duplicate warning shown; override audited.

### PAT-004 — Archival (P0, Enhance)
- Soft-delete exists; **must not** delete invoices/payments/treatment history. Prevent deleting a patient who has issued invoices (or require explicit archive semantics).
- Acceptance: archived patient hidden from lists but financial history intact and auditable.

### PAT-005 — Clinical access restriction (P0, New)
- Medical history and treatment notes visible only to Dentist/Clinic Admin (per permissions). Receptionist sees contact/appointment data only.
- Acceptance: receptionist cannot read medical history/treatment notes via API.

## D.4 Doctors & staff

### DOC-001 — Doctor profile (P0, Enhance)
- Name, specialization, contact, gender, active flag (exists) `[CODE]`; add optional link to a user account.
- **DOC-002 — Clinic membership & assignment (P0, New):** doctors belong to a clinic; appointments/treatments attributed within the clinic.
- **DOC-003 — Availability (P2, New):** working hours/chair — defer unless pilots need it.
- **DOC-004 — Staff roles (P0, covers AUTH-003):** create/deactivate staff with role.
- Acceptance: no cross-clinic usage of doctors in appointments/treatments.

## D.5 Service catalog

### SVC-001 — Catalog CRUD (P0, Enhance)
- Name, description, default price, active/inactive (exists) `[CODE]`; **SVC-002 (P1)** add optional expected duration.
- **SVC-003 — Price ≠ charge (P0, Rule):** default price is only a suggestion; the actual charge is captured on the treatment/invoice line. Changing the default must not alter issued invoices or historical records (acceptance `TEST-008`).
- **SVC-004 — Catalog ↔ treatment separation (P0, Rule):** catalog item is not a treatment record.
- Acceptance: price change leaves all existing invoices and treatment records unchanged.

## D.6 Appointments

### APT-001 — Create (P0, Enhance)
- Patient, doctor, **optional expected service**, date/time, duration, notes. (Master prompt says the service is *optional*; current code requires `ServiceId` `[CODE]`.)
- Acceptance: appointment created; expected service is a **plan**, not a performed procedure.

### APT-002 — Double-booking prevention (P0, Fix — D-03)
- Reject creating/rescheduling an appointment that overlaps an existing non-cancelled appointment for the same **doctor** within its duration window.
- Concurrency: enforce inside a transaction with a re-check (and/or a DB constraint strategy) so simultaneous requests can't both succeed (`TEST-006`).
- Acceptance: overlapping book → 409; non-overlapping adjacent slots allowed.

### APT-003 — Lifecycle (P0, Enhance)
- States: `Scheduled`, `Completed`, `Cancelled`, `NoShow` (exists) `[CODE]`; consider `InProgress` if the workflow needs an "open visit" state (see `E.1`, `OPEN-QUESTIONS Q3`).
- Transitions and guards as in Part E. `Complete` must **not** create invoices/payments.

### APT-004 — Reschedule/cancel/no-show (P0, Enhance)
- Existing operations; reschedule must run overlap check; cancel/no-show audited.

### APT-005 — Operating hours & capacity (P1, New)
- Enforce `OpeningTime`/`ClosingTime` and optionally `MaxSpots` (config exists but unused `[CODE]`).

### APT-006 — Calendar & filters (P1, Enhance)
- Day/week calendar view; server-side status/date filters (today's appointments etc.). UI currently filters the current page client-side `[UI]`.

## D.7 Clinical treatment records

### TRT-001 — Record a procedure (P0, Enhance)
- Fields: patient, clinic, doctor, **optional appointment**, **optional service**, **tooth (optional)**, procedure details, clinical notes, **status**, **actual fee**, treatment date, timestamps.
- **GAP to fix:** tooth number is currently mandatory `[CODE]`; make it optional for non-tooth procedures.

### TRT-002 — Status & billing flag (P0, New)
- Status: `Planned` → `Performed` → (`Cancelled` / `NotPerformed`). Only `Performed` items are billable.
- A `Planned` item must **never** be billed without an explicit authorized "mark as performed" action (`TEST-009`).

### TRT-003 — Multiple procedures per visit (P0, Enhance)
- Many treatment records per appointment; query by appointment `[CODE]` supports this.

### TRT-004 — Correct / cancel (P0, New)
- Editable until billed; after a draft exists, follow the synchronization rule (`INV-007`/`E.4`); once issued, changes are controlled corrections, never silent edits.

### TRT-005 — Clinical vs financial separation (P0, Rule)
- Clinical notes stay in the clinical record; invoices carry only billing-relevant descriptions (avoid leaking clinical detail into invoices unnecessarily).

## D.8 Appointment-to-treatment workflow

### WFL-001 — Open visit & record treatment (P0, New/Enhance)
- From an appointment, open a treatment-entry screen; add multiple performed items; save as you go.
- Acceptance: records are linked to the appointment and patient.

### WFL-002 — Request draft invoice (P0, New)
- From the visit, "Create draft invoice" collects eligible `Performed` billable items for that appointment (and optionally unbilled items for the patient).
- Draft is editable; **no payment is created**; **not issued**.

### WFL-003 — Review & issue (P0, New)
- Authorized user reviews/adjusts the draft and explicitly issues it (see `INV-004`).

### WFL-004 — Record without appointment (P0, Enhance)
- Supported today (`AppointmentId` nullable) `[CODE]`; keep allowed, clinic-scoped.

### WFL-005 — Idempotency & retries (P0, New)
- Repeating "create draft" for the same appointment/item set must not duplicate invoices/lines (`TEST-004`).
- Acceptance: idempotency key or deterministic rule; concurrent requests safe.

## D.9 Invoicing

### INV-001 — Manual invoice (P0, Enhance)
- Create an invoice manually for a patient (no appointment required) with editable lines.

### INV-002 — Invoice line items (P0, New — D-02/D-11)
- Each line: description snapshot, quantity, unit price, optional discount, line total, optional source treatment id, optional service id.
- `Invoice.TotalAmount` = Σ line totals, recalculated on the **server**.

### INV-003 — Draft lifecycle (P0, New)
- States: `Draft` → `Issued` → (`PartiallyPaid`/`Paid`) with separate `PaymentStatus`; plus `Cancelled/Voided`.
- Draft is editable; only issuance locks it.

### INV-004 — Issuance (P0, New — D-13/workflow)
- Explicit, confirmed action; assigns an invoice number; locks lines; records issuer + timestamp; audited.
- Acceptance: no automatic issuance from completing an appointment (`TEST-003`).

### INV-005 — Protection of issued invoices (P0, Rule)
- Issued invoices are immutable except via controlled operations (void with reason, credit note, superseding correction). Never silent edits (`TEST-011`).

### INV-006 — Duplicate prevention (P0, New)
- Prevent duplicate invoices from repeated "generate from appointment" requests; enforce at the DB level where possible (e.g., unique index on `(ClinicId, SourceAppointmentId)` for generated invoices with a rule, or an idempotency token).

### INV-007 — Synchronization rule (P0, New)
- Define exactly what happens when a performed billable treatment is added/edited/removed **after** a draft exists (auto-sync while Draft; forbidden/controlled once Issued). See E.4.

### INV-008 — Cancellation/void/credit (P1, New — pending accounting decision)
- MVP: `Void` a draft (no financial effect) and a controlled cancel of an issued invoice **only if unpaid** with reason + audit. Refunds/credit notes depend on `OPEN-QUESTIONS Q7`.

### INV-009 — Currency & precision (P0, New)
- Store currency code + decimal(18,2); never hardcode USD in the domain; format per clinic. Web currently hardcodes `USD` `[CODE]`.

### INV-010 — Numbering (P0, New)
- Human-readable, per-clinic, monotonic invoice number; displayed and searchable.

## D.10 Payments

### PAY-001 — Register payment (P0, Enhance)
- Amount, date, optional method, reference/notes; partial allowed; server recalculates `PaidAmount`/`Status`.
- Acceptance: overpayment rejected (`TEST-007`); no payment auto-created by treatment/invoice events.

### PAY-002 — Concurrency (P0, Fix — D-04)
- Two simultaneous payments must not exceed the balance and must not lose updates. Use a transaction + optimistic concurrency token on `Invoice`.

### PAY-003 — History & balance (P0, Enhance)
- Payment history displayed (exists) `[UI]`; authoritative balance from server.

### PAY-004 — Correction/reversal (P1, New)
- Permissioned reversal with reason + audit; never a silent delete.

### PAY-005 — Duplicate submission protection (P0, New)
- Idempotency on payment submission (double-click safe).

## D.11 Dashboard & reports

### DASH-001 — Operational metrics (P0, Enhance)
- Today's appointments + status mix; patient count; draft vs issued invoices; outstanding balance; payments received (period).
- **Must** come from server endpoints (not client-side sums of page 1) `[CODE gap D-09]`.
- Definitions: totals must not mix drafts with issued financials.

### DASH-002 — Reports/export (P2) — defer until pilots ask.

## D.12 Settings & localization

### SET-001 — Clinic settings (P0, New)
- Clinic name, contact, currency, timezone, invoice numbering prefix, default language. Removes hardcoded "Al Noor Dental" `[CODE]`.

### SET-002 — Localization & RTL (P0, Enhance)
- Preserve Arabic RTL `[UI]`; date/time/number formatting per clinic locale; ensure all new screens are localized.

### SET-003 — Currency/timezone correctness (P0, New)
- Store UTC; render in clinic timezone; single source of truth for "now".

## D.13 Notifications & integrations

### NOT-001 — In-app feedback (P0, Enhance)
- Keep existing toasts `[CODE]`; add validation/empty/loading states consistency.

### NOT-002 — Reminders (P1/P2)
- Defer SMS/WhatsApp/email until a channel decision and pilot demand (`OPEN-QUESTIONS Q6`).

---

# Part E — Business Rules & State Transitions

## E.1 Appointment lifecycle

```
                 +-----------+   Complete   +-----------+
   create  --->  | Scheduled | -----------> | Completed |   (terminal for MVP)
                 +-----------+              +-----------+
                    |    |                     ^
          Cancel    |    |  MarkNoShow         | (rule: Complete only from Scheduled)
                    v    v
             +-----------+  +---------+
             | Cancelled |  | NoShow  |
             +-----------+  +---------+
                (terminal)    (terminal)
```

Rules:
- `Complete` allowed only from `Scheduled` (current code allows only if not already Completed/Cancelled `[CODE]`; tighten to Scheduled-only).
- **Completing never creates an invoice or payment** (INV-004, PAY-001).
- Reschedule allowed from `Scheduled` only, and must pass the overlap check (APT-002).
- If `InProgress` is adopted (`OPEN-QUESTIONS Q3`), it sits between Scheduled and Completed and represents an open visit.

## E.2 Treatment record lifecycle

```
  Planned ──mark performed──► Performed ──bill (draft line)──► Billed
     │                           │                              │
     └──── cancel ───► Cancelled  └── correct (before billed)    └── corrected via controlled action
```

- Only `Performed` is billable. `Planned` requires an explicit authorized transition.
- Editing a `Billed` item requires the sync rule (`INV-007`).
- Anonymous-tooth allowed (TRT-001 fix).

## E.3 Invoice lifecycle

```
   (manual create)         "generate from visit"
          │                        │
          v                        v
      +--------+   edit    +--------+
      | Draft  | <----------| new draft |
      +--------+             +--------+
          │  Issue (explicit, confirmed)
          v
      +--------+   payment    +----------------+   payment    +------+
      | Issued | -----------> | PartiallyPaid  | -----------> | Paid |
      +--------+              +----------------+              +------+
          │                           
          └── Void/Cancel (paid==0, reason, audited) ──► Cancelled/Voided
```

- **PaymentStatus is separate from invoice lifecycle.** Store both: `Status` ∈ {Draft, Issued, Cancelled/Voided} and `PaymentStatus` ∈ {Unpaid, PartiallyPaid, Paid}.
- Issuance assigns the number and freezes lines.

## E.4 Draft generation & synchronization rules (acceptance criteria)

| # | Rule | Atomic? | Auth |
|---|---|---|---|
| 1 | Completing an appointment never auto-issues an invoice | — | — |
| 2 | A user must explicitly request draft creation | — | invoice.create |
| 3 | Only `Performed` + `Billable` treatments for the clinic/patient/appointment enter the draft | yes | invoice.create |
| 4 | Each line stores a **snapshot** of description + unit price (so later catalog/treatment edits don't mutate it) | yes | — |
| 5 | Invoice references patient + clinic; references appointment/treatment when applicable | yes | — |
| 6 | Draft creation registers **no** payment | yes | — |
| 7 | Draft is editable before issuance | — | invoice.create |
| 8 | Repeating the request creates no duplicate invoice/lines | yes | — |
| 9 | Concurrent requests are safe (transaction + idempotency) | yes | — |
| 10 | Changing a service default price never alters a draft/issued invoice | — | — |
| 11 | **Post-draft treatment changes:** while Draft → optionally auto-sync (add missing performed lines / flag removed ones); once Issued → no auto-change, use controlled correction | yes | invoice.create |
| 12 | Issued invoices follow controlled correction (void/credit), never silent edit | yes | invoice.void |
| 13 | Server recalculates & validates all totals | yes | — |
| 14 | Tenant boundaries enforced on every related record | yes | — |

## E.5 Payment rules

- `amount > 0`; `Σ payments ≤ TotalAmount`; balance server-computed.
- Concurrent payments: transaction + optimistic concurrency; reject if it would overpay (`TEST-007`).
- Reversal is a new compensating entry (not a delete), audited (`PAY-004`).
- Idempotency key per submission (`PAY-005`).

## E.6 Price history rules

- Catalog `Price` change affects only future suggestions.
- Treatment `Fee` is captured at record time.
- Invoice line stores its own snapshot.
- Therefore: **issued and draft invoices and historical treatment records are never mutated by a catalog price change** (`TEST-008`).

## E.7 Atomic operations & authorization summary

| Operation | Atomic required | Minimum permission |
|---|---|---|
| Create appointment / overlap check | yes | appointment.write |
| Complete appointment | no (but transactional) | appointment.write |
| Create/patch treatment | no | treatment.write |
| Generate draft invoice | yes | invoice.create |
| Issue invoice (numbering + lock) | yes | invoice.issue |
| Register payment (+ concurrency) | yes | payment.register |
| Void invoice | yes | invoice.void |
| Change service price | no | catalog.manage |
| Create clinic / suspend | yes | platform.manage |

---

# Part F — Data Model

## F.1 Existing entities `[CODE]`

| Entity | Key fields | Notes |
|---|---|---|
| `Patient` : AuditableEntity | First/Last, `ContactInfo` (owned), DOB, Gender, `MedicalHistory` (owned) | soft delete; query filter |
| `Doctor` : AuditableEntity | First/Last, Specialization, `ContactInfo`, Gender, `IsActive` | soft delete; query filter |
| `DentalService` (table `Services`) | Name, Description, Price(18,2), IsActive | soft delete; query filter |
| `Appointment` : AuditableEntity | PatientId, DoctorId, ServiceId, ScheduledDateTime, DurationInMinutes, Status(string), Notes | soft delete; query filter; FKs Restrict |
| `TreatmentRecord` : AuditableEntity | PatientId, DoctorId, AppointmentId?, ToothNumber, ProcedureDetails, Cost(18,2) | FKs Patient/Doctor Cascade; **no** query filter |
| `Invoice` : AuditableEntity | PatientId, AppointmentId?, TotalAmount(18,2), PaidAmount(18,2), Status(PaymentStatus), Payments[] | soft delete; query filter; **no line items** |
| `Payment` : AuditableEntity | InvoiceId, Amount(18,2), PaidAtUtc, TransactionNotes | FK Invoice Cascade; **no** query filter |
| `RefreshToken` : AuditableEntity | Token(unique), UserId, ExpiresOnUtc | Identity-linked |
| `AppUser` : IdentityUser | ASP.NET Identity tables | no clinic link |

## F.2 Proposed changes (PROPOSED)

New / changed entities:

| Entity | Change | Purpose |
|---|---|---|
| `Clinic` (new) | Id, Name, Contact, Currency, TimeZone, IsActive, timestamps | tenant root & settings (TEN-001, SET-001) |
| `ClinicUser` (new) | UserId, ClinicId, Role | membership & scoping (TEN-004) |
| `Invoice` (change) | add `ClinicId`, `InvoiceNumber`, `IssuedAtUtc`, `IssuedBy`, `Status` (lifecycle enum), keep `PaymentStatus`, add `RowVersion` | lifecycle + concurrency + numbering |
| `InvoiceLine` (new) | Id, ClinicId, InvoiceId, Description, Quantity, UnitPrice, DiscountAmount, LineTotal, SourceTreatmentRecordId?, ServiceId? | real line items (INV-002) |
| `TreatmentRecord` (change) | add `ClinicId`, `ServiceId?`, `ClinicalNotes?`, `Status`, `PerformedAtUtc`; make `ToothNumber` **nullable**; `Fee` semantics; link to `InvoiceLine`? | clinical depth + billing source |
| `Appointment` (change) | add `ClinicId`; service optional (`ServiceId?`) per prompt | tenancy + optional expected service |
| `Patient`/`Doctor`/`DentalService`/`Payment` (change) | add `ClinicId` | isolation |
| `AuditLog` (new) | Id, ClinicId?, ActorUserId, Action, EntityName, EntityId, OccurredAtUtc, Summary | audit (TEN-006) |

## F.3 Keys, constraints & indexes (PROPOSED)

- `ClinicId` indexed on every tenant-owned table; composite indexes lead with `ClinicId` where it improves typical queries (e.g., `(ClinicId, ScheduledDateTime)` on appointments).
- Unique: `(ClinicId, InvoiceNumber)`.
- Unique (rule-based): `(ClinicId, PatientId, Phone)`-style duplicate guard is a *warning*, not a hard constraint.
- Concurrency: `RowVersion` (SQL `rowversion`) on `Invoice` (and optionally `Payment`).
- Duplicate-draft guard: unique index or idempotency-key table for generated invoices.
- Global query filters: extend to **all** tenant-scoped and soft-deletable entities (fixes D-08).
- FKs: keep `Restrict`/`NoAction` toward patients/clinics for financial tables; avoid accidental cascades deleting history.

## F.4 Audit requirements

- Row audit fields already exist `[CODE]`.
- Add append-only `AuditLog` for the sensitive actions listed in TEN-006.
- Never store full clinical payloads in audit summaries.

## F.5 Migration & backward compatibility

- Migrations exist but are **not applied** (D-05). Switch startup to `MigrateAsync()` and reconcile the model snapshot.
- Backfill plan: existing rows get the pilot `ClinicId` via a data migration; existing invoices become `Issued` (they represent already-charged amounts) or `Draft` depending on pilot policy (`OPEN-QUESTIONS Q4`).
- Existing invoices have no lines → backfill a single line "Legacy balance" per existing invoice to preserve totals.
- Because `EnsureCreatedAsync` may have created the schema, verify the `__EFMigrationsHistory` state before migrating (`OPEN-QUESTIONS Q4`).

## F.6 Do not

- Do not produce a whole new schema; extend the existing one.
- Do not introduce a database-per-tenant architecture at this stage.

---

# Part G — API & Integration Requirements

## G.1 Existing endpoints `[CODE]`

| Area | Endpoints |
|---|---|
| Identity | `POST api/v1/identity/token/generate`, `POST …/token/refresh-token`, `GET …/current-user/claims` |
| Patients | `GET/POST api/v1/patients`, `GET/PUT/DELETE api/v1/patients/{id}` (per `ApiClient`) |
| Doctors | `GET/POST api/v1/doctors`, `GET/PUT/DELETE api/v1/doctors/{id}` |
| Services | `GET/POST api/v1/dentalservice`, `GET/PUT/DELETE api/v1/dentalservice/{id}` |
| Appointments | `GET/POST api/v1/appointments`, `GET …/{id}`, `PUT …/{id}/reschedule`, `PATCH …/{id}/complete|cancel|no-show` |
| Treatment records | `GET/POST api/v1/treatment-records`, `GET/PUT …/{id}` |
| Invoices | `GET/POST api/v1/invoices`, `GET …/{id}`, `POST …/{invoiceId}/payments` |

Conventions: route versioning, ProblemDetails errors, `[Authorize]` at controller level `[CODE]`.

## G.2 Proposed endpoints (clearly PROPOSED — do not assume they exist)

| Requirement | Proposed endpoint |
|---|---|
| TEN-001 | `POST /api/v1/platform/clinics`, `POST …/{id}/activate`, `POST …/{id}/suspend`, `GET …/{id}` |
| AUTH-003 | `POST /api/v1/clinic/users`, `POST …/users/{id}/deactivate` |
| TEN-003 | (server derives clinic from token; no client field accepted as authority) |
| APT-002 | overlap check inside `POST /appointments` and `PUT /appointments/{id}/reschedule` |
| TRT-002 | `PATCH /treatment-records/{id}/perform`, `…/cancel` |
| WFL-002 | `POST /invoices/drafts/from-appointment/{appointmentId}` (idempotent) |
| INV-001/002 | `POST /invoices/drafts`, `POST /invoices/{id}/lines`, `PUT /invoices/{id}/lines/{lineId}`, `DELETE …` |
| INV-004 | `POST /invoices/{id}/issue` |
| INV-008 | `POST /invoices/{id}/void` |
| PAY-001 | `POST /invoices/{id}/payments` (add idempotency key header/body) |
| PAY-004 | `POST /invoices/{id}/payments/{paymentId}/reverse` |
| DASH-001 | `GET /api/v1/dashboard/summary` |
| SET-001 | `GET/PUT /api/v1/clinic/settings` |

## G.3 Cross-cutting API rules

- **Authorization:** every endpoint declares required permission; clinic scope enforced server-side.
- **Validation:** FluentValidation at the pipeline; structured field errors.
- **Idempotency:** draft generation and payment registration accept an idempotency key; duplicate keys return the original result.
- **Transaction boundaries:** draft generation, issuance, payment, clinic provisioning are transactional.
- **No client-supplied money authority:** `TotalAmount` is computed; reject mismatches.
- **Errors:** meaningful domain errors; never leak stack traces `[CODE]` global handler exists.

---

# Part H — UX & Screen Requirements

Preserve the current visual identity, layout shell, RTL, theming and shared components (`Modal`, `Drawer`, `Pager`, `EmptyState`, badges, toasts) `[UI]`.

| Screen | Required behavior |
|---|---|
| Login | Preserve; remove demo credentials from production builds (`D-07`); improve error messaging without user enumeration. |
| Dashboard | Server-driven metrics with clear definitions (DASH-001); distinguish draft vs issued; today's appointments. |
| Appointments | Create with **optional** expected service; overlap warnings; status actions; day/week calendar (P1); server-side filters. |
| Appointment detail / visit | **New**: open visit, see treatments recorded, "Create draft invoice" action. |
| Treatment entry | FDI chart, **optional tooth**, service picker, clinical notes, fee, status (Planned/Performed). |
| Treatment history | Existing list preserved; add status, link to invoice/line where applicable. |
| Service catalog | Existing; show that price is a default; warn price changes don't affect history. |
| Manual invoice | Line-item editor (description/qty/unit price/discount). |
| Draft invoice review | Editable lines, totals, "Issue" action with confirmation; show source visit/treatments. |
| Issued invoice | Read-only lines, number, issued date/by, payment history, remaining balance; void action per permission. |
| Payment registration | Amount/method/reference; overpayment blocked; double-submit safe. |
| Platform administration | **New**: clinic list, create clinic, suspend/activate. |
| Clinic administration | **New/extended**: clinic settings, staff management, currency/timezone. |
| Settings | Extend from profile/theme to clinic settings (SET-001). |
| Notifications | Either implement or remove the decorative bell (`D-16`). |

Usability requirements: loading/empty/error states everywhere `[UI exists in several screens]`; keyboard-accessible modals; Arabic strings for every new control; readable money/date formatting per clinic locale.

---

# Part I — Security, Privacy & Reliability

## I.1 Threat-oriented review

| Threat | Current exposure | Required mitigation |
|---|---|---|
| Privilege escalation (any user does anything) | High — D-01 | Server-side permission checks (`SEC-002`) |
| Cross-tenant data access | Critical if >1 clinic — D-06 | Tenant query filters + `ICurrentTenant` + tests (`TEN-003`, `TEST-005`) |
| Tampered financial totals | High — D-02 | Server-computed totals from lines (`INV-002`) |
| Lost-update on payments | High — D-04 | Rowversion + transaction (`PAY-002`) |
| Secret leakage | High — D-07 | Secrets out of repo (`SEC-001`) |
| User enumeration | Medium | Generic auth errors (`AUTH-001`) |
| Brute force | Medium | Lockout + rate limiting (exists at edge `[CODE]`) (`AUTH-005`) |
| Unauthorized invoice edits | High | Immutability + void/correction rules (`INV-005`, `INV-008`) |
| Clinical data exposure to non-clinical staff | Medium | Permissioned clinical fields (`PAT-005`) |
| Audit gaps | Medium | `AuditLog` (`TEN-006`) |
| Weak deployed config | Medium — D-12 | Fix config, remove leftovers |

## I.2 Access control requirements

- Authenticate (JWT) **and** authorize (permission) on every request.
- Enforce tenant scope on read **and** write.
- Deny by default; least privilege.
- Never trust client-provided tenant id, totals, or role.

## I.3 Financial invariants

`TotalAmount = Σ LineTotal ≥ 0`; `0 ≤ PaidAmount ≤ TotalAmount`; `RemainingAmount = Total − Paid`;
counter monotonic; issued invoices immutable; every money mutation audited.

## I.4 Reliability

- Transactions for draft/issue/payment/provision.
- Optimistic concurrency on `Invoice`.
- DB constraints for invariants where possible.
- **Backup/restore:** introduce automated SQL Server backups + a tested restore procedure (`REL-001`, `TEST-010`). Not present today `[CODE]`.

## I.5 Performance targets (provisional — HYPOTHESES to validate)

- p95 read endpoints < 300 ms; p95 write < 500 ms at pilot scale (≤ 20 concurrent users, ≤ 50k patients).
- Avoid premature distributed infrastructure (master-prompt rule 13).

## I.6 Observability & monitoring

- Keep OpenTelemetry/Prometheus/Grafana `[CODE]`. Fix the Seq endpoint (`seq` not `ops.seq`) D-12.
- Add uptime/health checks (`/health`), DB connectivity probe, error-rate alerting.

## I.7 Privacy

- Collect the minimum patient data needed (already modest `[CODE]`).
- Restrict clinical fields by role.
- Define retention/deletion policy with the clinic (`OPEN-QUESTIONS Q9`).
- Do **not** claim GDPR/HIPAA/local compliance until legally reviewed (`OPEN-QUESTIONS Q8`).

---

# Part J — Test Plan

> No results are asserted here. The repo has 273 `[Fact]` + 46 `[Theory]` `[CODE]`; the suite was not run during discovery.

## J.1 Test layers

- **Unit (Domain):** entity invariants (already broad: `AppointmentTest`, `InvoiceTest`, `DentalServices`, `Patients`, `Doctors`, `RefreshToken`, `ContactInfo`) `[TEST]`.
- **Unit (Application):** behaviours + mappers `[TEST]`.
- **Subcutaneous:** handlers/queries against a real DB `[TEST]`.
- **API integration:** controllers over the HTTP pipeline `[TEST]`.
- **New:** authorization, tenant isolation, concurrency, idempotency, financial correctness, e2e.

## J.2 Scenario coverage

| ID | Scenario | Expected |
|---|---|---|
| TEST-001 | Patient → appointment → treatment → draft → issue → partial payment | completes; balances correct |
| TEST-002 | Manual invoice, full payment | completes; Paid |
| TEST-003 | Complete a visit, **don't** issue | no invoice/payment created |
| TEST-004 | Retry draft creation | no duplicates |
| TEST-005 | Cross-tenant access to patient/treatment/invoice/payment | 404/403 |
| TEST-006 | Conflicting appointment booking | 409; concurrency-safe |
| TEST-007 | Payment exceeding balance | rejected; balance unchanged |
| TEST-008 | Change service price | historical docs unchanged |
| TEST-009 | Bill a `Planned` treatment | blocked without explicit perform |
| TEST-010 | Restore backup | records/relationships intact |
| TEST-011 | Silent edit of issued invoice/line | blocked |
| TEST-012 | Double-submit payment | single payment (idempotent) |
| TEST-013 | Concurrent payments | no overpay, no lost update |
| TEST-014 | Receptionist attempts invoice issue / doctor edit | 403 |
| TEST-015 | Dashboard totals definitions | matches server aggregation |

## J.3 Acceptance gates for release

1. All P0 requirements have at least one passing test (see Part O).
2. Tenant-isolation and authorization tests green.
3. Financial-correctness + concurrency tests green.
4. Backup/restore rehearsal completed.
5. No secrets in source; config reviewed.
6. Arabic RTL smoke test on every P0 screen.
7. Migrations apply cleanly to a fresh DB **and** to a copy of existing data.

---

# Part K — MVP Scope

| Feature | Priority | Business value | Risk if omitted | Effort (person-days, rough) | Depends on | Decision |
|---|---|---|---|---|---|---|
| Tenant foundation (`Clinic`, `ClinicId`, filters, `ICurrentTenant`) | P0 | Isolation/trust | Data leak across clinics | 6–10 | — | **MVP** |
| Role & permission enforcement | P0 | Safety | Any user does anything | 4–7 | Tenant | **MVP** |
| Clinic provisioning (platform admin) | P0 | Onboard pilots | Can't onboard | 3–5 | Tenant | **MVP** |
| Secrets/password/seed hardening | P0 | Security | Secret leak, weak accounts | 2–3 | — | **MVP** |
| Migrations applied (`MigrateAsync`) | P0 | Safe schema evolution | Prod breakage | 1–2 | — | **MVP** |
| Appointment overlap prevention | P0 | Correct schedule | Double bookings | 2–4 | — | **MVP** |
| Treatment status + clinical fields + optional tooth | P0 | Accurate clinic record | Bad data | 3–5 | — | **MVP** |
| Invoice line items + server totals | P0 | Financial correctness | Wrong money | 6–9 | — | **MVP** |
| Draft/issue lifecycle + numbering | P0 | Core workflow | No controlled billing | 5–8 | Line items | **MVP** |
| Draft from visit + sync/idempotency | P0 | Core workflow | Duplicate/wrong invoices | 4–7 | Above | **MVP** |
| Payment concurrency/idempotency | P0 | Money safety | Lost/duplicate payments | 3–5 | Line items | **MVP** |
| Void/correct issued invoice (basic) | P0/P1 | Trust | Silent edits | 3–5 | Draft/issue | **MVP (void only)** |
| Audit log | P0 | Accountability | No accountability | 3–5 | Tenant | **MVP** |
| Dashboard server metrics | P0 | Decisions | Misleading numbers | 3–5 | — | **MVP** |
| Clinic settings (currency/timezone/name) | P0 | Usability | Wrong locale/price display | 3–4 | Tenant | **MVP** |
| Backup/restore + monitoring | P0 | Reliability | Data loss | 3–5 | Infra | **MVP** |
| Soft-delete consistency fix | P0 | Integrity | Ghost rows | 1–2 | Tenant | **MVP** |
| Web container + session persistence | P0/P1 | Deploy/UX | Can't deploy cleanly | 2–4 | — | **MVP** |
| Operating hours/capacity | P1 | Scheduling quality | Manual policing | 2–3 | — | Later |
| Calendar view, server filters | P1 | Usability | Slower front desk | 3–5 | — | Later |
| Reminders (SMS/WhatsApp/email) | P1/P2 | Retention | — | 5–10 | Channel | Later |
| Reports/export | P2 | Insight | — | 5–10 | — | Later |
| Subscription/billing/self-onboarding | P2 | Monetization | — | 10–20 | Validation | Reject for MVP |
| Refunds/credit notes/e-invoicing | P1/P2 | Compliance | Legal risk in some markets | ? | Legal input | Deferred pending Q7 |
| Inventory/labs/imaging/ortho | P2 | — | — | large | — | Reject for MVP |

Effort estimates are **ranges with stated assumptions** (small senior team familiar with the stack); they are not commitments.

---

# Part L — Implementation Roadmap

Detailed in [`MVP-ROADMAP.md`](./MVP-ROADMAP.md). Summary:

| Phase | Goal | Key work | Exit criteria |
|---|---|---|---|
| 0 | Stabilise & secure baseline | secrets, password policy, migrations, soft-delete filters, config leftovers, run tests | clean build; migrations apply; no secrets |
| 1 | Tenant foundation | `Clinic`, `ClinicId` on entities, `ICurrentTenant`, global filters, backfill | isolation tests green |
| 2 | Authorization | permission model, handler/endpoint enforcement, current-user permissions | authz tests green; AUTH-001/004/006 |
| 3 | Clinical core | treatment status/notes/optional tooth/service link | TRT tests green |
| 4 | Scheduling integrity | overlap prevention + concurrency | APT-002 tests green |
| 5 | Billing core | invoice lines, server totals, currency, numbering | INV-002/009/010 tests green |
| 6 | Draft→issue workflow | generate from visit, idempotency, sync, issue/lock, void | WFL/INV/TEST-003/004/011 green |
| 7 | Payments hardening | concurrency, idempotency, reversal | PAY tests green |
| 8 | Dashboard, settings, audit UI | server metrics, clinic settings, audit views | DASH/SET/TEN-006 green |
| 9 | Operations | backups/restore, health, monitoring, web container, session persistence | runbook + rehearsal green |
| 10 | Pilot onboarding | provision first clinic, train, gather feedback | go/no-go checklist |

Each phase's rollback, DB changes, code areas, tests, effort and definition-of-done are in `MVP-ROADMAP.md`.

---

# Part M — Risks & Open Questions

| ID | Type | Risk | Mitigation |
|---|---|---|---|
| R-01 | Technical | Applying tenant `ClinicId` + migrating unused migrations may conflict with existing DBs | Rehearse on a data copy; verify `__EFMigrationsHistory`; phased backfill |
| R-02 | Technical | Adding authorization may break existing UI flows | Permission-aware UI + staged rollout |
| R-03 | Security | Secrets already committed → rotate | Rotate JWT secret, SA password; purge from history |
| R-04 | Product | Scope creep into accounting | Enforce MVP scope; Q7 decision |
| R-05 | Financial | Client-trusted totals until Phase 5 | Treat as release blocker, not optional |
| R-06 | Data migration | Existing invoices have no lines/number/draft state | Backfill policy (Q4) |
| R-07 | Legal/Privacy | Regulatory/localization unknown | Legal review (Q8); no compliance claims |
| R-08 | Operational | Single-instance background job + no backups | Health checks; backups; accept single instance for pilot |
| R-09 | Testing | Integration tests need Docker/SQL; CI must provide it | Confirm CI service strategy (Q10) |
| R-10 | UX | Session loss on refresh (D-14) | Persist session (Phase 9) |

Full decision list is in [`OPEN-QUESTIONS.md`](./OPEN-QUESTIONS.md).

---

# Part N — MVP Launch Checklist

**Security**
- [ ] Secrets removed from repo and rotated (JWT, SA password).
- [ ] Password policy + lockout enforced.
- [ ] Permission checks on every endpoint; verified by tests.
- [ ] Tenant isolation verified by tests.
- [ ] HTTPS enforced in the deployment; CORS origins corrected.

**Data**
- [ ] Migrations apply to a fresh DB and to a copy of pilot data.
- [ ] Automated backups scheduled; restore rehearsed (TEST-010).
- [ ] Soft-delete filters consistent across tenant entities.

**Financial**
- [ ] Server-computed totals; no client-trusted amounts.
- [ ] Concurrency + idempotency for payments.
- [ ] Issued invoices immutable; void audited.

**Operations**
- [ ] Health checks + alerting; Seq endpoint fixed.
- [ ] Web app deployable (container/host) and reachable.
- [ ] Support & issue-reporting channel defined.

**Pilot**
- [ ] First clinic provisioned; admin account created.
- [ ] Staff trained on the visit → draft → issue → payment flow.
- [ ] Feedback collection cadence agreed.
- [ ] Success metrics defined as hypotheses (see below).
- [ ] Go/no-go decision recorded.

**Provisional pilot success metrics (HYPOTHESES, no baseline invented):**
≥90% of completed visits get a treatment record; ≥85% of eligible visits produce a correct draft;
<10% invoice corrections; <2% payment errors; weekly active clinic users ≥ 3; median visit-to-invoice time ≤ 3 min;
pilot retention ≥ 1 quarter; willingness-to-pay signals captured.

---

# Part O — Traceability Matrix

| Requirement | User story | Module | Phase | Test scenario | Acceptance |
|---|---|---|---|---|---|
| TEN-001 | US-10 | Multi-tenancy | 1 | TEST-005 | clinic + admin isolated |
| TEN-002/003 | US-11 | Multi-tenancy | 1 | TEST-005 | no cross-tenant access |
| TEN-004 | US-11 | Multi-tenancy | 1/2 | TEST-014 | claims scope queries |
| TEN-006 | US-09 | Audit | 8 | — | sensitive actions logged |
| AUTH-001/005 | US-01 | Identity | 0/2 | — | generic errors; lockout |
| AUTH-004 | US-10 | Identity | 2 | — | deactivated user blocked |
| AUTH-006 | US-07 | Identity | 2 | TEST-014 | permissions returned/enforced |
| PAT-003 | US-01 | Patients | 3 | — | duplicate warning |
| PAT-004 | US-11 | Patients | 0/3 | — | archive keeps history |
| PAT-005 | US-11 | Patients | 2 | TEST-014 | clinical hidden from reception |
| APT-002 | US-01 | Appointments | 4 | TEST-006 | overlap 409 |
| APT-003 | US-02 | Appointments | 4 | TEST-003 | complete ≠ invoice |
| TRT-001/002 | US-03/04 | Treatments | 3 | TEST-009 | planned not billed |
| TRT-004 | US-03 | Treatments | 3/6 | TEST-011 | corrections controlled |
| WFL-002/005 | US-05 | Workflow | 6 | TEST-004 | idempotent draft |
| INV-002 | US-05/06 | Invoicing | 5 | TEST-001 | server totals |
| INV-003/004 | US-06 | Invoicing | 6 | TEST-003 | draft ≠ issued |
| INV-005/008 | US-08 | Invoicing | 6 | TEST-011 | immutable; void audited |
| INV-006/007 | US-05 | Invoicing | 6 | TEST-004 | no dup; sync rule |
| INV-009/010 | US-06 | Invoicing | 5/8 | TEST-001 | currency + number |
| SVC-003 | US-06 | Catalog | 5 | TEST-008 | history unchanged |
| PAY-001/002/005 | US-07 | Payments | 7 | TEST-007/012/013 | correct balance; safe |
| PAY-004 | US-08 | Payments | 7 | — | reversal audited |
| DASH-001 | US-06 | Dashboard | 8 | TEST-015 | definitions correct |
| SET-001/003 | US-12 | Settings | 8 | — | clinic locale/currency |
| SEC-001/002 | — | Security | 0/2 | TEST-014 | secrets out; authz enforced |
| REL-001 | — | Reliability | 9 | TEST-010 | restore verified |

---

*End of PRD. Implementation must not begin until this document, the MVP scope, and the release
acceptance criteria are reviewed and approved. See `OPEN-QUESTIONS.md` for decisions required first.*
