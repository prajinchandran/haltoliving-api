# Halto API — Sample Requests & Documentation

## Base URL
```
http://localhost:5000/api
```

## Swagger UI
```
http://localhost:5000/swagger
```

---

## Seed Data (Ready to use)

| Role              | Email                        | Password     |
|-------------------|------------------------------|--------------|
| SuperAdmin        | admin@halto.local            | Admin@123    |
| OrganizationOwner | owner@greenvalley.local      | Owner@123    |
| Staff             | staff1@greenvalley.local     | Staff@123    |
| Staff             | staff2@greenvalley.local     | Staff@123    |

---

## Auth Endpoints

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@halto.local",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGci...",
    "refreshToken": "abc123...",
    "expiresAt": "2024-01-02T00:00:00Z",
    "user": {
      "id": "00000000-0000-0000-0000-000000000001",
      "email": "admin@halto.local",
      "fullName": "Super Administrator",
      "role": "SuperAdmin",
      "isActive": true,
      "organizationId": null
    }
  }
}
```

### Register Owner (Self-service)
```http
POST /api/auth/register-owner
Content-Type: application/json

{
  "email": "newowner@mygym.com",
  "password": "Gym@12345",
  "fullName": "John Fitness",
  "phone": "9876500000",
  "organizationName": "FitZone Gym",
  "businessType": 3
}
```
> BusinessType: 1=Hostel, 2=Tuition, 3=Gym, 4=Other

### Get Current User
```http
GET /api/auth/me
Authorization: Bearer {token}
```

---

## SuperAdmin Endpoints

> All require: `Authorization: Bearer {superadmin-token}`

### Create Organization + Owner
```http
POST /api/super/organizations
Authorization: Bearer {token}
Content-Type: application/json

{
  "organizationName": "Bright Minds Tuition",
  "businessType": 2,
  "ownerEmail": "owner@brightminds.com",
  "ownerPassword": "Owner@123",
  "ownerFullName": "Meena Iyer",
  "ownerPhone": "9988776655"
}
```

### List All Organizations
```http
GET /api/super/organizations
Authorization: Bearer {token}
```

### Activate / Deactivate Organization
```http
PATCH /api/super/organizations/{orgId}/status
Authorization: Bearer {token}
Content-Type: application/json

{ "isActive": false }
```

---

## Owner Endpoints

> All require: `Authorization: Bearer {owner-token}`

### Create Staff
```http
POST /api/owner/staff
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "newstaff@greenvalley.local",
  "password": "Staff@456",
  "fullName": "Sunita Rao",
  "phone": "9123456789"
}
```

### List Staff
```http
GET /api/owner/staff
Authorization: Bearer {token}
```

### Deactivate Staff
```http
PATCH /api/owner/staff/{staffId}/status
Authorization: Bearer {token}
Content-Type: application/json

{ "isActive": false }
```

---

## Member Management (Owner + Staff)

### Add Member — Hostel
```http
POST /api/members
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "Arjun Mehta",
  "email": "arjun@example.com",
  "phone": "9000000001",
  "joinedAt": "2024-01-01T00:00:00Z",
  "extraFields": {
    "room": "201",
    "bed": "B",
    "monthlyRent": 7000,
    "advance": 14000,
    "deposit": 6000,
    "admissionFee": 500,
    "discount": 200
  }
}
```

### Add Member — Tuition Center
```http
POST /api/members
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "Priya Das",
  "email": "priya@example.com",
  "phone": "9000000002",
  "extraFields": {
    "course": "JEE Advanced",
    "batch": "Morning Batch A",
    "monthlyFee": 4500,
    "subjects": ["Physics", "Chemistry", "Math"]
  }
}
```

### Add Member — Gym
```http
POST /api/members
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "Rohan Shah",
  "phone": "9000000003",
  "extraFields": {
    "plan": "Premium",
    "amount": 2500,
    "personalTrainer": true,
    "lockerNumber": "L42"
  }
}
```

### List Members
```http
GET /api/members?search=arjun&page=1&pageSize=20
Authorization: Bearer {token}
```

### Get Member Detail
```http
GET /api/members/{memberId}
Authorization: Bearer {token}
```

### Update Member
```http
PATCH /api/members/{memberId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "phone": "9000000099",
  "isActive": true,
  "extraFields": {
    "room": "301",
    "bed": "A",
    "monthlyRent": 7500
  }
}
```

### Create Member Login (Owner only)
```http
POST /api/members/{memberId}/create-login
Authorization: Bearer {owner-token}
Content-Type: application/json

{
  "email": "arjun.login@example.com",
  "password": "Member@123"
}
```

---

## Dues & Payments

### Generate Dues — All Members for Current Month
```http
POST /api/dues/generate-month
Authorization: Bearer {token}
Content-Type: application/json

{
  "year": 2024,
  "month": 3
}
```
> Amount auto-extracted from member.ExtraFieldsJson (looks for: monthlyRent, monthlyFee, amount, fee, rent)

### Generate Dues — Single Member with Override
```http
POST /api/dues/generate-month
Authorization: Bearer {token}
Content-Type: application/json

{
  "year": 2024,
  "month": 3,
  "memberId": "00000000-0000-0000-0000-000000000020",
  "amountOverride": 6500,
  "notes": "Late fee included"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "generated": 3,
    "skipped": 0,
    "skippedReasons": []
  }
}
```

### Query Dues
```http
GET /api/dues?year=2024&month=3&status=Due
GET /api/dues?memberId={memberId}
GET /api/dues?status=Partial
Authorization: Bearer {token}
```

### Get Due Detail (with payment history)
```http
GET /api/dues/{dueId}
Authorization: Bearer {token}
```

### Record Payment (supports partial)
```http
POST /api/dues/{dueId}/payments
Authorization: Bearer {token}
Content-Type: application/json

{
  "amountPaid": 3500,
  "paidOn": "2024-03-15T10:00:00Z",
  "method": 4,
  "notes": "UPI payment by Arjun"
}
```
> Method: 1=Manual, 2=Cash, 3=BankTransfer, 4=UPI, 5=Card, 6=Other

> If this payment completes the due (cumulative >= due amount), status auto-updates to **Paid**.
> Otherwise it becomes **Partial**.

### Payment History
```http
GET /api/payments?memberId={memberId}&from=2024-01-01&to=2024-03-31
Authorization: Bearer {token}
```

---

## Dashboard (Owner + Staff)

### Summary Totals
```http
GET /api/dashboard/summary?from=2024-01-01&to=2024-03-31
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalDueAmount": 52500.00,
    "totalPaidAmount": 36000.00,
    "totalUnpaidAmount": 16500.00,
    "memberCount": 3,
    "dueCount": 9,
    "paidCount": 6,
    "partialCount": 1,
    "unpaidCount": 2
  }
}
```

### Monthly Chart (for charting)
```http
GET /api/dashboard/monthly?year=2024
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    { "month": 1, "monthName": "Jan", "totalDue": 17500.00, "totalPaid": 15000.00, "balance": 2500.00, "dueCount": 3, "paidCount": 2 },
    { "month": 2, "monthName": "Feb", "totalDue": 17500.00, "totalPaid": 17500.00, "balance": 0.00, "dueCount": 3, "paidCount": 3 },
    ...
  ]
}
```

### Members Due Status Table
```http
GET /api/dashboard/members-due?year=2024&month=3&search=&page=1&pageSize=20
Authorization: Bearer {token}
```

---

## Member Portal (Member role only)

### My Dues
```http
GET /api/member/dues
GET /api/member/dues?year=2024&month=3
GET /api/member/dues?status=Partial
Authorization: Bearer {member-token}
```

### My Payment History
```http
GET /api/member/payments
GET /api/member/payments?from=2024-01-01&to=2024-03-31
Authorization: Bearer {member-token}
```

---

## Error Response Format

All errors follow:
```json
{
  "success": false,
  "error": "Descriptive error message",
  "statusCode": 400
}
```

Common HTTP codes:
- `400` Bad Request — validation or business logic error
- `401` Unauthorized — missing or invalid token
- `403` Forbidden — valid token but wrong role or cross-org access
- `404` Not Found — resource does not exist in caller's org
- `500` Internal Server Error — unexpected error (logged server-side)

---

## Multi-Tenancy Rules

- Every Owner/Staff/Member token contains `org` claim (OrganizationId)
- All service queries are automatically scoped to that org
- Cross-org access is impossible — wrong org returns 404 (not 403, to avoid data leakage)
- SuperAdmin tokens have no org claim and can access everything
- Deactivated org prevents all logins for users in that org

---

## ExtraFieldsJson — Business-Specific Fields

The `extraFields` field in member create/update accepts any JSON object.
Common patterns by business type:

**Hostel:**
```json
{ "room": "101", "bed": "A", "monthlyRent": 6000, "advance": 12000, "deposit": 5000, "admissionFee": 500, "discount": 200 }
```

**Tuition:**
```json
{ "course": "NEET", "batch": "Evening B", "monthlyFee": 4500, "subjects": ["Biology", "Chemistry"] }
```

**Gym:**
```json
{ "plan": "Gold", "amount": 2500, "personalTrainer": true, "lockerNumber": "L12", "startDate": "2024-01-01" }
```

**Due generation auto-reads:** `monthlyRent`, `monthlyFee`, `amount`, `fee`, `rent` (first match wins).

---

## Partial Payment Flow

1. Due created: amount=6000, status=**Due**
2. Payment of 3000: cumulative=3000 < 6000 → status=**Partial**
3. Payment of 3000: cumulative=6000 >= 6000 → status=**Paid**
4. Further payments rejected (already Paid)
