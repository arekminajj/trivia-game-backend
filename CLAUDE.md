# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the server (http://localhost:5114)
dotnet run

# Build
dotnet build

# Python integration tests (server must be running first)
cd Tests
pip install -r requirements.txt
python test_game.py
```

## Architecture

ASP.NET Core 10 Web API using **Clean Architecture** (single project, folder-based layers). Real-time game events use **SignalR** (`/hubs/game`). Questions are fetched from the Open Trivia Database (opentdb.com) at room creation time.

```
Domain/          Pure business logic — no framework dependencies
  Entities/        Room, RoomMember, TriviaQuestion, TriviaCategory
  Enums/           RoomStatus (Waiting → InProgress → Finished)
  Exceptions/      RoomNotFoundException, InvalidGameOperationException
  Interfaces/
    Repositories/  IRoomRepository
    Providers/     ITriviaProvider

Application/     Orchestrates use cases — depends on Domain only
  DTOs/            Request/response records (never expose Domain entities over the wire)
  Interfaces/      IRoomService, ITriviaService, IGameService
  Mappings/        RoomMapper (static internal helpers)
  Services/        RoomService, TriviaService, GameService

Infrastructure/  External dependencies — implements Domain interfaces
  ExternalApis/OpenTdb/   OpenTdbClient (ITriviaProvider) + OpenTDB JSON models
  Repositories/           InMemoryRoomRepository (IRoomRepository, Singleton)

Presentation/    Entry points — depends on Application only
  Controllers/   RoomController (REST), TriviaController (REST)
  Hubs/          GameHub (SignalR)
```

## Key design decisions

- **IRoomRepository is Singleton** — shared in-memory state; services are Scoped.
- **Room is an aggregate root** — game state transitions (`Start`, `SubmitAnswer`, `AdvanceQuestion`, `ScoreCurrentRound`) live on the entity, not in services.
- **Answer shuffle is deterministic** — seeded with `(room.Uuid + questionIndex).GetHashCode()` so all clients see the same answer order without storing it externally.
- **GameService** is the hub's only dependency — it returns typed result records (`ConnectToRoomResult`, `StartGameResult`, `SubmitAnswerResult`) so the hub is a thin adapter.
- **OpenTDB JSON models** are `internal` to the infrastructure layer; the domain entity `TriviaQuestion` has no JSON attributes.

## REST endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/rooms` | Create room, fetch questions → returns `RoomResponse` (owner UUID in `owner.uuid`) |
| POST | `/api/rooms/join` | Join room → returns `JoinRoomResponse` (`yourPlayerUuid` + `room`) |
| GET | `/api/rooms` | List active rooms |
| GET | `/api/trivia/categories` | OpenTDB category list |

## SignalR hub (`/hubs/game`)

Call HTTP create/join first to get UUIDs, then connect via SignalR.

**Client → Server:**
| Method | Args | Notes |
|--------|------|-------|
| `ConnectToRoom` | `roomCode, playerUuid` | Joins the SignalR group for the room |
| `StartGame` | `roomCode, playerUuid` | Owner only; transitions room to InProgress |
| `SubmitAnswer` | `roomCode, playerUuid, answer` | Answer must match one of the shuffled strings |

**Server → Client:**
| Event | Payload | When |
|-------|---------|------|
| `ConnectedToRoom` | `RoomResponse` | Caller only, after ConnectToRoom |
| `PlayerConnected` | `PlayerResponse` | Broadcast to others when a player connects |
| `GameStarted` | `QuestionResponse` | Group, owner called StartGame |
| `AnswerAccepted` | — | Caller only, answer recorded |
| `RoundEnded` | `RoundResultResponse` | Group, when all players answered |
| `QuestionReceived` | `QuestionResponse` | Group, next question after RoundEnded |
| `GameEnded` | `List<PlayerResponse>` (leaderboard) | Group, after last round |
| `Error` | `string` | Caller only, operation rejected |

## Points

- Easy: 100 pts · Medium: 200 pts · Hard: 300 pts
- Points are awarded per round by `Room.ScoreCurrentRound()` before `AdvanceQuestion()`.
