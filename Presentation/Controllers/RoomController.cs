using Microsoft.AspNetCore.Mvc;
using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;
using trivia_game.Domain.Exceptions;

namespace trivia_game.Presentation.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomController(IRoomService roomService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RoomResponse>> CreateRoom([FromBody] CreateRoomRequest request) =>
        Ok(await roomService.CreateRoomAsync(request));

    [HttpPost("join")]
    public ActionResult<JoinRoomResponse> JoinRoom([FromBody] JoinRoomRequest request)
    {
        try { return Ok(roomService.JoinRoom(request)); }
        catch (RoomNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpGet]
    public ActionResult<IEnumerable<RoomResponse>> GetRooms() =>
        Ok(roomService.GetActiveRooms());
}
