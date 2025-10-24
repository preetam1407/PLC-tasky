==============================================
🧠 PLC Home Coding Assignment – October 2025
=============================================

Author: Kamal Vaishnav
Platform: macOS 15
Framework: .NET 8.0.415 (ASP.NET Core Minimal APIs)
Completed: October 2025

---

📘 Overview
-----------

This repository contains both Home Assignments implemented in .NET 8.

ASSIGNMENT 1 → Basic Task Manager (In-Memory)
ASSIGNMENT 2 → Mini Project Manager (EF Core + JWT + Smart Scheduler)

---

🧰 Tech Stack
-------------

- .NET 8 Web API (Minimal APIs)
- Entity Framework Core 8 (SQLite)
- JWT Authentication (HS256)
- FluentValidation
- BCrypt.Net-Next (password hashing)
- Swagger / OpenAPI 3
- ProblemDetails (RFC 7807)
- CORS enabled for React (localhost:5173 / 3000)

---

📂 Project Structure
--------------------

src/
 ├─ Tasky.Domain/
 ├─ Tasky.Application/
 ├─ Tasky.Infrastructure/
 ├─ Tasky.Api/                  ← Assignment 1 (In-Memory)
 │
 ├─ TaskyV2.Domain/             ← Assignment 2 (User, Project, ProjectTask)
 ├─ TaskyV2.Application/        ← DTOs + Validators
 ├─ TaskyV2.Infrastructure/     ← EF Core + Auth + Services
 └─ TaskyV2.Api/                ← JWT + Smart Scheduler

Each layer follows Clean Architecture:
Domain → Application → Infrastructure → API

---

🧱 Prerequisites (macOS)
------------------------

1️⃣ Install .NET 8 SDK
   brew install --cask dotnet-sdk@8

2️⃣ Install EF CLI
   dotnet tool install -g dotnet-ef

3️⃣ Install VS Code + Extensions

- ms-dotnettools.csharp
- ms-dotnettools.csdevkit
- humao.rest-client
- EditorConfig.EditorConfig

Verify setup:
   dotnet --info

---

🚀 Getting Started
------------------

STEP 1 — Clone & Restore
   git clone https://github.com/`<your-repo>`.git
   cd plc-assignment
   dotnet restore

---

ASSIGNMENT 1 – Basic Task Manager (In-Memory)
----------------------------------------------

Run:
   dotnet watch --project src/Tasky.Api

Swagger:
   https://localhost:7200/swagger

Endpoints:
   GET    /api/v1/tasks
   POST   /api/v1/tasks
   PUT    /api/v1/tasks/{id}
   PATCH  /api/v1/tasks/{id}/toggle
   DELETE /api/v1/tasks/{id}

Features:

- CRUD + toggle task completion
- In-memory store (resets on restart)
- Validation via FluentValidation
- Swagger UI for all endpoints

---

ASSIGNMENT 2 – Mini Project Manager (EF + JWT)
-----------------------------------------------

Run database migrations:
   dotnet ef database update -p src/TaskyV2.Infrastructure -s src/TaskyV2.Api

Start the API:
   dotnet watch --project src/TaskyV2.Api

Swagger:
   https://localhost:7300/swagger

Default Ports:
   Tasky.Api (A1): 7200
   TaskyV2.Api (A2): 7300

Database Path:
   src/TaskyV2.Api/data/tasky_v2.db

---

🔑 Authentication Flow (Assignment 2)
-------------------------------------

REGISTER
POST /api/v1/auth/register
Body:
{
  "email": "demo@tasky.dev",
  "password": "Passw0rd!"
}
→ 204 No Content

LOGIN
POST /api/v1/auth/login
Body:
{
  "email": "demo@tasky.dev",
  "password": "Passw0rd!"
}
→ 200 OK
{
  "token": "`<JWT-token>`"
}

AUTHORIZE (Swagger)
Click Authorize → Paste ONLY the JWT (no "Bearer ")

Now you can use protected endpoints:

- /api/v1/projects
- /api/v1/projects/{id}/tasks
- /api/v1/projects/{id}/schedule

---

🔐 Protected Endpoints (Assignment 2)
-------------------------------------

PROJECTS
GET    /api/v1/projects
POST   /api/v1/projects
PUT    /api/v1/projects/{projectId}
DELETE /api/v1/projects/{projectId}

TASKS (scoped under projects)
GET    /api/v1/projects/{projectId}/tasks
POST   /api/v1/projects/{projectId}/tasks
PUT    /api/v1/projects/{projectId}/tasks/{taskId}
PATCH  /api/v1/projects/{projectId}/tasks/{taskId}/toggle
DELETE /api/v1/projects/{projectId}/tasks/{taskId}

SCHEDULER
POST   /api/v1/projects/{projectId}/schedule

---

🧮 Smart Scheduler API Example
------------------------------

POST /api/v1/projects/{projectId}/schedule

Request:
{
  "startDate": "2025-10-25",
  "endDate": "2025-11-10",
  "dailyCapacity": 3,
  "workingDays": ["Mon","Tue","Wed","Thu","Fri"]
}

Response:
{
  "projectId": "…",
  "generatedAtUtc": "2025-10-24T14:05:00Z",
  "days": [
    { "date": "2025-10-27", "taskIds": ["…","…"] },
    { "date": "2025-10-28", "taskIds": ["…"] }
  ]
}

---

🧪 Testing & Verification
-------------------------

# 1. Register → Login → Get Token

POST /api/v1/auth/register
POST /api/v1/auth/login

# 2. Authorize Swagger using token

# 3. CRUD Projects & Tasks

POST /api/v1/projects
POST /api/v1/projects/{projectId}/tasks

# 4. Smart Scheduler

POST /api/v1/projects/{projectId}/schedule

# 5. Health Check

GET /healthz

All protected endpoints should return 200/201 after authorization.
Without a token → 401 Unauthorized.

---

🏗️ Architecture Summary
-------------------------

- Domain layer: core entities & invariants.
- Application layer: DTOs + Validation logic.
- Infrastructure layer: EF Core context, Auth & Services.
- API layer: Minimal APIs (grouped routes).
- Clean DI: Each layer injected via constructor.
- JWT token carries sub (userId) & email claims.
- CORS open for local React dev.

---

💾 Persistence
--------------

- EF Core 8 + SQLite file (`tasky_v2.db`)
- Cascade deletes (Projects → Tasks)
- Unique constraint on User.Email
- Emails stored lowercased

---

🛠️ Developer Tips
-------------------

- JWT token lifespan: 2 hours
- Secret key in `appsettings.Development.json`
  Must be >= 128-bit (32 chars recommended)
- DataAnnotations + FluentValidation for all inputs
- ProblemDetails (RFC 7807) responses on validation errors
- CORS pre-configured for React dev ports

---

🧑‍⚖️ Reviewer Checklist
---------------------------

✅ Verify Assignment 1:

- Swagger (Tasky.Api)
- CRUD + toggle working (in-memory)

✅ Verify Assignment 2:

- Register & Login → 204 + 200
- Swagger Authorize → paste token
- Projects & Tasks CRUD work
- 401 without token, 200 with token
- Scheduler generates valid plan
- ProblemDetails JSON on bad payloads

✅ Code Quality:

- .NET 8 syntax (Minimal APIs)
- Clean Architecture layers
- Proper dependency injection
- Clear project structure
- JWT implemented correctly
- Swagger organized with tags

---

🚦 Common Issues & Fixes
------------------------

SQLite Error 14 → create "data" folder manually.
JWT IDX10653 → key too short, use 32+ char key.
401 on login → check DB path or normalize email.
401 on projects → forgot to Authorize Swagger.

---

🧭 Future Improvements
----------------------

- Move SQLite → PostgreSQL for production
- Add refresh tokens & role-based auth
- Unit tests for services (xUnit + FluentAssertions)
- Optional React client for Assignment 2

---

✅ Summary
----------

✔ Assignment 1 – Completed (Tasky.Api)
✔ Assignment 2 – Completed (TaskyV2.Api)
✔ Swagger working with JWT Auth
✔ Smart Scheduler implemented
✔ EF Core migrations & DB verified
✔ Tested end-to-end on macOS 15, .NET 8.0.415

---

🎯 End of README
----------------
