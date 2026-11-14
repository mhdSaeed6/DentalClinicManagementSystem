# 🦷 DentalClinic Management System API

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue?style=for-the-badge)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![CQRS](https://img.shields.io/badge/Pattern-CQRS%20%2B%20MediatR-brightgreen?style=for-the-badge)](https://github.com/jbogard/MediatR)
[![Tests Passing](https://img.shields.io/badge/Tests-passing-success?style=for-the-badge&logo=xunit)](https://github.com/mhdSaeed6/DentalClinicManagementSystem)

A production-ready, highly scalable, and fully tested RESTful API built for managing **Dental Clinics**. Designed using **Clean Architecture** principles and **CQRS pattern** with modern **.NET 10** technologies to ensure high maintainability, testability, and performance.

---

## 🌟 Key Features

- **🔒 Authentication & Authorization:** Secure JWT-based authentication with fine-grained role & permission access control.
- **👨‍⚕️ Patient & Doctor Management:** Complete lifecycle management for patients, doctors, and clinic staff, including administrator-managed accounts and doctor↔account linking.
- **📅 Appointment Scheduling:** Advanced scheduling system with collision checks and status tracking.
- **🦷 FDI Tooth Treatment Records:** Comprehensive treatment recording system adhering to standard international tooth numbering systems.
- **🪥 Odontogram & Treatment Plans:** Per-tooth condition tracking, multi-session treatment plans with performed/estimated totals, and printable plan sheets.
- **🧾 Billing & Invoicing:** Automated invoice generation, payment handling, clinic financial summaries, and outstanding-debt reporting.
- **🧪 Dental Lab Tracking:** Lab-case workflow (sent → at lab → trial → tried-in → returned/failed) with overdue detection.
- **🖼 Clinical Images:** X-ray and before/after uploads linked to a patient, tooth, or treatment record, stored behind an `IImageStorage` abstraction.
- **📥 Excel Patient Import:** Administrator-only, all-or-nothing workbook import with dry-run preview, column mapping, and a downloadable rejection report.
- **🛟 Operations:** Nightly SQL Server backups through a SQL Agent job (see `docs/operations/BACKUP-RESTORE.md`) and health endpoints.
- **📚 OpenApi / Scalar Documentation:** Interactive API documentation using Scalar & Swagger UI.
- **📈 Observability & Metrics:** Integrated **OpenTelemetry** and **Prometheus** metrics collection alongside structured logging with **Serilog**.

---

## 🏗 Architecture & Design Patterns

The solution is structured following **Clean Architecture** and **Domain-Driven Design (DDD)** concepts to strictly separate concerns:

```text
DentalClinicManagementSystem/
├── src/
│   ├── DentalClinic.Api/                     # Controllers, Middlewares, & Configurations
│   ├── DentalClinic.Application/             # CQRS Handlers, DTOs, Behaviors, & FluentValidation
│   ├── DentalClinic.Contracts/               # Public Request/Response DTOs & Contracts
│   ├── DentalClinic.Domain/                  # Core Entities, Enums, Value Objects, & Business Rules
│   ├── DentalClinic.Infrastructure/          # EF Core Persistence, Migrations, & External Services
│   └── DentalClinic.Web/                     # Blazor Server UI (interactive server components)
└── tests/
    ├── DentalClinic.Api.IntegrationTests/            # Full HTTP Pipeline Integration Tests
    ├── DentalClinic.Application.SubcutaneousTests/   # Subcutaneous Testing without HTTP Overhead
    ├── DentalClinic.Application.UnitTests/           # Command & Query Handler Unit Tests
    ├── DentalClinic.Domain.UnitTests/                # Domain Logic & Entity Unit Tests
    └── DentalClinic.Tests.Common/                    # Shared Test Utilities, Builders, & Fixtures

```

### Applied Patterns & Practices:

* **CQRS Pattern:** Complete separation of Read (Queries) and Write (Commands) paths using `MediatR`.
* **Validation Pipeline:** Pre-request validation using `FluentValidation` via MediatR Pipeline Behaviors.
* **Result Flow:** Expected failures return `Result<T>` and map to RFC 7807 responses; exceptions are reserved for genuinely unexpected faults.
* **Persistence:** EF Core `DbContext` behind the `IAppDbContext` interface (no repository/unit-of-work layer), with soft-delete filters, auditable entities, and migrations applied on startup only in Development.
* **Caching:** `HybridCache` for read queries, with the caller scope baked into every restricted cache key and tag-based invalidation on mutation.

---

## 🧪 Testing & Quality Assurance

Quality is at the heart of this project. The system includes an extensive suite of automated tests covering all domain rules, application logic, and full HTTP API execution paths.

* **Test Suites:** Domain (entities/value objects), Application (handlers, behaviours, mappers), Web (components), Subcutaneous (in-process HTTP pipeline), and full-stack Integration (Testcontainers SQL Server). Run them all with `dotnet test DentalClinic.slnx`; the last two need Docker.
* **Current suite:** Domain **292**, Application **105**, Web **69**, Subcutaneous **341**, Integration **258** (1,065 tests). Counts are a snapshot; the badge above only claims that the suites pass.
* **Unit Testing:** Comprehensive test coverage for Domain entities, Value Objects, Mappers, and Application Handlers.
* **Subcutaneous & Integration Testing:** Full end-to-end HTTP pipeline tests using `WebApplicationFactory` and real SQL Database side-effect assertions for all Controllers.

---

## 🛠 Tech Stack

* **Framework:** .NET 10.0 (ASP.NET Core Web API)
* **UI:** Blazor Server (interactive server rendering) in `src/DentalClinic.Web`
* **ORM:** Entity Framework Core
* **Database:** Microsoft SQL Server
* **Excel:** ClosedXML (patient import + template)
* **Caching:** `HybridCache` (in-memory + optional distributed)
* **Containerization:** Docker, Docker Compose
* **Design Patterns:** CQRS, Clean Architecture, MediatR
* **Validation:** FluentValidation
* **Observability:** OpenTelemetry, Prometheus, Serilog, Seq
* **Testing Frameworks:** xUnit, WebApplicationFactory, Testcontainers
* **Documentation:** Scalar API Reference, Swagger UI

---

## 🚀 Getting Started

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/?utm_source=gemini)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/?utm_source=gemini) *(Optional for containerized run)*

### Option 1: Running with Docker Compose (Recommended)

1. **Clone the repository:**
```bash
git clone [https://github.com/mhdSaeed6/DentalClinicManagementSystem.git](https://github.com/mhdSaeed6/DentalClinicManagementSystem.git)
cd DentalClinicManagementSystem

```


2. **Spin up the full infrastructure (API, Web, SQL Server, Seq, Prometheus, Grafana):**
```bash
docker compose up -d --build

```


3. **Access Services:**
* **Web UI:** `http://localhost:7204`
* **Scalar UI:** `http://localhost:5004/scalar/v1`
* **Swagger UI:** `http://localhost:5004/swagger`
* **Seq Logs:** `http://localhost:8081`
* **Prometheus Metrics:** `http://localhost:9090`
* **Grafana:** `http://localhost:3000`
* **SQL Server:** `localhost,1434`



---

### Option 2: Running Locally (.NET CLI)

1. **Clone the repository & navigate:**
```bash
git clone [https://github.com/mhdSaeed6/DentalClinicManagementSystem.git](https://github.com/mhdSaeed6/DentalClinicManagementSystem.git)
cd DentalClinicManagementSystem

```


2. **Update Connection String:**
Set your local SQL Server connection string in `src/DentalClinic.Api/appsettings.json`.
3. **Run the Application:**
```bash
dotnet run --project src/DentalClinic.Api

```
4. **Run the Blazor Web UI** (in a second terminal — it calls the API on `:5004`):
```bash
dotnet run --project src/DentalClinic.Web

```


5. **Explore Endpoints:**
* **Web UI:** `http://localhost:7204`
* **Scalar UI:** `http://localhost:5004/scalar/v1`
* **Swagger UI:** `http://localhost:5004/swagger`

> **Port note:** the Web UI uses `7204`, not `5048`. Windows reserves `5041-5140` for
> Hyper-V / Docker Desktop / WSL2, and binding a port inside that range fails with
> socket error `10013` ("access permissions") instead of the usual "port already in
> use". Run `netsh interface ipv4 show excludedportrange protocol=tcp` to see the
> ranges currently reserved on your machine.



---

## 🛟 Operations

* **Backups:** Compose enables SQL Agent and the `sql-backup-init` sidecar registers a nightly full backup at 02:00 with 7-day local retention under `containers/sqlserver-backups/`. A `restic` sidecar copies the dump plus the images and Data Protection volumes off-box at 03:30, and backup age is exported as a Prometheus metric with missing/stale alert rules. `pwsh ./scripts/backup/restore-rehearsal.ps1` proves a restore and logs it; procedures, RPO/RTO and the remaining gaps are in [`docs/operations/BACKUP-RESTORE.md`](docs/operations/BACKUP-RESTORE.md).
* **Clinical images:** Stored outside `wwwroot` at `Images__StoragePath` (default `App_Data/images`), backed by the `dentalclinic-images` compose volume.
* **Patient import:** Admin-only `/import` page; the workbook template and rejection report are generated on demand, and no row is written unless the whole file validates.
* **Health:** `/health` (liveness) and `/health/ready` (database probe) are mapped in every environment.

---

## 📄 License

This project is proprietary software. All rights reserved. See `LICENSE` for details.