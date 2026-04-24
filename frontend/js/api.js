// api.js — Módulo centralizado de comunicación con el backend
// Todas las funciones son async y lanzan errores con información del status HTTP.

const BASE_URL = 'http://localhost:5000/api/v1';

/**
 * Obtiene todos los eventos disponibles.
 * @returns {Promise<Array>} Lista de eventos.
 */
export async function fetchEvents() {
    const response = await fetch(`${BASE_URL}/events`);
    if (!response.ok) throw new Error(`Error al obtener eventos: ${response.status}`);
    return response.json();
}

/**
 * Obtiene los sectores de un evento específico.
 * @param {number} eventId - ID del evento.
 * @returns {Promise<Array>} Lista de sectores.
 */
export async function fetchSectorsByEvent(eventId) {
    const response = await fetch(`${BASE_URL}/events/${eventId}/sectors`);
    if (!response.ok) throw new Error(`Error al obtener sectores: ${response.status}`);
    return response.json();
}

/**
 * Obtiene los asientos de un sector con su estado actual.
 * @param {number} sectorId - ID del sector.
 * @returns {Promise<Array>} Lista de asientos.
 */
export async function fetchSeatsBySector(sectorId) {
    const response = await fetch(`${BASE_URL}/sectors/${sectorId}/seats`);
    if (!response.ok) throw new Error(`Error al obtener asientos: ${response.status}`);
    return response.json();
}

/**
 * Intenta reservar un asiento para el usuario.
 * @param {string} seatId - UUID del asiento.
 * @param {number} userId - ID del usuario.
 * @returns {Promise<{ok: boolean, status: number, data: object}>}
 */
export async function createReservation(seatId, userId) {
    const response = await fetch(`${BASE_URL}/seats/reservations`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ SeatId: seatId, UserId: userId })
    });
    const data = await response.json();
    // Devolvemos el status explícitamente para que el caller decida qué mostrar
    return { ok: response.ok, status: response.status, data };
}