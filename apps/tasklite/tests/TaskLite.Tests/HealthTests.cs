using System.Net;
using System.Text.Json;

namespace TaskLite.Tests;

public class HealthTests : IDisposable
{
    private readonly TaskLiteFactory _factory = new();

    [Fact]
    public async Task GetHealth_WithoutAuthHeader_Returns200WithOkStatus()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
