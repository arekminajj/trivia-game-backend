
using Microsoft.AspNetCore.Mvc;
using trivia_game.Models;
using trivia_game.DTOs;
using trivia_game.Services;

namespace trivia_game.Controllers;

[ApiController]
[Route("api/room")]
public class RoomController : ControllerBase
{
    private readonly ITriviaService _triviaService;
    private readonly IRoomService _roomService;
    public RoomController(
        ITriviaService triviaService,
        IRoomService roomService)
    {
        _triviaService = triviaService;
        _roomService = roomService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<Room>> CreateRoom([FromBody] CreateRoomDto createRoomDto)
    {
        var room = await _roomService.CreateRoomAsync(
            createRoomDto);

        return Ok(room);
    }

    [HttpPost("join")]
    public async Task<ActionResult<Room>> JoinRoom([FromBody] JoinRoomDto joinRoomDto)
    {
        return Ok(_roomService.JoinRoom(joinRoomDto));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetAllActiveRooms()
    {
        return Ok(_roomService.GetAllActiveRooms());
    }
}