namespace TaskLite.Api.Domain;

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public string? Assignee { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
