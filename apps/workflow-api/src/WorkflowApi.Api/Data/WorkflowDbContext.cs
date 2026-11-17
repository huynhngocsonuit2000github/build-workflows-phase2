using Microsoft.EntityFrameworkCore;
using WorkflowApi.Domain;

namespace WorkflowApi.Api.Data;

public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowExecution> Executions => Set<WorkflowExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkflowExecution>(b =>
        {
            b.ToTable("WorkflowExecutions");
            b.HasKey(e => e.Id);

            b.Property(e => e.JiraIssueKey).IsRequired().HasMaxLength(50);
            b.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            b.Property(e => e.CurrentState).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(e => e.FailedFromState).HasConversion<string>().HasMaxLength(50);

            // Optimistic concurrency: EF thêm "AND Version = <giá trị đã đọc>" vào câu UPDATE.
            // Không có dòng nào khớp (ai đó đã đổi trước) -> DbUpdateConcurrencyException -> HTTP 409.
            b.Property(e => e.Version).IsConcurrencyToken();

            b.HasIndex(e => e.JiraIssueKey);
            b.HasIndex(e => e.CorrelationId);

            b.OwnsMany(e => e.History, h =>
            {
                h.ToTable("WorkflowHistory");
                h.WithOwner().HasForeignKey("WorkflowExecutionId");
                h.Property<long>("Id").ValueGeneratedOnAdd();
                h.HasKey("Id");
                h.Property(x => x.FromState).HasConversion<string>().HasMaxLength(50);
                h.Property(x => x.ToState).HasConversion<string>().HasMaxLength(50).IsRequired();
                h.Property(x => x.ChangedBy).IsRequired().HasMaxLength(150);
                h.Property(x => x.Reason).HasMaxLength(1000);
            });

            b.Navigation(e => e.History).HasField("_history");
        });
    }
}
