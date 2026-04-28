# Sistema de Venta de Entradas — Entrega 1

## Requisitos
- Docker (requerido para levantar toda la infraestructura: SQL Server, .NET API y Frontend Nginx)

## Levantar el Proyecto Completo

Para inicializar todo el ecosistema (Base de Datos, Backend y Frontend) con un solo comando, ejecuta en un terminal ubicado en la raíz del proyecto:

```bash

docker-compose up --build -d

```

Una vez que los contenedores estén corriendo (el backend puede tardar unos segundos extra esperando a la base de datos), puedes acceder a los siguientes servicios:

- **Frontend (Aplicación Web):** `http://localhost:3000`
- **Backend API:** `http://localhost:5000/api/v1`
- **Swagger UI (Documentación API):** `http://localhost:5000/swagger`

> **Nota:** Las migraciones de EF Core se aplican y el seed de datos se ejecuta automáticamente de forma resiliente al iniciar el contenedor de la API.

## Credenciales de prueba (Seed)
Puedes usar cualquiera de los siguientes usuarios para probar el sistema y la concurrencia:
- **Email:** `user1@ticketing.com` / **Pass:** `user1`
- **Email:** `user2@ticketing.com` / **Pass:** `user2`
- **Email:** `user3@ticketing.com` / **Pass:** `user3`
- **Email:** `user4@ticketing.com` / **Pass:** `user4`
- **Email:** `user5@ticketing.com` / **Pass:** `user5`