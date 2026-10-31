# DentalClinic — Next Phase Execution Plan

| Field | Value |
|---|---|
| Date | 2026-10-28 |
| Baseline | `feature/odontogram` @ `33ce043` — odontogram phase S0–S8 complete |
| Scope | Close S8, then treatment plans → patient balance → WhatsApp → docs |
| Status | Planning only. This file is intentionally **gitignored**; no code changes accompany it |
| Companion docs | `AUDIT.md` (§6 gap-closure plan), `ODONTOGRAM_PLAN.md` (completed phase), `plan.md` (business review, tracked) |

**How to use this document.** Work one slice per turn, stop after each slice with a review summary, and never commit on the user's behalf. The agent proposes; the owner reviews and commits. Everything below is ordered so each slice leaves the build and all six test suites green.

---

## 1. Where the product stands

### 1.1 Shipped and verified

| Area | What exists in code today |
|---|---|
| Identity | JWT access tokens, SHA-256-hashed rotating refresh tokens, lockout, uniform login errors, roles Admin / Doctor / Receptionist. No user-administration UI. |
| Patients | CRUD, soft delete, search, medical history flags (diabetes, hypertension, pregnancy, allergies, etc.). |
| Doctors | CRUD, activation, specialization, contact info with WhatsApp flag. No doctor schedule/working-hours model. |
| Services | CRUD with price and active flag. |
| Appointments | Book, reschedule, cancel, complete; serializable double-booking guard; background no-show cleanup. Table UI, no calendar. |
| Treatment records | CRUD, FDI tooth validation from a single source, optional tooth (S8), list filter by tooth (S7). |
| Odontogram | `PatientTooth` conditions, per-tooth history, interactive chart with legend and drawer, demo seed. |
| Invoices | Generate from a completed appointment, draft lifecycle, line items, payments with `RowVersion` + serializable isolation, overpayment guard. No invoice number (D-11), no print. |
| Reporting | None server-side. Dashboard sums are computed client-side over the first 100 rows (D-09). |
| Web | Blazor Server, 12 pages, Arabic/English, light/dark theme, in-memory session (D-14). |
| Ops | Docker (API + SQL + Seq + Prometheus + Grafana), health endpoints, GitHub Actions CI. |

Test baseline: **780 green** — Domain 184, Application 63, Web 42, Subcutaneous 303, Integration 188 (including the migration replay).

### 1.2 What a paying clinic still cannot do

| Business gap | Consequence for the clinic | Audit | Effort |
|---|---|---|---|
| No treatment plan with sessions and a printable sheet | The dentist plans in a notebook; the patient leaves without an itemised, signed estimate. This is the core clinical sales artefact. | §6.2 | M–L |
| No server-side patient balance / debt list | Reception cannot answer "how much does this patient owe?" reliably; totals are wrong beyond one page (D-09). | §6.3 | S–M |
| No reminder link | Every confirmation is typed manually in WhatsApp; no-shows stay high. | §6.4 | S |
| No invoice number or invoice print | Invoices are not legally presentable; UI shows the first 8 hex chars of a GUID (D-11). | D-11 | S |
| No financial dashboard | The owner cannot see revenue, outstanding debt, or doctor productivity on true data. | §6.5 | L |
| No clinic settings / branding | "Al Noor Dental" is hardcoded in layout, login and page titles (D-15). | §6.6 | M–L |
| No images, lab tracking, alerts completion | Clinical record is text-only; lab work is tracked outside the system. | §6.7, §6.10, §6.8 | M each |
| No user administration or audit log | The owner cannot onboard a receptionist or investigate a change. | — | M |

**Read:** the engineering base is sound; the missing value is mostly *clinical workflow and money visibility*, not infrastructure. Do not rewrite the foundation.

---

## 2. Architectural guardrails (do not break)

These are enforced by the existing code and tests. Every slice below must respect them.

1. **Result flow** — handlers return `Result<T>`; controllers unwrap with `result.Match(Ok, Problem)`. No exceptions for expected failures.
2. **Two-layer authorization** — controller policy plus an ownership check in the handler via `ClinicAuthorization` (`PatientScope()`, `DoctorScope()`, `CanAccessAppointment`, `CanWriteClinicalRecords`). Client-supplied doctor/patient ids are never trusted.
3. **Cache correctness** — every cached query bakes `Scope.IsRestricted` / `Scope.DoctorId` into `CacheKey`; every mutating handler ends with `RemoveByTagAsync("<entity>")` using the singular entity name.
4. **Money concurrency** — invoice payment stays serializable with `RowVersion`; appointment creation stays serializable. Do not weaken either to fix a test.
5. **Schema changes** — configuration in `Infrastructure/Data/Configurations/*.cs` **and** a migration + snapshot, replayed by `MigrationTests`. Never edit a migration after it has been reviewed.
6. **DI hygiene** — never read `IConfiguration` during service registration; read it inside lazily-invoked callbacks or at use time.
7. **Contracts** — packages come from `Directory.Packages.props` with no `Version=` on `PackageReference`.
8. **Files** — CRLF, 4-space indent, no final newline; file-scoped namespaces; private fields `_camelCase`.

---

## 3. Execution order at a glance

| # | Slice | Type | Effort | Depends on |
|---|---|---|---|---|
| 0 | Close S8: commit hygiene + manual Web walkthrough | Housekeeping | 0.5 day | — |
| 1 | Treatment plans + printable sheet (`AUDIT` §6.2) | Feature, 4 sub-slices | M–L | S8 nullable tooth |
| 2 | Patient balance & debt (`AUDIT` §6.3) | Feature, 3 sub-slices | S–M | — |
| 3 | WhatsApp reminder link (`AUDIT` §6.4) | Feature, 2 sub-slices | S | — |
| 4 | Documentation truth-up | Docs | 0.5 day | Slices 0–3 |
| 5 | Backlog (ordered, not scheduled) | — | — | — |

Slices 2 and 3 are independent of slice 1. If the clinic needs money visibility sooner than clinical plans, swap 1 and 2 after the decision register is answered.

---

## 4. Step 0 — Close the odontogram phase

### 0.1 Commit hygiene (owner action)

`33ce043` has the subject `\` — a malformed commit message. Before pushing or merging:

```powershell
git commit --amend -m "feat(treatment): make TreatmentRecord.ToothNumber nullable"
```

Then confirm the branch contains S0–S8 and that `ODONTOGRAM_PLAN.md` says "Done (committed)" for S8.

### 0.2 Manual Web walkthrough

Start the API (`dotnet run --project src/DentalClinic.Api`, port 5004) and the Web app (`dotnet run --project src/DentalClinic.Web`, port 7204).

- [ ] Log in as `admin@dentalclinic.com` / `Admin!Pass123`.
- [ ] **Treatments** page: create a treatment with "Not linked to a specific tooth" checked; the row shows the em dash, not a blank or `0`.
- [ ] Edit that treatment, select tooth 26, save; then edit again, re-check the box and save. Value clears back to null.
- [ ] **Patient details → Odontogram**: a tooth-less record does not appear under any tooth; a tooth-specific record still appears in the correct drawer.
- [ ] **Appointments**: complete an appointment through the modal with one tooth-less and one tooth-specific treatment; verify the generated invoice lines (no `(Tooth )` fragment, correct subtotal).
- [ ] Arabic RTL and dark theme on the two forms above.
- [ ] Legacy `ToothChart` behaviour unchanged where it is still used.

### 0.3 Environment note (not code)

Host ports **1431–1530** are currently in a Hyper-V excluded range, so `docker compose up -d sqlserver` cannot bind `1434` (socket error 10013). Use an elevated `net stop winnat` → `docker compose up -d sqlserver` → `net start winnat`, or remap the port in `docker-compose.yml` to a range outside the exclusions. Verify with `netsh interface ipv4 show excludedportrange protocol=tcp`.

**Exit criteria:** S8 committed with a readable message, walkthrough signed off, no open defect from the slice.

---

## 5. Step 1 — Treatment plans + printable sheet (`AUDIT` §6.2)

### 5.1 Business outcome

A dentist records a multi-session plan (for example: root canal on 26, crown on 26, filling on 36), prints or shares an itemised estimate with the patient, and marks items as performed session by session. Only performed items are billable, so the plan and the invoice cannot drift apart.

### 5.2 Design decision (must be answered before P1.1)

| Option | Shape | Verdict |
|---|---|---|
| A — lightweight (recommended) | `TreatmentPlan` header + `TreatmentRecord.PlanId?`, `Status`, `EstimatedCost?`, `SessionNumber`; plan items **are** treatment records | Reuses the existing history, odontogram and invoice pipeline; one new table |
| B — full | Separate `TreatmentPlanItem` entity linked to a treatment record or free text | Duplicates the record model and forces a materialisation step; defer until a plan item can exist without ever becoming treatment |

Adopt **A**. It keeps one clinical fact table, which is what makes the odontogram and invoices trustworthy.

### 5.3 Target model

`TreatmentRecord` additions:

| Field | Type | Rule |
|---|---|---|
| `PlanId` | `Guid?` | Null for ad-hoc treatments; FK to `TreatmentPlans`, `OnDelete(Restrict)` — use soft delete on the plan |
| `Status` | enum `Planned / Performed / Cancelled` | Default `Performed` for direct create; `Planned` only when created through a plan |
| `EstimatedCost` | `decimal?` | Precision (18,2); plan-time estimate, never billed |
| `SessionNumber` | `int` | Default 1; groups items for per-session totals and printing |

`TreatmentPlan` header: `PatientId`, `DoctorId`, `Title`, `Notes`, `Status` (`Draft / Active / Completed / Cancelled`), `CreatedAtUtc`, audit fields, soft delete.

Invariants:
- Only `Status == Performed && Cost > 0` is billable.
- `Cost` remains the actual amount; `EstimatedCost` never enters an invoice.
- A cancelled item is never billable and never disappears from history.
- A plan with performed items cannot be hard-deleted or cancelled without an explicit warning; soft delete is the only delete.

### 5.4 Sub-slices (one per turn)

**P1.1 — Domain + persistence**
- New `Domain/TreatmentPlans/TreatmentPlan.cs`, `TreatmentPlanErrors.cs`, `TreatmentPlanStatus.cs`; new `Domain/TreatmentRecords/TreatmentRecordStatus.cs`.
- Extend `TreatmentRecord`: new fields, factory/update overloads, status transitions (`MarkPerformed(decimal actualCost)`, `Cancel()`), keep FDI and null-tooth rules.
- `Infrastructure/Data/Configurations/TreatmentPlanConfiguration.cs` and update `TreatmentRecordConfiguration.cs` (FK, precision, default, index on `(PatientId, Status)` and `PlanId`).
- `IAppDbContext` + `AppDbContext` `DbSet<TreatmentPlan>` (expression-bodied).
- Migration `AddTreatmentPlans`: create table, add columns, **backfill all existing records to `Status = Performed`, `SessionNumber = 1`, `EstimatedCost = Cost`**.
- Domain tests: status transitions, cancelled items not billable, estimate vs cost separation, invalid transition rejected via `Result`.
- **Exit:** build green, Domain suite green, `MigrationTests` green.

**P1.2 — Application layer**
- `Features/TreatmentPlans/Commands`: `CreateTreatmentPlan`, `UpdateTreatmentPlan`, `AddTreatmentPlanItem`, `UpdateTreatmentPlanItem`, `RemoveTreatmentPlanItem` (soft), `PerformTreatmentPlanItem` (sets actual cost + optional `AppointmentId`), `CancelTreatmentPlan`.
- `Features/TreatmentPlans/Queries`: `GetTreatmentPlansQuery` (paged, filters patient/doctor/status), `GetTreatmentPlanByIdQuery` (items + totals), `GetTreatmentPlanSessionsQuery` (cost grouped by `SessionNumber`).
- Validators alongside each request; FluentValidation registered by convention.
- Billing rule: `GenerateAppointmentInvoiceCommandHandler.BuildLines` filters `Status == Performed`. `CompleteAppointmentCommand` and `CompleteAppointmentWithDraftCommand` create `Performed` records. `CreateTreatmentRecordCommand` defaults to `Performed`; status changes go through the plan commands only.
- Cache: plan queries keyed by scope + filters, tags `treatment-plan` and `treatment-record`; every mutating handler invalidates both (items are records).
- Tests: handler tests for create/add/perform/cancel, invoice generation excludes planned and cancelled items, session totals with mixed statuses, cache-key scope.
- **Exit:** build green, Application suite green.

**P1.3 — REST surface**
- `src/DentalClinic.Contracts/Requests/TreatmentPlans/*`.
- `TreatmentPlanController` under `api/v{version}/treatment-plans`:
  - `GET /` (paged list), `POST /`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}` (soft)
  - `POST /{id}/items`, `PUT /{id}/items/{itemId}`, `DELETE /{id}/items/{itemId}`
  - `POST /{id}/items/{itemId}/perform`
  - `GET /{id}/sessions`
- Policies: read for all authenticated roles; write through `CanWriteClinicalRecords()`; doctor-scoped handlers reuse the appointment ownership pattern.
- Integration tests: one happy path per endpoint, a doctor-cannot-touch-another-doctor's-plan test, a performed-item-appears-in-invoice test.
- **Exit:** build green, Integration suite green, Scalar shows the endpoints.

**P1.4 — Web UI + print sheet**
- `Pages/TreatmentPlans.razor`: list, create/edit modal, item editor, status badges, session totals, perform/cancel actions.
- `PatientDetails.razor`: new "Treatment plans" tab beside the odontogram.
- `Components/Shared/PlanPrintSheet.razor`: clinic header, patient block, date, items grouped by session with estimate/actual/total columns, signature line.
- `wwwroot/app.css`: `@media print` block (hide nav/buttons, page-break rules, RTL-safe table direction).
- Branding: static `wwwroot/img/logo.svg` plus `Clinic:Name` / `Clinic:LogoPath` read lazily from configuration for now; replace with `ClinicSetting` in §6.6.
- `ApiClient` + `ApiModels` mirrors; bilingual labels and `ClinicFormatting` money formatting.
- Web tests: total-per-session formatting, print-sheet view model, RTL label presence.
- **Exit:** manual walkthrough: create a 3-item plan over 2 sessions, perform one item, print preview in both languages, verify the invoice only contains performed work.

### 5.5 Billing impact analysis (do before P1.2)

- `BuildLines` currently bills every record with `Cost > 0`; after the change, planned items are silent until performed.
- Existing invoice-generation tests seed treatment records directly; they must set `Status = Performed` explicitly or rely on the entity default. Update them deliberately, not by loosening the filter.
- `CompleteAppointment*` must never emit `Planned` records — work completed in an appointment is performed by definition.
- Draft invoices generated before an item is performed remain editable; no retroactive rewrite of issued invoices.
- `Cost`, `EstimatedCost` and `RemainingAmount` all stay `decimal(18,2)`.

### 5.6 Risks

| Risk | Mitigation |
|---|---|
| Billing semantics change breaks existing tests | Land the filter in P1.2 with the test updates in the same commit-sized slice; the migration backfills existing data |
| Two money fields confuse the UI | Label them "Estimated" and "Charged"; only "Charged" appears on invoices |
| Arabic print layout breaks | Print CSS test in both directions; page-break before patient block |
| Plan and history diverge | Items are treatment records by construction (Option A) |

---

## 6. Step 2 — Patient balance & debt (`AUDIT` §6.3)

### 6.1 Sub-slices

**P2.1 — Query + aggregation**
- `Features/Billing/Queries/GetPatientBalanceQuery`: `TotalBilled`, `TotalPaid`, `Balance`, `OpenInvoiceCount`, `OldestUnpaidDate` — real `SumAsync` over all non-deleted invoices, not a page.
- `Features/Billing/Queries/GetOutstandingDebtsQuery`: paginated, optional `minAmount` / `asOf`, patient name resolved in the same round trip.
- Cache tag `invoice`, scope baked into the key.
- Tests: patient with more than one page of invoices (D-09 regression), fully paid vs partial vs unpaid, soft-deleted invoices excluded, doctor scoping.
- **Decision required:** do `Draft` invoices count toward debt? Recommended: **no** — balance is over issued invoices only, and the UI labels drafts separately.

**P2.2 — REST surface**
- `BillingController`: `GET /api/v1/billing/patients/{id}/balance`, `GET /api/v1/billing/outstanding`.
- Contracts + validators; integration tests for scoping and paging.

**P2.3 — Web**
- `Components/Shared/BalanceBadge.razor` fed by the API.
- Replace the client-side sums in `Invoices.razor` (around lines 32 and 37) and `PatientDetails.razor` (around lines 98 and 103).
- Leave `Dashboard.razor` alone in this slice — its full rework is §6.5; adding a badge there now would duplicate the fix.
- **Exit:** a patient with 25 invoices shows the correct balance in both pages; a doctor cannot read another doctor's patient balance.

---

## 7. Step 3 — WhatsApp reminder link (`AUDIT` §6.4)

**P3.1 — Pure builder + tests**
- `src/DentalClinic.Web/Components/WhatsAppMessageBuilder.cs`: normalize to digits (strip `+`, spaces, dashes, leading `00`), prefix the clinic country code when the number is local, URL-encode the body, return `https://wa.me/<digits>?text=<encoded>`.
- Template placeholders: clinic name, patient name, date/time, doctor, service. Arabic and English variants.
- Tests: `+963 999-123-456` → `963999123456`; `0999...` → country code applied; empty/invalid number → no link; body encoding of Arabic and `&`/`?`.

**P3.2 — UI**
- `Components/Shared/WhatsAppButton.razor`, disabled with a tooltip when `HasWhatsAppOnPrimary` is false.
- Wire into the `Appointments.razor` row actions next to complete/reschedule/cancel.
- **Exit:** clicking the button opens a pre-filled chat in a new tab; disabled state is correct; no server round trip.

**Reality check:** `wa.me` links are a convenience, not a delivery guarantee. Do not label this "reminders" in the UI; label it "Send WhatsApp message".

---

## 8. Step 4 — Documentation truth-up

- `AUDIT.md` §6.1: mark the odontogram complete, record S7/S8.
- `AGENTS.md`: remove the stale claim that soft-delete filters are missing on `TreatmentRecord`/`Payment` (they exist; the real gap is `InvoiceLine`); confirm D-14 still applies.
- `README.md`: correct the test counts and the stale port `7198` (actual: API 5004, Web 7204).
- `docs/product/PRD.md`: mark the stale defect list as superseded by `AUDIT.md`, or fix the individual entries — do not leave two contradictory sources.
- `ODONTOGRAM_PLAN.md`: progress table to "Done (committed)" after the owner commits.

---

## 9. Later backlog (ordered, not scheduled)

| Order | Item | Why now | Effort |
|---|---|---|---|
| 5.1 | Invoice numbering + invoice print (D-11) | Small, legally visible, pairs with the plan print sheet | S |
| 5.2 | Financial dashboard, server-side (`AUDIT` §6.5) | Depends on §6.3; replaces D-09 client sums | L |
| 5.3 | Clinic settings: name, logo, currency, timezone (D-15) | Unblocks branding in print, currency display, timezone correctness | M |
| 5.4 | Dual currency with editable exchange rate (`AUDIT` §6.6) | Needs clinic settings first | M–L |
| 5.5 | Medical alerts completion (`AUDIT` §6.8) | Clinical safety; partially present | S–M |
| 5.6 | Role-based permissions (`AUDIT` §6.9) | Partially present; needs user administration and audit log | M |
| 5.7 | X-ray / before-after images (`AUDIT` §6.7) | Storage design decision required | M |
| 5.8 | Dental lab tracking (`AUDIT` §6.10) | Independent domain slice | M |
| 5.9 | Backups + Excel import (`AUDIT` §6.11) | Operational maturity; import is a sales enabler | S + M |

**Explicitly out of scope for this phase:** multi-tenant `ClinicId`, permissions rewrite, and any large refactor of the CQRS/Result foundation. The current base is the asset; protect it.

---

## 10. Decision register

| ID | Decision | Recommended | Needed by |
|---|---|---|---|
| D-P1 | Plan model: Option A (records are items) vs Option B (separate item entity) | A | P1.1 |
| D-P2 | Estimate and actual money kept in separate fields | Yes — `EstimatedCost` vs `Cost` | P1.1 |
| D-P3 | Default status of a directly created treatment record | `Performed` | P1.2 |
| D-P4 | Can a performed item be reopened? | Yes — back to `Planned`, blocked if already on an issued invoice | P1.2 |
| D-P5 | Plan delete semantics | Soft delete; block if performed items exist unless confirmed | P1.1 |
| D-P6 | Do draft invoices count as debt? | No — issued only | P2.1 |
| D-P7 | Who can read a patient's balance? | Admin + the doctor who owns the patient's care, via `PatientScope()` | P2.1 |
| D-P8 | WhatsApp country-code fallback | `Clinic:DefaultCountryCode` in configuration until clinic settings exist | P3.1 |
| D-P9 | Print branding source | Static logo + configuration now, `ClinicSetting` in §6.6 | P1.4 |

Answer these in the review conversation before the corresponding sub-slice starts. If an answer contradicts this plan, update this file first.

---

## 11. Definition of Done (every slice)

- [ ] `dotnet build DentalClinic.slnx` — 0 errors, no new warnings beyond the ~28 baseline.
- [ ] New tests at the right layer: domain rules in Domain, handlers/validators in Application, endpoints in Integration, aggregate maths in Subcutaneous or Application.
- [ ] All six suites green: Domain, Application, Web, Subcutaneous, Integration (with `MigrationTests`).
- [ ] Schema change shipped as a migration + snapshot update; backfill explicit and reversible in intent.
- [ ] Every new cached query includes the scope in its key and has a tag; every mutating handler invalidates by tag.
- [ ] Authorization checked in the handler, not only on the controller.
- [ ] Failures returned as `Result`, never thrown for business rules.
- [ ] New files CRLF, no final newline, file-scoped namespaces, `Directory.Packages.props` untouched unless a package is genuinely needed.
- [ ] Docs updated in the same slice when behaviour or status changes.
- [ ] Review summary lists behaviour changes, migration impact, verification evidence, and anything the owner must do manually.

---

## 12. Verification cheat-sheet

```powershell
dotnet build DentalClinic.slnx

dotnet test tests/DentalClinic.Domain.UnitTests --no-build
dotnet test tests/DentalClinic.Application.UnitTests --no-build
dotnet test tests/DentalClinic.Web.Tests --no-build
dotnet test tests/DentalClinic.Application.SubcutaneousTests --no-build    # needs Docker
dotnet test tests/DentalClinic.Api.IntegrationTests --no-build             # needs Docker, includes MigrationTests

dotnet run --project src/DentalClinic.Api      # http://localhost:5004, Scalar at /scalar/v1
dotnet run --project src/DentalClinic.Web      # http://localhost:7204
```

Migration command:

```powershell
dotnet ef migrations add <Name> --project src/DentalClinic.Infrastructure --startup-project src/DentalClinic.Api
```

CI already runs restore → build → test on `ubuntu-latest` with Docker asserted, so a Docker-dependent suite needs no workflow change.

---

## 13. When to re-plan

Re-open this document and revisit the order if any of these happens:

- A decision in §10 is answered differently from the recommendation.
- A slice turns out to need a data backfill that cannot be expressed as a deterministic SQL default.
- The billing filter in P1.2 changes the meaning of an already-issued invoice (it must not).
- The clinic asks for money visibility before clinical plans — swap Steps 1 and 2 rather than running them in parallel.
- A migration fails the `MigrationTests` replay on a clean database — stop and fix before adding another slice.