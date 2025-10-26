using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;
using Npgsql;
using TaskyV2.Application.DTOs;
using TaskyV2.Application.Validation;
using TaskyV2.Infrastructure.Auth;
using TaskyV2.Infrastructure.Data;
using TaskyV2.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

static string? PickConnectionString(IConfiguration configuration) =>
    configuration.GetConnectionString("Default")
    ?? configuration["ConnectionStrings:Default"]
    ?? configuration["DATABASE_URL"];

static bool LooksLikePostgres(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return false;
    value = value.Trim();
    return value.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("Username=", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}

static string NormalizePostgresConnectionString(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        throw new ArgumentException("Connection string cannot be empty.", nameof(raw));

    raw = raw.Trim();

    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            throw new InvalidOperationException($"Invalid PostgreSQL URL '{raw}'.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/')
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var tokens = pair.Split('=', 2);
                if (tokens.Length == 2 && !string.IsNullOrEmpty(tokens[0]))
                {
                    builder[tokens[0]] = Uri.UnescapeDataString(tokens[1]);
                }
            }
        }

        if (!builder.ContainsKey("SslMode"))
        {
            builder.SslMode = SslMode.Require;
        }

        if (!builder.ContainsKey("Trust Server Certificate"))
        {
        }

        return builder.ConnectionString;
    }
    else
    {
        var builder = new NpgsqlConnectionStringBuilder(raw);
        if (builder.Port == 0)
        {
            builder.Port = 5432;
        }

        if (!builder.ContainsKey("SslMode"))
        {
            builder.SslMode = SslMode.Require;
        }

        if (!builder.ContainsKey("Trust Server Certificate"))
        {
        }

        return builder.ConnectionString;
    }
}

static string NormalizeSqliteConnectionString(string raw)
{
    var builder = new SqliteConnectionStringBuilder(raw);
    if (string.IsNullOrWhiteSpace(builder.DataSource))
    {
        builder.DataSource = "/opt/render/project/data/tasky_v2.db";
    }

    if (!Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);
    }

    var directory = Path.GetDirectoryName(builder.DataSource);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return builder.ToString();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Tasky V2 API",
        Version = "v1",
        Description = "Assignment 2: Auth + Projects + Tasks + Smart Scheduler",
    });

    // JWT bearer
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste ONLY the JWT (no 'Bearer ' prefix)."
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);

    // IMPORTANT: reference the scheme by Id so Swagger UI applies it
    var bearerRef = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { bearerRef, Array.Empty<string>() }
    });
});

var rawConnectionString = PickConnectionString(config);
var usePostgres = LooksLikePostgres(rawConnectionString);
string effectiveConnectionString;
string providerLabel;

if (usePostgres)
{
    effectiveConnectionString = NormalizePostgresConnectionString(rawConnectionString!);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(effectiveConnectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
    providerLabel = "PostgreSQL";
}
else
{
    effectiveConnectionString = NormalizeSqliteConnectionString(rawConnectionString ?? "Data Source=/opt/render/project/data/tasky_v2.db");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(effectiveConnectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
    providerLabel = "SQLite";
}

builder.Services.AddSingleton(new DbProviderInfo(providerLabel));


builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;

                if (origin.StartsWith("http://localhost:5174") ||
                    origin.StartsWith("http://localhost:3000") ||
                    origin.StartsWith("https://mini-project-manager-eta.vercel.app"))
                {
                    return true;
                }

                return origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
            })
            .AllowCredentials());
});

builder.Services.AddProblemDetails();



builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<SchedulerService>();

// JWT auth
var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.IncludeErrorDetails = true;
        o.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT auth failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine("JWT challenge: " + ctx.Error + " - " + ctx.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database provider {ProviderName} ready.", db.Database.ProviderName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed.");
    }
}

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
    {
        var s = auth.ToString().Trim();
        if (!string.IsNullOrEmpty(s) && !s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Request.Headers["Authorization"] = "Bearer " + s;
        }
    }
    await next();
});

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Helper to get current userId from JWT
Guid GetUserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ??
    user.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

// ====== AUTH ======
var auth = app.MapGroup("/api/v1/auth").WithTags("Auth");

auth.MapPost("/register", async (RegisterRequest req, IValidator<RegisterRequest> v, AuthService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    try
    {
        await svc.RegisterAsync(req, ct);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(
            statusCode: 409,
            title: "Email already registered",
            detail: ex.Message);
    }
    catch (DbUpdateException dbEx)
    {
        var message = dbEx.InnerException?.Message ?? dbEx.Message;
        if (!string.IsNullOrWhiteSpace(message) && message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: 409,
                title: "Email already registered",
                detail: "Please use a different email address.");
        }
        return Results.Problem(
            statusCode: 500,
            title: "Unable to register user",
            detail: message);
    }
});

auth.MapPost("/login", async (LoginRequest req, IValidator<LoginRequest> v, AuthService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    try
    {
        var token = await svc.LoginAsync(req, ct);
        return Results.Ok(token);
    }
    catch (Exception ex)
    {
        // TEMP: show the reason
        return Results.Problem(statusCode: 401, title: "Login failed", detail: ex.Message);
    }
});


// ====== PROJECTS ======
var projects = app.MapGroup("/api/v1/projects").WithTags("Projects").RequireAuthorization();

projects.MapGet("/", async (ClaimsPrincipal u, ProjectService svc, CancellationToken ct) =>
{
    var userId = GetUserId(u);
    return Results.Ok(await svc.GetAllAsync(userId, ct));
});

projects.MapGet("/{projectId:guid}", async (ClaimsPrincipal u, Guid projectId, ProjectService svc, CancellationToken ct) =>
{
    var res = await svc.GetAsync(GetUserId(u), projectId, ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
});

projects.MapPost("/", async (ClaimsPrincipal u, ProjectCreateRequest req, IValidator<ProjectCreateRequest> v, ProjectService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    var userId = GetUserId(u);
    var created = await svc.CreateAsync(userId, req, ct);
    return Results.Created($"/api/v1/projects/{created.Id}", created);
});

projects.MapPut("/{projectId:guid}", async (ClaimsPrincipal u, Guid projectId, ProjectUpdateRequest req, IValidator<ProjectUpdateRequest> v, ProjectService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    var ok = await svc.UpdateAsync(GetUserId(u), projectId, req, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

projects.MapDelete("/{projectId:guid}", async (ClaimsPrincipal u, Guid projectId, ProjectService svc, CancellationToken ct) =>
{
    var ok = await svc.DeleteAsync(GetUserId(u), projectId, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

// ====== TASKS ======
projects.MapGet("/{projectId:guid}/tasks", async (ClaimsPrincipal u, Guid projectId, TaskService svc, CancellationToken ct) =>
{
    return Results.Ok(await svc.ListAsync(GetUserId(u), projectId, ct));
});

projects.MapPost("/{projectId:guid}/tasks", async (ClaimsPrincipal u, Guid projectId, TaskCreateRequest req, IValidator<TaskCreateRequest> v, TaskService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    var res = await svc.CreateAsync(GetUserId(u), projectId, req, ct);
    return res is null ? Results.NotFound() : Results.Created($"/api/v1/projects/{projectId}/tasks/{res.Id}", res);
});

projects.MapPut("/{projectId:guid}/tasks/{taskId:guid}", async (ClaimsPrincipal u, Guid projectId, Guid taskId, TaskUpdateRequest req, IValidator<TaskUpdateRequest> v, TaskService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    var ok = await svc.UpdateAsync(GetUserId(u), projectId, taskId, req, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

projects.MapPatch("/{projectId:guid}/tasks/{taskId:guid}/toggle", async (ClaimsPrincipal u, Guid projectId, Guid taskId, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.ToggleAsync(GetUserId(u), projectId, taskId, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

projects.MapDelete("/{projectId:guid}/tasks/{taskId:guid}", async (ClaimsPrincipal u, Guid projectId, Guid taskId, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.DeleteAsync(GetUserId(u), projectId, taskId, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

// ====== TASKS (flat routes) ======
var tasks = app.MapGroup("/api/v1/tasks").WithTags("Tasks").RequireAuthorization();

tasks.MapGet("/{taskId:guid}", async (ClaimsPrincipal u, Guid taskId, TaskService svc, CancellationToken ct) =>
{
    var res = await svc.GetAsync(GetUserId(u), taskId, ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
});

tasks.MapPut("/{taskId:guid}", async (ClaimsPrincipal u, Guid taskId, TaskUpdateRequest req, IValidator<TaskUpdateRequest> v, TaskService svc, CancellationToken ct) =>
{
    var vr = await v.ValidateAsync(req, ct);
    if (!vr.IsValid) return Results.ValidationProblem(vr.ToDictionary());
    var ok = await svc.UpdateAsync(GetUserId(u), taskId, req, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

tasks.MapPatch("/{taskId:guid}/toggle", async (ClaimsPrincipal u, Guid taskId, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.ToggleAsync(GetUserId(u), taskId, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

tasks.MapDelete("/{taskId:guid}", async (ClaimsPrincipal u, Guid taskId, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.DeleteAsync(GetUserId(u), taskId, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

// ====== SCHEDULER ======
projects.MapPost("/{projectId:guid}/schedule",
    async (ClaimsPrincipal u, Guid projectId, SchedulerService svc, ScheduleInput input, CancellationToken ct) =>
{
    var res = await svc.GenerateAsync(GetUserId(u), projectId, input, ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
})
.WithTags("Projects");


app.MapGet("/healthz", async (AppDbContext db, IConfiguration configuration, DbProviderInfo provider) =>
{
    bool canConnect;
    try
    {
        canConnect = await db.Database.CanConnectAsync();
    }
    catch
    {
        canConnect = false;
    }

    var allowed = configuration.GetValue<string>("AllowedOrigins");
    return Results.Ok(new
    {
        status = "ok",
        timeUtc = DateTime.UtcNow,
        provider = provider.ProviderName,
        database = canConnect,
        environment = app.Environment.EnvironmentName,
        allowedOrigins = allowed ?? "*.vercel.app",
    });
});

app.MapGet("/debug/users", async (AppDbContext db) =>
{
    var data = await db.Users
        .OrderBy(u => u.CreatedAtUtc)
        .Select(u => new { u.Email, u.PasswordHash })
        .ToListAsync();
    return Results.Ok(data);
});

app.MapGet("/debug/verify", async (AppDbContext db, string email, string password) =>
{
    try
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized);
        if (user is null) return Results.NotFound(new { email });
        var ok = BCrypt.Net.BCrypt.Verify(password.Trim(), user.PasswordHash);
        return Results.Ok(new { user.Email, ok });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[debug/verify] {ex}");
        return Results.Text(ex.ToString(), "text/plain", statusCode: 500);
    }
});

app.Run();

sealed record DbProviderInfo(string ProviderName);



