using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Models;

public class UTask
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string Description { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsCompleted { get; set; }
}

