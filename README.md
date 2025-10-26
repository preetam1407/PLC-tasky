Assignments
PLC October 2025 home assignments. The repository contains two independent solutions that share .NET backends and TypeScript/React frontends.

## Deployment URL:
Assignment 1 
 ```bash
[https://basic-task-manager-flame.vercel.app/](https://basic-task-manager-flame.vercel.app/)
   ```
Assignment 2 
 ```bash
[https://basic-task-manager-flame.vercel.app/](https://mini-project-manager-eta.vercel.app/)
   ```
   
## Project Layout

```
tasky/
├── src/
│   ├── Tasky.Api                 # Assignment 1
│   ├── Tasky.Application
│   ├── Tasky.Domain
│   ├── Tasky.Infrastructure
│   ├── TaskyV2.Api               # Assignment 2 
│   ├── TaskyV2.Application
│   ├── TaskyV2.Domain
│   └── TaskyV2.Infrastructure
└── apps/
    ├── tasky-v1-client           # React + Vite frontend for Assignment 1
    └── tasky-v2-client           # React + Vite frontend for Assignment 2
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- Node.js 18+ (Node 22 LTS recommended)
- npm, pnpm, or yarn (examples below use npm)

> **Note**  
> The execution environment used to prepare this submission does not ship with the .NET SDK, so `dotnet build` could not be executed here. The solution compiles locally on macOS/Windows with .NET 8.0.4xx.

## Assignment 1 – Basic Task Manager

### Backend (`Tasky.Api`)

1. Restore and run:
   ```bash
   dotnet restore
   dotnet watch --project src/Tasky.Api
   ```
2. Swagger UI is available at `https://localhost:7200/swagger`.
3. Endpoints (`/api/v1/tasks`):
   - `GET /` – list with filtering (status, search) & pagination metadata
   - `POST /` – create
   - `PUT /{id}` – update description/completion
   - `PATCH /{id}/toggle` – toggle completion
   - `DELETE /{id}` – remove

### Frontend (`apps/tasky-v1-client`)

1. Install dependencies and start Vite:
   ```bash
   cd apps/tasky-v1-client
   npm install
   npm run dev
   ```
2. The dev server runs on `http://localhost:5173` and proxies API calls to the backend.
3. Features:
   - Task list with live filters (All / Active / Completed) and search
   - Create, inline edit, toggle, delete tasks
   - Persisted filter selection via `localStorage`
   - Modern UI with keyboard shortcuts, loading/busy states

## Assignment 2 – Mini Project Manager

### Backend (`TaskyV2.Api`)

1. Apply database migrations (SQLite):
   ```bash
   dotnet ef database update -p src/TaskyV2.Infrastructure -s src/TaskyV2.Api
   ```
2. Start the API:
   ```bash
   dotnet watch --project src/TaskyV2.Api
   ```
3. Swagger UI: `https://localhost:7302/swagger` (HTTP fallback: `http://localhost:5302/swagger`).
4. Highlights:
   - JWT auth (`/api/v1/auth/register`, `/api/v1/auth/login`)
   - Projects (`GET/POST/GET{id}/PUT{id}/DELETE{id}`)
   - Tasks nested under projects plus flat routes (`PUT/PATCH/DELETE /api/v1/tasks/{taskId}`)
   - Smart scheduler (`POST /api/v1/projects/{projectId}/schedule`) generating day-by-day plans
   - ProblemDetails + FluentValidation for consistent errors

### Frontend (`apps/tasky-v2-client`)

1. Install and run:
   ```bash
   cd apps/tasky-v2-client
   npm install
   npm run dev
   ```
2. The app runs on `http://localhost:5174` with proxying to `https://localhost:7302` (override via `VITE_API_PROXY_TARGET` if needed).
3. Features:
   - Registration & login backed by JWT, token stored securely in memory + `localStorage`
   - Dashboard with project CRUD, inline rename/delete, creation form
   - Project detail page with task CRUD, completion toggle, due dates, stats
   - Smart Scheduler UI (working days, capacity, start/end dates) with rich results
   - Responsive layout, accessible focus states, consistent error handling

## Testing 

- [ ] `dotnet build` (requires local .NET SDK)
- [ ] `npm run build` inside each frontend for production bundles
- [ ] Manual end-to-end verification via Swagger + Vite dev servers
