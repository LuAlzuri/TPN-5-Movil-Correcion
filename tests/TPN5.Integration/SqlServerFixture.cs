using System.Security.Cryptography;
using Testcontainers.MsSql;
using Xunit;

namespace TPN5.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server descartable";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    // Pinned SQL Server 2022 image. Testcontainers chooses an available host port.
    private readonly MsSqlContainer container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
        .WithPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!")
        .Build();

    public Task InitializeAsync() => container.StartAsync();
    public async Task DisposeAsync() => await container.DisposeAsync();

    public Task<TestApplication> CreateApplicationAsync(bool loginLimitScenario = false) =>
        TestApplication.CreateAsync(container.GetConnectionString(), loginLimitScenario);
}
