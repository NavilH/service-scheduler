using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// In production, secrets are supplied via Azure App Service Application Settings (env vars).
// To go further, replace with Azure Key Vault + Managed Identity:
//   builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration["KeyVaultUri"]!), new DefaultAzureCredential());
// Azure.Security.KeyVault.Secrets and Azure.Identity are already referenced.
// Locally, secrets are stored in .NET User Secrets (dotnet user-secrets set ...).

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigin = builder.Configuration["AllowedOrigin"] ?? "http://localhost:5173";
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = 500;
        var problem = new ProblemDetails
        {
            Status = 500,
            Title = "An unexpected error occurred.",
            Instance = ctx.Request.Path
        };
        if (app.Environment.IsDevelopment())
        {
            var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
            problem.Detail = ex?.ToString();
        }
        await ctx.Response.WriteAsJsonAsync(problem);
    });
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "UP" }));

app.Run();

public partial class Program { }
