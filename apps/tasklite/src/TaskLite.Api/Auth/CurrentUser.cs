namespace TaskLite.Api.Auth;

public record CurrentUser(string Id)
{
    public static ValueTask<CurrentUser> BindAsync(HttpContext context)
    {
        var val = context.Request.Headers.TryGetValue("X-User-Id", out var values)
            ? values.ToString().Trim()
            : string.Empty;
        return ValueTask.FromResult(new CurrentUser(val));
    }
}

public class RequireUserFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var values) ||
            string.IsNullOrWhiteSpace(values.ToString()))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "X-User-Id header is required.");
        }

        return await next(context);
    }
}
