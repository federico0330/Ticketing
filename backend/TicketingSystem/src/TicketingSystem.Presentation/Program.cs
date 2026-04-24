using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Handlers;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Infrastructure.Persistence;
using TicketingSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── Base de Datos ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Repositorios (Infrastructure → Application interfaces) ────────────────
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ISectorRepository, SectorRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ─── Handlers (Application) ────────────────────────────────────────────────
builder.Services.AddScoped<GetAllEventsHandler>();
builder.Services.AddScoped<GetSectorsByEventIdHandler>();
builder.Services.AddScoped<GetSeatsBySectorIdHandler>();
builder.Services.AddScoped<CreateReservationHandler>();

// ─── Controllers y JSON ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serializar enums como strings y respetar los nombres de propiedades
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ─── Swagger / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ticketing System API", Version = "v1" });
    // Incluir comentarios XML para documentar endpoints
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ─── CORS (para que el frontend pueda consumir la API) ─────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Aplicar migraciones y seed automáticamente al iniciar ─────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retryCount = 5;
    var delay = TimeSpan.FromSeconds(3);

    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            await db.Database.MigrateAsync(); // Aplica migraciones pendientes
            await DbSeeder.SeedAsync(db);     // Precarga datos si la BD está vacía
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

// ─── Middleware Pipeline ────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticketing API v1"));
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();