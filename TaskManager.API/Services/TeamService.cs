using CommonTypes;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.API.Services;

public class TeamService
{
    private readonly TaskDbContext _context;

    public TeamService(TaskDbContext context) => _context = context;

    public async Task<Team> CreateTeam(string name, int ownerUserId)
    {
        var team = new Team { Name = name };
        _context.Teams.Add(team);

        // Add owner as first member
        team.Members.Add(new TeamMember
        {
            UserId = ownerUserId,
            Role = TeamMemberRole.Owner
        });

        await _context.SaveChangesAsync();
        return team;
    }
    public async Task<Team?> GetTeamById(int id)
    {
        return await _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}