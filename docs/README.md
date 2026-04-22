# Halto — Multi-Tenant Recurring Payments Platform

A production-quality .NET 8 Web API backend for recurring payment management.
Supports Hostel, Tuition Center, Gym, and any other recurring-amount business.

## Project Structure

```
Halto/
├── Halto.sln
├── Halto.Domain/                    # Entities, Enums (no dependencies)
│   ├── Entities/
│   │   ├── Organization.cs
│   │   ├── User.cs
│   │   ├── Member.cs
│   │   ├── Due.cs
│   │   └── Payment.cs
│   └── Enums/
│       └── Enums.cs
│
├── Halto.Application/               # DTOs, Interfaces, Business contracts
│   ├── Common/
│   │   └── Result.cs               # Generic Result<T> + PagedResult<T>
│   ├── DTOs/
│   │   ├── Auth/AuthDtos.cs
│   │   ├── Organizations/OrganizationDtos.cs
│   │   ├── Staff/StaffDtos.cs
│   │   ├── Members/MemberDtos.cs
│   │   ├── Dues/DueDtos.cs          (also contains PaymentDtos)
│   │   └── Dashboard/DashboardDtos.cs
│   └── Interfaces/
│       └── IServices.cs            # All service interfaces
│
├── Halto.Infrastructure/            # EF Core, Implementations
│   ├── Data/
│   │   ├── HaltoDbContext.cs
│   │   ├── DbSeeder.cs
│   │   └── Migrations/
│   │       ├── 20240101000000_InitialCreate.cs
│   │       └── HaltoDbContextModelSnapshot.cs
│   ├── Auth/
│   │   └── TokenService.cs
│   └── Services/
│       ├── AuthService.cs
│       ├── OrganizationService.cs
│       ├── StaffService.cs
│       ├── MemberService.cs
│       ├── DueService.cs
│       ├── PaymentService.cs
│       └── DashboardService.cs
│
├── Halto.Api/                       # Controllers, Middleware, Program.cs
│   ├── Controllers/
│   │   ├── HaltoControllerBase.cs
│   │   ├── AuthController.cs
│   │   ├── SuperAdminController.cs
│   │   ├── OwnerController.cs
│   │   ├── MembersController.cs
│   │   ├── DuesController.cs        (also PaymentsController)
│   │   ├── DashboardController.cs
│   │   └── MemberPortalController.cs
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   ├── Extensions/
│   │   └── ServiceExtensions.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
│
└── docs/
    ├── README.md                    # This file
    └── API_GUIDE.md                 # All sample API requests
```

## Tech Stack

- **.NET 8** Web API (Controller-based)
- **Entity Framework Core 8** + SQL Server provider
- **Azure SQL / SQL Server** (GUID PKs, decimal columns, unique constraints)
- **JWT Bearer Authentication** (HS256, 24h expiry)
- **BCrypt.Net-Next** for password hashing
- **Serilog** for structured logging
- **Swashbuckle** (Swagger/OpenAPI)
- **Global Exception Middleware** with consistent error format

## Database Schema

```
Organizations        Users               Members
─────────────        ─────               ───────
Id (PK)              Id (PK)             Id (PK)
Name                 Email (UQ)          FullName
BusinessType         PasswordHash        Email
IsActive             FullName            Phone
CreatedAt            Phone               IsActive
UpdatedAt            Role                JoinedAt
                     IsActive            ExtraFieldsJson   ← JSON, business-specific
                     CreatedAt           OrganizationId (FK)
                     UpdatedAt           UserId (FK, nullable, UQ)
                     OrganizationId (FK) CreatedAt
                                         UpdatedAt

Dues                             Payments
────                             ────────
Id (PK)                          Id (PK)
Year                             AmountPaid
Month                            PaidOn
Amount                           Method
Status (Due/Partial/Paid)        Notes
Notes                            OrganizationId (FK)
OrganizationId (FK)              MemberId (FK)
MemberId (FK)                    DueId (FK)
CreatedAt                        MarkedByUserId (FK)
UpdatedAt                        CreatedAt

UNIQUE(MemberId, Year, Month)    
```

## Prerequisites

- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- SQL Server (local, Docker, or Azure SQL)

### Quick SQL Server via Docker
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password" \
  -p 1433:1433 --name halto-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

## Setup

### 1. Configure Connection String

Edit `Halto.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HaltoDB;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyMustBeAtLeast32CharsLongForHS256!!",
    "Issuer": "halto-api",
    "Audience": "halto-clients",
    "ExpiryHours": "24"
  }
}
```

> **Security**: Change JWT Secret before deploying. Use environment variables or Azure Key Vault in production.

### 2. Apply Migrations

```bash
cd Halto

# Install EF tools (once)
dotnet tool install --global dotnet-ef

# Apply migration and create database
dotnet ef database update --project Halto.Infrastructure --startup-project Halto.Api
```

### 3. Run

```bash
cd Halto.Api
dotnet run
```

Or from solution root:
```bash
dotnet run --project Halto.Api
```

App starts at: `http://localhost:5000`
Swagger: `http://localhost:5000/swagger`

### 4. Seed Data Applied Automatically

On first run, the app seeds:
- SuperAdmin: `admin@halto.local` / `Admin@123`
- Organization: "Green Valley Hostel"
- Owner: `owner@greenvalley.local` / `Owner@123`
- Staff x2: `staff1@greenvalley.local`, `staff2@greenvalley.local` / `Staff@123`
- Members x3 with hostel-style ExtraFieldsJson
- Sample dues and payments for current month

### 5. Run with hot reload (development)

```bash
dotnet watch --project Halto.Api
```

## Running Migrations from Scratch

If you want to regenerate migrations:

```bash
# Delete existing migration files (keep the Migrations folder)
# Then:
dotnet ef migrations add InitialCreate \
  --project Halto.Infrastructure \
  --startup-project Halto.Api \
  --output-dir Data/Migrations

dotnet ef database update \
  --project Halto.Infrastructure \
  --startup-project Halto.Api
```

## API Quick Start

```bash
# 1. Login as SuperAdmin
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@halto.local","password":"Admin@123"}'

# 2. Copy token from response, then:
export TOKEN="eyJhbGci..."

# 3. List organizations
curl http://localhost:5000/api/super/organizations \
  -H "Authorization: Bearer $TOKEN"

# 4. Login as Owner and generate dues
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"owner@greenvalley.local","password":"Owner@123"}'

export OWNER_TOKEN="..."

curl -X POST http://localhost:5000/api/dues/generate-month \
  -H "Authorization: Bearer $OWNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"year":2024,"month":4}'
```

See `docs/API_GUIDE.md` for complete request examples.

## Architecture Decisions

- **Clean Architecture**: Domain → Application → Infrastructure → API. Each layer only depends on inner layers.
- **Result<T> pattern**: Services return `Result<T>` instead of throwing for expected errors. Controllers translate to HTTP responses.
- **Multi-tenancy via OrganizationId**: All org-scoped queries filter by OrganizationId extracted from JWT. SuperAdmin bypasses this.
- **ExtraFieldsJson**: Flexible `nvarchar(max)` JSON column on Member for business-specific fields. No schema migration needed to support new business types.
- **Partial payment support**: Payments are additive. Due status auto-updates to Partial/Paid based on cumulative sum.
- **Role-based security**: Enforced at both `[Authorize(Roles=...)]` attribute level and service layer (org ownership check).

## Production Checklist

- [ ] Rotate JWT secret (use environment variable or Azure Key Vault)
- [ ] Use Azure SQL or managed SQL Server
- [ ] Enable HTTPS (configure Kestrel or use reverse proxy)
- [ ] Add rate limiting middleware
- [ ] Store refresh tokens in DB (current implementation returns but doesn't persist them)
- [ ] Add email notification service for due reminders
- [ ] Add audit log table for payment modifications
- [ ] Configure Serilog sinks (Azure App Insights, file, etc.)
- [ ] Add health check endpoint (`/health`)
- [ ] Containerize with Dockerfile

## Future Extension Points

Marked with comments in code:
- **Discounts table**: Add `Discounts` entity linked to Member/Due; adjust due amount calculation in DueService
- **Automated due generation**: Add a background service (IHostedService) to call GenerateDues on 1st of each month
- **Notifications**: Inject INotificationService into DueService; call after generation
- **Payment gateway**: Add `GatewayTransactionId` to Payment; implement IPaymentGatewayService
- **Multi-currency**: Add `Currency` to Organization; store amounts with currency
