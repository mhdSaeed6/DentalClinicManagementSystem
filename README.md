# 🦷 DentalClinic Management System API

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue?style=for-the-badge)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![CQRS](https://img.shields.io/badge/Pattern-CQRS%20%2B%20MediatR-brightgreen?style=for-the-badge)](https://github.com/jbogard/MediatR)
[![Tests Passing](https://img.shields.io/badge/Tests-380%2F380%20Passed-success?style=for-the-badge&logo=xunit)](https://github.com/mhdSaeed6/DentalClinicManagementSystem)

A production-ready, highly scalable, and fully tested RESTful API built for managing **Dental Clinics**. Designed using **Clean Architecture** principles and **CQRS pattern** with modern **.NET 10** technologies to ensure high maintainability, testability, and performance.

---

## 🌟 Key Features

- **🔒 Authentication & Authorization:** Secure JWT-based authentication with fine-grained role & permission access control.
- **👨‍⚕️ Patient & Doctor Management:** Complete lifecycle management for patients, doctors, and clinic staff.
- **📅 Appointment Scheduling:** Advanced scheduling system with collision checks and status tracking.
- **🦷 FDI Tooth Treatment Records:** Comprehensive treatment recording system adhering to standard international tooth numbering systems.
- **🧾 Billing & Invoicing:** Automated invoice generation, payment handling, and clinic financial summaries.
- **📚 OpenApi / Scalar Documentation:** Interactive API documentation using Scalar & Swagger UI.
- **📈 Observability & Metrics:** Integrated **OpenTelemetry** and **Prometheus** metrics collection alongside structured logging with **Serilog**.

---

## 🏗 Architecture & Design Patterns

The solution is structured following **Clean Architecture** and **Domain-Driven Design (DDD)** concepts to strictly separate concerns:

```text
DentalClinicManagementSystem/
├── src/
│   ├── DentalClinic.Domain/          # Core Business Logic, Entities, Enums, & Domain Rules
│   ├── DentalClinic.Application/     # CQRS Handlers, DTOs, Behaviors, & Fluent Validation
│   ├── DentalClinic.Infrastructure/  # EF Core, Persistence, Migrations, & External Services
│   ├── DentalClinic.Contracts/       # Public Request/Response DTOs
│   └── DentalClinic.Api/             # Controllers, Middlewares, OpenAPI/Scalar Configs
└── tests/
    ├── DentalClinic.Domain.UnitTests/
    ├── DentalClinic.Application.UnitTests/
    └── DentalClinic.Api.IntegrationTests/
```


### Applied Patterns & Practices:
* **CQRS Pattern:** Complete separation of Read (Queries) and Write (Commands) paths using `MediatR`.
* **Validation Pipeline:** Pre-request validation using `FluentValidation` via MediatR Pipeline Behaviors.
* **Global Exception Handling:** Centralized custom middleware handling business exceptions cleanly without leaking stack traces.
* **Repository & Unit of Work:** Managed cleanly through Entity Framework Core.

---

## 🧪 Testing & Quality Assurance

Quality is at the heart of this project. The system includes an extensive suite of automated tests covering all domain rules, application logic, and full HTTP API execution paths.

- **Total Tests Executed:** `380` / `380` **Passed** 🟢
- **Unit Testing:** Comprehensive test coverage for Domain entities, Value Objects, Mappers, and Application Handlers.
- **Subcutaneous & Integration Testing:** Full end-to-end HTTP pipeline tests using `WebApplicationFactory` and real SQL Database side-effect assertions for all Controllers.

---

## 🛠 Tech Stack

* **Framework:** .NET 10.0 (ASP.NET Core Web API)
* **ORM:** Entity Framework Core
* **Database:** Microsoft SQL Server
* **Design Patterns:** CQRS, Clean Architecture, MediatR
* **Validation:** FluentValidation
* **Observability:** OpenTelemetry, Prometheus, Serilog
* **Testing Frameworks:** xUnit, FluentAssertions, WebApplicationFactory
* **Documentation:** Scalar API Reference, Swagger UI

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/)
* [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or LocalDB)

### Local Setup
1. **Clone the repository:**
   ```bash
   git clone [https://github.com/mhdSaeed6/DentalClinicManagementSystem.git](https://github.com/mhdSaeed6/DentalClinicManagementSystem.git)
   cd DentalClinicManagementSystem
Update Connection String:
Set your local SQL Server connection string in src/DentalClinic.Api/appsettings.json.

Run Database Migrations:
The application automatically initializes and updates the database on startup in the Development environment.

Run the Application:

Bash
dotnet run --project src/DentalClinic.Api
Explore API Documentation & Metrics:

Scalar UI: https://localhost:7198/scalar/v1

Swagger UI: https://localhost:7198/swagger

Prometheus Metrics: https://localhost:7198/metrics

📄 License
This project is proprietary software. All rights reserved. See LICENSE for details.
