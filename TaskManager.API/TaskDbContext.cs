using CommonTypes;
using Microsoft.EntityFrameworkCore;


namespace TaskManager.API;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }
    public DbSet<UTask> Tasks => Set<UTask>();
}
