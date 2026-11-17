using Npgsql;
using Testcontainers.PostgreSql;

namespace WorkflowApi.Api.Tests;

/// <summary>
/// Một container PostgreSQL dùng chung cho cả bộ test (khởi động một lần, vài giây).
/// Mỗi test tạo một database riêng bên trong nó nên không ảnh hưởng nhau.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Tạo database mới, trả về (tên, chuỗi kết nối).</summary>
    public async Task<(string Name, string ConnectionString)> CreateDatabaseAsync()
    {
        var name = $"wf_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await cmd.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name };
        return (name, builder.ConnectionString);
    }

    public async Task DropDatabaseAsync(string name)
    {
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(_container.GetConnectionString());
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", admin);
        await cmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
