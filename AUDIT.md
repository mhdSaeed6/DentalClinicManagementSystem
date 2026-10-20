# DentalClinic — Architecture & Feature Audit

**Date:** 2026-10-19 · **Scope:** full repository (`src/`, `tests/`, `docs/`, infra files) · **Method:** read-only source inspection.

> `AGENTS.md`, `README.md` and `docs/product/PRD.md` were cross-read. Where they disagree with the code, **the code wins** and the disagreement is called out in §8.

---

## 1. Tech stack & architecture

### 1.1 Projects

| Project | Path | Role |
|---|---|---|
| `DentalClinic.Domain` | `src/DentalClinic.Domain` | Entities, value objects, domain errors, `Result<T>`, domain events |
| `DentalClinic.Contracts` | `src/DentalClinic.Contracts` | Request DTOs with DataAnnotations |
| `DentalClinic.Application` | `src/DentalClinic.Application` | CQRS (MediatR) handlers, validators, pipeline behaviours, `IAppDbContext`, security helpers |
| `DentalClinic.Infrastructure` | `src/DentalClinic.Infrastructure` | EF Core, ASP.NET Identity, JWT, HybridCache, seeder, migrations |
| `DentalClinic.Api` | `src/DentalClinic.Api` | Controllers, middleware, OpenAPI/Scalar, Serilog, background services, health checks |
| `DentalClinic.Web` | `src/DentalClinic.Web` | Blazor **Web App** (server-interactive, `AddInteractiveServerComponents`) |
| Tests (5) | `tests/` | `Domain.UnitTests`, `Application.UnitTests`, `Application.SubcutaneousTests`, `Api.IntegrationTests`, `Tests.Common` |

### 1.2 Frameworks & tooling

- **.NET 10** — `global.json` pins SDK `10.0.100` (`rollForward: latestFeature`); installed SDK reports `10.0.301`.
- **ORM:** EF Core 10.0.11/12 + SQL Server (`Directory.Packages.props:17,21`). `AppDbContext : IdentityDbContext<AppUser>` (`src/DentalClinic.Infrastructure/Data/AppDbContext.cs:20`).
- **DB:** SQL Server 2022 (`docker-compose.yml`, `mcr.microsoft.com/mssql/server:2022-latest`, host port `1434`).
- **CQRS:** MediatR 14.2.0 (`Directory.Packages.props:35`). Slice-per-feature folders under `Application/Features/<Feature>/{Commands|Queries}/<UseCase>/`.
- **Validation:** FluentValidation 12.1.1 in the pipeline (`Application/Common/Behaviours/ValidationBehaviour.cs`), *plus* DataAnnotations on `Contracts`.
- **Auth:** ASP.NET Core Identity + **JWT bearer**. Access token 60 min (`src/DentalClinic.Api/appsettings.json:52`); refresh tokens 7 days (hard-coded, `src/DentalClinic.Infrastructure/Identity/TokenProvider.cs:152`), SHA-256 hashed at rest (`src/DentalClinic.Domain/Identity/RefreshToken.cs:51-56`), rotated on every use with all prior sessions revoked (`TokenProvider.cs:132-142`).
- **Observability:** Serilog → Console + Seq (`appsettings.json:37`), OpenTelemetry traces/metrics → OTLP + Prometheus (`src/DentalClinic.Api/DependencyInjection.cs:67-88`).
- **API surface:** `Asp.Versioning` (URL-segment, `api/v{version:apiVersion}`), OpenAPI + Swagger UI + Scalar, ProblemDetails.
- **Caching:** `Microsoft.Extensions.Caching.Hybrid` (in-process only), opt-in per query via `ICachedQuery`.
- **Tests:** xUnit 2.9.3 + NSubstitute 6.2.0 + Testcontainers.MsSql 4.15.0 + Respawn 7.0.0. **No FluentAssertions, no bUnit, no browser/E2E tooling.**

### 1.3 Patterns actually used

- **Clean Architecture** (4 layers + Contracts), strict dependency direction.
- **CQRS with MediatR**, one file per command/query/validator, plus DTO + mapper extensions.
- **Result flow, not exceptions.** `Domain/Common/Results/Result.cs`; controllers unwrap with `result.Match(Ok, Problem)` (`Api/Controllers/ApiController.cs:11-39`).
- **No repositories.** Handlers use `IAppDbContext` directly; `SaveChangesAsync` acts as the unit of work. README's "Repository & Unit of Work" claim is wrong (`README.md:50`).
- **Pipeline behaviours** registered in `Application/DependencyInjection.cs:15-22`: `Validation → Performance → UnhandledException → Caching`. (`LoggingBehaviour` is a MediatR pre-processor, not a pipeline behaviour.)
- **Two-layer authorization** — `[Authorize(Policy=…)]` on controllers **plus** ownership checks in handlers via `Application/Common/Security/ClinicAuthorization.cs`.
- **EF interceptors** (`Infrastructure/Data/Interceptors/`): auditing, soft delete, domain-event dispatch.
- **Cache-tag invalidation**: every mutating handler ends with `cache.RemoveByTagAsync("<entity>")`.

### 1.4 How layers talk to each other

```
Web (Blazor)  --HTTP/JSON-->  Api (Controllers)  --ISender (MediatR)-->  Application handlers
                                                                              │
                                                              IAppDbContext  │  IUser, ICachedQuery,
                                                                              │  IIdentityService, ITokenProvider
                                                                            ▼
                                                                     Infrastructure (EF + Identity)
```

- `DentalClinic` has **zero `PackageReference`** entries (`src/DentalClinic.Web/DentalClinic.Web.csproj`) — the frontend talks to the API purely over HTTP through `ApiClient` (`src/DentalClinic.Web/Components/Api/ApiClient.cs`).
- Application layer references Domain only; Infrastructure implements `IAppDbContext` and is the only project that knows about EF.

### 1.5 Frontend: framework, state, i18n, theming

| Concern | Implementation | Evidence |
|---|---|---|
| Framework | Blazor Server interactive, one root `App.razor`, no JS framework, no npm/`package.json` | `src/DentalClinic.Web/Program.cs:7-8,47-48` |
| State management | `ClinicSessionState` (scoped singleton per circuit) + an in-component-field pattern per page. **No Fluxor/Redux/store library.** | `src/DentalClinic.Web/Components/ClinicSessionState.cs` |
| Data fetching | `ApiClient` hand-written HTTP client with bearer injection + transparent refresh-and-retry on 401 | `ApiClient.cs:290-369` |
| UI kit | Hand-written CSS design system, ~1200 lines, CSS custom properties | `src/DentalClinic.Web/wwwroot/app.css` |
| i18n | **Inline `Session.T(english, arabic)` string pairs** — no resource files, no `IStringLocalizer`, no `.resx`. | `ClinicSessionState.cs:155`; ~150 call sites across pages |
| RTL | `dir="@Session.Dir"` on the app root, `IsRtl => Language == Arabic` | `MainLayout.razor:6`, `ClinicSessionState.cs:68-70` |
| Dark mode | `data-theme="dark"` attribute on the root + CSS variables | `MainLayout.razor:6-8` |
| Forms | Raw `<input>`/`<select>` with `@bind` + manual validation, except `Login.razor` which uses `EditForm` | `Treatments.razor:300-341` |
| Notifications | `NotificationService` → toast host (in-process only). The topbar bell button is **non-functional** (`MainLayout.razor:109-111`). | `src/DentalClinic.Web/Components/NotificationService.cs` |

> ⚠️ Theme, language and sidebar preference are **not persisted** anywhere (no cookie, no localStorage, no profile) — they reset on refresh. Session tokens likewise (`ClinicSessionState`, defect D-14).

---

## 2. Domain model

Aggregate roots are rich (`private set` + factory methods returning `Result<T>`); all derive from `AuditableEntity` (`Id`, `CreatedAtUtc`, `CreatedBy`, `LastModifiedUtc`, `LastModifiedBy`, `DeletedAtUtc`, `DeletedBy`, `IsDeleted`).

### 2.1 Entities

| Entity | File | Key properties |
|---|---|---|
| **Patient** | `Domain/Patients/Patient.cs` | `FirstName`, `LastName`, `DateOfBirth`, `Gender`, **`ContactInfo`** (owned), **`MedicalHistory`** (owned), `IsDeleted` |
| **Doctor** | `Domain/Doctors/Doctor.cs` | `FirstName`, `LastName`, `Specialization`, **`ContactInfo`**, `Gender`, `IsActive`, **`UserId?`** (FK → `AppUser.Id`, unique filtered index) |
| **DentalService** | `Domain/DentalServices/DentalService.cs` | `Name`, `Description?`, `Price` (decimal 18,2), `IsActive` |
| **Appointment** | `Domain/Appointments/Appointment.cs` | `PatientId`, `DoctorId`, **`ServiceId?` (nullable since migration `20261014103922_MakeAppointmentServiceOptional`)**, `ScheduledDateTime`, `DurationInMinutes`, `Status`, `Notes?` |
| **Invoice** | `Domain/Invoices/Invoice.cs` | `PatientId`, `AppointmentId?`, `TotalAmount`, `PaidAmount`, `RemainingAmount` (computed), `PaymentStatus`, `InvoiceLifecycleStatus` (Draft/Issued), `HasManualAdjustments`, **`RowVersion`** (`rowversion`) |
| **InvoiceLine** | `Domain/Invoices/InvoiceLine.cs` | `InvoiceId`, `Description`, `Amount` (18,2). **Free-text description only — no `ServiceId`/`ToothNumber` link.** |
| **Payment** | `Domain/Invoices/Payment.cs` | `InvoiceId`, `Amount` (18,2), `PaidAtUtc`, `TransactionNotes?`. **No payment method / currency / payer.** |
| **TreatmentRecord** | `Domain/TreatmentRecords/TreatmentRecord.cs` | `PatientId`, `DoctorId`, `AppointmentId?`, **`ToothNumber`** (FDI 11-48 / 51-85), `ProcedureDetails` (free text), `Cost` (18,2) |
| **RefreshToken** | `Domain/Identity/RefreshToken.cs` | `TokenHash` (SHA-256 hex, unique), `UserId`, `ExpiresOnUtc` |
| **AppUser / Role** | `Infrastructure/Identity/AppUser.cs`, `Domain/Identity/Role.cs` | `AppUser : IdentityUser` — **no extra columns**. Roles: `Admin`, `Doctor`, `Receptionist` (`Domain/Identity/Roles.cs`). |

### 2.2 Value objects & enums

- **`ContactInfo`** (`Domain/Common/ValueObjects/ContactInfo.cs`) — owned by Patient and Doctor: `PrimaryPhone` (required, regex `^\+?\d{7,15}$`), `SecondaryPhone?`, `HasWhatsAppOnPrimary` (bool, **DB default `true`**), `SocialMediaLink?` (unvalidated string).
- **`MedicalHistory`** (`Domain/Patients/MedicalHistory.cs`) — owned by Patient: `HasDiabetes`, `HasHypertension`, `HasHeartDisease`, `Allergies?` (free text), `Notes?`. **No pregnancy flag, no blood-pressure reading.**
- **Enums:** `AppointmentStatus` (Scheduled/Completed/Cancelled/NoShow), `PaymentStatus` (Unpaid/PartiallyPaid/Paid), `InvoiceLifecycleStatus` (Draft/Issued), `Gender`.

### 2.3 ER description

```
AppUser 1 ──── 0..1 Doctor          (Doctor.UserId, unique filtered index)
AppUser 1 ──── *  RefreshToken

Patient 1 ──── * Appointment * ──── 1 Doctor
                 │  ServiceId? ──── 0..1 DentalService   (nullable!)
Patient 1 ──── * TreatmentRecord * ──── 1 Doctor
                 └── AppointmentId? ──── 0..1 Appointment
Patient 1 ──── * Invoice * ──── 0..1 Appointment
                 ├── 1..* InvoiceLine   (Cascade; NO soft-delete filter)
                 └── *    Payment       (Cascade)
Patient owns ContactInfo + MedicalHistory (OwnsOne, same table)
Doctor  owns ContactInfo                  (OwnsOne, same table)
```

- Delete behaviour: `Appointment→Patient/Doctor/Service` = **Restrict**; `Invoice→Patient` = Restrict; `Invoice→Payments`, `Invoice→InvoiceLines` = **Cascade**.
- Money precision: `decimal(18,2)` on `Services.Price`, `Invoices.TotalAmount`, `Invoices.PaidAmount`, `InvoiceLines.Amount`, `Payments.Amount`, `TreatmentRecords.Cost` (`Infrastructure/Data/Configurations/*.cs`).
- Soft-delete query filters exist on Patient, Doctor, DentalService, Appointment, Invoice, InvoiceLines (no), Payment, TreatmentRecord, RefreshToken. **`InvoiceLine` is the only `AuditableEntity` without a `HasQueryFilter`** (`InvoiceLineConfiguration.cs:16-17`).

---

## 3. Implemented features

Status legend: **Complete** = backend + UI both usable · **Partial** = present but incomplete/limited · **Stub** = scaffolding only.

### 3.1 Patients — **Complete**

| | |
|---|---|
| Backend | `GET/POST /api/v1/patients`, `GET/PUT/DELETE /api/v1/patients/{id}` (`Api/Controllers/PatientController.cs`). Handlers `Application/Features/Patients/{Commands,Queries}/*`. Search across first/last name + primary phone (`GetPatientsQueryHandler.cs:42-49`). Soft delete. Medical history round-trips as an owned entity. |
| Frontend | `Pages/Patients.razor` (list + modal form), `Pages/PatientDetails.razor` (4 tabs: overview / appointments / treatments / invoices). |
| Authz | `StaffOnly`; `DELETE` = `AdminOnly` (`PatientController.cs:169`). `PatientScope()` grants **all** staff the full registry (`ClinicAuthorization.cs:51-54`). |

### 3.2 Appointments — **Complete** (best-developed feature)

| | |
|---|---|
| Backend | Full CRUD subset in `AppointmentController.cs`: list (filters: patient/doctor/service), get, create, `PUT /{id}/reschedule`, `PATCH /{id}/complete`, `PATCH /{id}/complete-with-draft`, `PATCH /{id}/cancel`, `PATCH /{id}/no-show`. |
| Domain | `Appointment.Create/Complete/Cancel/Reschedule/MarkAsNoShow/HasEndedBy` (`Appointment.cs`), 5 domain events. |
| Integrity | Double-booking overlap check under `IsolationLevel.Serializable` (`CreateAppointmentCommandHandler.cs:87-102`, transaction at `:35`). ⚠️ **Not applied on reschedule** (see §8). |
| Automation | `AppointmentNoShowBackgroundService` (`Api/BackgroundServices/`) marks overdue scheduled appointments `NoShow` on a `PeriodicTimer` (interval + grace period from `AppSettings`). |
| Frontend | `Pages/Appointments.razor` — 1213 lines: filter chips, table, detail drawer with billing panel, complete/reschedule/cancel/no-show modals, inline treatment entry, invoice-draft generation. |
| Authz | `StaffOnly` + per-handler `CanAccessAppointment`. |

### 3.3 Doctors — **Complete** (CRUD only)

- Backend: `DoctorController.cs` — list/get/create/update/delete. `DELETE` = `AdminOnly`; create/update = `AdminOnly`. **No endpoint links/unlinks a Doctor profile to an `AppUser`** — `AssignUser`/`UnassignUser` exist in the domain (`Doctor.cs:131,136`) but are only called by the seeder (`ApplicationDbContextInitialiser.cs:167-168,211,217`). A doctor created through the UI therefore has **no `doctor_id` claim** and falls into the `Guid.Empty` restricted scope → sees nothing.
- Frontend: `Pages/Doctors.razor`.
- Missing: schedule view, earnings, performance.

### 3.4 Dental services — **Complete** (CRUD only)

- Backend: `DentalServiceController.cs`, `Application/Features/DentalServices/*`. `AdminOnly` for writes.
- Frontend: `Pages/Services.razor`.
- `Price` is a single scalar; **no per-session/per-tooth price model, no category, no duration**.

### 3.5 Treatments — **Partial**

- Backend: `TreatmentRecordController.cs` — list (patient/doctor/appointment filters), get, create (`ClinicalWrite`), update (`ClinicalWrite`). No delete.
- FDI validation exists and is centralized in the domain (`TreatmentRecord.IsValidToothNumber`, `TreatmentRecord.cs:135-148`) — but **duplicated** in two validators (`CreateTreatmentRecordCommandValidator.cs:27-40`, `UpdateTreatmentRecordCommandValidator.cs:30-43`).
- Frontend: `Pages/Treatments.razor` + `Shared/ToothChart.razor` (32 permanent teeth, two rows, selectable).
- **No odontogram semantics** — `ToothNumber` is a scalar on an event log, not per-tooth state. No diagnosis, no surface (M/D/B/F), no per-tooth history view, no status.

### 3.6 Billing / invoicing — **Partial** (strong core, no patient-level view)

| Capability | Status | Evidence |
|---|---|---|
| Invoice CRUD (standalone) | Complete | `POST /invoices` (`InvoiceController.cs:72-95`) |
| Draft generation from appointment | Complete | `POST /invoices/from-appointment/{id}` (`GenerateAppointmentInvoiceCommandHandler.cs`), idempotent, `Serializable`, reuses a reusable draft |
| Pre-flight breakdown | Complete | `GET /invoices/breakdown/{appointmentId}` |
| Draft edit | Complete | `PUT /invoices/{id}/draft` |
| Draft refresh | Complete | `POST /invoices/{id}/draft/refresh` |
| Issue | Complete | `POST /invoices/{id}/issue` (idempotent) |
| **Partial payments** | **Complete** | `POST /invoices/{id}/payments`; `PaymentStatus.PartiallyPaid`; serializable + `RowVersion` → 409 on conflict (`AddInvoicePaymentCommandHandler.cs:29,63-78`) |
| **Patient balance / debt view** | **Missing** | No aggregation query anywhere. `GetInvoicesQueryHandler` has no `Sum`/`GroupBy`. Balances shown in UI are computed **client-side over one page** (`Invoices.razor:32,37` with `_pageSize = 10`; `PatientDetails.razor:98,103` with `pageSize: 100`; `Dashboard.razor:309-311` with `pageSize: 100`). |
| Invoice numbering | **Missing** | UI displays `#@invoice.Id.ToString()[..8]` (`Invoices.razor:105`, `PatientDetails.razor:226`, `Appointments.razor:366`) — defect D-11. |
| Printable invoice / logo | **Missing** | No print stylesheet, no logo asset, no PDF path (`wwwroot/` contains only `app.css` + `favicon.svg`). |
| Discounts / tax / refunds / credit notes | **Missing** | `BuildLines` only emits service price + treatment costs (`GenerateAppointmentInvoiceCommandHandler.cs:162-202`). |

### 3.7 Dashboard — **Partial**

- `Pages/Dashboard.razor`: patient count, appointments today, collected revenue, outstanding balance, appointment-mix bars, upcoming appointments, recent invoices, newest patients.
- **All aggregates are client-side over the first ≤100 rows** of five separate paged API calls (`Dashboard.razor:287-311`). Wrong beyond 100 records — defect D-09.
- No date range, no per-doctor breakdown, no time series, no chart library.

### 3.8 Auth & roles — **Partial**

- Backend: `POST /identity/token/generate`, `POST /identity/token/refresh-token` (both `[EnableRateLimiting(Auth)]`), `GET /identity/current-user/claims`. Uniform login errors (`IdentityService.cs:43-77`), lockout 5/15 min, HMAC-SHA256 with algorithm-confusion guard (`TokenProvider.cs:62-66`), lazy ephemeral dev secret (`JwtSecretProvider.cs:50-78`).
- **No user-management endpoints at all** — no list/invite/disable/reset-password. Staff accounts are seeded only.
- Frontend: `Pages/Login.razor`, `MainLayout.razor` user menu.
- 🔴 **Bug:** the three "tap to fill" demo buttons inject **wrong passwords** — `Login.razor:110,115,120` use `Admin123!`, `Doctor123!`, `Reception123!`; the seeder creates `Admin!Pass123`, `Doctor!Pass123`, `Receptionist!Pass123` (`ApplicationDbContextInitialiser.cs:32-34`). All three quick-login buttons fail.
- Frontend role handling is cosmetic: `Session.IsDoctor` only hides the "New invoice" button and the doctor filter (`Invoices.razor:15,79`, `Appointments.razor:31`).

### 3.9 Settings — **Stub**

- `Pages/Settings.razor` shows profile, roles, language toggle, theme toggle, sidebar toggle, API base URL, sign-out. **No persisted settings, no clinic profile, no currency, no exchange rate, no users/roles management.** Nothing is persisted to the backend.

### 3.10 Other

- Health: `/health`, `/health/ready` (`Api/Program.cs:52-57`).
- Localization: UI only (inline EN/AR pairs). **No localized API responses, no localized data entry** (patients/doctors/services have single-language name fields).

---

## 4. Cross-cutting concerns

| Concern | Implementation |
|---|---|
| **Roles / permissions** | 3 Identity roles: `Admin`, `Doctor`, `Receptionist` (`Domain/Identity/Roles.cs`). Policies `AdminOnly`, `DoctorOnly`, `StaffOnly`, `ClinicalWrite` (`Infrastructure/DependencyInjection.cs:122-126`). **`DoctorOnly` is registered but never applied.** Handler-level ownership in `ClinicAuthorization.cs`. ⚠️ `InvoiceController` uses bare `[Authorize]` (any authenticated user) — see §8. |
| **Validation** | Three layers: DataAnnotations on `Contracts` → FluentValidation in the pipeline → domain invariants returning `Result<T>`. Good coverage. Gaps: `GetPatientsQuery` has no `SearchTerm` max-length (other query validators cap at 100); `UpdateInvoiceDraftCommand` / `RefreshInvoiceDraftCommand` have **no validator** (validation is ad-hoc inside the handlers); `SocialMediaLink` is `[Url]` in Contracts but unvalidated in the domain. |
| **Error handling** | `Result<T>` everywhere; `ApiController.Problem(List<Error>)` maps `ErrorKind → status` (`ApiController.cs:26-39`). RFC7807 ProblemDetails + `GlobalExceptionHandler`. ⚠️ Only `errors[0]` is surfaced for mixed-kind lists; `ErrorKind.Unauthorized → 403` (should be 401). |
| **Logging** | Serilog → Console + Seq; `LoggingBehaviour` logs **the full request object**; `PerformanceBehaviour` warns >500 ms. ⚠️ See §8 for the secret-leak issue. |
| **Localization** | Frontend-only, inline `T(en, ar)`. No `.resx`, no `IStringLocalizer`, no RTL-aware date/number formats (`ClinicFormatting` hardcodes `CultureInfo.InvariantCulture`). |
| **Multi-currency** | **Absent.** `public const string Currency = "USD"` hardcoded in the UI (`ClinicFormatting.cs:9`). Every money column is a bare `decimal(18,2)` with no currency code; `Payment` has no currency field. |
| **File uploads** | **None.** No `IFormFile`, no multipart endpoint, no blob storage package, no `wwwroot/uploads`. (Orphan helper `ClinicFormatting.FileSize` at `ClinicFormatting.cs:154` suggests intent.) |
| **Background jobs** | Exactly one: `AppointmentNoShowBackgroundService` (in-process `BackgroundService`, in-process only, no distributed lock — unsafe if the API is scaled >1 replica). No Hangfire/Quartz. |
| **Notifications** | **None outbound.** `NotificationService` is a client-side toast. Domain events exist but `DispatchDomainEventsInterceptor` has **zero subscribers** — no handler exists. The topbar bell is a dead button. |
| **Backups** | **None in code.** `sqlserver-data` is a Docker named volume (`docker-compose.yml`) — that is not a backup. |
| **Tests** | ~408 `[Fact]` + 48 `[Theory]` across 4 test projects. Domain + Application unit tests are pure. Subcutaneous + Integration use Testcontainers SQL Server (Docker required). `MigrationTests.cs` replays the real migration chain. `AuthorizationIntegrationTests.cs` — 17 real-JWT authorization scenarios. **Zero tests for `DentalClinic.Web`.** |

---

## 5. Gap analysis against the target feature list

| # | Target feature | Status | Evidence |
|---|---|---|---|
| 1 | **Interactive odontogram** (FDI, per-tooth diagnosis & treatment history) | **Partial** | FDI numbering ✅ (`TreatmentRecord.IsValidToothNumber`, `TreatmentRecord.cs:135-148`); selectable tooth chart UI ✅ (`Shared/ToothChart.razor`, used only in the treatment-creation modal). Missing: per-tooth persistent state, diagnosis field, tooth surfaces, per-tooth history/aggregation, missing-tooth/mobility markers. There is **no odontogram endpoint or query** — `TreatmentRecord` is an append-only event log with a scalar `ToothNumber`. `ToothNumber` is **non-nullable**, so a procedure not tied to a tooth cannot be recorded (`PRD.md:476` asks for nullable). |
| 2 | **Treatment plans, cost per session, printable sheet with clinic logo** | **Missing** | No `TreatmentPlan` entity. `TreatmentRecord` has no status (Planned/Performed), no session grouping (`AppointmentId?` is the closest thing). Cost exists per record (`Cost`) but there is no plan total, no per-session aggregate, no print view, no logo asset. Closest existing: `Pages/Appointments.razor:257-302` shows an on-screen estimated total only. |
| 3 | **Patient balance & partial payments, outstanding debt tracking** | **Partial** | Partial payments ✅ complete and concurrency-safe (`Payment`, `PaymentStatus.PartiallyPaid`, `AddInvoicePaymentCommandHandler`). Per-invoice balance ✅ (`Invoice.RemainingAmount`). **Patient-level balance: Missing** — no aggregation endpoint exists; all UI totals are client-side sums over a single page (`Invoices.razor:32,37` with page size 10; `PatientDetails.razor:98,103`; `Dashboard.razor:309-311`). No debt/aging report, no cross-invoice patient ledger. |
| 4 | **WhatsApp appointment reminders** (wa.me link + prepared message) | **Missing** | Only the data flag exists: `ContactInfo.HasWhatsAppOnPrimary` (`ContactInfo.cs:12`), surfaced as a badge (`PatientDetails.razor:47-50`) and an `whatsapp` icon in the icon set (`ClinicIcon.razor:48`). **Zero outbound messaging code, no `wa.me` construction, no notification service, no SMS/WhatsApp NuGet package.** Explicitly a non-goal in `docs/product/PRD.md:100,590`. |
| 5 | **Doctor/owner financial dashboard** (daily/monthly income, debts, commission %) | **Missing** | `Dashboard.razor` has no revenue-by-period, no per-doctor figure, no debt ageing, no commission concept (0 occurrences of "commission" in the entire repo). `Doctor` has no financial fields. No reporting endpoint. |
| 6 | **Dual currency (USD + local) with editable exchange rate** | **Missing** | `ClinicFormatting.Currency = "USD"` is a compile-time constant (`ClinicFormatting.cs:9`). No `Currency` entity, no `ExchangeRate`, no currency column on any money field, no settings persistence. `Directory.Packages.props` has no FX library (none needed, but nothing exists). `PRD.md:210` records "Currency hardcoded USD" as a known gap. |
| 7 | **X-ray / before-after image upload linked to patient & tooth** | **Missing** | No image/file entity, no `IFormFile` endpoint, no multipart config, no blob storage package, no `<input type="file">` in any component. `wwwroot/` holds only `app.css` + `favicon.svg`. Explicitly out of scope in `PRD.md:103,959`. |
| 8 | **Medical alerts** (allergies, diabetes, BP, pregnancy) shown on patient file | **Partial** | Diabetes / hypertension / heart disease / allergies ✅ stored (`MedicalHistory.cs`) and rendered with an "Attention needed" badge (`PatientDetails.razor:108-134`, `HasAlerts` at `:254`). **Missing: pregnancy flag, blood-pressure readings, structured allergy list (free text only), and any alert on other screens** — the badge appears only on the Patient Details *Overview* tab; nothing on `Patients.razor`, `Appointments.razor`, or the dashboard. No server-side alert derivation. |
| 9 | **Role-based permissions (secretary / doctor / owner)** | **Partial** | Three roles exist and are enforced (`Admin` ≈ owner, `Receptionist` ≈ secretary, `Doctor`), with policies + handler-level ownership + 17 integration tests (`AuthorizationIntegrationTests.cs`). **Gaps: (a) `InvoiceController` uses bare `[Authorize]` — no role restriction and no ownership check on any invoice mutation; (b) no user/role management UI or endpoint, so "owner" cannot grant roles; (c) no per-doctor user-link endpoint, so doctor scoping silently breaks for UI-created doctors; (d) "secretary"/"owner" are not role names — mapping is implicit.** |
| 10 | **Dental lab tracking** (case sent, expected return, cost) | **Missing** | No entity, no endpoint, no page, no status enum. Nothing in the repo. Rejected for MVP in `PRD.md:103,959`. |
| 11 | **Automatic backups and Excel import** | **Missing** | Backups: no code, no job, no script; compose uses a named volume (not a backup). Excel import: no `ClosedXML`/`EPPlus`/`NPOI`/`CsvHelper` package (`Directory.Packages.props`), no import endpoint, no import UI. The only export-adjacent thing is nothing at all — there is no CSV/Excel export either. |

**Summary: 0 Exists · 5 Partial · 6 Missing.**

---

## 6. Gap closure plan (per Partial/Missing item)

Effort scale: **S** ≤ 3 days · **M** ≈ 1 week · **L** > 1 week.

---

### 6.1 Interactive odontogram — **Partial → complete (M)**

**Extend/reuse**
- `Domain/TreatmentRecords/TreatmentRecord.cs` — keep as the *history* record; make `ToothNumber` nullable.
- `Domain/Common/ValueObjects` — add a `ToothNumber`/`FdiTooth` value object wrapping `TreatmentRecord.IsValidToothNumber` so the three duplicated copies collapse into one.
- `Web/Components/Shared/ToothChart.razor` — reuse as the base; wrap each button in per-tooth state instead of a single selected `Value`.

**New**
- Entity `PatientTooth` (`PatientId`, `ToothNumber`, `Condition` enum — e.g. `Healthy/Caries/Filled/Extracted/Crown/Bridge/Implant/Missing`, `Notes`, `Surface?`); unique index `(PatientId, ToothNumber)`.
- Entity/enum `DiagnosisCode` (optional reference table).
- `Domain/Common/Enums/ToothCondition.cs`; `Infrastructure/Data/Configurations/PatientToothConfiguration.cs`.
- Migration `AddPatientOdontogram`; update `AppDbContextModelSnapshot`.
- Application: `Features/Odontograms/Queries/GetPatientOdontogramQuery` (one round trip: teeth + all history grouped by tooth), `Features/Odontograms/Commands/UpsertToothConditionCommand`, `Features/Odontograms/Commands/RemoveToothConditionCommand`. Cache tag `tooth`; invalidate on treatment create/update.
- `TreatmentRecordController` → add `GET /treatment-records?patientId=&toothNumber=` filter.
- API `GET /api/v1/patients/{id}/odontogram`; `DentalClinic.Contracts/Requests/Odontograms/*`.
- Web: `Components/Shared/Odontogram.razor` (per-tooth colouring, legend, click → drawer with history), embedded as a new tab in `PatientDetails.razor`.
- Tests: domain (condition transitions), validator tests, one integration test per endpoint, one subcutaneous authorization test.

**Dependencies** — Billing (tooth-labelled invoice lines already read `ToothNumber`), item 2 (plans), item 8 (alerts per tooth).
**Risks** — Data migration for existing `TreatmentRecords` (backfill `PatientTooth` rows from history, or mark chart "unknown until touched"). Tooth-condition vocabulary should be agreed with the clinician before coding.

---

### 6.2 Treatment plans + printable sheet with logo — **Missing (M–L)**

**Extend/reuse**
- `TreatmentRecord` — add `Status` (`Planned` / `Performed` / `Cancelled`), `SessionNumber`, `EstimatedCost` vs `ActualCost`. Only `Performed` becomes billable (already the effective rule in `GenerateAppointmentInvoiceCommandHandler.BuildLines`, which currently bills *every* record with `Cost > 0`).
- `Appointment` — a plan is proposed at booking time; `CompleteAppointmentRequest.Treatments` (`Contracts/Requests/Appointments/CompleteAppointmentRequest.cs`) already carries performed work.
- `ApiClient` + `Modal`/`Drawer`/`PageHeader` shared components.

**New**
- Entity `TreatmentPlan` (`PatientId`, `DoctorId`, `Title`, `Status`, `CreatedAt`) + `TreatmentPlanItem` (`PlanId`, `TreatmentRecordId` or free `Description`, `ToothNumber?`, `EstimatedCost`, `Sequence`, `Status`). Alternative (lighter): add `PlanId?` + `Status` directly to `TreatmentRecord` — recommended for a first cut.
- Migration `AddTreatmentPlans`.
- Application: `Features/TreatmentPlans/{Commands,Queries}/*` — create plan, add/remove item, accept plan → materialise `TreatmentRecord`s, per-session costing (`GET /treatment-plans/{id}/sessions` returning cost grouped by `SessionNumber`/`AppointmentId`).
- API `TreatmentPlanController`.
- Web: `Pages/TreatmentPlans.razor` + `Shared/PlanPrintSheet.razor`; a `@media print` block in `wwwroot/app.css`; `wwwroot/img/logo.svg` (or a `ClinicSetting`-sourced logo URL).
- Tests: cost-per-session aggregation correctness (the money maths — highest-value test in this section).

**Dependencies** — item 1 (per-tooth items), item 6 (currency, before printing money), item 3 (plan total vs balance).
**Risks** — Changing `TreatmentRecord` billing semantics will break existing invoice-generation tests; the "only Performed is billable" rule must be introduced with a backfill of existing records to `Performed`. Printing Arabic RTL text needs its own CSS pass.

---

### 6.3 Patient balance & debt tracking — **Partial → complete (S–M)**

**Extend/reuse**
- `Invoice` / `Payment` / `InvoiceController` — nothing to change on the write side; payment capture is already correct and concurrency-safe.
- `Application/Common/Interfaces/IAppDbContext.cs` — add the aggregation queries here.
- `IUser` + `ClinicAuthorization.PatientScope()` — the new endpoints must reuse the same scope checks.

**New**
- `Features/Billing/Queries/GetPatientBalanceQuery` (returns `TotalBilled`, `TotalPaid`, `Balance`, `OpenInvoiceCount`, oldest unpaid invoice date) — a real `SumAsync` over all non-deleted invoices for one patient, **not** over a page.
- `Features/Billing/Queries/GetOutstandingDebtsQuery` (paginated, optional `minAmount`/`asOf` filters, `PatientName` resolved in one round trip).
- `BillingController` (`GET /api/v1/billing/patients/{id}/balance`, `GET /api/v1/billing/outstanding`); cache-tag `invoice`.
- Web: a `Components/Shared/BalanceBadge.razor`; replace the client-side sums in `Invoices.razor:32,37`, `PatientDetails.razor:98,103`, `Dashboard.razor:309-311`.
- Optional: patient detail "Debt" tab with per-invoice ageing.
- Tests: aggregation correctness with >1 page of invoices (regression for D-09), scoping (a doctor must not see another doctor's patient debt).

**Dependencies** — item 5 (feeds the financial dashboard).
**Risks** — Low technical risk; the main trap is **not** shipping another client-side sum. Decide whether "debt" spans all invoices or only issued ones (currently drafts also carry `RemainingAmount`).

---

### 6.4 WhatsApp reminders (wa.me link) — **Missing (S)**

**Extend/reuse**
- `ContactInfo.HasWhatsAppOnPrimary` + `PrimaryPhone` — already there; `PhoneRegex` guarantees a digit-only number suitable for `wa.me`.
- `ApiClient` + `ClinicFormatting` — link construction is a **client-side** concern; no server round trip needed.
- `Pages/Appointments.razor` — add a per-row action next to Complete/Reschedule/Cancel.

**New**
- `Web/Components/WhatsAppMessageBuilder.cs` — builds `https://wa.me/<digits>?text=<urlencoded>` with a templated body (clinic name, patient name, date/time, doctor).
- `Components/Shared/WhatsAppButton.razor` — opens in a new tab; disabled with a tooltip when `!HasWhatsAppOnPrimary`.
- Optional template strings in `wwwroot/` so receptionists can edit them without a rebuild.

**Dependencies** — i18n (an Arabic message template). Optionally item 11/backup work is unrelated.
**Risks** — Phone must be normalized (`+963…` → `963…`, strip non-digits) or the link silently fails; add a unit-testable normalizer. `wa.me` links cannot be verified server-side and are a client-only convenience — no real delivery guarantee, so do not present it as a reminder *system*.

---

### 6.5 Doctor/owner financial dashboard — **Missing (L)**

**Extend/reuse**
- `Invoice` + `Payment` + `Appointment` — the underlying facts already exist and are correctly scoped.
- `IAppDbContext`, `DoctorAccessScope`, `IUser` — scoping primitives are ready.
- `Pages/Dashboard.razor` — extend rather than replace; but move all maths server-side (fixes D-09 at the same time).

**New**
- `Features/Reporting/Queries/GetFinancialSummaryQuery(from, to, doctorId?)` → income (payments received), invoiced, outstanding, by-period buckets; `GetDoctorEarningsQuery(from,to,doctorId)` → per-doctor collected revenue.
- **Commission model** — new entity `CommissionRule` (`DoctorId`, `Percent`, `EffectiveFrom`, `EffectiveTo?`) and computed field `DoctorCommission` on the reporting DTO (derived, not stored, unless payroll-grade accuracy is needed later).
- `ReportingController` — `GET /api/v1/reporting/financial-summary`, `/reporting/doctors`. **Admin-only** (`AdminOnly` policy) — owner sees clinic-wide; a doctor may see only their own via `DoctorScope()`.
- Web: `Pages/Reports.razor` with a date-range picker, per-doctor table with commission %, and daily/monthly buckets. No chart library exists today — either add one or render simple CSS bars as `Dashboard.razor:147-149` already does.
- Tests: date-boundary correctness, scope enforcement, commission rounding.

**Dependencies** — **item 3** (debt figures), **item 6** (currency), and realistically item 2 (plan vs performed revenue).
**Risks** — Largest item. Commission semantics are a business decision (% of collected revenue vs invoiced; who pays the lab; are discounts absorbed). Timezone handling for daily buckets is a real correctness risk — all datetimes are stored UTC (`Appointment.ScheduledDateTime`, `Payment.PaidAtUtc`) and the clinic is Syrian (`+963` seed numbers), so bucketing must be explicit about a clinic timezone.

---

### 6.6 Dual currency with editable exchange rate — **Missing (M–L)**

**Extend/reuse**
- All money columns already are `decimal(18,2)` (`Infrastructure/Data/Configurations/*.cs`) — extend rather than reshape.
- `Web/Components/ClinicFormatting.cs:9-15` — the single hardcoded constant is the seam to cut.
- `Api/DependencyInjection.cs` + `Infrastructure/DependencyInjection.cs` — `AppSettings` binding pattern already exists (but see §8: 7 of 15 current settings are dead).

**New**
- Entity `ClinicSetting` (single-row clinic profile: `Name`, `LogoUrl`, `BaseCurrency`, `SecondaryCurrency`, `TimeZone`) — this also fixes the hardcoded "Al Noor Dental" branding (D-15) and gives item 2 its logo and item 5 its timezone.
- Entity `ExchangeRate` (`BaseCurrency`, `QuoteCurrency`, `Rate decimal(18,6)`, `EffectiveAtUtc`) — historical rates so past invoices remain reproducible.
- Migration `AddClinicSettingsAndExchangeRates`.
- `Features/Settings/Commands/UpdateClinicSettings`, `UpdateExchangeRate` + `GetExchangeRate` queries; `SettingsController` (`AdminOnly`).
- Decision: store amounts in base currency only + a display-time conversion, **or** add `Currency` + `ExchangeRateId` to `Invoice`/`Payment`. The latter is more correct for a multi-currency clinic but touches every money read/write and the existing `RowVersion` flows. Recommend: store `Invoice`/`Payment` in the currency they were transacted in (add `Currency` char(3), non-null, defaulted) and convert only for reporting.
- Web: currency selector + rate editor in `Settings.razor` (which currently persists nothing); `ClinicFormatting.Money` becomes an injected service rather than a static.
- Tests: conversion round-trip, historical-rate lookup, rounding to 2 dp, reporting aggregation across currencies.

**Dependencies** — **item 5** (reporting must know which currency it sums), item 2 (printed plans).
**Risks** — Highest blast radius in the whole list. A migration on money columns invalidates the existing 22-test invoice suite and the concurrency tests. Do this **after** item 3 so the debt numbers are already server-side. Requires a business decision on rounding and on which currency the ledger is kept in.

---

### 6.7 X-ray / before-after images — **Missing (M)**

**Extend/reuse**
- `Patient`, `TreatmentRecord` (→ `ToothNumber`), `AppDbContext`, the migration/audit interceptor pattern.
- `ApiClient` — needs multipart support added (currently JSON-only, `ApiClient.cs:269-273`).

**New**
- Entity `PatientImage` (`PatientId`, `TreatmentRecordId?`, `ToothNumber?`, `Kind` enum `Xray`/`Before`/`After`/`Other`, `FileName`, `ContentType`, `SizeBytes`, `StoragePath`, `UploadedBy`, `CapturedAtUtc`, soft-delete).
- Storage: **decide local-disk vs object storage now.** Local `wwwroot/uploads` breaks the read-only-container model implied by the compose setup and won't scale; Azure Blob/S3 is the durable choice. Implement behind an `IImageStorage` interface so it can be swapped.
- API: `POST /api/v1/patients/{id}/images` (`IFormFile`, `[RequestSizeLimit]`, content-type allow-list, magic-byte sniffing, generated server-side filename — never trust the client's), `GET`, `DELETE`. Note the global output-cache base policy skips `Authorization` requests (`Api/DependencyInjection.cs:58-61`), which is the right default for images too.
- Web: `Components/Shared/ImageUpload.razor`, gallery on `PatientDetails.razor`, tooth-linked upload from the odontogram (item 1).
- Tests: upload validation (wrong MIME, oversized, double extension), authorization, tooth-link integrity.

**Dependencies** — item 1 (tooth linking), item 4/`ClinicSetting` (storage config).
**Risks** — Security-critical: unrestricted upload is the classic path to RCE via web shells. Malware scanning, size limits and a strict allow-list are non-negotiable. Also a GDPR/PHI concern for retention.

---

### 6.8 Medical alerts — **Partial → complete (S–M)**

**Extend/reuse**
- `Domain/Patients/MedicalHistory.cs` (`OwnsOne`, `PatientConfiguration.cs:59-80`) — extend in place.
- `PatientDto` / `MedicalHistoryDto` / `PatientFormRequest` already round-trip the flags; `Patients.razor` and `PatientDetails.razor` already render chips.

**New**
- `MedicalHistory`: add `IsPregnant`, `PregnancyWeeks?`, `BloodPressureReading?`, `HasBleedingDisorder`, `HasEpilepsy`, `HasAsthma`, `Notes`. Replace free-text `Allergies` with `PatientAllergy` (`Substance`, `Reaction`, `Severity`, `Notes`) while keeping `Allergies` for backward compatibility.
- Derived `MedicalAlertSeverity` computed in a `MedicalAlertEvaluator` in the Domain (single source of truth, unit-testable).
- Surface the alert on **`PatientDto` itself** as `AlertSeverity` so *every* screen can render it without re-deriving: `Patients.razor` row badge, an `Appointments.razor` strip on the appointment drawer, and a dashboard "patients with alerts today" card.
- Hard-stop guard: block clinical record creation for a critical allergy until the dentist acknowledges (design decision — a warning, not a block, is safer for MVP).
- Tests: evaluator table tests; API test asserting `AlertSeverity` is returned.

**Dependencies** — none blocking; benefits from item 1 (per-tooth risk).
**Risks** — Low. Biggest risk is **not** doing it: the data exists today but is only visible on one tab of one page.

---

### 6.9 Role-based permissions (secretary / doctor / owner) — **Partial → complete (M)**

**Extend/reuse**
- `Infrastructure/DependencyInjection.cs:122-126` — policies already exist; `DoctorOnly` just needs to be applied.
- `Application/Common/Security/ClinicAuthorization.cs` — extend with a `ClinicPermission` enum and a single `user.Can(Permission)` resolver.
- `AuthorizationIntegrationTests.cs` — the harness for proving any new rule.

**New**
- **Fix first (S):** add ownership checks to `IssueInvoiceCommandHandler`, `UpdateInvoiceDraftCommandHandler`, `RefreshInvoiceDraftCommandHandler`, `AddInvoicePaymentCommandHandler`, `CreateInvoiceCommandHandler`, `GetAppointmentInvoiceBreakdown`; change `InvoiceController` from bare `[Authorize]` to `[Authorize(Policy="StaffOnly")]`.
- **Then (M):** `StaffUserController` — list users, create invite, assign/revoke roles, deactivate, reset password (Admin-only). This is the capability that makes "owner" real.
- `DoctorController`: `POST /{id}/link-user` / `unlink-user` to close the §3.3 gap.
- Define an explicit permission matrix (`patient.read/write`, `clinical.write`, `invoice.read/create/issue/pay`, `staff.manage`, `settings.manage`, `reports.view`) enforced in `ClinicAuthorization`, replacing ad-hoc per-handler `if`s.
- Web: a role-aware nav (`Session.Role`) and route guards; an Admin "Staff" page.
- Tests: extend `AuthorizationIntegrationTests` to a matrix test that enumerates every endpoint × every role.

**Dependencies** — none. **Do this first** — items 5 and 6 both introduce sensitive data that must land behind a correct permission model.
**Risks** — Changing invoice authorization may break existing tests that assume any authenticated user can pay. Multi-clinic/tenant scoping (`PRD.md` TEN-00x) is a *separate*, much larger piece of work — do not conflate it with roles.

---

### 6.10 Dental lab tracking — **Missing (M)**

**Extend/reuse**
- `Patient`, `Doctor`, `TreatmentRecord` (a crown/implant record *is* the lab case), `Invoice` (lab cost is a billable line), `Appointment`.
- `Status` enums and `ICachedQuery` tag pattern.

**New**
- Entity `LabCase` (`PatientId`, `DoctorId`, `TreatmentRecordId?`, `ToothNumber?`, `LabName`, `CaseType`, `Status` enum `Sent/AtLab/Trial/TriedIn/Returned/Failed`, `SentAtUtc`, `ExpectedReturnAtUtc`, `ReturnedAtUtc?`, `Cost`, `Notes`).
- Overdue-lab background service — **clone the existing `AppointmentNoShowBackgroundService` pattern** (`Api/BackgroundServices/`), with its own `AppSettings` interval. Warning: use a **single-replica-safe** trigger; the existing service has no distributed lock.
- `Features/LabCases/*` CRUD + `GET /lab-cases?status=&overdue=true`; `LabCaseController` (`StaffOnly`; `ClinicalWrite` for status changes).
- Web: `Pages/LabCases.razor` — kanban by status, "expected return" column, overdue highlighting; link from the patient file and optionally auto-create a `LabCase` from a `TreatmentRecord` marked `SentToLab`.
- Tests: status transition validity, overdue query, cost inclusion in invoice generation.

**Dependencies** — item 1 (tooth), item 3 (lab cost → patient balance), item 7 (lab photos).
**Risks** — Medium. Adding lab cost to `BuildLines` changes invoice totals and will move existing test expectations. Decide up front whether lab cost is recharged to the patient (invoice line) or absorbed (commission base) — that decision also determines item 5's commission calculation.

---

### 6.11 Automatic backups + Excel import — **Missing (S + M, independent)**

**Automatic backups (S–M, ops not code)**
- No code changes required in the application. Prefer SQL Server native `BACKUP DATABASE` scheduled via **SQL Agent** in the `sqlserver` container (add a job via an init script mounted from `containers/`), or an external tool (`sqlbackup`/`minio`) — **not** an app-level `BackgroundService`, which would put backups in the web process's failure domain and produce no off-host copy.
- Add a retention policy + off-box copy (S3/Backblaze). Document and **rehearse** a restore; `PRD.md:866` (`REL-001`) and `MVP-ROADMAP.md:394` both call for a tested restore, not just a backup.
- Compose: add the Agent job init script; do **not** commit credentials for the backup target.

**Excel import (M)**
- Extend/reuse: `Patient`/`Doctor`/`DentalService` bulk create; `PatientConfiguration` indexes; the existing `EnsureCreated`-vs-`Migrate` test split (`MigrationTests.cs` must keep passing).
- New: package `ClosedXML` (add a `PackageVersion` in `Directory.Packages.props` — never `Version=` on the reference); entity `ImportJob` (`FileName`, `Status`, `RowCounts`, `ErrorReportPath`, `ImportedBy`); `ImportRowError` for per-row diagnostics.
- API: `POST /api/v1/imports/patients` (multipart), `GET /imports/{id}/errors` (download the rejection report), `GET /imports/{id}/preview` (dry-run with the first N rows parsed and validated).
- **Import is a bulk-insert path around every existing handler-level authorization and validation** — it must go through the *same* domain factories (`Patient.Create`) and a scope check, not raw `AddRange`. Cache tags (`patient`, `doctor`, `service`) must be invalidated.
- Web: `Pages/Import.razor` — upload, preview, column-mapping, error download.
- Tests: happy path, malformed workbook, duplicate/blank phone (`ContactInfo` regex), and an authorization test.

**Dependencies** — none. Excel import benefits from item 9 (bulk user creation) and item 6 (currency column in the template).
**Risks** — Backup: silent failure is the real risk; alert on staleness. Import: bulk data entry is the highest-volume source of PHI corruption — make preview + rejection report mandatory, and never auto-commit.

---

## 7. Recommended implementation order

**Phase 0 — Fix what is already broken (S, do before anything else)**
1. Fix the wrong demo passwords in `Login.razor:110,115,120` — the documented quick-login path is 100% broken.
2. Add ownership/role checks to the five unprotected invoice handlers + change `InvoiceController` to `StaffOnly` (§6.9).
3. Stop logging request objects (`LoggingBehaviour.cs:29`) and gate `GlobalExceptionHandler` detail on `IsDevelopment()`.
4. Wrap the reschedule overlap check in the same serializable transaction as create.

*Reasoning:* these are all **existing-feature defects**, not new scope. They cost days and remove live security exposure. Nothing new should be built on top of them.

**Phase 1 — Close the cheap, high-value gaps (S each, 1 week total)**
5. Server-side patient balance (§6.3) — reuses existing queries, removes the wrong client-side sums.
6. WhatsApp `wa.me` link (§6.4) — client-side only, one new component.
7. Medical alerts surfaced everywhere (§6.8) — data already exists, needs surfacing + a few flags.
8. Automatic backups (§6.11a) — ops task, can run in parallel with everything below.

*Reasoning:* items 5/7 are mostly retrieval and rendering. Item 6 is the only "wow" feature that costs under a day. Backups are independent and shouldn't be deferred to the end.

**Phase 2 — The clinical core (M, ~2 weeks)**
9. Odontogram (§6.1) — then treatment plans (§6.2).

*Reasoning:* the odontogram is the centrepiece of a dental system and everything clinical keys off it (items 2, 7, 10). Doing it before plans avoids modelling plan items twice.

**Phase 3 — Money gets real (M–L, ~3 weeks)**
10. Role/permission completion + user management (§6.9 part 2).
11. Dual currency + exchange rate (§6.6).
12. Doctor/owner financial dashboard + commission (§6.5).

*Reasoning:* **currency must precede reporting** — a dashboard that sums across currencies is worse than no dashboard. And the financial dashboard must land *behind* a correct permission model. Debt tracking (item 5, Phase 1) is the prerequisite data.

**Phase 4 — Expansive features (M each)**
13. Dental lab tracking (§6.10).
14. X-ray / before-after images (§6.7).
15. Excel import (§6.11b).

*Reasoning:* these are the largest and least-coupled. They can be parallelised or deferred entirely without blocking the rest. Images need a storage decision (§6.7 risks) that should not be rushed.

**Critical path:** Phase 0 → Phase 1 (item 5) → Phase 2 (item 9) → Phase 3 (item 11 → item 12).
Everything else floats.

---

## 8. Technical debt & problems noticed

### 🔴 High severity

1. **Invoice mutations have no authorization or ownership check.** `InvoiceController.cs:26` uses bare `[Authorize]`. `IssueInvoiceCommandHandler`, `UpdateInvoiceDraftCommandHandler`, `RefreshInvoiceDraftCommandHandler`, `AddInvoicePaymentCommandHandler`, `CreateInvoiceCommandHandler` and `GetAppointmentInvoiceBreakdown` do **not** inject `IUser` — only `GenerateAppointmentInvoiceCommandHandler` (`:26`) does. A logged-in **Doctor** can issue, edit and pay **any** invoice, including invoices they cannot even *read* (`GetInvoiceById` does scope). This directly contradicts the two-layer rule in `AGENTS.md`.

2. **Demo passwords in the login page are wrong.** `Login.razor:110,115,120` fill `Admin123!` / `Doctor123!` / `Reception123!`; the seeder creates `Admin!Pass123` / `Doctor!Pass123` / `Receptionist!Pass123` (`ApplicationDbContextInitialiser.cs:32-34`). All three "tap to fill" buttons fail. (`AGENTS.md` warns the two *sets* must stay in sync — this is a third, unsynchronised set.)

3. **Secrets are written to logs.** `Application/Common/Behaviours/LoggingBehaviour.cs:29` logs `{@Request}` — the whole DTO — for every MediatR request. `RefreshTokenQuery` carries the refresh token; `GenerateTokenQuery` carries the **password**. With Serilog's `Microsoft: Information` floor (`Api/appsettings.json:28`) these land in Seq. `UNSAFE_PASSWORD`/`Sensitive` masking or a request-type allow-list is required.

4. **Unhandled exceptions leak type + message to every caller.** `Api/Infrastructure/GlobalExceptionHandler.cs:21,23` sets `Type = exception.GetType().Name` and `Detail = exception.Message`, in **all** environments.

5. **`IdentityService.GetUserByIdAsync` throws instead of returning an error.** `Infrastructure/Identity/IdentityService.cs:84` `throw new InvalidOperationException(nameof(userId))`. The `IsError` branch in `RefreshTokenQueryHandler.cs:45-49` is unreachable, so a user deleted mid-refresh yields a 500 (with message, per #4).

6. **Reschedule double-booking race.** `CreateAppointmentCommandHandler` wraps its overlap check in `IsolationLevel.Serializable` (`:35`); `RescheduleAppointmentCommandHandler:42-49` runs the *same* read-then-write check with **no transaction**. The race the create path closed remains open on reschedule.

7. **Duplicate invoices possible.** `CreateInvoiceCommandHandler:65-74` does check-then-insert with **no transaction**, and `InvoiceConfiguration.cs:69` is a **non-unique** index on `AppointmentId`. Two concurrent requests → two invoices for one visit. `GenerateAppointmentInvoice` is protected; `CreateInvoice` is not.

### 🟠 Medium severity

8. **Client-side aggregations are silently wrong.** `Dashboard.razor:287-311` sums over `pageSize: 100`; `PatientDetails.razor:98,103` over `pageSize: 100`; `Invoices.razor:32,37` over `_pageSize = 10`. Any patient with >10 invoices, or a clinic with >100, shows an incorrect balance and incorrect revenue. This is the live form of defect D-09.

9. **Dashboard does 5 paged API calls on every load** and resolves names by linear scan in C# (`Dashboard.razor:336-349`) — O(n·m). No server aggregate endpoint exists.

10. **`PatientScope()` exposes the whole registry to every staff role** (`ClinicAuthorization.cs:51-54`), including full medical history, to receptionists. Documented as intentional (`AuthorizationIntegrationTests.cs:266`) but it means there is **no confidentiality tier**.

11. **No endpoint links a Doctor to an `AppUser`.** `AssignUser`/`UnassignUser` exist in the domain but are only called by the seeder. A doctor created through the UI gets **no `doctor_id` claim** → `CurrentUser.DoctorId` is null → `DoctorScope()` falls back to `Guid.Empty` (`ClinicAuthorization.cs:43`) → the doctor sees **zero** rows everywhere. Silent, confusing failure with no error message.

12. **Invoices with `AppointmentId == null` are invisible to restricted doctors.** `GetInvoicesQueryHandler:21-27` scopes via `i.AppointmentId != null && Appointments.Any(...)`. A doctor's own standalone invoices simply don't appear.

13. **FDI validation is triplicated** — `TreatmentRecord.IsValidToothNumber` (domain), `CreateTreatmentRecordCommandValidator:27-40`, `UpdateTreatmentRecordCommandValidator:30-43`. Divergence risk. The `Contracts` `[Range(11,85)]` is *looser* than the real rule (accepts 19, 29, …).

14. **No `IValidator` for `UpdateInvoiceDraftCommand` / `RefreshInvoiceDraftCommand`.** Validation happens ad-hoc in the handlers — inconsistent with every other feature.

15. **`GetPatientsQuery` has no `SearchTerm` length cap** (other query validators cap at 100). It is interpolated raw into the cache key (`GetPatientsQuery.cs:15`), so an attacker can flood the HybridCache with unbounded distinct keys. `GetPatientsRequest` has no `[StringLength]` either.

16. **`GetPatientsQueryHandler:42-49` search is inconsistent** — names are lowercased before `Contains`, `PrimaryPhone.Contains` is neither lowercased nor case-insensitive.

17. **Dead configuration — 7 of 15 `AppSettings` properties are never read**: `OpeningTime`, `ClosingTime`, `MaxSpots`, `DefaultAppointmentDurationInMinutes`, `LocalCacheExpirationInMins`, `DistributedCacheExpirationMins`, `DefaultPageNumber`, `DefaultPageSize` (`Infrastructure/AppSettings/AppSettings.cs`). Controllers hard-code `= 1` / `= 10`. `AppSettings:RateLimiting` is also **absent from `appsettings.json`**, so limits silently fall back to class defaults.

18. **`"DoctorOnly"` policy is registered but applied to nothing** (`Infrastructure/DependencyInjection.cs:124`). Dead config.

19. **`Base` `appsettings.json` ships `TrustServerCertificate=True` and `Integrated Security=True`** (`:2-4`). The base file is the default for every non-Development environment that doesn't override — a production override that forgets the connection string disables TLS chain validation. `Issuer`/`Audience` are the literal `"localhost"`.

20. **`IAppTransaction.RollbackAsync` is never called.** Every handler relies purely on `await using` disposal. Correct today, but the advertised rollback path is untested and unexercised.

21. **`AppSettings:MaxSpots = 4`** suggests an intended capacity/concurrency limit that was never implemented — a magic number with no consumer and no explanation.

### 🟡 Low severity / hygiene

22. **Hardcoded clinic identity.** `"Al Noor Dental" / "عيادة النور"` is in `MainLayout.razor:30`, `Login.razor:15`, and every `<PageTitle>` (defect D-15). No `ClinicSetting` entity.

23. **`CreateAppointmentRequest.Notes` defaults to the literal string `"Routine checkup"`** (`Contracts/Requests/Appointments/CreateAppointmentRequest.cs:25`) — a real note is silently fabricated if the client omits it.

24. **Notifications bell is a dead button** (`MainLayout.razor:109-111`), and `DispatchDomainEventsInterceptor` has **zero subscribers** despite 5 appointment domain events being raised. The whole domain-event pipeline is currently dead weight (defect D-16).

25. **No invoice number.** UI shows `#@invoice.Id.ToString()[..8]` in three places (defect D-11).

26. **Theme/language/sidebar preferences are not persisted.** `ClinicSessionState` is scoped and in-memory; refresh resets everything (related to D-14).

27. **`GlobalExceptionHandler` is registered but `AppSettings:OpeningTime/ClosingTime` etc. aside, `AddValidation()` is an empty method** (`Api/DependencyInjection.cs:192-195`).

28. **Two files are in the global namespace**, breaking the project convention: `Application/Common/Behaviours/ValidationBehaviour.cs` and `Application/Features/Invoices/Commands/AddInvoicePayment/AddInvoicePaymentCommandHandler.cs`.

29. **Stale Prometheus config** — `containers/prometheus/prometheus.yml` scrapes job `mechanicshop-api` / target `api:80`; the actual compose service is `dentalclinic-api`. The scrape silently fails (leftover from the upstream template — defect D-12).

30. **`Microsoft.EntityFrameworkCore.Sqlite` is referenced by both Testcontainers projects** (`Directory.Packages.props:20`) but neither uses it — leftover from a prototype.

31. **`Directory.Packages.props` comment headers are scrambled** — `Asp.Versioning.*` sits under `<!-- FluentValidation -->`, `StyleCop`/`NSubstitute` under `<!-- EF Core -->`, and `<!-- UnitTests -->` is duplicated.

32. **README is materially wrong**: claims "380/380 tests" (actual: 408 `[Fact]` + 48 `[Theory]`), ports `7198` (actual `5004`/`5048`), "Repository & Unit of Work" (there are no repositories), and omits `DentalClinic.Web` from the solution tree entirely. `AGENTS.md` claims CI "asserts `docker info` first" — **`.github/workflows/build-and-test.yml` has no such step**, so the Testcontainers suites' passing in CI is unverified (this is exactly what `docs/product/OPEN-QUESTIONS.md` Q10 flags as BLOCKING).

33. **`docs/product/PRD.md` §B.4 defect list is stale.** D-01/D-03/D-04/D-05/D-07/D-08/D-14 have been remediated in code. Conversely `AGENTS.md`'s claim that soft-delete filters are *missing* on `TreatmentRecord` and `Payment` is stale — both now have filters; the actual gap is `InvoiceLine`.

### Demo-data observations

- The seeded demo appointment **does** carry a service (`ApplicationDbContextInitialiser.cs:244` passes `services[0].Id`). However `Appointment.ServiceId` is **nullable** (`20261014103922_MakeAppointmentServiceOptional`), and the UI explicitly offers "— No service —" (`Appointments.razor:418-419`), so service-less appointments are a supported, reachable state — they simply cannot be invoiced on their own (`BuildLines` produces no service line; `NoBillableItems` if there are also no positive-cost treatments).
- Seed data is thin: **2 doctors, 5 services, 2 patients, 1 appointment, 0 invoices, 0 payments, 0 treatments** (`ApplicationDbContextInitialiser.cs:148-251`). Any financial or reporting feature (§6.5) will look broken until realistic demo data exists.
- Seeding is gated on `ASPNETCORE_ENVIRONMENT=Development` (`Api/Program.cs:24,39`) — but `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Development` for the API container, so **compose always runs migrations + demo seeding**, and always uses the ephemeral JWT secret (every restart invalidates all tokens).
- `docker-compose.yml` also exposes Seq with `SEQ_FIRSTRUN_NOAUTHENTICATION=true` and bind-mounts real log files from `containers/seq/Logs/` into the repo.