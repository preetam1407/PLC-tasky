using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskyV2.Application.DTOs;
using TaskyV2.Application.Validation;
using TaskyV2.Infrastructure.Auth;
using TaskyV2.Infrastructure.Data;
using TaskyV2.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

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



builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(config.GetConnectionString("Default")));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins("http://localhost:5173","http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod()));

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
    await svc.RegisterAsync(req, ct);
    return Results.NoContent();
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

// ====== SCHEDULER ======
projects.MapPost("/{projectId:guid}/schedule",
    async (ClaimsPrincipal u, Guid projectId, SchedulerService svc, ScheduleInput input, CancellationToken ct) =>
{
    var res = await svc.GenerateAsync(GetUserId(u), projectId, input, ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
})
.WithTags("Projects");


app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));


app.Run();
