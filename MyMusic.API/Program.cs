using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyMusic.API;
using MyMusic.API.Auth;
using MyMusic.API.Validation;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;
using MyMusic.Mongo.Db;
using MyMusic.Services;

var builder = WebApplication.CreateBuilder(args);

// Persistence and domain services
builder.Services.AddMongoPersistence(builder.Configuration);
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<IMusicService, MusicService>();
builder.Services.AddScoped<IComposerService, ComposerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// JWT authentication. The signing key comes from configuration (the
// Jwt__Key environment variable or a secret store), never from source.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Jwt:Key is not configured. Set it via the Jwt__Key environment variable.");
    }

    jwt.Key = "development-only-signing-key-do-not-use-in-production";
    builder.Configuration["Jwt:Key"] = jwt.Key;
}

// HS256 needs a key of at least 256 bits. Fail at startup rather than on the
// first sign-in, so a short key can't pass the deploy smoke tests.
if (Encoding.UTF8.GetByteCount(jwt.Key) < JwtOptions.MinimumKeyBytes)
{
    throw new InvalidOperationException(
        $"Jwt:Key must be at least {JwtOptions.MinimumKeyBytes} bytes (256 bits) for HS256. Generate one with `openssl rand -base64 48`.");
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        // A token outlives nothing: reject it once its user has been deleted.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                if (userId is null || await users.GetByIdAsync(userId, context.HttpContext.RequestAborted) is null)
                {
                    context.Fail("The user no longer exists.");
                }
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "unique_name"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

// In production the API runs in a Cloudflare Container behind a Worker that
// terminates TLS; trust its forwarded headers.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GoodMusic API",
        Version = "v1",
        Description = "Music catalog on ASP.NET Core 10 and MongoDB. Sign in with POST /api/User/authenticate, then use Authorize."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token from /api/User/authenticate or /api/User/register."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

await app.Services.GetRequiredService<MongoContext>().InitializeAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();

// The API docs are the landing page.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GoodMusic API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "GoodMusic API";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> in the tests.
public partial class Program { }
