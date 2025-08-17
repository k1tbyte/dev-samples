using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using AccessRefresh.Data.Context;
using AccessRefresh.Extensions;
using AccessRefresh.Middleware;
using AccessRefresh.Repositories.SessionRepository;
using AccessRefresh.Repositories.UserRepository;
using AccessRefresh.Services.Application;
using AccessRefresh.Services.Application.AuthService;
using AccessRefresh.Services.Domain;
using AccessRefresh.Services.Domain.CacheService;
using AccessRefresh.Services.Domain.TokenService;
using AccessRefresh.Services.Infrastructure.GeolocationService;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AccessRefresh;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });
        

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");
            return string.IsNullOrEmpty(redisConnectionString) ? 
                null! : 
                ConnectionMultiplexer.Connect(redisConnectionString);
        });
        
     //   builder.Services.AddHttpClient
     
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();
        builder.Services.AddLogging();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEnvironmentVariablesMapper();
        
        #region Services
        
        builder.Services.AddSingleton<JwtService>();
        builder.Services.AddSingleton<JwtSecurityTokenHandler>();
        
        builder.Services.AddKeyedSingleton<ICacheProvider, RedisCacheProvider>("primary");
        builder.Services.AddKeyedSingleton<ICacheProvider, MemoryCacheProvider>("fallback");
        builder.Services.AddSingleton<ICacheManager, CacheManager>();
        
        builder.Services.AddSingleton<IGeolocationService, IpApiService>();
        builder.Services.AddDbContext<AppDbContext>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ISessionRepository, SessionRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        #endregion

        var app = builder.Build();

        app.MapEnvironmentVariables();
        
#if !DEBUG
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
#endif
        
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseMiddleware<AuthenticationMiddleware>();


        app.MapControllers();
        app.MapExceptionsHandler();

        app.Run();
    }
}