using Microsoft.AspNetCore.Mvc;

namespace trivia_game.Presentation.Controllers;

[ApiController]
[Route("privacy")]
public class PrivacyController : ControllerBase
{
    [HttpGet]
    [Produces("text/plain")]
    public IActionResult Get()
    {
        var policy = """
            Privacy Policy — Trivia Game
            Last updated: 2026-05-29

            1. Overview
            Trivia Game is a real-time multiplayer trivia application. This policy describes what data is collected, how it is used, and the rights of users.

            2. Data collected
            - Display name (chosen by the user): identifies the player in the game session; stored in memory only, discarded on server restart.
            - Game score and correct-answer count: shown on the leaderboard; stored in memory only, discarded on server restart.
            - Room join code: allows players to connect to the same session; stored in memory only.

            The App does NOT collect email addresses, passwords, device identifiers, location data, or any information that can personally identify a real-world individual.

            3. Data retention
            All game data is stored exclusively in the server's RAM. No data is written to a persistent database or sent to any third party. All data is permanently discarded when the server restarts.

            4. Third-party services
            Trivia questions are fetched from the Open Trivia Database (opentdb.com). Only request parameters (question count, category, type) are sent — no user data is transmitted.

            5. Children's privacy
            The App does not knowingly collect personal information from children under 13. Only anonymous display names are used and discarded after the session.

            6. Security
            Game sessions are identified by randomly generated UUIDs. Communication between the mobile client and backend uses HTTPS (TLS).

            7. Changes to this policy
            Any future changes will be reflected in this document with an updated date.

            8. Contact
            Arkadiusz Cios — arkadiuszcios@outlook.com
            """;

        return Content(policy, "text/plain");
    }
}
