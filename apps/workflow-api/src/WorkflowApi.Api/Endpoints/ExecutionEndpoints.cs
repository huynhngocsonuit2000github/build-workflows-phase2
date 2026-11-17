using Microsoft.EntityFrameworkCore;
using WorkflowApi.Api.Data;
using WorkflowApi.Domain;

namespace WorkflowApi.Api.Endpoints;

public static class ExecutionEndpoints
{
    public static void MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/executions");

        group.MapPost("/", CreateExecution);
        group.MapGet("/{id:guid}", GetExecution);
        group.MapGet("/{id:guid}/history", GetHistory);
        group.MapPost("/{id:guid}/transitions", Transition);
    }

    private static async Task<IResult> CreateExecution(
        CreateExecutionRequest request,
        WorkflowDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.JiraIssueKey) || request.JiraIssueKey.Trim().Length > 50)
        {
            errors["jiraIssueKey"] = ["jiraIssueKey is required and must be at most 50 characters."];
        }

        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();
        if (correlationId.Length > 100)
        {
            errors["correlationId"] = ["correlationId must be at most 100 characters."];
        }

        if (!TryParseActor(request.Actor, errors, out var actor))
        {
            return Results.ValidationProblem(errors);
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var execution = WorkflowExecution.Create(request.JiraIssueKey!, correlationId, actor!, clock.GetUtcNow().UtcDateTime);
        db.Executions.Add(execution);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/executions/{execution.Id}", ExecutionResponse.From(execution));
    }

    private static async Task<IResult> GetExecution(Guid id, WorkflowDbContext db, CancellationToken ct)
    {
        var execution = await db.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return execution is null ? NotFound(id) : Results.Ok(ExecutionResponse.From(execution));
    }

    private static async Task<IResult> GetHistory(Guid id, WorkflowDbContext db, CancellationToken ct)
    {
        var execution = await db.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (execution is null)
        {
            return NotFound(id);
        }

        // Lịch sử theo thứ tự thời gian: dòng ghi trước nằm trước.
        var rows = execution.History
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.ToState == WorkflowState.Created ? 0 : 1)
            .Select(HistoryResponse.From)
            .ToList();

        return Results.Ok(rows);
    }

    private static async Task<IResult> Transition(
        Guid id,
        TransitionRequestDto request,
        WorkflowDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (!TryParseState(request.ToState, out var toState))
        {
            errors["toState"] = [$"toState must be one of: {string.Join(", ", Enum.GetNames<WorkflowState>())}."];
        }

        if (request.ExpectedVersion is null)
        {
            errors["expectedVersion"] = ["expectedVersion is required."];
        }

        if (request.Reason is { Length: > 1000 })
        {
            errors["reason"] = ["reason must be at most 1000 characters."];
        }

        if (!TryParseActor(request.Actor, errors, out var actor) || errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var execution = await db.Executions.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (execution is null)
        {
            return NotFound(id);
        }

        var result = WorkflowStateMachine.Apply(
            execution,
            new TransitionRequest(toState, actor!, request.ExpectedVersion!.Value, request.Reason),
            clock.GetUtcNow().UtcDateTime);

        if (!result.Succeeded)
        {
            return Rejected(result);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Hai request cùng đọc version N; request kia ghi trước. Câu UPDATE của mình
            // (WHERE Version = N) không khớp dòng nào, nên không ghi gì (cả lịch sử cũng rollback).
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "The execution was changed by another request. Reload it and retry with the new version.",
                extensions: new Dictionary<string, object?> { ["error"] = nameof(TransitionError.VersionConflict) });
        }

        return Results.Ok(ExecutionResponse.From(execution));
    }

    /// <summary>
    /// Ánh xạ lỗi của state machine sang HTTP:
    /// lệch version = 409; sai người gửi = 403; sai chuyển, trạng thái cuối, hết lượt = 422.
    /// </summary>
    private static IResult Rejected(TransitionResult result)
    {
        var (status, title) = result.Error switch
        {
            TransitionError.VersionConflict => (StatusCodes.Status409Conflict, "Conflict"),
            TransitionError.ActorNotAllowed => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity")
        };

        return Results.Problem(
            statusCode: status,
            title: title,
            detail: result.Message,
            extensions: new Dictionary<string, object?> { ["error"] = result.Error.ToString() });
    }

    private static IResult NotFound(Guid id) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Execution {id} not found.");

    /// <summary>Chỉ nhận đúng tên trạng thái; "1" hay "99" bị từ chối (bài học từ lỗi số của TaskLite).</summary>
    private static bool TryParseState(string? value, out WorkflowState state)
    {
        state = default;
        var text = value?.Trim();
        return !string.IsNullOrEmpty(text)
               && !int.TryParse(text, out _)
               && Enum.TryParse(text, ignoreCase: true, out state)
               && Enum.IsDefined(state);
    }

    private static bool TryParseActor(ActorDto? dto, Dictionary<string, string[]> errors, out Actor? actor)
    {
        actor = null;
        var type = dto?.Type?.Trim();

        if (string.IsNullOrEmpty(type)
            || int.TryParse(type, out _)
            || !Enum.TryParse<ActorType>(type, ignoreCase: true, out var actorType)
            || !Enum.IsDefined(actorType))
        {
            errors["actor.type"] = [$"actor.type must be one of: {string.Join(", ", Enum.GetNames<ActorType>())}."];
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto?.Id) || dto.Id.Trim().Length > 100)
        {
            errors["actor.id"] = ["actor.id is required and must be at most 100 characters."];
            return false;
        }

        actor = new Actor(actorType, dto.Id);
        return true;
    }
}
