using Microsoft.EntityFrameworkCore;
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

// Registramos por interfaz para cumplir con el Principio de Inversión de Dependencias (DIP de SOLID)
builder.Services.AddScoped<IGetAllEventsHandler, GetAllEventsHandler>();
builder.Services.AddScoped<IGetSectorsByEventIdHandler, GetSectorsByEventIdHandler>();
builder.Services.AddScoped<IGetSeatsBySectorIdHandler, GetSeatsBySectorIdHandler>();
builder.Services.AddScoped<IGetUserReservationsHandler, GetUserReservationsHandler>();
builder.Services.AddScoped<ICreateReservationHandler, CreateReservationHandler>();
builder.Services.AddScoped<IConfirmPaymentHandler, ConfirmPaymentHandler>();
builder.Services.AddScoped<ILoginHandler, LoginHandler>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddHostedService<TicketingSystem.Infrastructure.BackgroundServices.ReservationExpirationWorker>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ticketing System API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

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
// app.UseAuthorization(); // No hay autenticación configurada
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();
