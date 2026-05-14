#!/usr/bin/env python3
"""
Integration tests for the trivia-game backend.

Usage:
    pip install -r requirements.txt
    dotnet run &          # start the server first
    python test_game.py

The server must be running on BASE_URL before tests start.
"""

import sys
import time
import threading
import requests
from signalrcore.hub_connection_builder import HubConnectionBuilder

BASE_URL = "http://localhost:5114"
HUB_URL  = f"{BASE_URL}/hubs/game"

PASS = "\033[92mPASS\033[0m"
FAIL = "\033[91mFAIL\033[0m"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def assert_eq(label, actual, expected):
    if actual != expected:
        print(f"  {FAIL} {label}: expected {expected!r}, got {actual!r}")
        sys.exit(1)

def assert_true(label, value):
    if not value:
        print(f"  {FAIL} {label}")
        sys.exit(1)

def ok(label):
    print(f"  {PASS} {label}")

# ---------------------------------------------------------------------------
# REST tests
# ---------------------------------------------------------------------------

def test_get_categories():
    print("\n[GET /api/trivia/categories]")
    r = requests.get(f"{BASE_URL}/api/trivia/categories")
    assert_eq("status", r.status_code, 200)
    cats = r.json()
    assert_true("at least one category", len(cats) > 0)
    assert_true("category has id", "id" in cats[0])
    assert_true("category has name", "name" in cats[0])
    ok(f"{len(cats)} categories returned")
    return cats


def test_create_room(owner_name="Alice", amount=3):
    print(f"\n[POST /api/rooms] owner={owner_name}, amount={amount}")
    payload = {"ownerName": owner_name, "amount": amount, "categoryId": None, "type": "multiple"}
    r = requests.post(f"{BASE_URL}/api/rooms", json=payload)
    assert_eq("status", r.status_code, 200)
    room = r.json()
    assert_true("joinCode present", bool(room.get("joinCode")))
    assert_eq("status=Waiting(0)", room["status"], 0)
    assert_eq("member count", len(room["members"]), 1)
    assert_eq("totalQuestions", room["totalQuestions"], amount)
    ok(f"room {room['joinCode']} created, owner={room['owner']['uuid'][:8]}…")
    return room


def test_join_room(join_code, display_name="Bob"):
    print(f"\n[POST /api/rooms/join] code={join_code}, name={display_name}")
    r = requests.post(f"{BASE_URL}/api/rooms/join", json={"joinCode": join_code, "displayName": display_name})
    assert_eq("status", r.status_code, 200)
    body = r.json()
    room = body["room"]
    assert_eq("member count", len(room["members"]), 2)
    player_uuid = body["yourPlayerUuid"]
    assert_true("yourPlayerUuid present", bool(player_uuid))
    ok(f"{display_name} joined, uuid={player_uuid[:8]}…")
    return player_uuid, room


def test_join_invalid_code():
    print("\n[POST /api/rooms/join] invalid code")
    r = requests.post(f"{BASE_URL}/api/rooms/join", json={"joinCode": "ZZZZZZ", "displayName": "Ghost"})
    assert_eq("status", r.status_code, 404)
    ok("404 returned for unknown join code")


def test_get_rooms(min_count=1):
    print("\n[GET /api/rooms]")
    r = requests.get(f"{BASE_URL}/api/rooms")
    assert_eq("status", r.status_code, 200)
    rooms = r.json()
    assert_true(f"at least {min_count} room", len(rooms) >= min_count)
    ok(f"{len(rooms)} active room(s)")

# ---------------------------------------------------------------------------
# SignalR game-flow test
# ---------------------------------------------------------------------------

class HubClient:
    """Thin wrapper around a SignalR hub connection with event capture."""

    def __init__(self, name: str):
        self.name = name
        self.events: list[tuple[str, list]] = []
        self._lock = threading.Lock()
        self._conn = (
            HubConnectionBuilder()
            .with_url(HUB_URL)
            .build()
        )
        for event in [
            "ConnectedToRoom", "PlayerConnected", "GameStarted",
            "QuestionReceived", "AnswerAccepted", "RoundEnded",
            "GameEnded", "Error",
        ]:
            self._conn.on(event, self._handler(event))

    def _handler(self, name):
        def handle(args):
            with self._lock:
                self.events.append((name, args))
        return handle

    def start(self):
        self._conn.start()
        time.sleep(0.5)

    def stop(self):
        self._conn.stop()

    def send(self, method, args):
        self._conn.send(method, args)

    def wait_for(self, event_name, timeout=10) -> list:
        """Block until event_name is received, return its args."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            with self._lock:
                for name, args in self.events:
                    if name == event_name:
                        return args
            time.sleep(0.1)
        raise TimeoutError(f"[{self.name}] Timed out waiting for '{event_name}'")

    def last(self, event_name) -> list | None:
        with self._lock:
            for name, args in reversed(self.events):
                if name == event_name:
                    return args
        return None

    def has_error(self) -> bool:
        with self._lock:
            return any(n == "Error" for n, _ in self.events)


def test_full_game(room, owner_uuid, bob_uuid):
    print("\n[SignalR] Full game flow")
    join_code = room["joinCode"]
    total_q   = room["totalQuestions"]

    alice = HubClient("Alice")
    bob   = HubClient("Bob")

    alice.start()
    bob.start()

    # Both connect to the room group
    alice.send("ConnectToRoom", [join_code, owner_uuid])
    args = alice.wait_for("ConnectedToRoom")
    assert_true("ConnectedToRoom room present", args[0] is not None)
    ok("Alice connected to room")

    bob.send("ConnectToRoom", [join_code, bob_uuid])
    args = bob.wait_for("ConnectedToRoom")
    assert_true("ConnectedToRoom room present", args[0] is not None)
    ok("Bob connected to room")

    # Alice also receives PlayerConnected for Bob
    alice.wait_for("PlayerConnected")
    ok("Alice received PlayerConnected for Bob")

    # Bob tries to start game (not owner — should error)
    bob.send("StartGame", [join_code, bob_uuid])
    time.sleep(0.5)
    assert_true("Bob gets Error for unauthorized start", bob.has_error())
    ok("Non-owner start correctly rejected")

    # Alice starts the game
    alice.send("StartGame", [join_code, owner_uuid])
    gs_args = alice.wait_for("GameStarted")
    question = gs_args[0]
    assert_eq("GameStarted index=0", question["index"], 0)
    assert_eq("GameStarted total", question["total"], total_q)
    assert_true("answers list non-empty", len(question["answers"]) > 0)
    ok(f"Game started — Q1/{total_q}: {question['text'][:60]}…")

    bob.wait_for("GameStarted")
    ok("Bob also received GameStarted")

    # Play through all questions
    for q_num in range(total_q):
        if q_num == 0:
            question = alice.last("GameStarted")[0]
        else:
            args = alice.wait_for("QuestionReceived")
            question = args[0]
            assert_eq(f"Q{q_num+1} index", question["index"], q_num)
            ok(f"Q{q_num+1}/{total_q}: {question['text'][:60]}…")

        answers = question["answers"]
        # Alice picks first answer, Bob picks second (if available) for score variety
        alice.send("SubmitAnswer", [join_code, owner_uuid, answers[0]])
        alice.wait_for("AnswerAccepted")
        ok(f"  Alice submitted '{answers[0][:30]}'")

        bob.send("SubmitAnswer", [join_code, bob_uuid, answers[-1]])
        bob.wait_for("AnswerAccepted")
        ok(f"  Bob submitted '{answers[-1][:30]}'")

        # Both should receive RoundEnded
        re_args = alice.wait_for("RoundEnded")
        round_result = re_args[0]
        assert_true("RoundEnded has correctAnswer", bool(round_result.get("correctAnswer")))
        assert_true("RoundEnded has scores", len(round_result["scores"]) == 2)
        ok(f"  Correct answer: {round_result['correctAnswer']}")

        # Clear RoundEnded so wait_for works next iteration
        with alice._lock:
            alice.events = [(n, a) for n, a in alice.events if n != "RoundEnded"]
        with bob._lock:
            bob.events = [(n, a) for n, a in bob.events if n != "RoundEnded"]

        # Clear QuestionReceived so next iteration's wait_for picks up the new one
        if q_num < total_q - 1:
            with alice._lock:
                alice.events = [(n, a) for n, a in alice.events if n != "QuestionReceived"]

    # Final: both receive GameEnded
    ge_args = alice.wait_for("GameEnded")
    leaderboard = ge_args[0]
    assert_true("GameEnded has players", len(leaderboard) == 2)
    assert_true("sorted by points desc", leaderboard[0]["points"] >= leaderboard[1]["points"])
    print("  Final leaderboard:")
    for i, p in enumerate(leaderboard):
        print(f"    {i+1}. {p['displayName']}: {p['points']} pts ({p['correctAnswers']} correct)")
    ok("Game ended correctly")

    alice.stop()
    bob.stop()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("=" * 55)
    print("  Trivia Game Integration Tests")
    print(f"  Server: {BASE_URL}")
    print("=" * 55)

    try:
        test_get_categories()
        room  = test_create_room(amount=3)
        bob_uuid, updated_room = test_join_room(room["joinCode"])
        test_join_invalid_code()
        test_get_rooms(min_count=1)
        test_full_game(room, room["owner"]["uuid"], bob_uuid)

        print("\n" + "=" * 55)
        print("  All tests passed!")
        print("=" * 55)

    except (AssertionError, TimeoutError) as e:
        print(f"\n{FAIL}: {e}")
        sys.exit(1)
    except requests.exceptions.ConnectionError:
        print(f"\n{FAIL}: Could not connect to {BASE_URL} — is the server running?")
        sys.exit(1)
