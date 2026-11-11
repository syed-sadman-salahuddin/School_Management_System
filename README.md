# School Admission System

Online admission system for a private school with First Come, First Served (FCFS) seat reservation. Parents hold a seat for a class/shift, pay the (simulated) admission fee to confirm, and the School Admin manages classes and capacity. Seat capacity is never exceeded, even under heavy concurrent load.

## Tech stack
- Backend: C#, .NET 10, ASP.NET Core Web API, Entity Framework Core, PostgreSQL 16
- Frontend: React 19, Vite, Tailwind CSS
- Infrastructure: Docker, Docker Compose, nginx
- Tests: xUnit with WebApplicationFactory against real PostgreSQL

## Project structure
```
backend/
  SchoolAdmission.Api/     Controllers, Services, Models, DTOs, Data, Middleware, Exceptions
  SchoolAdmission.Tests/   xUnit tests (hold, confirm idempotency, concurrency)
frontend/                  React app (src/pages, src/components, api.js)
docker-compose.yml
README.md  ASSUMPTIONS.md  AI_USAGE.md
```

## Run with Docker (only Docker is required)
```
docker compose up --build
```
- Frontend: http://localhost:3000
- Backend API: http://localhost:8080 (Swagger UI: http://localhost:8080/swagger)

Data is stored in the `pgdata` Docker volume and survives `docker compose down` followed by `docker compose up`. Do not use `docker compose down -v`, which deletes the volume.

## Using the app
- **School Admin** tab: create a class/shift with a seat capacity and see live seat statistics.
- **Parent / Guardian** tab: enter the student's birth registration number, apply to a class, pay the admission fee before the hold expires, cancel, and view all applications.
- The UI refreshes automatically every 3 seconds.

## Configuration
| Variable | Where | Default |
|---|---|---|
| `ConnectionStrings__Default` | backend | set in docker-compose.yml |
| `HOLD_DURATION_SECONDS` | backend | `600` |
| `VITE_API_URL` | frontend build arg | `http://localhost:8080` |

## Run the automated tests
Requires the database container to be running with port 5432 published:
```
docker compose up -d db
cd backend
dotnet test
```
Tests: (1) basic seat hold, (2) idempotent confirmation, (3) concurrency: 100 simultaneous requests for a 10-seat class result in exactly 10 holds. A separate `admission_test` database is used; override with `TEST_CONNECTION_STRING`.

## API
| Method | Endpoint | Header |
|---|---|---|
| POST | /classes | |
| GET | /classes | |
| GET | /classes/{classId} | |
| POST | /classes/{classId}/holds | X-Student-Id |
| GET | /holds/{holdId} | |
| POST | /holds/{holdId}/confirm | X-Student-Id |
| DELETE | /holds/{holdId} | X-Student-Id |
| GET | /me/holds | X-Student-Id |

## How concurrency is handled
Applying runs in a database transaction that locks the class row (`SELECT ... FOR UPDATE`), then counts seats and inserts the hold. Requests for the same class are serialized; different classes do not block each other. A partial unique index enforces one active or confirmed application per student per class.