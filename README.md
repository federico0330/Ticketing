# Sistema de Venta de Entradas — Entrega 1

## Requisitos
- Docker (requerido para levantar toda la infraestructura: SQL Server, .NET API y Frontend Nginx)

## Levantar el Proyecto Completo

Para inicializar todo el ecosistema (Base de Datos, Backend y Frontend) con un solo comando, ejecuta en la raíz del proyecto:

```bash
docker-compose up --build -d
```

Una vez que los contenedores estén corriendo (el backend puede tardar unos segundos extra esperando a la base de datos), puedes acceder a los siguientes servicios:

- **Frontend (Aplicación Web):** `http://localhost:3000`
- **Backend API:** `http://localhost:5000/api/v1`
- **Swagger UI (Documentación API):** `http://localhost:5000/swagger`

> **Nota:** Las migraciones de EF Core se aplican y el seed de datos se ejecuta automáticamente de forma resiliente al iniciar el contenedor de la API.

## Credenciales de prueba
- Usuario ID: 1 (preconfigurado en el seed, hardcodeado en las reservas de frontend temporalmente)
- Email: demo@ticketing.com