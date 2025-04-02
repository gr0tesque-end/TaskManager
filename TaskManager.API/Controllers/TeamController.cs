using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly TeamService _teamService;

    public TeamsController(TeamService teamService) => _teamService = teamService;

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var team = await _teamService.CreateTeam(model.Name, userId);
        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeam(int id)
    {
        var team = await _teamService.GetTeamById(id);
        if (team == null)
            return NotFound();

        return Ok(team);
    }
}

public record CreateTeamModel(string Name);