# BenefitBridge

BenefitBridge is a full-stack benefits platform for **ICHRA** (individual coverage health reimbursement arrangements). Employers set up a monthly health allowance for their employees, see what it will cost, and check whether it meets federal affordability rules using real plan prices from the federal Health Insurance Marketplace.

> **Status:** in active development. The feature list below is the target; the roadmap shows what's built so far.

## What it does

- **Employer setup:** create employee classes (full-time, part-time, by location) with a flat or age-rated allowance and a per-dependent add-on.
- **Rule enforcement:** allowances can vary within a class only by age and family size, increase with age, and stay within a 3:1 ratio between oldest and youngest. Invalid setups are rejected with a clear explanation.
- **Census import:** bulk-load employees, with duplicate detection and row-level error reporting.
- **Cost reporting:** monthly cost per class (headcount, total, average, highest), computed in SQL and fast at 50,000 employees.
- **Affordability checks:** compares each employee's cost of the lowest-cost silver plan in their county (from the CMS Marketplace API) against the year's IRS affordability threshold.
- **Role-based access:** employer admins manage only their own company; employees see only their own allowance and affordability.

## Tech stack

| Layer | Technology |
|---|---|
| API | C#, ASP.NET Core (.NET 10), REST, OpenAPI |
| Data | SQL Server, EF Core (migrations), T-SQL reporting |
| Integrations | CMS Marketplace API via typed `HttpClient` with resilience (retries, circuit breaker, timeouts) and caching |
| Front end | React, TypeScript (strict), Redux Toolkit + RTK Query, Material UI, React Hook Form + Zod |
| Testing | xUnit, WebApplicationFactory + Testcontainers (real SQL Server), Vitest, React Testing Library, MSW |
| Auth | JWT bearer authentication, role- and tenant-based authorization policies |
| Cloud and CI/CD | Azure App Service, Azure SQL, Static Web Apps, Key Vault (managed identity), Application Insights, GitHub Actions with OIDC |

## Architecture

The backend follows a **hexagonal (ports and adapters)** architecture. Dependencies point inward: the domain has no framework dependencies, and infrastructure plugs in from the edges.

```
src/
  BenefitBridge.Domain/          entities, value objects, business rules (no dependencies)
  BenefitBridge.Application/     use cases, ports (interfaces), input validation
  BenefitBridge.Infrastructure/  EF Core, SQL Server, CMS Marketplace client (adapters)
  BenefitBridge.Api/             ASP.NET Core host, endpoints, auth, composition root
tests/
  BenefitBridge.Domain.Tests/
  BenefitBridge.Application.Tests/
  BenefitBridge.Api.IntegrationTests/
web/                             React front end
```

Key design decisions:

- **Always-valid domain objects:** money is a `decimal`-backed value object, and ICHRA rules are enforced in constructors, so invalid configurations can't exist.
- **Allowance strategies:** flat and age-rated allowances are interchangeable implementations of one interface, so new allowance types don't change the calculator.
- **Configuration, not code, for yearly rules:** the IRS affordability threshold changes every year and lives in validated configuration.
- **Tenant isolation in depth:** enforced in authorization policies and in every data query, and verified by an integration test matrix.
- **Graceful degradation:** if the marketplace API is unavailable, affordability reports "temporarily unavailable" instead of failing.

## Running locally

Prerequisites: .NET 10 SDK, Node.js 22+, Docker.

```bash
# Database (SQL Server in Docker)
cp .env.example .env              # set a local SA password
docker compose up -d

# API
dotnet user-secrets set "ConnectionStrings:BenefitBridge" "<local connection string>" --project src/BenefitBridge.Api
dotnet ef database update --project src/BenefitBridge.Infrastructure --startup-project src/BenefitBridge.Api
dotnet run --project src/BenefitBridge.Api

# Front end
cd web
npm ci
npm run dev
```

Running against the real marketplace requires a free CMS Marketplace API key from [developer.cms.gov](https://developer.cms.gov/marketplace-api), stored with user secrets. Without one, the API can run with a fake plan-pricing adapter.

> The SQL Server container image supports x86-64 hosts only. On Apple Silicon it runs under emulation, which is unsupported and slower.

## Tests

```bash
dotnet test                 # unit + integration tests (integration tests need Docker)
cd web && npm run test:run  # front-end tests
```

## Roadmap

- [ ] Domain model and allowance calculator
- [ ] Allowance strategies and ICHRA rule enforcement
- [ ] Census import and roster
- [ ] REST API
- [ ] SQL Server persistence with EF Core
- [ ] Monthly cost report (T-SQL, indexed)
- [ ] Hexagonal refactor, validation, consistent errors
- [ ] CMS Marketplace integration and affordability checks
- [ ] Employer admin UI (React, Material UI)
- [ ] Redux Toolkit / RTK Query data layer
- [ ] Front-end test suite
- [ ] Authentication and tenant isolation
- [ ] API integration tests against real SQL Server
- [ ] CI/CD to Azure

## Data sources

Plan and pricing data come from the [CMS Marketplace API](https://developer.cms.gov/marketplace-api), the public API behind HealthCare.gov. This project is not affiliated with CMS or HealthCare.gov.