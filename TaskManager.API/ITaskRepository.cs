using CommonTypes;

namespace TaskManager.API;

public interface ITaskRepository
{
    Task<List<UTask>> GetAllAsync();
    Task<UTask?> GetByIdAsync(int id);
    Task<UTask> CreateAsync(UTask task);
    Task<bool> UpdateAsync(UTask task);
    Task<bool> DeleteAsync(int id);
}
