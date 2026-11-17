using Microsoft.EntityFrameworkCore;
using WorkflowApi.Api.Data;
using WorkflowApi.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối KHÔNG nằm trong appsettings (có mật khẩu): lấy từ biến môi trường
// ConnectionStrings__Default hoặc `dotnet user-secrets` (xem README).
// Đọc trễ (lúc DbContext được tạo) để test có thể ghi đè bằng cấu hình của factory.
builder.Services.AddDbContext<WorkflowDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "Connection string 'Default' is missing. Set ConnectionStrings__Default or use dotnet user-secrets.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapExecutionEndpoints();

app.Run();

public partial class Program;
