using Microsoft.EntityFrameworkCore;
using TaskLite.Api.Domain;

namespace TaskLite.Api.Data;

public static class DevSeeder
{
    public static async Task SeedAsync(TaskLiteDbContext db)
    {
        if (await db.Projects.AnyAsync())
        {
            return;
        }

        var demoProject = new Project
        {
            Name = "Demo",
            MemberIds = ["alice", "bob"],
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(demoProject);
        await db.SaveChangesAsync();

        var tasks = new List<TaskItem>
        {
            new()
            {
                ProjectId = demoProject.Id,
                Title = "Set up initial workspace",
                Status = TaskItemStatus.Todo,
                Assignee = null,
                DueDate = null,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = demoProject.Id,
                Title = "Design and implement API endpoints",
                Status = TaskItemStatus.InProgress,
                Assignee = "bob",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = demoProject.Id,
                Title = "Review project scope and specifications",
                Status = TaskItemStatus.Done,
                Assignee = null,
                DueDate = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }
}
