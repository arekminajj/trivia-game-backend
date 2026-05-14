# Development Plan

## Context

This is the backend service for the trivia game mobile app built for the
**Programowanie aplikacji mobilnych** course. The project is graded by an LLM
that receives the full repository and evaluates it against the criteria below.

---

## Grading Criteria (100 pts total)

| Category | Points | Notes |
|----------|--------|-------|
| Working, stable basic features | 30 | Core game flow must be solid |
| Code quality and architecture | 20 | Clean Architecture already in place |
| UI and UX | 15 | Mobile client responsibility |
| Network communication and data | 15 | External API + error handling |
| Presentation and documentation | 10 | README, docs, clear instructions |
| Additional features and creativity | 10 | Tests, auth, CI/CD, sensors, etc. |

**Individual grade = 60% role grade + 40% overall app grade**

---

## Current State Assessment

### Strong
- Clean Architecture (Domain / Application / Infrastructure / Presentation)
- SignalR real-time hub with full game flow
- External API integration (OpenTDB)
- Per-question server-side timer with race-condition guard
- README + SignalR protocol docs (`docs/SIGNALR.md`)
- Python integration tests covering REST + full 2-player game

### Gaps (ordered by grading impact)

| Priority | Gap | Grading impact |
|----------|-----|----------------|
| 1 | No CI/CD pipeline | Grader explicitly checks for it |
| 2 | No C# unit tests | Tests are a stated criterion; Python tests don't count as unit tests |
| 3 | Network errors silently swallowed | "Network communication and data" (15 pts) |
| 4 | No authentication | "Additional features" (10 pts) |
| 5 | In-memory storage only | "Local database" is a stated criterion (15 pts) |
| 6 | No disconnect handling | Game deadlocks if a player drops mid-game |
| 7 | No answer validation | Any string accepted, not just valid answers |

---

## Implementation Plan

### Step 1 — GitHub Actions CI  `feat: add CI pipeline`
- Workflow: build + run tests on every push/PR
- File: `.github/workflows/ci.yml`
- Runs `dotnet build` and `dotnet test`

### Step 2 — C# Unit Tests (xUnit)  `test: add xUnit unit tests`
- New project: `trivia-game.Tests/`
- Test targets:
  - `Room` entity — state machine transitions, scoring, answer shuffle
  - `GameService` — `SubmitAnswer`, `TimeOutRound`, race-condition guard
  - `RoomService` — create room, join room, invalid code
- Mock `IRoomRepository` and `ITriviaProvider` with NSubstitute or Moq

### Step 3 — Network Error Handling  `feat: improve network error handling`
- Add exception middleware (`UseExceptionHandler`) with structured JSON error responses
- OpenTDB client: add retry policy with Polly (3 retries, exponential backoff)
- Return meaningful HTTP error codes when OpenTDB is unavailable (503)
- Document offline behaviour in README

### Step 4 — Authentication  `feat: add API key authentication`
- Simple API-key middleware on room creation (`X-Api-Key` header)
- Key configured in `appsettings.json`
- Protects `POST /api/rooms` from anonymous abuse
- Document in README

### Step 5 — Disconnect Handling  `feat: handle player disconnect`
- Override `OnDisconnectedAsync` in `GameHub`
- Remove player from room or mark as disconnected
- If game is in progress, treat disconnected players as having timed out
  (skip their answer in `AllPlayersAnswered`)

### Step 6 — Answer Validation  `fix: validate submitted answers`
- `Room.SubmitAnswer` — reject answers not in `CurrentShuffledAnswers`
- Return meaningful error to client

---

## Deferred / Out of Scope for Backend

- **Local database (SQLite/Redis)** — in-memory store is intentional MVP;
  noted in code comments. Adds complexity for limited grading gain.
- **Push notifications** — mobile client responsibility.
- **Sensors** — mobile client responsibility.
- **Dark mode / UI** — mobile client responsibility.

---

## Commit Convention

Format: `type: short description`

Types: `feat`, `fix`, `test`, `docs`, `refactor`, `ci`

Always pause before committing — user commits manually.
