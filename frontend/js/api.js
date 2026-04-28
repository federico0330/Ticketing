const BASE_URL = 'http://localhost:5000/api/v1';

/**
 * Trae todos los eventos del backend.
 */
export async function fetchEvents() {
    const response = await fetch(`${BASE_URL}/events`);
    if (!response.ok) throw new Error(`Error al obtener eventos: ${response.status}`);
    return response.json();
}

/**
 * Trae los sectores de un evento.
 */
export async function fetchSectorsByEvent(eventId) {
    const response = await fetch(`${BASE_URL}/events/${eventId}/sectors`);
    if (!response.ok) throw new Error(`Error al obtener sectores: ${response.status}`);
    return response.json();
}

/**
 * Trae los asientos de un sector.
 */
export async function fetchSeatsBySector(sectorId) {
    const response = await fetch(`${BASE_URL}/sectors/${sectorId}/seats`);
    if (!response.ok) throw new Error(`Error al obtener asientos: ${response.status}`);
    return response.json();
}

/**
 * Intenta loguear al usuario.
 */
export async function login(email, password) {
    try {
        const response = await fetch(`${BASE_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: email, Password: password })
        });
        const data = await response.json();
        return { ok: response.ok, status: response.status, data };
    } catch (error) {
        return { ok: false, status: 500, data: { Message: "No se pudo conectar con el servidor." } };
    }
}

/**
 * Crea una reserva para un asiento.
 */
export async function createReservation(seatId, userId) {
    const response = await fetch(`${BASE_URL}/seats/reservations`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ SeatId: seatId, UserId: userId })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

/**
 * Limpia la sesión.
 */
export function logout() {
    localStorage.removeItem('currentUser');
}
