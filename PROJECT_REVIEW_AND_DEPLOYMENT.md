# DentalClinic — Senior Review, Remaining Gaps, and Server Deployment Plan

| Field | Value |
|---|---|
| Review date | 2026-11-08 |
| Baseline | `main` @ `e82b46b` (clean working tree) |
| Method | Read the source, ran the build and all five test suites, cross-read `AUDIT.md`, `NEXT_PHASE_PLAN.md`, `README.md`, `AGENTS.md`, `docs/product/*` |
| Verdict | **Feature-complete for a single-clinic pilot, but NOT deployable to a public server as-is.** The remaining blockers are deployment/bootstrap/security, not features. |

---

## 0. Bottom line

1. **All 11 target features in `AUDIT.md` §5 are now implemented** — including the last two (financial dashboard and dual currency) that the AUDIT snapshot still lists as missing. The code moved past the document.
2. **Verified green:** build **0 errors / 57 warnings**, tests **1,065 passing** — Domain 292, Application 105, Web 69, Subcutaneous 341, Integration 258 (including the migration replay).
3. **The product base is sound.** Two-layer authorization, Result flow, scope-aware caching, serializable money paths, hashed refresh tokens, soft delete, audit interceptors, health endpoints, migrations, CI — all present.
4. **What is left is not "more features".** It is:
   - a production bootstrap path (first admin, roles, clinic profile),
   - migrations and secrets on a real server,
   - TLS / reverse proxy / forwarded headers,
   - backup off-box + a rehearsed restore,
   - a handful of honest product gaps (audit log, refunds/tax/discounts, real notification delivery, calendar, multi-tenant).
5. **`NEXT_PHASE_PLAN.md` is correctly marked superseded, but its successor `dentalclinic-next-phase.md` does not exist**, and `AUDIT.md` still says "9 complete · 2 missing". The docs are now behind the code and need a truth-up (see §3).

**Recommendation:** freeze feature work for one short hardening phase (§5), deploy to a staging server, run the go-live checklist (§5.5), then resume product work on the order in §6.

---

## 1. Verification evidence

### 1.1 Build and tests (run in this review)

| Suite | Result | Notes |
|---|---|---|
| `dotnet build DentalClinic.slnx` | **0 errors, 57 warnings** | Warning baseline in `AGENTS.md` says ~28; it has grown to 57 (mostly StyleCop + obsolete Testcontainers ctor). Not a blocker, but worth trimming before a release branch. |
| `DentalClinic.Domain.UnitTests` | **292 passed** | pure, fast |
| `DentalClinic.Application.UnitTests` | **105 passed** | pure |
| `DentalClinic.Web.Tests` | **69 passed** | pure |
| `DentalClinic.Application.SubcutaneousTests` | **341 passed** | Testcontainers SQL, Docker required |
| `DentalClinic.Api.IntegrationTests` | **258 passed** | Testcontainers SQL + migration replay |

Total: **1,065 tests**, matching the claim in `README.md`.

### 1.2 Repository shape

- **16 API controllers** (`Patients`, `Doctors`, `DentalServices`, `Appointments`, `TreatmentRecords`, `TreatmentPlans`, `Odontograms`, `PatientImages`, `Invoices`, `LabCases`, `Imports`, `StaffUsers`, `Reports`, `Clinic`, `Identity`).
- **19 Web pages** (Dashboard, Patients, PatientDetails, Appointments, Treatments, TreatmentPlans, Invoices, InvoicePrintSheet, PlanPrintSheet, LabCases, Staff, Settings, Import, Doctors, Services, Login, Error, NotFound).
- **15 migrations**, ending at `20261107212526_AddImportJobs`, plus snapshot. `MigrationTests` replays the chain.
- **49 commits** since the AUDIT baseline, ~35k lines added across 346 files.

---

## 2. What has actually been delivered

### 2.1 Feature matrix against the original target list

| # | Target | Status | Evidence / caveat |
|---|---|---|---|
| 1 | Interactive odontogram | **Complete** | `PatientTooth`, `OdontogramController`, per-tooth state + history, interactive chart in `PatientDetails.razor`. `TreatmentRecord` stays the append-only procedure log. |
| 2 | Treatment plans + printable sheet | **Complete, one caveat** | `TreatmentPlan` aggregate + items-as-records, `TreatmentPlanController`, perform/cancel transitions, session totals, `PlanPrintSheet.razor`. **Caveat:** the print sheets render the clinic name/address but **never render `Branding.LogoUrl`** — there is no `<img>` and no `wwwroot/branding/` asset. "Printable sheet with logo" is therefore slightly overstated. |
| 3 | Patient balance & debt | **Complete** | `GET /invoices/summary` and `GET /invoices/outstanding-debts`, `InvoiceSummaryReader`, server-side aggregates over the full set, drafts excluded from totals. No aging buckets. |
| 4 | WhatsApp reminders | **Complete, one caveat** | `WhatsAppMessageBuilder` + `WhatsAppButton`; phone normalisation and URL encoding are unit-tested. **Caveat:** the builder still hardcodes `"Al Noor Dental"` and **ignores `ClinicSetting.DefaultCountryCode`**; a local-format number (`09…`) is not upgraded to international format. |
| 5 | Financial dashboard | **Complete** | `GET /reports/dashboard` computes every figure in SQL in the clinic timezone; `Dashboard.razor` and `Reports.razor` consume it. `Revenue`, `CashCollected` and `Outstanding` are deliberately distinct. |
| 6 | Dual currency + editable rate | **Complete as display conversion, not multi-currency accounting** | `ClinicSetting.BaseCurrency/SecondaryCurrency`, append-only `ExchangeRate`, `ClinicFormatting.Money(amount, currency)`. Invoices/payments store bare `decimal(18,2)` in the base currency — no per-invoice currency code. Fine for "show USD and SYP side by side"; not sufficient if the clinic actually *transacts* in two currencies. |
| 7 | X-ray / before-after images | **Complete** | `PatientImage`, `IImageStorage` on local disk outside `wwwroot`, MIME allow-list + magic-byte sniff + 10 MB cap, authenticated content endpoint, gallery fetched as data URLs. Not launched from the odontogram; no malware scanner. |
| 8 | Medical alerts | **Complete** | `MedicalAlertEvaluator` derives severity server-side (including pregnancy and BP), badge on patients list, patient file, appointment drawer, dashboard counter. Allergies remain free text. |
| 9 | Role-based permissions | **Complete with residual gaps** | Three roles, policies, handler-level ownership, Admin-only `StaffUserController`, doctor↔account linking, Web `Staff` page. **No `ClinicPermission` matrix** (roles + ownership instead) and **no audit log** — the owner still cannot answer "who changed this?" |
| 10 | Dental lab tracking | **Complete** | `LabCase` with explicit transition map, overdue computed at query time, `LabCases.razor`. Lab cost is **not** auto-added to invoices. |
| 11 | Backups + Excel import | **Complete, ops partly open** | SQL Agent nightly backup + sidecar + runbook; ClosedXML all-or-nothing patient import with preview/mapping/CSV rejection report. Off-box copy, staleness alert and a rehearsed restore are still open. |

### 2.2 Security controls already implemented (AUDIT §8 "Phase 0" items)

These were high-severity defects in the original audit and are **fixed in the current code** — do not "re-fix" them:

- `InvoiceController` is now `[Authorize(Policy = "StaffOnly")]` and every invoice mutation checks ownership through `CanAccessInvoiceAsync`.
- `LoggingBehaviour` logs a **redacted** request via `SensitiveDataRedactor`, not the raw DTO.
- `GlobalExceptionHandler` returns a generic detail outside Development; exception type/message only in Development.
- `IdentityService.GetUserByIdAsync` returns `ApplicationErrors.UserNotFound` instead of throwing.
- `RescheduleAppointmentCommandHandler` now wraps its overlap check in `IsolationLevel.Serializable`.
- `CreateInvoiceCommandHandler` uses a serializable transaction and `Invoice.AppointmentId` has a **unique filtered index** — the duplicate-invoice race is closed.
- Login demo buttons now use the real seeded passwords (`Admin!Pass123`, etc.).
- Prometheus job name is now `dentalclinic-api` (was `mechanicshop-api`).

### 2.3 Data/schema

17 entity configurations, 15 migrations, money columns at `decimal(18,2)`, `Invoice.RowVersion` concurrency token, `InvoiceNumberCounters` under `UPDLOCK, HOLDLOCK`, soft-delete filters on every `AuditableEntity` except the deliberately unfiltered `InvoiceLine` and the singleton `ClinicSetting`.

---

## 3. Documentation reconciliation (what is stale)

| Document | Current claim | Reality | Action |
|---|---|---|---|
| `AUDIT.md` | "Current summary: 9 Complete · 2 Missing — remaining: 5 financial dashboard, 6 dual currency" | Items 5 and 6 shipped in `0271137` and `cfd8bd4` | Update §5 status refresh: **11/11 complete**, with the caveats in §2.1 |
| `AUDIT.md` §8 | Several "high severity" items listed as open | Items 1–7 are fixed (see §2.2) | Mark them resolved with commit references |
| `NEXT_PHASE_PLAN.md` | "Superseded… the current plan is `dentalclinic-next-phase.md`" | That file **does not exist**; only the historical plan is present | Either create the successor or point at this document |
| `NEXT_PHASE_PLAN.md` header | "This file is intentionally gitignored" | It is **tracked** (`git ls-files` shows it; the `.gitignore` line is commented out) | Fix the statement or untrack/ignore it deliberately |
| `README.md` | Test counts 292/105/69/341/258 = 1,065; ports 5004/7204 | Verified correct | No change |
| `README.md` | "production-ready" | Not until §5 is done | Soften to "pilot-ready" or finish §5 |
| `AGENTS.md` | "~28 warnings baseline" | 57 warnings now | Update the baseline or clean them |
| `AGENTS.md` | "`InvoiceLine` is the only `AuditableEntity` without a query filter" | `ClinicSetting` also has none, deliberately | Correct the wording |
| `docs/product/PRD.md` | WhatsApp, images, lab tracking, import are explicit non-goals / out of scope | All four are implemented | Mark the non-goal list as superseded |
| `docs/product/MVP-ROADMAP.md` | Phase 9 restore rehearsal "pending" | Still true | Keep; track it as an ops task |

---

## 4. What is still genuinely missing or imperfect

Ordered by impact. This is the honest delta after the feature work.

### 4.1 Product / correctness gaps

| # | Gap | Why it matters | Effort |
|---|---|---|---|
| 1 | **No audit log** | An owner cannot investigate who changed a patient, invoice or clinical record. `AUDIT.md` §1.2 listed this alongside user administration; user admin shipped, the audit trail did not. | M |
| 2 | **No discounts, tax, refunds, credit notes or invoice voiding** | Real clinics need corrected bills. Today `BuildLines` only emits service price + performed treatment costs; an issued invoice cannot be voided or refunded (only paid). | M–L |
| 3 | **Dual currency is display-only** | If the clinic accepts payment in both currencies, the ledger cannot record which currency a payment was taken in. Historical conversion is a display rate, not an accounting fact. | M |
| 4 | **Plan print sheet has no logo** | `Branding.LogoUrl` is stored and editable but never rendered; no logo file is served. | S |
| 5 | **WhatsApp builder ignores clinic country code and clinic name** | `DefaultCountryCode` exists in `ClinicSetting` but is unused; the message hardcodes "Al Noor Dental". Local-format numbers will not link correctly. | S |
| 6 | **No calendar view** | Appointments are a table only; no day/week grid. | M |
| 7 | **No real notification delivery** | WhatsApp is a `wa.me` convenience link, not a reminder system. No email/SMS, no background sender, no delivery status. | M–L |
| 8 | **Standalone invoices invisible to a restricted doctor** | `GetInvoicesQueryHandler` scopes on `i.AppointmentId != null && Appointments.Any(...)`. A doctor's own invoice with no appointment simply does not appear. | S–M |
| 9 | **`PatientScope()` gives every staff role the full registry** | Receptionists can read full medical history. Documented as intentional, but there is no confidentiality tier. | M (decision first) |
| 10 | **No payment method / payer / currency on `Payment`** | Cash vs card vs transfer cannot be reported. | S |
| 11 | **`CreateAppointmentRequest.Notes` defaults to `"Routine checkup"`** | A real note is fabricated if the client omits it. | S |
| 12 | **`GetPatientsQuery` search term has no length cap** and is interpolated raw into the cache key | Other query validators cap at 100; this one can flood HybridCache with distinct keys. | S |
| 13 | **Dead configuration** | `OpeningTime`, `ClosingTime`, `MaxSpots`, `DefaultAppointmentDurationInMinutes`, `LocalCacheExpirationInMins`, `DistributedCacheExpirationMins`, `DefaultPageNumber`, `DefaultPageSize` are never read; `AppSettings:RateLimiting` is absent from `appsettings.json` so defaults apply silently. | S |
| 14 | **`"DoctorOnly"` policy is registered but never applied** | Dead config; doctor scoping is ownership-based instead. | S (delete or document) |
| 15 | **`IAppTransaction.RollbackAsync` is never called** | Correct today via disposal, but the advertised path is untested. | S |
| 16 | **Theme/language/sidebar and auth session are not persisted (D-14)** | A browser refresh logs the user out and resets UI preferences. Acceptable for a pilot, poor for daily use. | S–M |
| 17 | **No production-grade logging sink** | Seq runs with `SEQ_FIRSTRUN_NOAUTHENTICATION=true`; if exposed, anyone can read PHI-adjacent logs. | S (part of §5) |
| 18 | **Lab cost is not billable automatically** | Reported on the case only; staff must add a manual invoice line. | S |
| 19 | **No aging buckets on debt** | Only total outstanding and oldest unpaid date. | S |

### 4.2 Explicit non-goals (do not accidentally scope-creep)

Multi-tenant `ClinicId`, a full `ClinicPermission` matrix, online booking/patient portal, e-invoicing, payroll-grade commission, and a native mobile app. These are large and not required for a single-clinic pilot.

---

## 5. Deploying to a real server — what to edit

> **Do not simply change `ASPNETCORE_ENVIRONMENT` to `Production` and run the existing compose file.** In Production the app deliberately does **not** migrate, does **not** seed, and **throws at startup without a JWT secret**. You will get a running container with an empty database, no users and no way to create the first admin. Follow the order below.

### 5.1 P0 blockers (must be fixed before the first deploy)

| # | Blocker | Current state | Fix |
|---|---|---|---|
| 1 | **No first-admin bootstrap** | `ApplicationDbContextInitialiser.InitialiseDatabaseAsync()` is only called in Development (`Program.cs:39`); `TrySeedAsync()` creates roles, clinic profile, demo users **and demo data** together. In Production there is no way to create the first Admin, and `StaffUserController` is Admin-only (chicken-and-egg). | Split seeding into `SeedSystemDataAsync()` (roles + clinic profile) and `SeedDemoDataAsync()` (patients/doctors/appointments). Call the system part in every environment. Add an env-gated one-shot admin bootstrap (see §5.4). |
| 2 | **Migrations do not run in Production** | `Program.cs:39` calls `InitialiseDatabaseAsync()` only under `IsDevelopment()`. The runtime image has no SDK, so `dotnet ef` cannot run inside it. | Generate an **idempotent SQL script** in CI (`dotnet ef migrations script --idempotent`) and apply it with a one-shot `migrate` service / `sqlcmd`, **or** ship an EF migration bundle. See §5.4. |
| 3 | **JWT secret is required but not supplied** | `JwtSecretProvider` throws outside Development when `JwtSettings:Secret` is empty. Compose sets no secret. | Supply `JwtSettings__Secret` from `.env` / Docker secret / vault. Changing it invalidates all refresh tokens — treat it as a long-lived secret. |
| 4 | **Compose runs Development** | `docker-compose.yml:11` and `:42` set `ASPNETCORE_ENVIRONMENT=Development`. Consequences: migrations + full demo seed (demo users/passwords, demo patients) on every start, **ephemeral JWT secret** (every restart logs everyone out), Swagger/Scalar exposed, exception details in responses. | Use a `docker-compose.prod.yml` override with `Production`. Never run the demo seeder against real data. |
| 5 | **No TLS / reverse proxy / forwarded headers** | API and Web are plain HTTP on `5004:80` and `7204:80`; `UseHttpsRedirection()` exists but there is no HTTPS endpoint and no `UseForwardedHeaders`. | Put Caddy / Nginx / Traefik in front, terminate TLS, proxy to the two containers. Enable forwarded headers in both apps (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` or explicit `UseForwardedHeaders`). Blazor Server needs WebSockets; enable them on the proxy. |
| 6 | **Secrets are in git** | `docker-compose.yml:13` has the `sa` password, `:56` `SA_PASSWORD`, `:120` Grafana admin password; Seq runs with no auth and ports `8081`/`5342` are published; SQL is published on `1434`. | Move every secret to `.env` (already gitignored) or Docker secrets. Use a dedicated least-privilege SQL login, not `sa`. Stop publishing SQL/Seq/Grafana/Prometheus ports publicly (bind to `127.0.0.1` or internal network only). Enable Seq auth and API keys; change the Grafana password. |
| 7 | **No clinic profile row in Production** | The `AddClinicSettings` migration only creates the table; the singleton row is inserted by the Development seeder. | Add an `InsertData` migration **or** seed `ClinicSetting` in the system seed. Otherwise `GET /clinic/settings` returns 404, branding falls back to hardcoded defaults, and invoice numbering uses the host timezone. |
| 8 | **Host timezone is wrong for the clinic** | `Dockerfile` defaults `TZ=America/Montreal`; the seed uses `TimeZoneInfo.Local.Id`. | Set `TZ=Asia/Damascus` (or the clinic's zone) in the production env **before** the first seed, and confirm `ClinicSetting.TimeZoneId` after bootstrap. Day windows and invoice years depend on it. |
| 9 | **Backups are host-local and unproven** | Nightly `.bak` on the same host; no off-box copy, no staleness alert, no rehearsed restore; images volume not backed up. | Complete `docs/operations/BACKUP-RESTORE.md` §3: off-box copy, staleness alert, run and record one restore, back up `dentalclinic-images`. |

### 5.2 P1 hardening (do before real patients, can follow first boot)

| # | Item | Current state | Fix |
|---|---|---|---|
| 1 | `/metrics` is not mapped | `AddPrometheusExporter()` is registered, but there is no `MapPrometheusScrapingEndpoint()` / `MapMetrics()` call. Prometheus scrapes `dentalclinic-api:80/metrics` and gets a 404. | Map the endpoint (and `UseHttpMetrics` if required by the package), then verify `curl /metrics` returns 200. |
| 2 | DataProtection keys are ephemeral | No `AddDataProtection` / key persistence. Antiforgery tokens (Web) and any protected payloads break across container recreation; scaling out is impossible. | Persist the key ring to a mounted volume (`PersistKeysToFileSystem`) or a shared store. |
| 3 | Containers run as root | `Dockerfile` has no `USER`. | Run as the built-in `app` user; ensure `/app/App_Data/images` is writable (volume ownership) and the app does not need root. |
| 4 | No container healthchecks | Compose `depends_on` has no `condition`. | Add `HEALTHCHECK` (or compose `healthcheck`) for API `/health/ready` and Web `/`; use `depends_on: condition: service_healthy` / `service_completed_successfully`. |
| 5 | CORS origins are localhost-only | `appsettings.json:18`. | Set the real Web origin(s) in `appsettings.Production.json`. |
| 6 | Rate limiter config is implicit | `AppSettings:RateLimiting` is absent from `appsettings.json`; class defaults apply. | Add explicit `GeneralPermitLimit` / `AuthPermitLimit` values for production. |
| 7 | Session is in-memory (D-14) | `ClinicSessionState` is scoped; refresh logs out. | Persist tokens in `ProtectedSessionStorage` / a cookie, or accept re-login for the pilot. If scaling >1 replica, also add a SignalR backplane + sticky sessions. |
| 8 | Web app has no health endpoint | Only the API maps `/health` and `/health/ready`. | Add a trivial health endpoint to the Web app, or probe `/`. |
| 9 | Seq/Grafana/Prometheus are open | No auth, default password, published ports. | Enable auth, bind to localhost, or keep them on an internal network. If Seq auth is enabled, add `Serilog__WriteTo__1__Args__apiKey` and `OTEL_EXPORTER_OTLP_HEADERS=X-Seq-ApiKey=…`. |
| 10 | `AllowedHosts` / JWT issuer/audience are placeholders | `AllowedHosts: "*"`; `Issuer`/`Audience` = `"localhost"`. | Set real hostnames in production config. |
| 11 | Base connection string is development-shaped | `appsettings.json:3` uses `Integrated Security=True; TrustServerCertificate=True`. | Override with a SQL login and `Encrypt=True` (keep `TrustServerCertificate=True` only if the internal certificate is genuinely self-signed, and document it). |
| 12 | No image publishing / CD | CI only builds and tests. | Add a job to build/push the two images (tag by commit SHA) and a deploy job. Keep the deployable artifact immutable. |

### 5.3 File-by-file edit list

| File | Change |
|---|---|
| `docker-compose.prod.yml` **(new)** | `ASPNETCORE_ENVIRONMENT=Production`; all secrets from `${...}`; `JwtSettings__Secret`; `TZ=Asia/Damascus`; `ApiBaseUrl=http://dentalclinic-api`; no host ports for SQL/Seq/Grafana/Prometheus (or bind `127.0.0.1`); `restart: unless-stopped`; healthchecks; `depends_on` conditions; `sql-backup-init` unchanged; a one-shot `migrate` service. |
| `.env.example` **(new)** | Document every variable: `SA_PASSWORD`, `DB_PASSWORD`, `JWT_SECRET`, `GRAFANA_ADMIN_PASSWORD`, `SEQ_ADMIN_PASSWORD`, `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD`. Never commit `.env`. |
| `docker-compose.yml` | Keep as the developer/demo file (it is useful). Do **not** use it on a server. If you prefer one file, replace the hardcoded secrets with `${...}` and flip the environment. |
| `Dockerfile` | Set `ENV ASPNETCORE_URLS=http://+:80`; change the default `TZ`; create `/app/App_Data/images`; add `USER app` (with correct ownership); optionally add a `HEALTHCHECK`. |
| `src/DentalClinic.Api/appsettings.Production.json` **(new)** | Real CORS origins, JWT issuer/audience, rate limits, Serilog sink with API key, optional `Clinic` fallback section (`Name`, `NameArabic`, `BaseCurrency`, `TimeZoneId`). **No connection string, no secret.** |
| `src/DentalClinic.Api/Program.cs` | Add forwarded headers (`UseForwardedHeaders` or rely on `ASPNETCORE_FORWARDEDHEADERS_ENABLED`); map the metrics endpoint; optionally add an explicit `--migrate` / `--bootstrap-admin` mode (see §5.4); keep `/health` and `/health/ready` mapped in all environments. |
| `src/DentalClinic.Api/DependencyInjection.cs` | If not using the env var, add `UseForwardedHeaders` before `UseHttpsRedirection` (`DependencyInjection.cs:240`). Review `AllowedOrigins` binding. |
| `src/DentalClinic.Web/Program.cs` | Add forwarded headers; persist DataProtection keys; persist session (or document the refresh limitation); set `ApiBaseUrl` from env (`:16`); add a health endpoint. |
| `src/DentalClinic.Infrastructure/Data/ApplicationDbContextInitialiser.cs` | Split `TrySeedAsync` (`:78`) into system data (roles + clinic profile) and demo data. Call system seeding in Production; never call demo seeding outside Development. Add the env-gated first-admin creation. |
| `src/DentalClinic.Infrastructure/Data/Migrations/*` | If you cannot seed the clinic row in code, add a small `InsertData` migration. Do not edit an existing reviewed migration. |
| `.github/workflows/build-and-test.yml` | Add image build/push and an idempotent migration script artifact. Optionally add `dotnet test --collect:"XPlat Code Coverage"`. |
| `docs/operations/BACKUP-RESTORE.md` | Add the off-box copy job, the staleness alert, the restore rehearsal record, and the images-volume backup. |
| `README.md` / `AUDIT.md` / `NEXT_PHASE_PLAN.md` | Truth-up per §3. |

### 5.4 Production bootstrap and migration sequence

**Option A (recommended):** add two explicit modes to the API entry point.

```text
dotnet DentalClinic.Api.dll --migrate            # apply migrations and exit
dotnet DentalClinic.Api.dll --bootstrap-admin    # create roles + first admin from env, then exit
dotnet DentalClinic.Api.dll                      # normal server
```

Environment variables used only during bootstrap:

```text
Bootstrap__AdminEmail=owner@clinic.example
Bootstrap__AdminPassword=<strong, 12+ chars, complexity>
Bootstrap__ClinicName=...
Bootstrap__ClinicNameArabic=...
Bootstrap__TimeZoneId=Asia/Damascus
Bootstrap__BaseCurrency=USD
```

Remove `Bootstrap__*` from the environment after the first successful run.

**Option B (no code change):** build the idempotent SQL script in CI and apply it with a one-shot container:

```powershell
dotnet ef migrations script --idempotent `
  --project src/DentalClinic.Infrastructure `
  --startup-project src/DentalClinic.Api `
  --output artifacts/migrate.sql
```

Then run `sqlcmd -i /scripts/migrate.sql` in a `migrate` service. This still does **not** solve the first-admin problem — you would have to create the admin out-of-band (not recommended, because the Identity password hash format is an implementation detail). Use Option A for the admin.

**Order of operations on a fresh server:**

1. Provision host, DNS, TLS certificate, firewall. Only 80/443 public.
2. Create `.env` from `.env.example`; generate a long random `JWT_SECRET`; set strong SQL/Grafana/Seq passwords.
3. `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d sqlserver sql-backup-init`
4. Wait for SQL readiness, then run the `migrate` service (or the `--migrate` mode).
5. Run `--bootstrap-admin` once with the `Bootstrap__*` variables; remove them.
6. Start `dentalclinic-api` and `dentalclinic-web`.
7. Sign in as the bootstrap admin. In **Settings**, set:
   - clinic name (EN/AR), address, phone, email;
   - timezone (e.g. `Asia/Damascus` — verify it is a valid IANA id);
   - base currency (e.g. `USD`) and optional secondary currency (e.g. `SYP`);
   - default country code (e.g. `963`);
   - the first exchange rate if a secondary currency is used.
8. Create doctors/services, then link each doctor account to its profile (`Staff` page / `PUT /doctors/{id}/user-link`). **A doctor with no linked account sees no rows.**
9. Run the smoke pass (§5.5).
10. Verify the backup job ran and record a restore rehearsal.

### 5.5 Go-live checklist

- [ ] `docker compose ... config` resolves with no hardcoded secret.
- [ ] API starts with `ASPNETCORE_ENVIRONMENT=Production` and a real `JwtSettings__Secret`.
- [ ] `GET /health` returns 200; `GET /health/ready` returns 200 with `database: Healthy`.
- [ ] `GET /metrics` returns 200 (Prometheus target is UP).
- [ ] HTTPS works; HTTP redirects; HSTS present; WebSockets (Blazor circuit) connect through the proxy.
- [ ] Sign in as Admin; refresh the browser and confirm the expected session behaviour (D-14 is documented).
- [ ] Create a patient, book an appointment, complete it, generate a draft invoice, issue it, take a partial payment, print it.
- [ ] Create a treatment plan over two sessions, perform one item, confirm the invoice contains only the performed item, print the plan.
- [ ] Upload an X-ray; confirm it renders and is stored outside `wwwroot`.
- [ ] Create a lab case and move it through the transition map; confirm overdue highlighting.
- [ ] Import a small valid workbook; import an invalid one and confirm zero patients are written plus a rejection CSV.
- [ ] Sign in as Doctor; confirm they see only their own appointments/invoices and cannot mutate another doctor's data.
- [ ] Sign in as Receptionist; confirm they cannot write clinical records.
- [ ] Confirm Seq/Grafana/Prometheus are not reachable from the public internet.
- [ ] Confirm the nightly backup produced a `.bak`; copy it off-host; restore it into a scratch DB and spot-check counts.
- [ ] Confirm the images volume is included in the backup plan.

---

## 6. What to do next (recommended order)

### Phase D0 — Deployment blockers (1–3 days)

1. Split system seeding from demo seeding; add the first-admin bootstrap (§5.1 #1).
2. Add the production compose override and `.env.example` (§5.1 #4, #6).
3. Add the migration step and the idempotent script (§5.1 #2).
4. Set `JwtSettings__Secret`, `TZ`, and the clinic profile (§5.1 #3, #7, #8).
5. Put TLS + reverse proxy + forwarded headers in front (§5.1 #5).

**Exit:** a staging server boots from a clean database with a real admin and no demo data.

### Phase D1 — Production hardening (2–4 days)

1. `/metrics` + Prometheus verification; Seq auth; Grafana password; close ports.
2. DataProtection key persistence.
3. Non-root containers; healthchecks; `depends_on` conditions.
4. CORS, rate limits, issuer/audience, `AllowedHosts`.
5. Session persistence (D-14) or a documented pilot limitation.
6. Trim the 57 warnings back toward the baseline.

**Exit:** the go-live checklist passes end to end.

### Phase D2 — Operations (1–2 days)

1. Off-box backup copy + staleness alert.
2. Restore rehearsal, recorded.
3. Images-volume backup.
4. Basic alerting (error rate, health, backup age).

**Exit:** a documented, rehearsed recovery path.

### Phase P1 — Product gaps that matter after launch (2–4 weeks)

1. **Audit log** (who changed what) — the strongest trust gap.
2. **Billing corrections:** discounts, tax, void/credit note, refund, payment method.
3. **Dual-currency accounting** if the clinic transacts in two currencies (per-invoice/payment currency).
4. **Plan logo + WhatsApp country code** — small, visible fixes.
5. **Calendar view** for appointments.
6. **Real notification delivery** (email/SMS/WhatsApp API) if the clinic wants reminders, not just a link.
7. **Aging buckets** and a proper debt report.
8. **Fix doctor visibility for standalone invoices** and the `PatientScope` confidentiality decision.

### Phase P2 — Deliberately deferred

Multi-tenant `ClinicId`, a real permission matrix, online booking/patient portal, e-invoicing, payroll-grade commission, mobile app.

---

## 7. Appendix

### 7.1 Verification commands

```powershell
dotnet build DentalClinic.slnx

dotnet test tests/DentalClinic.Domain.UnitTests --no-build
dotnet test tests/DentalClinic.Application.UnitTests --no-build
dotnet test tests/DentalClinic.Web.Tests --no-build
dotnet test tests/DentalClinic.Application.SubcutaneousTests --no-build   # Docker
dotnet test tests/DentalClinic.Api.IntegrationTests --no-build            # Docker + MigrationTests

dotnet run --project src/DentalClinic.Api      # http://localhost:5004, Scalar /scalar/v1 (Development only)
dotnet run --project src/DentalClinic.Web      # http://localhost:7204
```

Migration:

```powershell
dotnet ef migrations add <Name> --project src/DentalClinic.Infrastructure --startup-project src/DentalClinic.Api
```

### 7.2 Key file references

| Concern | File |
|---|---|
| API startup / migration gate / health | `src/DentalClinic.Api/Program.cs` |
| Middleware order / CORS / rate limiting / cache | `src/DentalClinic.Api/DependencyInjection.cs` |
| JWT secret failure mode | `src/DentalClinic.Infrastructure/Identity/JwtSecretProvider.cs` |
| Seeding (system + demo) | `src/DentalClinic.Infrastructure/Data/ApplicationDbContextInitialiser.cs` |
| Authorization rules | `src/DentalClinic.Application/Common/Security/ClinicAuthorization.cs` |
| Money concurrency | `Application/Features/Invoices/Commands/AddInvoicePayment`, `Domain/Invoices/Invoice.cs` |
| Report aggregates | `Application/Features/Reports/Queries/GetDashboardSummary` |
| Clinic settings / exchange rates | `Domain/Clinics/ClinicSetting.cs`, `ExchangeRate.cs`, `Api/Controllers/ClinicController.cs` |
| Deployment | `docker-compose.yml`, `Dockerfile`, `.github/workflows/build-and-test.yml` |
| Backup runbook | `docs/operations/BACKUP-RESTORE.md` |

### 7.3 Known-good but easy to break (do not regress)

- Result flow, not exceptions, for expected failures.
- Two-layer authorization: controller policy **plus** handler ownership.
- Scope baked into every cached query key; tag invalidation on every mutation.
- Serializable + `RowVersion` on payment and appointment/invoice creation.
- Migrations replayed by `MigrationTests`; never edit a reviewed migration.
- No configuration read during service registration.
- No `Version=` on `PackageReference`; use `Directory.Packages.props`.
- CRLF, 4-space indent, file-scoped namespaces, `_camelCase` private fields.
