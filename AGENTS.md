# AGENTS.md

.NET 10 dental-clinic system: CQRS REST API + Blazor Web App, Clean Architecture. Details below are verified against the code, not the README.

## Commands

```powershell
dotnet build DentalClinic.slnx                      # or: dotnet build src/DentalClinic.Api
dotnet test tests/DentalClinic.Domain.UnitTests     # no Docker needed
dotnet test tests/DentalClinic.Application.UnitTests
dotnet run --project src/DentalClinic.Api           # http://localhost:5004, Scalar at /scalar/v1 (Development only)
dotnet run --project src/DentalClinic.Web           # http://localhost:7204, calls API on :5004
docker compose up -d --build                        # API :5004, Web :7204, SQL :1434, Seq :5342/:8081, Prometheus :9090, Grafana :3000
```

EF migrations (the CLI needs both flags; there is no design-time factory):

```powershell
dotnet ef migrations add <Name> --project src/DentalClinic.Infrastructure --startup-project src/DentalClinic.Api
```

## Testing gotchas

- `DentalClinic.SubcutaneousTests` and `DentalClinic.Api.IntegrationTests` spin up **Testcontainers SQL Server** — Docker must be running or they hang/fail. The two `*.UnitTests` projects are pure and fast.
- Integration tests reset state with **Respawn** between tests (`ResetDatabaseAsync`); Respawn ignores `AspNetUsers`/`AspNetRoles`/`__EFMigrationsHistory`, so seeded identities persist across tests. Call `ResetDatabaseAsync` from a fresh scope, not inside a shared one.
- Integration tests build the schema with `EnsureCreatedAsync()`, but the app runs `MigrateAsync()`. `tests/DentalClinic.Api.IntegrationTests/MigrationTests.cs` covers that gap by replaying the real migration chain against the Testcontainers database — **keep it passing** when you add a migration, and don't switch the other fixtures to `MigrateAsync()` casually (they depend on `EnsureCreatedAsync` semantics).
- `tests/DentalClinic.Api.IntegrationTests/Common/WebAppFactory.cs` is declared in namespace `DentalClinic.Application.SubcutaneousTests.Common` (copy-paste from the other project). Don't "fix" the namespace casually — `AppHttpClient`/`WebAppFactory` are imported from the `DentalClinic.Api.IntegrationTests.Common` namespace, so both must be present.
- `tests/.../Tests.Common/Security/TestCurrentUser.cs` defaults to the `Admin` role. Tests that switch roles must reset it (`AsAdmin()`), since the instance is shared per collection fixture.
- Credentials live in `Tests.Common/Security/TestUsers.cs`; the app-side seed passwords live in `Infrastructure/Data/ApplicationDbContextInitialiser.cs`. They must stay in sync **and satisfy the Identity password policy** (`Admin!Pass123`, `Doctor!Pass123`, `Receptionist!Pass123`). A mismatch fails at `CreateAsync`, not at compile time.

## Build/CI facts

- `.github/workflows/build-and-test.yml` runs `restore → build -c Release --no-restore → test -c Release --no-build` on `ubuntu-latest`. It does **not** run `docker info` (an earlier claim here was wrong), so the Testcontainers suites only pass there because GitHub-hosted runners happen to ship Docker — verify that assumption if CI ever moves. If you add a Docker-dependent suite, no workflow change is needed.
- Package versions are centralized in `Directory.Packages.props` (`ManagePackageVersionsCentrally`). **Never** add `Version=` to a `PackageReference`; add a `PackageVersion` entry instead.
- `global.json` pins the SDK to `10.0.100` with `rollForward: latestFeature`. Bump it deliberately rather than letting the SDK float.
- `Directory.Build.props` sets `net10.0` for every project and injects `StyleCop.Analyzers` everywhere. The build is not warning-clean (~28 SA/AV/CS warnings) — this is the baseline, not something you introduced. Don't chase them unless you touched the file.
- **`dotnet ef` tools are 10.0.2 while the runtime is 10.0.12** and it prints a version-mismatch warning on every command. Harmless, but update the global tool if it ever misbehaves.
- `.editorconfig`: CRLF, 4-space indent, `insert_final_newline = false` (files deliberately end without a newline), block-scoped namespace *setting* but all real code uses file-scoped namespaces (severity is `silent`, no error). Private fields `_camelCase`, private statics `s_camelCase`. Using directives go **outside** the namespace, grouped (System first, then others, blank-line separated).
- Comments are a deliberate mix of English and Arabic. Match the surrounding file's language when editing.

## Architecture rules worth not breaking

- **Never read `IConfiguration` eagerly during service registration.** `WebApplicationFactory` queues `ConfigureAppConfiguration` callbacks and applies them when the host is *built*, so any value read inside `Program.Main` misses what a test host supplied. This broke every integration test once already. Read configuration *inside* a lazily-invoked callback (`AddJwtBearer(options => …)`, `AddRateLimiter(options => …)`) or resolve it from DI at use time (`JwtSecretProvider`).
- **Result flow, no exceptions for expected failures.** Handlers return `Result<T>` built from `Domain/Common/Results`. Controllers unwrap with `result.Match(Ok, Problem)` — `ApiController.Problem(List<Error>)` maps `ErrorKind` to status codes. Never throw to signal a business rule.
- **Validation** is FluentValidation via `ValidationBehaviour`, registered in `Application/DependencyInjection.cs`. Naming convention is `XxxCommandValidator` / `XxxQueryValidator` alongside the request. Adding a new pipeline behaviour means editing that `AddOpenBehavior` list.
- **Authorization is two-layer and fail-closed.** `[Authorize(Policy = ...)]` on the controller is only the outer layer. Every handler must also check ownership through `Application/Common/Security/ClinicAuthorization.cs` (`user.CanAccessAppointment(doctorId)`, `user.DoctorScope()`, `user.PatientScope()`, `user.CanWriteClinicalRecords()`). Client-supplied doctor/patient ids are never trusted.
- **Scope is part of the cache key.** Cached queries (`ICachedQuery`) bake `Scope.IsRestricted` / `Scope.DoctorId` into `CacheKey`. Omitting it leaks one doctor's rows to another.
- **Commands invalidate by tag.** Every mutating handler ends with `cache.RemoveByTagAsync("<entity>")` using the singular entity name (`appointment`, `invoice`, `patient`, `doctor`, `service`, `treatment-record`). Skipping this is the most common source of stale-read bugs.
- **Bulk import is not a side door.** `ImportController` is `AdminOnly` and both preview/commit handlers re-check `Roles.Admin`. Rows are built through `Patient.Create`/`ContactInfo.Create`, never by `AddRange` of raw entities, so import obeys the same rules as the single-patient form. Commit is all-or-nothing in one `SaveChanges`: a workbook with any rejected row persists a `Failed` `ImportJob` + relational `ImportRowError` rows, writes zero patients, and returns HTTP 200 with the report; an unreadable workbook or missing required header is a 400. Only the `patient` cache tag is invalidated. The rejection CSV is rendered on demand — `ImportJob.ErrorReportPath` is deliberately unused.
- **Lab status changes go through `LabCase.ChangeStatus`.** Do not assign `Status` directly; the explicit transition map rejects illegal moves with a `Result` conflict. Overdue is derived at query time from `ExpectedReturnAtUtc` (`IsOverdue(asOfUtc)`), not stored and not a background service. `LabCaseController` is `StaffOnly`, but every status mutation additionally requires `ClinicalWrite`.
- **Images go through `IImageStorage`.** Bytes live outside `wwwroot` at `Images__StoragePath`; never serve them as static files and never trust the client's file name or declared content type. Uploads must pass the MIME allow-list, magic-byte sniff and 10 MB cap, and `PatientImage` is append-only.
- **Reports aggregate in SQL, never in the browser.** `/api/v1/reports/dashboard` is the only source for the dashboard and the reports page. Do not reintroduce `Get*Async(1, 100)` + client-side `.Count`/`Take` — that was defect `D-09` and it silently capped every figure at one page. Money is deliberately three separate fields: `Revenue` (invoices *issued* in the window), `CashCollected` (`Payment.PaidAtUtc` in the window) and `Outstanding` (all issued invoices, **not** window-scoped). Keeping them distinct is the requirement, not an inconsistency.
- **Date windows are clinic-local, inclusive, and resolved through `ClinicClock`.** Store/compare UTC half-open windows derived from `ClinicSetting.TimeZoneId`; never assume the server zone. A windowed query must sort/report in clinic days but filter in UTC. `InvoiceSummaryReader` is the single implementation of the invoice money maths — reuse it rather than re-deriving `Paid`/`Remaining`.
- **Output cache** has a base policy that skips any request carrying an `Authorization` header (`Api/DependencyInjection.cs`). Don't remove that guard.
- **`AppDbContext` is `IdentityDbContext<AppUser>`** and is reached through the `IAppDbContext` interface in Application/Domain layers — never inject the concrete type into handlers.
- EF `DbSet`s are exposed as expression-bodied properties, not `{ get; set; }`, and configurations are auto-applied via `ApplyConfigurationsFromAssembly`. Add schema changes in `Infrastructure/Data/Configurations/*.cs` **and** generate a migration.
- `DbContext` is configured with `QuerySplittingBehavior.SplitQuery` deliberately (cartesian joins corrupt change tracking on invoices with Lines + Payments). Don't switch it to single-query.
- Soft-delete query filters exist on every `AuditableEntity` except `InvoiceLine` (the remaining gap — `AUDIT.md` §8 item 33). That includes `TreatmentRecord`, `Payment`, `PatientTooth` and `TreatmentPlan`, contrary to the older `D-08` note in `docs/product/PRD.md`.

## Environment quirks (deliberate or known-broken)

- **JWT secret is not in git.** `appsettings.json` has no `Secret`. Non-Development environments must set `JwtSettings__Secret` (env var/user-secrets/vault) or the app throws at startup; Development auto-generates an ephemeral one so a fresh clone runs and no usable secret ever lives in git. It is resolved lazily by `JwtSecretProvider` — never read it during service registration, or `WebApplicationFactory` configuration sources (queued until host build) will be missed.
- Serilog's Seq sink is set per environment: `appsettings.Development.json` → `localhost:5342` (compose maps `5342:5341`), `docker-compose.yml` → `seq:5341`. Keep the host matching the compose service name.
- `Dockerfile` has two targets: `final` (API) and `web` (Blazor app). `docker-compose.yml` runs both — `dentalclinic-api` on `5004:80` and `dentalclinic-web` on `7204:80` — and the Web container reaches the API over the compose network via `ApiBaseUrl=http://dentalclinic-api`. Running the Web app locally instead uses `ApiBaseUrl` from `appsettings.json` (`http://localhost:5004`).
- `TZ` is applied at runtime via the `TZ` env var; the image no longer hard-links `/etc/localtime`, so the same image is deployable in any region.
- **Startup seeding is split.** `ApplicationDbContextInitialiser.SeedSystemDataAsync()` seeds the 3 roles + the singleton `ClinicSetting` (values from `Bootstrap:*`, defaults `Asia/Damascus`/`USD`/`963`) and is environment-agnostic; `SeedDemoDataAsync()` (accounts, doctors, services, patients, appointments, odontogram) is gated on Development and refuses to run elsewhere. Development startup (`InitialiseDatabaseAsync`) still runs `MigrateAsync()` + system + demo, so `dotnet run` and the suites are unchanged. Production startup never migrates or seeds: use the one-shot `dotnet DentalClinic.Api.dll --migrate` (migrations + system data) and `--bootstrap-admin` (`SeedSystemDataAsync` + first Admin from `Bootstrap__AdminEmail`/`Bootstrap__AdminPassword`, idempotent, exit 0/1). `docker-compose.prod.yml` runs both via its `migrate` service. `--migrate` does not need `JwtSettings__Secret`, because the signing secret is resolved lazily.

- Production compose is `docker-compose.prod.yml` + `.env` (from `.env.example`); `docker-compose.yml` stays the local/demo stack. Ops ports (SQL/Seq/Prometheus/Grafana, plus API/Web) bind to `127.0.0.1` — a TLS-terminating proxy is the public entry point, and both apps trust `X-Forwarded-For`/`X-Forwarded-Proto`. The Web app exposes `/health`; the API keeps `/health` and `/health/ready`.
- `DentalClinic.Web` is a Blazor Server app that stores the session in `ClinicSessionState` (in-memory) — it does not survive a refresh (defect `D-14`).
- **Health endpoints**: `/health` (liveness, no checks) and `/health/ready` (DB probe via `DatabaseHealthCheck`). Mapped in every environment, not just Development.
- **Clinical image storage** defaults to `App_Data/images` (`Images__StoragePath`); compose mounts the `dentalclinic-images` volume. The database stores metadata only, so back the volume up separately (or move `IImageStorage` to object storage).
- **Backups are an ops job, not app code.** Compose enables SQL Agent and the one-shot `sql-backup-init` sidecar registers a nightly 02:00 full backup with 7-day retention into `containers/sqlserver-backups/`. Restore/rehearsal steps are in `docs/operations/BACKUP-RESTORE.md`; off-box copy and staleness alerting are still open.

## Security invariants to preserve

- **`Invoice` carries a `RowVersion` concurrency token** and `AddInvoicePaymentCommandHandler` wraps its read-modify-write in `IsolationLevel.Serializable`, translating `DbUpdateConcurrencyException` into `InvoiceErrors.ConcurrentModification` (409). Removing either reintroduces double-payment. `CreateAppointmentCommandHandler` uses the same serializable pattern to close the double-booking race.
- **Refresh tokens are stored hashed** (`RefreshToken.TokenHash`, SHA-256 hex). Only the raw value is ever returned to the client, and `RefreshTokenQueryHandler` rotates on use. Look tokens up via `RefreshToken.HashToken(...)`, never by raw value.
- **Login errors are deliberately uniform** (`Auth_Invalid_Credentials`) for unknown-user, unconfirmed, locked-out, and wrong-password — do not reintroduce distinct messages or it becomes an account-enumeration oracle. Failed attempts call `AccessFailedAsync` to drive the lockout.
- **A missing identity user is a `Result`, not an exception.** `IdentityService.GetUserByIdAsync` returns `ApplicationErrors.UserNotFound`; `RefreshTokenQueryHandler` deliberately collapses that case into the same uniform invalid-token conflict, because a 404 on refresh would confirm that an account once existed.
- **Identity password policy is 12+ chars with complexity and lockout.** Changing it requires updating `TestUsers` and the seeded credentials in `ApplicationDbContextInitialiser` together, or the suite breaks at `CreateAsync`.
- **Rate limiter policies are named** in `Api/RateLimiterPolicyNames.cs` and applied per-endpoint with `[EnableRateLimiting]`. The token endpoints carry the `Auth` policy; a registered-but-unapplied policy is dead config. Do not move these constants onto `DependencyInjection` — that type name collides with the one in `DentalClinic.Application` and produces CS0436 in controllers.
- **Staff account mutations are Admin-only and self-protected.** The `StaffUserController` handlers reject self-modification, self-deactivation, role names outside the whitelist, and removal of the last active admin; deactivation is a permanent lockout. `Doctor.UserId` is unique — one account drives at most one profile.
- `TreatmentRecord.Cost` is explicitly `.HasPrecision(18, 2)`, as are all money columns — keep that on any new monetary field.

## Docs

`docs/product/` holds a PRD, a phased roadmap, and an open-questions register. They are marked **draft / not approved for implementation**, and they describe an aspirational target (multi-tenant `ClinicId`, a `ClinicPermission` matrix) that does **not** exist in the code yet. Use them for intent; use the code for reality. `README.md` was truthed up on 2026-11-08: it documents `IAppDbContext` instead of a repository/unit-of-work layer, includes `DentalClinic.Web`, and links the operations runbook. Backup claims in `PRD.md`/`MVP-ROADMAP.md` now separate "job scheduled" from "restore rehearsed"; the latter is still open. Actual dev ports are `5004` (API) and `7204` (Web). The Web port deliberately avoids `5048`: Windows reserves `5041-5140` for Hyper-V/Docker/WSL2, and binding there fails with socket error `10013` ("access permissions") rather than the usual "port already in use". Check `netsh interface ipv4 show excludedportrange protocol=tcp` before picking any new port.