using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
using BancoAna.Account.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BancoAna.Account.Tests.Integration
{
    public class AccountApiIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;
        private string? _dbFile;

        public AccountApiIntegrationTests()
        {
            // não usar _factory aqui
        }

        public async Task InitializeAsync()
        {
            _dbFile = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid():N}.db");

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, cfg) =>
                    {
                        cfg.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Sqlite"] = $"Data Source={_dbFile}"
                        });
                    });
                });

            _client = _factory.CreateClient();
        }

        public Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();

            if (File.Exists(_dbFile))
                File.Delete(_dbFile);

            return Task.CompletedTask;
        }
    }
}
