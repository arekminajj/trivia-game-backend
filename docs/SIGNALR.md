# SignalR Communication Protocol

Hub endpoint: **`/hubs/game`**

---

## Overview

The HTTP REST API is used only for room setup. All real-time game communication happens over SignalR. The typical session has three phases:

1. **Setup** — create/join a room via REST, get your `playerUuid`
2. **Connect** — open a SignalR connection and call `ConnectToRoom`
3. **Play** — owner starts the game; all players submit answers each round

---

## Room State Machine

```
  ┌─────────┐   StartGame (owner)   ┌────────────┐   last round ends   ┌──────────┐
  │ Waiting │ ────────────────────► │ InProgress │ ──────────────────► │ Finished │
  └─────────┘                       └────────────┘                      └──────────┘
       ▲                                  │
       │                                  │ round ends (all answered OR timer expired)
       │                          ┌───────┴────────┐
       │                          ▼                ▼
       │                    next question      game over
       │                   QuestionReceived   GameEnded
```

---

## Connection Setup

Connect using the SignalR client library for your platform:

| Platform | Package |
|----------|---------|
| .NET / MAUI | `Microsoft.AspNetCore.SignalR.Client` |
| JavaScript | `@microsoft/signalr` |
| Python (testing) | `signalrcore` |

**Base URL:** `http://<host>:5114`  
**Hub URL:** `http://<host>:5114/hubs/game`

---

## Sequence: Full Game

```
Alice (owner)                  Server                      Bob (player)
     │                            │                             │
     │── POST /api/rooms ────────►│                             │
     │◄─ RoomResponse ────────────│                             │
     │   (owner.uuid = Alice)     │                             │
     │                            │                             │
     │                            │◄── POST /api/rooms/join ───│
     │                            │──► JoinRoomResponse ───────►│
     │                            │    (yourPlayerUuid = Bob)   │
     │                            │                             │
     │── ConnectToRoom ──────────►│                             │
     │◄─ ConnectedToRoom ─────────│                             │
     │                            │◄── ConnectToRoom ───────────│
     │◄─ PlayerConnected ─────────│                             │
     │                            │──► ConnectedToRoom ─────────►│
     │                            │                             │
     │── StartGame ──────────────►│                             │
     │◄─ GameStarted(Q1) ─────────┼────────────────────────────►│
     │                            │   [30s timer starts]        │
     │── SubmitAnswer(Q1) ───────►│                             │
     │◄─ AnswerAccepted ──────────│                             │
     │                            │◄── SubmitAnswer(Q1) ────────│
     │                            │──► AnswerAccepted ──────────►│
     │                            │   [all answered, timer cancelled]
     │◄─ RoundEnded ──────────────┼────────────────────────────►│
     │◄─ QuestionReceived(Q2) ────┼────────────────────────────►│
     │                            │   [30s timer starts]        │
     │         ... repeat for each question ...                  │
     │◄─ RoundEnded ──────────────┼────────────────────────────►│
     │◄─ GameEnded(leaderboard) ──┼────────────────────────────►│
```

### Timeout path (if a player does not answer in time)

```
     │                            │   [timer expires]           │
     │◄─ QuestionTimedOut ────────┼────────────────────────────►│
     │◄─ RoundEnded ──────────────┼────────────────────────────►│
     │◄─ QuestionReceived(Qn) ────┼────────────────────────────►│
     │                            │   [new timer starts]        │
```

---

## Client → Server Methods

### `ConnectToRoom`

Joins the player's SignalR connection to the room's broadcast group. Must be called after the REST join/create.

**Call after:** `POST /api/rooms` or `POST /api/rooms/join`

| Parameter | Type | Description |
|-----------|------|-------------|
| `roomCode` | `string` | 6-character room code from REST response |
| `playerUuid` | `string` | Your player UUID from REST response |

**Responses:**
- `ConnectedToRoom` → sent to caller on success
- `PlayerConnected` → sent to all other players in the room
- `Error` → sent to caller if room or player not found

---

### `StartGame`

Starts the game. Only the room owner can call this. Fetches the first question and starts the per-question timer.

**Call after:** all players have called `ConnectToRoom`

| Parameter | Type | Description |
|-----------|------|-------------|
| `roomCode` | `string` | Room code |
| `playerUuid` | `string` | Must match the room's owner UUID |

**Responses:**
- `GameStarted` → sent to all players in the room
- `Error` → sent to caller if not owner or game already started

---

### `SubmitAnswer`

Submits the player's answer for the current question. Once all players have answered, the round closes automatically. The answer must exactly match one of the strings in the `answers` array of the current `QuestionResponse`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `roomCode` | `string` | Room code |
| `playerUuid` | `string` | Your player UUID |
| `answer` | `string` | Must be one of the `answers` values from the current question |

**Responses (to caller):**
- `AnswerAccepted` — always sent immediately if the answer was recorded

**Responses (to group, only when all players have answered):**
- `RoundEnded` + `QuestionReceived` — more questions remain
- `RoundEnded` + `GameEnded` — that was the last question

---

## Server → Client Events

### `ConnectedToRoom`

Sent to the **caller** after a successful `ConnectToRoom`.

```json
{
  "uuid": "string",
  "joinCode": "string",
  "status": 0,
  "owner": { "uuid": "string", "displayName": "string", "points": 0, "correctAnswers": 0 },
  "members": [
    { "uuid": "string", "displayName": "string", "points": 0, "correctAnswers": 0 }
  ],
  "totalQuestions": 10,
  "currentQuestionIndex": -1
}
```

`status` values: `0` = Waiting, `1` = InProgress, `2` = Finished

---

### `PlayerConnected`

Sent to **all other players** when someone calls `ConnectToRoom`.

```json
{ "uuid": "string", "displayName": "string", "points": 0, "correctAnswers": 0 }
```

---

### `GameStarted`

Sent to **all players** when the owner starts the game. Contains the first question.

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

> **Important:** `answers` is shuffled but consistent across all clients (seeded per room + question index). The correct answer is **not** identified — your UI should not highlight any answer until `RoundEnded` is received.

---

### `AnswerAccepted`

Sent only to the **caller** immediately after `SubmitAnswer` is recorded. No payload.

Use this to show a "waiting for others" state in the UI.

---

### `QuestionTimedOut`

Sent to **all players** when the question timer expires before all players answered. No payload.

Always followed immediately by `RoundEnded`. Use this to show a timeout indicator before revealing the correct answer.

---

### `RoundEnded`

Sent to **all players** when a round closes (either all answered or timer expired).

```json
{
  "correctAnswer": "Paris",
  "scores": [
    { "uuid": "string", "displayName": "Alice", "points": 300, "correctAnswers": 2 },
    { "uuid": "string", "displayName": "Bob",   "points": 100, "correctAnswers": 1 }
  ]
}
```

`scores` contains updated totals for all players. Show the correct answer and score changes here.

---

### `QuestionReceived`

Sent to **all players** after `RoundEnded` when more questions remain. Same shape as `GameStarted`.

```json
{
  "index": 1,
  "total": 10,
  "text": "Who painted the Mona Lisa?",
  "category": "Art",
  "difficulty": "medium",
  "type": "multiple",
  "answers": ["Michelangelo", "Raphael", "Leonardo da Vinci", "Donatello"]
}
```

---

### `GameEnded`

Sent to **all players** after the last `RoundEnded`. Contains the final leaderboard sorted by points descending.

```json
[
  { "uuid": "string", "displayName": "Alice", "points": 700, "correctAnswers": 4 },
  { "uuid": "string", "displayName": "Bob",   "points": 300, "correctAnswers": 2 }
]
```

---

### `Error`

Sent only to the **caller** when an operation is rejected.

```json
"Room 'ZZZZZZ' not found."
```

Always a plain string. Handle this to show error messages in the UI.

---

## Scoring

| Difficulty | Points per correct answer |
|------------|--------------------------|
| `easy`     | 100 |
| `medium`   | 200 |
| `hard`     | 300 |

Players who do not answer before the timer expires receive 0 points for that round.

---

## Question Timer

Each question has a server-side countdown (default **30 seconds**, configurable via `Game:QuestionTimeoutSeconds` in `appsettings.json`). When it expires:

1. Server submits an empty answer for any player who hasn't responded
2. `QuestionTimedOut` is broadcast to all
3. `RoundEnded` is broadcast with the correct answer and current scores
4. `QuestionReceived` (or `GameEnded`) is broadcast and the next timer starts

The client does not need to manage the timer — it is purely server-side. You may display a countdown UI by counting down from `QuestionTimeoutSeconds` locally after receiving `GameStarted` or `QuestionReceived`.

---

## Error Scenarios

| Situation | Event received | By |
|-----------|---------------|----|
| Room code not found | `Error` | Caller |
| Player UUID not in room | `Error` | Caller |
| Non-owner calls `StartGame` | `Error` | Caller |
| Game already started | `Error` | Caller |
| `SubmitAnswer` when game not in progress | `Error` | Caller |
| Answer submitted twice | second answer overwrites first | — |
