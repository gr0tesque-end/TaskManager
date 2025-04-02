namespace CommonTypes;
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Relationships
    public List<TeamMember> Members { get; set; } = new();
    public List<UTask> Tasks { get; set; } = new();
}

public class TeamMember
{
    public int Id { get; set; }
    public TeamMemberRole Role { get; set; }

    // Relationships
    public int TeamId { get; set; }
    public Team Team { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }
}

public enum TeamMemberRole
{
    Owner,
    Member
}