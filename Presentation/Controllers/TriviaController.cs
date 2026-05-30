using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trivia_game.Application.Interfaces;
using trivia_game.Domain.Entities;

namespace trivia_game.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/trivia")]
public class TriviaController(ITriviaService triviaService) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<TriviaCategory>>> GetCategories() =>
        Ok(await triviaService.GetCategoriesAsync());
}
