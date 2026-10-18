# AGENTS.md

.NET 10 dental-clinic system: CQRS REST API + Blazor Web App, Clean Architecture. Details below are verified against the code, not the README.

## Commands

```powershell
dotnet build DentalClinic.slnx                      # or: dotnet build src/DentalClinic.Api
dotnet test tests/DentalClinic.Domain.UnitTests     # no Docker needed
dotnet test tests/DentalClinic.Application.UnitTests
dotnet run --project src/DentalClinic.Api           # http://localhost:5004, Scalar at /scalar/v1 (Development only)
dotnet run --project src/DentalClinic.Web           # http://localhost:5048, calls API on :5004
docker compose up -d --build                        # API :5002, SQL :1434, Seq :8081, Prometheus :9090, Grafana :3000
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

- `.github/workflows/build-and-test.yml` runs `restore → build -c Release --no-restore → test -c Release --no-build` on `ubuntu-latest` and asserts `docker info` first, so the Testcontainers suites genuinely execute. If you add a Docker-dependent suite, no workflow change is needed.
- Package versions are centralized in `Directory.Packages.props` (`ManagePackageVersionsCentrally`). **Never** add `Version=` to a `PackageReference`; add a `PackageVersion` entry instead.
- `global.json` pins the SDK to `10.0.100` with `rollForward: latestFeature`. Bump it deliberately rather than letting the SDK float.
- `Directory.Build.props` sets `net10.0` for every project and injects `StyleCop.Analyzers` everywhere. The build is not warning-clean (~28 SA/AV/CS warnings) — this is the baseline, not something you introduced. Don't chase them unless you touched the file.
- **`dotnet ef` tools are 10.0.2 while the runtime is 10.0.12** and it prints a version-mismatch warning on every command. Harmless, but update the global tool if it ever misbehaves.
- `.editorconfig`: CRLF, 4-space indent, `insert_final_newline = false` (files deliberately end without a newline), block-scoped namespace *setting* but all real code uses file-scoped namespaces (severity is `silent`, no error). Private fields `_camelCase`, private statics `s_camelCase`. Using directives go **outside** the namespace, grouped (System first, then others, blank-line separated).
- Comments are a deliberate mix of English and Arabic. Match the surrounding file's language when editing.

## Architecture rules worth not breaking

- **Result flow, no exceptions for expected failures.** Handlers return `Result<T>` built from `Domain/Common/Results`. Controllers unwrap with `result.Match(Ok, Problem)` — `ApiController.Problem(List<Error>)` maps `ErrorKind` to status codes. Never throw to signal a business rule.
- **Validation** is FluentValidation via `ValidationBehaviour`, registered in `Application/DependencyInjection.cs`. Naming convention is `XxxCommandValidator` / `XxxQueryValidator` alongside the request. Adding a new pipeline behaviour means editing that `AddOpenBehavior` list.
- **Authorization is two-layer and fail-closed.** `[Authorize(Policy = ...)]` on the controller is only the outer layer. Every handler must also check ownership through `Application/Common/Security/ClinicAuthorization.cs` (`user.CanAccessAppointment(doctorId)`, `user.DoctorScope()`, `user.PatientScope()`, `user.CanWriteClinicalRecords()`). Client-supplied doctor/patient ids are never trusted.
- **Scope is part of the cache key.** Cached queries (`ICachedQuery`) bake `Scope.IsRestricted` / `Scope.DoctorId` into `CacheKey`. Omitting it leaks one doctor's rows to another.
- **Commands invalidate by tag.** Every mutating handler ends with `cache.RemoveByTagAsync("<entity>")` using the singular entity name (`appointment`, `invoice`, `patient`, `doctor`, `service`, `treatment-record`). Skipping this is the most common source of stale-read bugs.
- **Output cache** has a base policy that skips any request carrying an `Authorization` header (`Api/DependencyInjection.cs`). Don't remove that guard.
- **`AppDbContext` is `IdentityDbContext<AppUser>`** and is reached through the `IAppDbContext` interface in Application/Domain layers — never inject the concrete type into handlers.
- EF `DbSet`s are exposed as expression-bodied properties, not `{ get; set; }`, and configurations are auto-applied via `ApplyConfigurationsFromAssembly`. Add schema changes in `Infrastructure/Data/Configurations/*.cs` **and** generate a migration.
- `DbContext` is configured with `QuerySplittingBehavior.SplitQuery` deliberately (cartesian joins corrupt change tracking on invoices with Lines + Payments). Don't switch it to single-query.
- Soft-delete query filters exist on Patient/Doctor/DentalService/Appointment/Invoice/RefreshToken but **not** on `TreatmentRecord` or `Payment` (known defect `D-08` in `docs/product/PRD.md`).

## Environment quirks (deliberate or known-broken)

- **JWT secret is not in git.** `appsettings.json` has no `Secret`. Non-Development environments must set `JwtSettings__Secret` (env var/user-secrets/vault) or the app throws at startup; Development auto-generates an ephemeral one so a fresh clone runs and no usable secret ever lives in git.
- Serilog's Seq sink is set per environment: `appsettings.Development.json` → `localhost:5342` (compose maps `5342:5341`), `docker-compose.yml` → `seq:5341`. Keep the host matching the compose service name.
- `Dockerfile` builds and ships **only the API**. `DentalClinic.Web` has no container and no compose service; the Web app reaches the API over HTTP using `ApiBaseUrl`.
- `TZ` is applied at runtime via the `TZ` env var; the image no longer hard-links `/etc/localtime`, so the same image is deployable in any region.
- Migrations + demo seeding (`ApplicationDbContextInitialiser`) run **only** under `ASPNETCORE_ENVIRONMENT=Development` (see `Api/Program.cs`). Production startup does not migrate — apply migrations as a separate deploy step.
- `DentalClinic.Web` is a Blazor Server app that stores the session in `ClinicSessionState` (in-memory) — it does not survive a refresh (defect `D-14`).
- **Health endpoints**: `/health` (liveness, no checks) and `/health/ready` (DB probe via `DatabaseHealthCheck`). Mapped in every environment, not just Development.

## Security invariants to preserve

- **`Invoice` carries a `RowVersion` concurrency token** and `AddInvoicePaymentCommandHandler` wraps its read-modify-write in `IsolationLevel.Serializable`, translating `DbUpdateConcurrencyException` into `InvoiceErrors.ConcurrentModification` (409). Removing either reintroduces double-payment. `CreateAppointmentCommandHandler` uses the same serializable pattern to close the double-booking race.
- **Refresh tokens are stored hashed** (`RefreshToken.TokenHash`, SHA-256 hex). Only the raw value is ever returned to the client, and `RefreshTokenQueryHandler` rotates on use. Look tokens up via `RefreshToken.HashToken(...)`, never by raw value.
- **Login errors are deliberately uniform** (`Auth_Invalid_Credentials`) for unknown-user, unconfirmed, locked-out, and wrong-password — do not reintroduce distinct messages or it becomes an account-enumeration oracle. Failed attempts call `AccessFailedAsync` to drive the lockout.
- **Identity password policy is 12+ chars with complexity and lockout.** Changing it requires updating `TestUsers` and the seeded credentials in `ApplicationDbContextInitialiser` together, or the suite breaks at `CreateAsync`.
- **Rate limiter policies are named** in `Api/RateLimiterPolicyNames.cs` and applied per-endpoint with `[EnableRateLimiting]`. The token endpoints carry the `Auth` policy; a registered-but-unapplied policy is dead config. Do not move these constants onto `DependencyInjection` — that type name collides with the one in `DentalClinic.Application` and produces CS0436 in controllers.
- `TreatmentRecord.Cost` is explicitly `.HasPrecision(18, 2)` (as are all money columns). Soft-delete query filters are on **every** `AuditableEntity`, including `TreatmentRecord` and `Payment`.

## Docs

`docs/product/` holds a PRD, a phased roadmap, and an open-questions register. They are marked **draft / not approved for implementation**, and they describe an aspirational target (multi-tenant `ClinicId`, permissions, health checks) that does **not** exist in the code yet. Use them for intent; use the code for reality. `README.md` also overstates test counts and lists stale ports (`7198`); actual dev ports are `5004` (API) and `5048` (Web).
