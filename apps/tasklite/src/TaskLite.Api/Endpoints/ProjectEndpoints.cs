using Microsoft.EntityFrameworkCore;
using TaskLite.Api.Auth;
using TaskLite.Api.Data;
using TaskLite.Api.Domain;

namespace TaskLite.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/projects")
            .AddEndpointFilter<RequireUserFilter>();

        group.MapPost("/", CreateProject);
        group.MapGet("/", GetProjects);
        group.MapGet("/{projectId:int}", GetProjectById);

        return group;
    }

    private static async Task<IResult> CreateProject(
        CreateProjectRequest request,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name) || name.Length > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required and must be between 1 and 100 characters."]
            });
        }

        var memberSet = new HashSet<string>(StringComparer.Ordinal)
        {
            user.Id
        };

        if (request.MemberIds != null)
        {
            foreach (var rawMember in request.MemberIds)
            {
                var member = rawMember.Trim();
                if (string.IsNullOrEmpty(member) || member.Length > 50)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["memberIds"] = ["Each member ID must be non-empty and at most 50 characters."]
                    });
                }
                memberSet.Add(member);
            }
        }

        var project = new Project
        {
            Name = name,
            MemberIds = memberSet.ToList(),
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var response = new ProjectResponse(project.Id, project.Name, project.MemberIds, project.CreatedAt);
        return Results.Created($"/projects/{project.Id}", response);
    }

    private static async Task<IResult> GetProjects(
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var projects = await db.Projects
            .Where(p => p.MemberIds.Contains(user.Id))
            .OrderBy(p => p.Id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.MemberIds, p.CreatedAt))
            .ToListAsync();

        return Results.Ok(projects);
    }

    private static async Task<IResult> GetProjectById(
        int projectId,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Project {projectId} not found.");
        }

        if (!project.MemberIds.Contains(user.Id, StringComparer.Ordinal))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "User is not a member of this project.");
        }

        var response = new ProjectResponse(project.Id, project.Name, project.MemberIds, project.CreatedAt);
        return Results.Ok(response);
    }
}
