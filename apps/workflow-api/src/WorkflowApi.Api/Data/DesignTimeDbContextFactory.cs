using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowApi.Api.Data;

/// <summary>
/// Chỉ dùng cho `dotnet ef migrations add`: sinh migration không cần kết nối DB thật
/// và không chạy Program.cs (nơi có Migrate lúc khởi động). Chuỗi dưới đây là giả, không có bí mật.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only;Username=design;Password=design")
            .Options;
        return new WorkflowDbContext(options);
    }
}
