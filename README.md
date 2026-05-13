# Sistema de Venta de Entradas — Entrega 2

API REST en .NET 8 con Clean Architecture (Domain / Application / Infrastructure / Presentation), JWT, panel de administración con editor visual de asientos, control de concurrencia optimista, auditoría inmutable y job de expiración en background. Frontend HTML + Vanilla JS sobre Bootstrap 5.

---

## Requisitos

- **Docker** (única dependencia obligatoria para correr todo el stack: SQL Server + API .NET + Frontend Nginx).
- Para correr tests o trabajar el backend localmente: **.NET SDK 8.0**.

## Levantar el proyecto completo

Desde la raíz del repo:

```bash
docker compose up --build -d
```

Una vez arrancado:

| Servicio | URL |
|---|---|
| Frontend (App Web) | http://localhost:3000 |
| Backend API | http://localhost:5000/api/v1 |
| Swagger UI | http://localhost:5000/swagger |

> Las migraciones de EF Core y el seed se aplican automáticamente al iniciar el contenedor de la API (con retry resiliente esperando a SQL Server).

## Credenciales de prueba (Seed)

| Email | Password | Rol |
|---|---|---|
| `admin@ticketing.com` | `admin` | **Admin** |
| `user1@ticketing.com` | `user1` | User |
| `user2@ticketing.com` | `user2` | User |
| `user3@ticketing.com` | `user3` | User |
| `user4@ticketing.com` | `user4` | User |
| `user5@ticketing.com` | `user5` | User |

> El rol **Admin** se resuelve dinámicamente desde la lista `AdminUsers` en `appsettings.json` (la tabla `USER` no tiene columna Role por restricción de schema).

---

## Endpoints principales

Todos bajo `/api/v1`. Los marcados con 🔒 requieren JWT (`Authorization: Bearer <token>`); 🛡️ requieren rol `Admin`.

### Auth
- `POST /auth/sessions` → login (devuelve `token` + `role`)
- `POST /auth/register` → alta de usuario

### Eventos
- `GET /events` → lista de eventos activos (admin ve también `Deleted`)
- `POST /events` 🛡️ → crear evento con sectores y asientos explícitos
- `PUT /events/{id}` 🛡️ → modificar evento
- `DELETE /events/{id}` 🛡️ → soft-delete
- `GET /events/{id}/sectors` → sectores del evento

### Asientos
- `GET /sectors/{sectorId}/seats?userId={id}` → mapa de asientos (marca cuáles son del usuario actual)

### Reservas
- `POST /reservations` 🔒 → reserva de un asiento (5 min de hold)
- `POST /reservations/batch` 🔒 → reserva multi-asiento atómica
- `POST /reservations/payments` → pago tradicional (CC simulado)
- `POST /reservations/batch-payments` 🔒 → pago batch con token
- `GET /reservations/mine?userId={id}` 🔒 → reservas pendientes del usuario

### Códigos HTTP
- `200/201` éxito · `400` payload inválido · `401` sin auth · `404` no existe · `409` conflicto de concurrencia / asiento tomado · `500` error inesperado

---

## Funcionalidades clave

- **JWT** con roles dinámicos (`AdminUsers` en `appsettings.json`), claim `Role` en el token, `[Authorize(Roles = "Admin")]` en endpoints sensibles.
- **Editor visual de eventos** (panel Admin): grilla filas × columnas, click para activar/desactivar celdas, múltiples sectores por evento, capacidad calculada en función de asientos activos enviados.
- **Concurrencia optimista** sobre `Seat.Version` (token `IsConcurrencyToken`): dos requests pisando la misma butaca → uno completa, el otro recibe **HTTP 409** (jamás 500).
- **Reservas con TTL 5 minutos**: `BackgroundService` (`ReservationExpirationWorker`) recorre las expiradas cada 10s, libera la butaca y registra auditoría. Cada expiración va en su propia transacción (resiliente).
- **Auditoría inmutable** (`AUDIT_LOG`): cada acción queda con `UserId`, `Action`, `EntityType`, `EntityId`, `Details` (JSON) y `CreatedAt`. Los failures de concurrencia se distinguen del resto (`Reason = "Concurrency conflict"`).
- **Pago simulado**: tarjetas que terminan en `0000` simulan rechazo bancario para demostrar el flujo de error. Solo se loguean los **últimos 4 dígitos** enmascarados (`****1234`).
- **Frontend reactivo**: timer 5:00 → 0:00, polling de mapa cada 7s, toast en 409, auto-refresh tras conflicto, spinners durante requests, UI diferenciada Admin / User.

---

## Arquitectura — Clean Architecture

```
TicketingSystem/
├── src/
│   ├── TicketingSystem.Domain/         (Entities, Constants, Exceptions)
│   ├── TicketingSystem.Application/    (Handlers, DTOs, Interfaces, Commands/Queries, Security, Services)
│   ├── TicketingSystem.Infrastructure/ (AppDbContext, Repositories, UnitOfWork, BackgroundServices, Migrations)
│   └── TicketingSystem.Presentation/   (Controllers, Middleware, Program.cs, appsettings)
└── tests/
    └── TicketingSystem.Tests/          (xUnit + InMemory EF + Moq)
```

- **Domain**: sin EF, sin HTTP. Solo entidades y excepciones de negocio.
- **Application**: orquestación (handlers), contratos (interfaces de repositorios y servicios), DTOs. Sin dependencia de EF ni de ASP.NET.
- **Infrastructure**: implementación de repositorios, EF Core, migraciones, jobs.
- **Presentation**: API HTTP + middleware de mapeo de excepciones a códigos REST.

### Patrones implementados
- **Unit of Work** (`IUnitOfWork`) → transacciones explícitas con `Begin/Commit/Rollback`.
- **Repository** (`IEventRepository`, `ISeatRepository`, etc.) → abstracción sobre EF.
- **Handler/Use Case** (`ICreateReservationHandler`, etc.) → un handler por caso de uso.
- **Exception → HTTP mapping** centralizado en `GlobalExceptionHandler` (sin try/catch en cada controller).
- **JWT Token Service** (`IJwtTokenService`) y **Audit Logger** (`IAuditLogger`) reusables.

### Constantes de dominio
Estados como string en código pero tipados via constantes:
- `SeatStatus` → `Available`, `Reserved`, `Sold`
- `ReservationStatus` → `Pending`, `Paid`, `Expired`
- `EventStatus` → `Active`, `Deleted`
- `AuditAction` → `RESERVE_ATTEMPT`, `RESERVE_SUCCESS`, `RESERVE_FAILED`, `PAYMENT_SUCCESS`, `PAYMENT_FAILED`, `RESERVATION_EXPIRED`
- `UserRole` → `Admin`, `User`

---

## Tests

Proyecto `tests/TicketingSystem.Tests` con **xUnit + EF Core InMemory + Moq**.

```bash
cd backend/TicketingSystem
dotnet test
```

Cubre handlers críticos:
- Auth (login válido/inválido, registro nuevo/duplicado, rol Admin desde appsettings).
- Reservas (single + batch, butaca disponible, butaca tomada → `SeatNotAvailableException`, version++).
- Pagos (single + batch, tarjeta `0000` → `PaymentFailedException`, reserva expirada → 400, audit log creado).
- Concurrencia (dos reservas en paralelo → una gana, otra recibe `ConcurrencyException`).
- Eventos (admin crea con sectores y seats, soft-delete).

---

## Compilación local (sin Docker)

```bash
cd backend/TicketingSystem
dotnet restore
dotnet build
dotnet run --project src/TicketingSystem.Presentation
```

Necesita una instancia de SQL Server (ajustar `ConnectionStrings:DefaultConnection` en `appsettings.json`).

---

## Notas

- **Schema fijo** (`TABLA.md`): no se agregan columnas. Los roles van por `appsettings.json`.
- **Mensajes de commit** y distribución de trabajo visibles en el historial de Git.
