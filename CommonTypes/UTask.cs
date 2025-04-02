using System.ComponentModel.DataAnnotations;

namespace CommonTypes;

public class UTask
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string Description { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsCompleted { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public bool IsTeamTask => TeamId != null;
}

