using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Tasky.Application.Abstractions;
using Tasky.Application.DTOs;
using Tasky.Application.Services;
using Tasky.Application.Validation;
using Tasky.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();

// DI wiring
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddSingleton<TaskService>();

//Cors
builder.Services.AddCors(o =>
{
    o.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000") 
        .AllowAnyHeader()
        .AllowAnyMethod());
});


// ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseCors("Frontend");


app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}




// Map endpoints
var group = app.MapGroup("/api/v1/tasks").WithTags("Tasks");

// List all
// group.MapGet("/", async (TaskService svc, CancellationToken ct)
//     => Results.Ok(await svc.GetAllAsync(ct)));

// List with filters/search/sort/pagination
group.MapGet("/", async (
    TaskService svc,
    [FromQuery] string? status,      // "all" | "active" | "completed"
    [FromQuery] string? search,      // substring match on description
    [FromQuery] string? sort = "createdAt", // "createdAt" | "description"
    [FromQuery] string? order = "desc",     // "asc" | "desc"
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default) =>
{
    var q = (await svc.GetAllAsync(ct)).AsQueryable();

    // filter
    if (!string.IsNullOrWhiteSpace(status))
    {
        if (status.Equals("active", StringComparison.OrdinalIgnoreCase))   q = q.Where(t => !t.IsCompleted);
        else if (status.Equals("completed", StringComparison.OrdinalIgnoreCase)) q = q.Where(t => t.IsCompleted);
    }

    // search
    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(t => t.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

    // sort
    bool asc = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
    q = (sort?.ToLowerInvariant()) switch
    {
        "description" => asc ? q.OrderBy(t => t.Description) : q.OrderByDescending(t => t.Description),
        _             => asc ? q.OrderBy(t => t.CreatedAtUtc) : q.OrderByDescending(t => t.CreatedAtUtc),
    };

    // page
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 200);
    var total = q.Count();
    var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return Results.Ok(new { total, page, pageSize, items });
})
.WithName("ListTasks");


// Get by id
group.MapGet("/{id:guid}", async (Guid id, TaskService svc, CancellationToken ct) =>
{
    var t = await svc.GetAsync(id, ct);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

// Create
group.MapPost("/", async ([FromBody] CreateTaskRequest req, IValidator<CreateTaskRequest> validator, TaskService svc, CancellationToken ct) =>
{
    var val = await validator.ValidateAsync(req, ct);
    if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());
    var created = await svc.CreateAsync(req, ct);
    return Results.Created($"/api/v1/tasks/{created.Id}", created);
});

// Update
group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTaskRequest req, IValidator<UpdateTaskRequest> validator, TaskService svc, CancellationToken ct) =>
{
    var val = await validator.ValidateAsync(req, ct);
    if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());
    var ok = await svc.UpdateAsync(id, req, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

// Toggle
group.MapPatch("/{id:guid}/toggle", async (Guid id, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.ToggleAsync(id, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

// Delete
group.MapDelete("/{id:guid}", async (Guid id, TaskService svc, CancellationToken ct) =>
{
    var ok = await svc.DeleteAsync(id, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

app.Run();
