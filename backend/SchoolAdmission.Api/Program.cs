using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new ErrorResponse(
                "INVALID_REQUEST", "The request body is missing or invalid."));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// HOLD_DURATION_SECONDS comes from the environment (default 600)
var holdSeconds = builder.Configuration.GetValue<int?>("HOLD_DURATION_SECONDS") ?? 600;
if (holdSeconds <= 0) holdSeconds = 600;
builder.Services.AddSingleton(new HoldOptions { DurationSeconds = holdSeconds });

builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<HoldService>();
builder.Services.AddHostedService<HoldExpiryWorker>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

public partial class Program { }