# Test Strategy — Restful-Booker API

**Project:** AI-Augmented API Framework (C# / Playwright for .NET + NUnit) 
**Engineer:** Madhuri Goswami 
**Date:** May 2025  
**API Under Test:** https://restful-booker.herokuapp.com

---

## 1. Scope

This strategy covers automated API testing of the Restful-Booker microservice.
The service exposes a RESTful hotel booking API with seven endpoints covering
authentication, CRUD operations on bookings, and a health check.

**In scope:**
- All seven API endpoints
- Functional correctness (request → response contract)
- Authentication and authorisation enforcement
- Input validation and error handling
- Basic security hygiene (injection resistance, mass assignment)
- Response schema integrity (not just status codes)

**Out of scope:**
- Load / performance testing (the service is a shared public sandbox)
- UI testing (API-only service)
- Data persistence across restarts (the API resets periodically)

---

## 2. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Auth bypass allows unauthenticated mutations | Medium | High | Explicit 403 tests on all write endpoints |
| API accepts malformed data and corrupts state | Medium | High | Type-boundary and injection tests |
| Response schema drifts (field removed / renamed) | Low | High | Shape assertions on every response |
| Inverted dates accepted without error | High | Medium | Documented as known behaviour; flagged |
| Public API rate limiting during CI | Low | Medium | sequential NUnit execution, `throttleMs` delay |
| Token expiry mid-run | Low | Low | Fresh token fixture per test |

---

## 3. Test Categories

### 3.1 Positive (Happy Path)
Verify that the API behaves correctly when given valid, well-formed input.
These form the baseline — if these fail, everything else is moot.

Examples: valid credentials return a token; creating a booking with a full
payload returns the correct shape; updated data is readable via subsequent GET.

### 3.2 Negative
Verify that the API rejects or handles invalid input gracefully — without
crashing (no 5xx) and with a meaningful error signal.

Examples: missing required fields; wrong data types; invalid IDs;
unauthenticated write operations.

### 3.3 Edge / Boundary
Probe the extremes of valid input to find off-by-one errors, truncation bugs,
and undocumented limits.

Examples: single-character names; 1000-character strings; totalprice = 0;
totalprice = 9,999,999; Unicode in name fields; inverted checkin/checkout dates.

### 3.4 Security
Verify that the API does not expose dangerous behaviour under adversarial input.

Examples: SQL injection strings; XSS payloads; path traversal in ID params;
scrambled tokens; mass-assignment of internal fields.

---

## 4. Test Case Summary

| Suite | Tests | Positive | Negative | Edge | Security |
|---|---|---|---|---|---|
| Auth (POST /auth) | 12 | 3 | 6 | 3 | — |
| Create Booking (POST /booking) | 14 | 5 | 4 | 5 | — |
| Read Booking (GET /booking, GET /booking/{id}) | 12 | 7 | 3 | 2 | — |
| Full Update (PUT /booking/{id}) | 10 | 4 | 4 | 2 | — |
| Partial Update (PATCH /booking/{id}) | 11 | 7 | 2 | 2 | — |
| Delete (DELETE /booking/{id}) | 9 | 3 | 3 | 3 | — |
| Security | 11 | — | 2 | — | 9 |
| **Total** | **79** | **29** | **24** | **17** | **9** |

---

## 5. Assertion Strategy

Status codes alone are insufficient. Each test asserts:

1. **HTTP Status** — correct code for the scenario (200, 201, 403, 404, 500)
2. **Response Shape** — all expected fields are present with correct data types
3. **Business Logic** — values are within valid ranges (e.g. `totalprice >= 0`,
   dates are parseable, `bookingid > 0`)
4. **Consistency** — data written via POST/PUT/PATCH is readable via subsequent GET
5. **Negative Signal** — error responses contain a meaningful reason, not a token
6. **No 500s on Bad Input** — malformed input returns 4xx, not an unhandled crash

The `Helpers/BookingAssert.cs` module provides reusable domain-specific assertion
functions (`BookingAssert.Shape`, `BookingAssert.CreateResponse`, etc.) so that
assertion logic is defined once and used consistently across all test files.

---

## 6. AI Agent Architecture

The framework includes three AI agents powered by the OpenAI API:

### TestGeneratorAgent
Accepts an API endpoint specification (method, path, request/response schema)
and returns a structured list of test cases covering positive, negative, edge,
and security scenarios. Used via `dotnet test --filter "Name=GenerateTestCases"` to bootstrap test
case design or to re-analyse the spec after an API change.

### AssertionAgent
Given a real API response body, suggests additional Playwright `Assert.That()` calls
beyond status-code checks. Used during test authoring to ensure no important
assertion is missed. Can also analyse failures — given expected vs actual, it
writes a human-readable explanation of the discrepancy.

### ReportAnalyzerAgent
After a test run, reads the NUnit TRX results file and produces an executive
summary in Markdown: pass rate, failure analysis, risk patterns, and
recommended actions. Run via `dotnet test --filter "Name=AnalyzeReport"`.

All agents degrade gracefully when `OPENAI_API_KEY` is absent — tests still
run and pass; the agents simply skip enrichment with a console warning.

---

## 7. Known API Behaviours (Assumptions Documented)

These behaviours were discovered during test design and are documented here
rather than treated as bugs, since Restful-Booker is a training tool:

| Behaviour | Endpoint | Notes |
|---|---|---|
| Inverted dates (checkout before checkin) are accepted | POST /booking | No server-side validation |
| Auth failure returns 200 with `{ reason: "Bad credentials" }` | POST /auth | Not a 401 |
| Successful delete returns **201** (not 204) | DELETE /booking/{id} | Unusual status code |
| Non-existent ID on PUT/DELETE returns **405** (not 404) | PUT, DELETE | Method Not Allowed |
| `totalprice` with string value may be coerced silently | POST /booking | Lenient type handling |
| Booking is successfull with `totalprice` 0 | POST, PUT | Response returns `totalprice` as 0 |
| The API resets state periodically | All write endpoints | Tests must not rely on pre-existing data |
| Parallel writes can produce race conditions | POST, PUT | NUnit's sequential execution keeps tests ordered |


---

## 8. Test Data Strategy

All test data is generated by `DataFactory.cs` using static, deterministic values.
No production data is used. Each test that creates a booking cleans up after
itself via the `testBooking` fixture, which deletes the record in teardown.

For edge-case tests that require specific values (long strings, special chars,
wrong types), dedicated factory methods are provided
(dictionary payloads mutated in the test, e.g. `.Without("firstname")`).

---

## 9. CI/CD Strategy

The GitHub Actions workflow (`.github/workflows/tests.yml`) runs:
- On every push to `main` / `develop`
- On every pull request to `main`
- Nightly at 06:00 UTC (to catch API regressions)
- Manually via `workflow_dispatch`

Artefacts uploaded per run:
- TRX test results (30-day retention)
- AI summary Markdown (when API key is available)

Sequential execution and `throttleMs` keep the run stable against the
shared public sandbox without hammering it.

---

## 10. What I Would Add With More Time

1. **Contract testing (Pact)** — formalise the consumer-driven contract so
   any breaking API change is caught immediately.
2. **Data-driven tests** — parameterise boundary cases with a table so
   adding a new boundary value is a one-line change.
3. **Performance baseline** — add a lightweight timing assertion
   (`Assert.That(elapsed.TotalMilliseconds, Is.LessThan(2000))`) to detect regression in response times.
4. **Negative auth matrix** — test all write endpoints (PUT/PATCH/DELETE)
   against a full matrix of auth states (no header, wrong token, expired token,
   Basic vs Cookie) to ensure authorisation is enforced consistently.
5. **Schema snapshot testing** — capture the response schema in a JSON Schema
   file and diff it on every run to catch undocumented breaking changes.
