# Privacy Policy — Trivia Game

**Last updated: 2026-05-29**

## 1. Overview

Trivia Game ("the App") is a real-time multiplayer trivia application. This policy describes what data is collected, how it is used, and the rights of users.

## 2. Data collected

| Data | Purpose | Storage |
|------|---------|---------|
| Display name (chosen by the user) | Identify the player in the game session | In-memory only, discarded when the server restarts |
| Game score and correct-answer count | Show the leaderboard during and after the game | In-memory only, discarded when the server restarts |
| Room join code | Allow players to connect to the same game session | In-memory only |

The App does **not** collect:
- Email addresses or passwords
- Device identifiers
- Location data
- Any data that can personally identify a real-world individual

## 3. Data retention

All game data is stored exclusively in the server's RAM. No data is written to a persistent database or sent to any third party. All data is permanently discarded when the server process is stopped or restarted.

## 4. Third-party services

The App fetches trivia questions from the [Open Trivia Database](https://opentdb.com/) (opentdb.com). Only the request parameters (question count, category, type) are sent — no user data is transmitted. Please refer to the OpenTDB website for their data practices.

## 5. Children's privacy

The App does not knowingly collect any personal information from children under the age of 13. Because the App collects only anonymous display names chosen by the user and discards them immediately after the session, it is suitable for general audiences.

## 6. Security

Game sessions are identified by a randomly generated UUID. No authentication credentials are stored or transmitted. Communication between the mobile client and the backend uses HTTPS (TLS).

## 7. Changes to this policy

Any future changes to this policy will be reflected in this document with an updated date. Continued use of the App after changes constitutes acceptance of the updated policy.

## 8. Contact

For questions or concerns regarding this privacy policy, contact:

**Arkadiusz Cios** — arkadiuszcios@outlook.com
