# Assumptions

1. **Student ID format.** The example is 17 digits, but the spec doesn't fix a format. IDs are validated leniently: 5-32 letters, digits or hyphens. This avoids rejecting valid IDs; invalid or missing IDs return 400.
2. **Class and shift identification.** A class/shift is one record with a unique name (e.g. "Class 6 - Morning Shift"). Names are unique, so duplicates return 409.
3. **IDs.** Generated as prefixed random strings (`c_xxxxxxxxxx`, `h_xxxxxxxxxx`).
4. **Reservation expiry.** Duration comes from `HOLD_DURATION_SECONDS` (default 600). Expiry is enforced in two layers: reads treat a HELD row past `expires_at` as EXPIRED and do not count it as held, and a background worker persists EXPIRED every 5 seconds. Apply also expires stale holds of that class inside its transaction.
5. **Concurrency.** Row lock on the class (`SELECT ... FOR UPDATE`) inside a transaction. Assumes one PostgreSQL database; works across multiple backend instances.
6. **Duplicate applications.** A partial unique index on (class_id, student_id) where status is HELD or CONFIRMED enforces the rule in the database. Returns 409. A student may re-apply after EXPIRED or CANCELLED.
7. **Confirm.** Idempotent: confirming a CONFIRMED hold returns 200 without changing counts. Confirming an expired hold returns 409; a cancelled hold returns 409.
8. **Cancel.** Only the owner may cancel (403 otherwise). HELD becomes CANCELLED (204); already CANCELLED returns 204; CONFIRMED or EXPIRED return 409.
9. **Ownership.** Confirm and cancel compare `X-Student-Id` with the hold's student; mismatch returns 403. There is no authentication beyond this header, as the spec says.
10. **Error format.** All errors return `{ "code": "...", "message": "..." }` with 400/403/404/409. Unexpected errors return 500.
11. **Admin access.** The admin area has no login, as the spec does not require one.
12. **Payment.** Simulated: Pay Admission Fee calls confirm.
13. **Refresh.** The frontend polls every 3 seconds; countdowns tick every second.
14. **Persistence.** PostgreSQL data lives in a named Docker volume; migrations run automatically at startup.
15. **API URL.** `VITE_API_URL` is baked in at frontend build time and points to the host-reachable backend (`http://localhost:8080`), since the browser calls the API directly.