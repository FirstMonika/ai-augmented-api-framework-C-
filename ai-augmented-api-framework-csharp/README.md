# AI-Augmented API Framework (C#)

AI-powered **Playwright for .NET + NUnit** API testing framework for the Restful-Booker microservice.
This is the C# port of the original JavaScript framework: same 79 test cases, same IDs, same design.

- 79 API tests (positive, negative, edge, security) with shape / range / consistency assertions
- Environment switching (`dev` / `uat` / `prod`) with a single `ENV` variable
- Three optional OpenAI agents: test generator, assertion suggester, report analyzer
- Tests run fully without an OpenAI key

## Project Structure

```
AiAugmentedApiFramework.csproj
Agents/            BaseAgent, TestGeneratorAgent, AssertionAgent, ReportAnalyzerAgent
Base/              ApiTestBase  (request context, auth token, booking cleanup)
Config/            EnvConfig (ENV loader), GlobalSetup (/ping health check), env/{dev,uat,prod}.json
Helpers/           ApiClient, ApiResult, DataFactory, BookingAssert
Tests/             AuthTests, BookingCreateTests, BookingReadTests, BookingUpdateTests,
                   BookingPatchTests, BookingDeleteTests, SecurityTests, AgentRunner (explicit)
.github/workflows/tests.yml
```

JS → C# mapping: `ApiClient.js` → `ApiClient.cs`, `DataFactory.js` → `DataFactory.cs`,
`assertions.js` → `BookingAssert.cs`, `fixtures/index.js` → `ApiTestBase.cs`,
`global-setup.js` → `GlobalSetup.cs`, `environments.js` → `EnvConfig.cs`.

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |

No browser install is needed - API testing uses Playwright's request context only.

## Setup

```bash
git clone <your-repo-url>
cd ai-augmented-api-framework
dotnet restore
```

## Environments

Each file in `Config/env/` holds `baseURL`, `auth.username/password` and `throttleMs`.
Edit `uat.json` / `prod.json` with real URLs. The `ENV` variable picks the file (default `dev`);
an unknown value throws at startup. These environment variables override file values:
`BASE_URL`, `BOOKER_USERNAME`, `BOOKER_PASSWORD`, `OPENAI_API_KEY`.

## Running tests

```bash
# macOS / Linux                         # Windows PowerShell
dotnet test                             dotnet test
ENV=uat dotnet test                     $env:ENV="uat"; dotnet test

# one suite (by category): Auth, Create, Read, Update, Patch, Delete, Security
dotnet test --filter "Category=Auth"
dotnet test --filter "Category=Security"

# one test by ID
dotnet test --filter "Name~AUTH_001"
dotnet test --filter "Name~SEC_001"

# produce a results file for the report analyzer
dotnet test --logger "trx;LogFileName=results.trx" --results-directory reports
```

## AI agents (optional)

Set `OPENAI_API_KEY` (env var recommended - never commit it). Agents run as `[Explicit]` tests, so they
never execute during a normal `dotnet test`:

```bash
# generate test cases from the API spec -> reports/ai-generated-test-cases.json
dotnet test --filter "Name=GenerateTestCases"

# executive summary of the last run -> reports/ai-summary.md
dotnet test --logger "trx;LogFileName=results.trx" --results-directory reports
dotnet test --filter "Name=AnalyzeReport"
```

Using `AssertionAgent` in your own code:

```csharp
var json = await new AssertionAgent().SuggestAssertionsAsync(responseText, "POST /booking response");
```

Without a key the agents log a warning and skip; all 79 tests are unaffected.

## Assertion philosophy

Every test asserts more than the status code - shape and types (`BookingAssert.Shape`),
ranges (`totalprice >= 0`, `bookingid > 0`), and consistency (`BookingAssert.Matches` after a GET).

## Key design decisions

- **Sequential execution** - NUnit runs tests sequentially by default; the API is a shared sandbox.
- **Clean slate per test** - `NewBookingAsync()` creates a booking and `ApiTestBase` deletes everything a test created in `[TearDown]`.
- **Environment-first config** - nothing hard-coded in tests; switch with `ENV=`.
- **Agents are optional** - they own no assertions.
- **Dictionary payloads** - tests can remove or retype fields (`.Without("firstname")`, `payload["firstname"] = 12345`) to build negative cases without extra DTO classes.

## Test counts

| Suite | Tests |
|---|---|
| POST /auth | 12 |
| POST /booking | 14 |
| GET /booking + GET /booking/{id} | 12 |
| PUT /booking/{id} | 10 |
| PATCH /booking/{id} | 11 |
| DELETE /booking/{id} | 9 |
| Security | 11 |
| **Total** | **79** |

Test IDs map 1-to-1 to the `test-case-design.docx` IDs (`AUTH-001` → `AUTH_001_...`, `CRT-`, `READ-`, `PUT-`, `PATCH-`, `DEL-`, `SEC-`).

## CI/CD

`.github/workflows/tests.yml` runs on push to `main`/`develop`, PRs to `main`, nightly 06:00 UTC, and manually.
It uploads `reports/` (TRX results and the AI summary when a key is set). Optional secrets:
`OPENAI_API_KEY`, `BOOKER_USERNAME`, `BOOKER_PASSWORD`; optional variable `ENV`.

## Troubleshooting

- **Health check fails** - the public API sometimes sleeps/goes down; retry. For UAT/prod verify `baseURL`.
- **Wrong environment** - check `echo $ENV` (`$env:ENV` on PowerShell).
- **Intermittent 404s** - the sandbox resets periodically; re-run.
- **Rate limiting** - raise `throttleMs` in the env JSON file.
