# Sistema de Venta de Entradas — Entrega 1

## Requisitos
- .NET 8 SDK
- Node.js (opcional, solo para servidor estático del frontend)
- Docker (requerido para la base de datos SQL Server)

## Levantar la Base de Datos y el Backend

La aplicación utiliza SQL Server corriendo sobre Docker. Para inicializar el ecosistema completo:

```bash
docker-compose up -d db
cd backend/TicketingSystem
dotnet restore
dotnet run --project src/TicketingSystem.Presentation
```

La API estará disponible en: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

> **Nota:** Las migraciones se aplican y el seed de datos se ejecuta automáticamente al iniciar la aplicación mediante EF Core.

## Levantar el Frontend
Abrir `frontend/index.html` directamente en el navegador, o usar un servidor HTTP local (como `serve` de npm o Live Server):
```bash
cd frontend
npx serve .
```

## Credenciales de prueba
- Usuario ID: 1 (preconfigurado en el seed, hardcodeado en las reservas de frontend temporalmente)
- Email: demo@ticketing.com
