using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using TicketingSystem.Application.Handlers;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Infrastructure.Persistence;
using TicketingSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ISectorRepository, SectorRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Inyectamos por interfaz (DIP): los controllers no conocen la implementación concreta y los handlers son sustituibles en tests.
builder.Services.AddScoped<IGetAllEventsHandler, GetAllEventsHandler>();
builder.Services.AddScoped<IGetSectorsByEventIdHandler, GetSectorsByEventIdHandler>();
builder.Services.AddScoped<IGetSeatsBySectorIdHandler, GetSeatsBySectorIdHandler>();
builder.Services.AddScoped<IGetUserReservationsHandler, GetUserReservationsHandler>();
builder.Services.AddScoped<ICreateReservationHandler, CreateReservationHandler>();
builder.Services.AddScoped<ICreateEventHandler, CreateEventHandler>();
builder.Services.AddScoped<IUpdateEventHandler, UpdateEventHandler>();
builder.Services.AddScoped<IDeleteEventHandler, DeleteEventHandler>();
builder.Services.AddScoped<IConfirmPaymentHandler, ConfirmPaymentHandler>();
builder.Services.AddScoped<IConfirmBatchPaymentHandler, ConfirmBatchPaymentHandler>();
builder.Services.AddScoped<ICreateBatchReservationHandler, CreateBatchReservationHandler>();
builder.Services.AddScoped<ILoginHandler, LoginHandler>();
builder.Services.AddScoped<IRegisterHandler, RegisterHandler>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddHostedService<TicketingSystem.Infrastructure.BackgroundServices.ReservationExpirationWorker>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Mantenemos PascalCase en el JSON para que el frontend (que consume Id, Name, etc.) no necesite mapear nombres.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// JWT Authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ticketing System API", Version = "v1" });

    // Swagger JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Retry de migración/seed: en docker-compose la API arranca antes de que SQL Server termine de inicializar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retryCount = 15;
    var delay = TimeSpan.FromSeconds(3);

    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            await db.Database.MigrateAsync(); 
            await DbSeeder.SeedAsync(db);     
            logger.LogInformation("Database migrated and seeded successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (i == retryCount - 1)
            {
                logger.LogError(ex, "An error occurred while migrating or seeding the database. Max retries reached.");
                throw;
            }
            logger.LogWarning(ex, $"Database connection failed. Retrying in {delay.TotalSeconds} seconds... ({i + 1}/{retryCount})");
            await Task.Delay(delay);
        }
    }
}

app.UseMiddleware<TicketingSystem.Presentation.Middleware.GlobalExceptionHandler>();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Api-version", "1.0");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticketing API v1"));
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();