# Sistema de Venta de Entradas — Proyecto de Software 1C2026

API REST en .NET 8 con Clean Architecture, JWT, concurrencia optimista, auditoría inmutable y job de expiración en background. Frontend HTML + Vanilla JS sobre Bootstrap 5.

---

## Requisitos

- **Docker** (única dependencia para correr el stack completo: SQL Server + API .NET + Frontend Nginx).
- Para compilar o testear localmente: **.NET SDK 8.0**.

---

## Levantar el proyecto completo (Docker)

Desde la raíz del repo:

```bash
docker compose up --build -d
```

| Servicio | URL |
|---|---|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5000/api/v1 |
| Swagger UI | http://localhost:5000/swagger |

> Las migraciones y el seed se aplican automáticamente al iniciar el contenedor de la API.

---

## Credenciales de prueba (Seed)

| Email | Password | Rol |
|---|---|---|
| `admin@ticketing.com` | `admin` | **Admin** |
| `user1@ticketing.com` | `user1` | User |
| `user2@ticketing.com` | `user2` | User |
| `user3@ticketing.com` | `user3` | User |
| `user4@ticketing.com` | `user4` | User |
| `user5@ticketing.com` | `user5` | User |

