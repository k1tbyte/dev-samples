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
using AccessRefresh.Services.Domain.TokenService;
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
    public async Task Test_RefreshToken_Success()
    {
        var client = _getClient();
        var tokens = await _authenticate();

        var jwtService = _factory.Services.GetRequiredService<JwtService>();
        var decoded = jwtService.ReadToken(tokens.AccessToken)!;
        tokens.AccessToken = jwtService.GenerateAccessToken(
            decoded.Claims,
            DateTime.UtcNow.AddSeconds(5) // Set a short expiration time for testing
        );
        
        var refreshResponse = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        Assert.True(refreshResponse.IsSuccessStatusCode);
        
        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<TokensDto>();
        Assert.NotNull(newTokens);
        Assert.NotEmpty(newTokens.AccessToken);
        Assert.NotEmpty(newTokens.RefreshToken);
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
        var sessions = await client.GetFromJsonAsync<IEnumerable<SessionDto>>($"{Constants.RoutePrefix}/auth/sessions");
        Assert.NotNull(sessions);
        Assert.Single(sessions);
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

    [Fact]
    public async Task Test_LogOut()
    {
        var client = _getClient();
        var tokens = await _authenticate();
        
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.DeleteAsync($"{Constants.RoutePrefix}/auth/revoke");
        Assert.True(response.IsSuccessStatusCode);
    }
    
    [Fact]
    public async Task Test_RevokeAllSessions()
    {
        var client = _getClient();
        TokensDto tokens = null!;
        for (int i = 0; i < 3; i++)
        {
            tokens = await _authenticate();
        }
        
        
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        
        var response = await client.DeleteAsync($"{Constants.RoutePrefix}/auth/revoke?all=true");
        Assert.True(response.IsSuccessStatusCode);
        
        response = await client.GetAsync($"{Constants.RoutePrefix}/auth/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Verify that the session is revoked
        response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    
    [Fact]
    public async Task Test_MissingFingerprint_ShouldFail()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");
        client.DefaultRequestHeaders.Add("User-Agent", "TestClient/1.0");
        // No X-Fingerprint header
    
        var request = new AuthRequest
        {
            Username = AdminUsername,
            Password = TestPassword
        };
    
        var response = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/sign-in", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Test_MalformedJWT_ShouldFail()
    {
        var client = _getClient();
    
        var malformedTokens = new[]
        {
            "not.a.jwt",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.malformed",
            "",
            "Bearer token-without-bearer"
        };
    
        foreach (var token in malformedTokens)
        {
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        
            var response = await client.GetAsync($"{Constants.RoutePrefix}/user/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Test_DifferentFingerprint_ShouldFail()
    {
        var tokens = await _authenticate();

        var client2 = _getClient();
        // Simulate a different fingerprint by changing the header
        client2.DefaultRequestHeaders.Remove("X-Fingerprint");
        client2.DefaultRequestHeaders.Add("X-Fingerprint", "different-fingerprint");
    
        var jwtService = _factory.Services.GetRequiredService<JwtService>();
        var decoded = jwtService.ReadToken(tokens.AccessToken)!;
        tokens.AccessToken = jwtService.GenerateAccessToken(
            decoded.Claims,
            DateTime.UtcNow.AddSeconds(5)
        );
    
        var response = await client2.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Test_ConcurrentRefresh_SameToken()
    {
        var client1 = _getClient();
        var client2 = _getClient();
        var tokens = await _authenticate();
    
        // Expire token
        var jwtService = _factory.Services.GetRequiredService<JwtService>();
        var decoded = jwtService.ReadToken(tokens.AccessToken)!;
        tokens.AccessToken = jwtService.GenerateAccessToken(
            decoded.Claims,
            DateTime.UtcNow.AddSeconds(5)
        );
    
        // Concurrent refresh attempts
        var task1 = client1.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        var task2 = client2.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
    
        var responses = await Task.WhenAll(task1, task2);
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        Assert.Equal(2, successCount);
        
        var token1 = await responses[0].Content.ReadFromJsonAsync<TokensDto>();
        var token2 = await responses[1].Content.ReadFromJsonAsync<TokensDto>();
        Assert.Equal(token1!.AccessToken, token2!.AccessToken);
    }
    
    [Fact]
    public async Task Test_RefreshToken_ConcurrentReuse()
    {
        var client = _getClient();
        var tokens = await _authenticate();
    
        // Simulate token expiration
        var jwtService = _factory.Services.GetRequiredService<JwtService>();
        var decoded = jwtService.ReadToken(tokens.AccessToken)!;
        tokens.AccessToken = jwtService.GenerateAccessToken(
            decoded.Claims,
            DateTime.UtcNow.AddSeconds(5)
        );
        
        var firstRefresh =  client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        await Task.Delay(1000); 
        var secondRefresh =  client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/refresh", tokens);
        var responses = await Task.WhenAll(firstRefresh, secondRefresh);
        Assert.Equal(HttpStatusCode.OK, responses[0].StatusCode);
        Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
        var firstTokens = await responses[0].Content.ReadFromJsonAsync<TokensDto>();
        var secondTokens = await responses[1].Content.ReadFromJsonAsync<TokensDto>();
        Assert.NotNull(firstTokens);
        Assert.NotNull(secondTokens);
        Assert.Equal(firstTokens.AccessToken, secondTokens!.AccessToken);
    }
    
    [Fact]
    public async Task Test_SignUp_ValidationErrors()
    {
        var client = _getClient();
    
        var testCases = new[]
        {
            new AuthRequest { Username = "", Password = "validpassword123" }, // Empty username
            new AuthRequest { Username = "validuser", Password = "" }, // Empty password
            new AuthRequest { Username = "ab", Password = "validpassword123" }, // Too short username
            new AuthRequest { Username = "validuser", Password = "123" }, // Too short password
            new AuthRequest { Username = new string('a', 51), Password = "validpassword123" }, // Too long username
            new AuthRequest { Username = "user@invalid", Password = "validpassword123" }, // Invalid chars
        };
    
        foreach (var testCase in testCases)
        {
            var response = await client.PostAsJsonAsync($"{Constants.RoutePrefix}/auth/sign-in", testCase);
        
            _output.WriteLine($"Testing: {testCase.Username}/{testCase.Password}");
            _output.WriteLine($"Response: {response.StatusCode}");
        
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Content: {content}");
            }
        
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
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