# Trivia Game - Backend Service

Backend service for the mobile trivia game application developed for the **Programowanie aplikacji mobilnych** course.

Built with ASP.NET Core 10. Provides a REST API for room management and a SignalR hub for real-time multiplayer game flow. Questions are fetched from the [Open Trivia Database](https://opentdb.com/).

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Python 3.9+ (only for running the integration test script)

## Public deployment

The backend is publicly accessible via a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/) and is live at:

| Resource | URL |
|----------|-----|
| API base | `https://trivia.arkadiuszcios.online` |
| Swagger UI | [`https://trivia.arkadiuszcios.online/scalar/v1`](https://trivia.arkadiuszcios.online/scalar/v1) |
| SignalR hub | `wss://trivia.arkadiuszcios.online/hubs/game` |
| Privacy policy | [`https://trivia.arkadiuszcios.online/privacy`](https://trivia.arkadiuszcios.online/privacy) |

---

## Running locally

```bash
dotnet run
```

Server starts at `http://localhost:5114`.  
Swagger UI: `http://localhost:5114/scalar/v1`

---

## REST API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/rooms` | Create a room and fetch trivia questions |
| `POST` | `/api/rooms/join` | Join an existing room by code |
| `GET` | `/api/rooms` | List all active rooms |
| `GET` | `/api/trivia/categories` | List available trivia categories |

### Create room — `POST /api/rooms`

```json
{
  "ownerName": "Alice",
  "amount": 10,
  "categoryId": 9,
  "type": "multiple"
}
```

`type` accepts `"multiple"` or `"boolean"`. `categoryId` and `type` are optional.  
Returns a `RoomResponse`. The owner's player UUID is in `owner.uuid`.

### Join room — `POST /api/rooms/join`

```json
{
  "joinCode": "ABC123",
  "displayName": "Bob"
}
```

Returns `{ "yourPlayerUuid": "...", "room": { ... } }`.  
Save `yourPlayerUuid` — it is required for all SignalR calls.

---

## SignalR Hub — `/hubs/game`

> Full protocol spec: [docs/SIGNALR.md](docs/SIGNALR.md)

Connect via the SignalR client after calling the REST join/create endpoint.

### Client → Server

| Method | Arguments | Description |
|--------|-----------|-------------|
| `ConnectToRoom` | `roomCode, playerUuid` | Join the room's real-time group |
| `StartGame` | `roomCode, playerUuid` | Start the game (owner only) |
| `SubmitAnswer` | `roomCode, playerUuid, answer` | Submit an answer for the current question |

`answer` must be one of the strings returned in the question's `answers` array.

### Server → Client

| Event | Payload | Sent to |
|-------|---------|---------|
| `ConnectedToRoom` | `RoomResponse` | Caller |
| `PlayerConnected` | `PlayerResponse` | Others in room |
| `GameStarted` | `QuestionResponse` | Everyone in room |
| `AnswerAccepted` | — | Caller |
| `RoundEnded` | `RoundResultResponse` | Everyone in room |
| `QuestionReceived` | `QuestionResponse` | Everyone in room |
| `GameEnded` | `PlayerResponse[]` (leaderboard) | Everyone in room |
| `Error` | `string` | Caller |

### Typical flow

```
POST /api/rooms          → owner gets roomCode + ownerUuid
POST /api/rooms/join     → player gets playerUuid

[Both connect via SignalR]
ConnectToRoom(roomCode, playerUuid)

[Owner starts game]
StartGame(roomCode, ownerUuid)
  → GameStarted(question) broadcast to all

[Each player submits answer]
SubmitAnswer(roomCode, playerUuid, answer)
  → AnswerAccepted (caller)
  → RoundEnded + QuestionReceived (all, when everyone answered)
  → or RoundEnded + GameEnded (all, after last question)
```

### QuestionResponse shape

```json
{
  "index": 0,
  "total": 10,
  "text": "What is the capital of France?",
  "category": "Geography",
  "difficulty": "easy",
  "type": "multiple",
  "answers": ["Berlin", "Paris", "Madrid", "Rome"]
}
```

The correct answer is **not** included. `answers` is shuffled consistently across all clients.

### Scoring

| Difficulty | Points |
|------------|--------|
| Easy | 100 |
| Medium | 200 |
| Hard | 300 |

---

## Error handling

All API errors are returned as structured JSON:

```json
{ "error": "Human-readable message" }
```

| HTTP status | Cause |
|-------------|-------|
| `400` | Invalid request parameters (e.g. bad question type or filter combination) |
| `404` | Room not found |
| `422` | Invalid game operation (e.g. non-owner trying to start) |
| `503` | OpenTDB is unreachable after 3 retries with exponential back-off |
| `500` | Unexpected server error |

### Offline / OpenTDB unavailable

Room creation (`POST /api/rooms`) fetches questions from [OpenTDB](https://opentdb.com/).
If OpenTDB is unreachable the server retries the request **3 times** with exponential back-off
(2 s → 4 s → 8 s). If all retries fail the endpoint returns `503 Service Unavailable` with an
explanatory message. No game data is lost — in-progress rooms are unaffected because questions
are fetched once at room creation and stored in memory for the duration of the game.

---

## Privacy Policy

Available at [`https://trivia.arkadiuszcios.online/privacy`](https://trivia.arkadiuszcios.online/privacy) and in [`PRIVACY.md`](PRIVACY.md).

---

## Integration tests

```bash
cd Tests
pip install -r requirements.txt
python test_game.py
```

Covers: category listing, room create/join, invalid join, room listing, and a full 2-player SignalR game.
