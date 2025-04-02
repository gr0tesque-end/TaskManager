using CommonTypes;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.API.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;

    public TaskRepository(TaskDbContext context) => _context = context;

    public async Task<List<UTask>> GetAllAsync() =>
        await _context.Tasks.ToListAsync();

    public async Task<UTask?> GetByIdAsync(int id) =>
        await _context.Tasks.FindAsync(id);

    public async Task<UTask> CreateAsync(UTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> UpdateAsync(UTask task)
    {
        _context.Entry(task).State = EntityState.Modified;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<UTask>> GetUserTasksAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId ||
                       (t.TeamId != null && t.Team.Members.Any(m => m.UserId == userId)))
            .ToListAsync();
    }

    public async Task<UTask?> GetUserTaskAsync(int taskId, int userId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
    }
}
