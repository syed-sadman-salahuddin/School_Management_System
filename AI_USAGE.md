# AI Usage

## Tools used
- Claude (Anthropic) as a coding assistant.

## What AI helped with
- Backend code: EF Core models, services, controllers, middleware, background worker.
- React UI, Tailwind styling, Dockerfiles and docker-compose.

## How AI was used
I described the requirements, asked for a step-by-step plan, then pasted and ran each part, reporting errors back for fixes.

## Mistakes and how they were fixed
- **.NET version:** instructions assumed .NET 8, but only .NET 10 was installed. I switched to .NET 10 and updated packages and Docker images.
- **Swashbuckle conflict:** newest Swashbuckle clashed with the template's OpenApi package (NU1107). Fixed by removing `Microsoft.AspNetCore.OpenApi`.
- **Swagger test:** pasted PowerShell text into the Swagger body, giving a 400. The API's error handling was working; I retested with valid JSON.
- **Empty frontend files:** blank page and import errors were caused by files created but not filled. Found via the browser console and fixed by pasting the code.
- **Corrupted package.json / wrong npm package:** recreated the Vite project after clearing the npm cache.

## Final decisions
I chose the row-lock approach over serializable retries for simplicity, verified behavior with the automated tests, and checked persistence with `docker compose down` / `up`.