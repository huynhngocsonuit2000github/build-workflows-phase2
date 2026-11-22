using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkflowApi.Api.Data;
using WorkflowApi.Domain;

namespace WorkflowApi.Api.Endpoints;

public static class ExecutionEndpoints
{
    public static void MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/executions");

        group.MapPost("/", CreateExecution);
        group.MapGet("/", ListExecutions);
        group.MapGet("/{id:guid}", GetExecution);
        group.MapGet("/{id:guid}/history", GetHistory);
        group.MapPost("/{id:guid}/transitions", Transition);
        group.MapPost("/{id:guid}/retry", Retry);
    }

    public const int DefaultListLimit = 50;
    public const int MaxListLimit = 200;

    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotency-Replayed";

    /// <summary>
    /// Tạo lần chạy, idempotent theo header Idempotency-Key:
    /// - key mới: tạo execution + lưu kết quả (cùng một transaction), trả 201;
    /// - key đã có, cùng nội dung: trả lại NGUYÊN VĂN kết quả cũ (201, header Idempotency-Replayed: true), không tạo thêm;
    /// - key đã có, khác nội dung: 422 IdempotencyKeyReused;
    /// - hai request cùng key đến cùng lúc: unique index trên key cho một cái thắng, cái thua bắt lỗi và trả lại kết quả của cái thắng.
    /// </summary>
    private static async Task<IResult> CreateExecution(
        CreateExecutionRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        HttpContext http,
        WorkflowDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 200)
        {
            errors[IdempotencyKeyHeader] = ["Idempotency-Key header is required and must be at most 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.JiraIssueKey) || request.JiraIssueKey.Trim().Length > 50)
        {
            errors["jiraIssueKey"] = ["jiraIssueKey is required and must be at most 50 characters."];
        }

        var requestedCorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId.Trim();
        if (requestedCorrelationId is { Length: > 100 })
        {
            errors["correlationId"] = ["correlationId must be at most 100 characters."];
        }

        if (!TryParseActor(request.Actor, errors, out var actor) || errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var key = idempotencyKey!.Trim();
        var jiraIssueKey = request.JiraIssueKey!.Trim();
        var requestHash = HashRequest(jiraIssueKey, requestedCorrelationId, actor!);

        // Đường nhanh: key đã có thì trả lại kết quả cũ, không chạm vào execution.
        var existing = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Key == key, ct);
        if (existing is not null)
        {
            return Replay(existing, requestHash, http);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var execution = WorkflowExecution.Create(
            jiraIssueKey, requestedCorrelationId ?? Guid.NewGuid().ToString("N"), actor!, now);
        var body = JsonSerializer.Serialize(ExecutionResponse.From(execution), JsonSerializerOptions.Web);

        db.Executions.Add(execution);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            RequestHash = requestHash,
            ExecutionId = execution.Id,
            StatusCode = StatusCodes.Status201Created,
            ResponseBody = body,
            CreatedAt = now
        });

        try
        {
            // Execution và bản ghi idempotency lưu chung một transaction: hoặc có cả hai, hoặc không có gì.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Một request cùng key vừa lưu trước (cả hai cùng qua "đường nhanh" lúc chưa có gì).
            // Unique index chặn lại; transaction của mình rollback, execution của mình không tồn tại.
            db.ChangeTracker.Clear();
            var winner = await db.IdempotencyRecords.AsNoTracking().FirstAsync(r => r.Key == key, ct);
            return Replay(winner, requestHash, http);
        }

        return StoredResult(body, StatusCodes.Status201Created, execution.Id, http, replayed: false);
    }

    private static IResult Replay(IdempotencyRecord record, string requestHash, HttpContext http)
    {
        if (record.RequestHash != requestHash)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unprocessable Entity",
                detail: "This Idempotency-Key was already used with a different request body. Use a new key.",
                extensions: new Dictionary<string, object?> { ["error"] = "IdempotencyKeyReused" });
        }

        return StoredResult(record.ResponseBody, record.StatusCode, record.ExecutionId, http, replayed: true);
    }

    private static IResult StoredResult(string body, int statusCode, Guid executionId, HttpContext http, bool replayed)
    {
        http.Response.Headers.Location = $"/executions/{executionId}";
        if (replayed)
        {
            http.Response.Headers[ReplayedHeader] = "true";
        }

        return Results.Content(body, "application/json", Encoding.UTF8, statusCode);
    }

    /// <summary>Băm nội dung đã chuẩn hóa (đã cắt khoảng trắng, loại actor viết đúng tên enum).</summary>
    private static string HashRequest(string jiraIssueKey, string? correlationId, Actor actor)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            jiraIssueKey,
            correlationId,
            actorType = actor.Type.ToString(),
            actorId = actor.Id
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
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

        return await SaveTransitionAsync(execution, result, db, ct);
    }

    /// <summary>
    /// Retry lần chạy đang Failed. Không nhận toState: server tự quay về FailedFromState.
    /// Cùng một execution (cùng id), không tạo lần chạy mới. Hai retry cùng lúc: một 200, một 409.
    /// </summary>
    private static async Task<IResult> Retry(
        Guid id,
        RetryRequestDto request,
        WorkflowDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

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

        var result = WorkflowStateMachine.Retry(
            execution, actor!, request.ExpectedVersion!.Value, request.Reason, clock.GetUtcNow().UtcDateTime);

        return await SaveTransitionAsync(execution, result, db, ct);
    }

    /// <summary>
    /// Tìm lần chạy theo trạng thái (ví dụ ?state=Failed) và/hoặc issue. Mới cập nhật nằm trước.
    /// </summary>
    private static async Task<IResult> ListExecutions(
        string? state,
        string? jiraIssueKey,
        int? limit,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        WorkflowState parsedState = default;

        if (state is not null && !TryParseState(state, out parsedState))
        {
            errors["state"] = [$"state must be one of: {string.Join(", ", Enum.GetNames<WorkflowState>())}."];
        }

        var take = limit ?? DefaultListLimit;
        if (take is < 1 or > MaxListLimit)
        {
            errors["limit"] = [$"limit must be between 1 and {MaxListLimit}."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var query = db.Executions.AsNoTracking();
        if (state is not null)
        {
            query = query.Where(e => e.CurrentState == parsedState);
        }

        if (!string.IsNullOrWhiteSpace(jiraIssueKey))
        {
            var issue = jiraIssueKey.Trim();
            query = query.Where(e => e.JiraIssueKey == issue);
        }

        var rows = await query
            .OrderByDescending(e => e.UpdatedAt)
            .ThenBy(e => e.Id)
            .Take(take)
            .Select(e => new ExecutionSummary(
                e.Id, e.JiraIssueKey, e.CurrentState.ToString(), e.Version,
                e.FailedFromState == null ? null : e.FailedFromState.ToString(), e.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(rows);
    }

    /// <summary>Lưu kết quả của state machine; bắt va chạm ghi ở DB thành 409.</summary>
    private static async Task<IResult> SaveTransitionAsync(
        WorkflowExecution execution, TransitionResult result, WorkflowDbContext db, CancellationToken ct)
    {
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
