
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Contracts.Requests;
using AccessRefresh.Data.Context;
using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.UserRepository;
using AccessRefresh.Services.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace AccessRefresh.Tests;

public class AuthControllerTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    private const string AdminUsername = "adminuser";
    private const string TestUsername = "testuser";
    private const string TestPassword = "testpassword";

    public AuthControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _prepareForTests();
    }

    #region API Tests

    [Fact]
    public async Task Test_SignUp_Success()
    {
        var client = _getClient();
        
        var request = new AuthRequest
        {
            Password = TestPassword,
            Username = TestUsername
        };
        
        var response = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/sign-up", request);
        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content.ReadFromJsonAsync<TokensDto>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task Test_SignIn_Success()
    {
        await _authenticate();
    }
    
    [Fact]
    public async Task Test_SignIn_Failure_InvalidCredentials()
    {
        var client = _getClient();

        var request = new AuthRequest
        {
            Username = AdminUsername,
            Password = "wrongpassword",
        };
        
        var response = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/sign-in", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Test_RefreshToken_TooEarly()
    {
        var client = _getClient();
        var tokens = await _authenticate();
        var refreshResponse = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
    
    [Fact]
    public async Task Test_Me_Authorized()
    {
        var client = _getClient();
        var tokens = await _authenticate();
        
        // Set the access token in the Authorization header
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        
        var response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
        Assert.True(response.IsSuccessStatusCode);
        
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal(nameof(EUserRole.Admin), user.Role);
        Assert.Equal(AdminUsername, user.Username);
    }

    [Fact]
    public async Task Test_Me_NotAuthorized()
    {
        var client = _getClient();
        
        // No authentication header set
        var response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Set an invalid token
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalidtoken");
        
        response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Test_CacheLatency()
    {
        var client = _getClient();
        var tokens = await _authenticate();
        
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
            Assert.True(response.IsSuccessStatusCode);
        }
        stopwatch.Stop();
        _output.WriteLine($"Elapsed time for 1000 requests: {stopwatch.ElapsedMilliseconds} ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 1500, 
            "Response time should be less than 1.5 second for cached requests.");
    }

    #endregion

    #region Private methods

    private void _prepareForTests()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        userRepository.Add(new User
        {
            Role = EUserRole.Admin,
            PasswordHash = PasswordService.HashPassword(TestPassword),
            Username = AdminUsername
        });
        dbContext.SaveChanges();
    }

    private async Task<TokensDto> _authenticate()
    {
        var client = _getClient();
        // Sign in to get initial tokens
        var signInRequest = new AuthRequest
        {
            Username = AdminUsername,
            Password = TestPassword,
        };
        
        var signInResponse = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/sign-in", signInRequest);
        Assert.True(signInResponse.IsSuccessStatusCode);
        var tokens = await signInResponse.Content.ReadFromJsonAsync<TokensDto>();
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);
        return tokens;
    }
    
    private HttpClient _getClient()
    {
        var client = _factory.CreateClient();
    
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");
        client.DefaultRequestHeaders.Add("User-Agent", "TestClient/1.0");
        client.DefaultRequestHeaders.Add("X-Fingerprint", "test-fingerprint");
        
        return client;
    }

    #endregion
}