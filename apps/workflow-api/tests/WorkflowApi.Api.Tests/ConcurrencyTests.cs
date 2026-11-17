using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApi.Api.Data;
using WorkflowApi.Domain;

namespace WorkflowApi.Api.Tests;

[Collection("postgres")]
public class ConcurrencyTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WorkflowApiFactory _factory = null!;

    public async Task InitializeAsync() => _factory = await WorkflowApiFactory.CreateAsync(postgres);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Test của tiêu chí done: nhiều request cùng gửi cùng expectedVersion, đúng một cái thành công, còn lại 409.
    /// Lặp nhiều vòng để các request thật sự chồng lên nhau.
    /// </summary>
    [Fact]
    public async Task Transition_ParallelRequestsSameVersion_ExactlyOneSucceedsOthersGet409()
    {
        const int rounds = 10;
        const int parallelRequests = 8;

        for (var round = 0; round < rounds; round++)
        {
            var created = await _factory.CreateExecutionAsync($"TAS-{round}");

            var responses = await Task.WhenAll(Enumerable.Range(0, parallelRequests).Select(i =>
                _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version, $"racer-{i}")));

            var codes = responses.Select(r => r.StatusCode).ToList();
            Assert.Equal(1, codes.Count(c => c == HttpStatusCode.OK));
            Assert.Equal(parallelRequests - 1, codes.Count(c => c == HttpStatusCode.Conflict));

            // Không có bản ghi nào bị ghi đè hay nhân đôi: version tăng đúng 1, lịch sử có đúng 1 dòng mới.
            var fetched = await _factory.GetAsync(created.Id);
            Assert.Equal(created.Version + 1, fetched.Version);
            Assert.Equal("RequirementAnalysis", fetched.CurrentState);
            var history = await _factory.GetHistoryAsync(created.Id);
            Assert.Equal(2, history.Count);
        }
    }

    /// <summary>
    /// Chứng minh lớp bảo vệ ở tầng DB (không chỉ kiểm tra version trong code):
    /// hai DbContext cùng đọc version 1, cùng chuyển trạng thái, lần lưu thứ hai bị từ chối.
    /// </summary>
    [Fact]
    public async Task SaveChanges_TwoContextsLoadedSameVersion_SecondThrowsConcurrencyException()
    {
        var created = await _factory.CreateExecutionAsync();
        var now = new DateTime(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        var executionA = await dbA.Executions.SingleAsync(e => e.Id == created.Id);
        var executionB = await dbB.Executions.SingleAsync(e => e.Id == created.Id);

        var request = new TransitionRequest(
            WorkflowState.RequirementAnalysis, new Actor(ActorType.N8n, "n8n"), ExpectedVersion: 1);
        Assert.True(WorkflowStateMachine.Apply(executionA, request, now).Succeeded);
        Assert.True(WorkflowStateMachine.Apply(executionB, request, now).Succeeded); // cả hai đều qua kiểm tra trong code

        await dbA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        // Giao dịch của B rollback hoàn toàn, kể cả dòng lịch sử của B.
        var history = await _factory.GetHistoryAsync(created.Id);
        Assert.Equal(2, history.Count);
        Assert.Equal(2, (await _factory.GetAsync(created.Id)).Version);
    }
}
