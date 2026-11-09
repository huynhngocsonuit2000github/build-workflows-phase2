namespace TaskLite.Api.Domain;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }

    public List<TaskItem> Tasks { get; set; } = [];
}
