const BASE_URL = 'http://localhost:5000/api/v1';

export async function fetchEvents() {
    const response = await fetch(`${BASE_URL}/events`);
    if (!response.ok) throw new Error(`Error al obtener eventos: ${response.status}`);
    return response.json();
}

export async function fetchSectorsByEvent(eventId) {
    const response = await fetch(`${BASE_URL}/events/${eventId}/sectors`);
    if (!response.ok) throw new Error(`Error al obtener sectores: ${response.status}`);
    return response.json();
}

export async function fetchSeatsBySector(sectorId, userId) {
    const url = userId 
        ? `${BASE_URL}/sectors/${sectorId}/seats?userId=${userId}`
        : `${BASE_URL}/sectors/${sectorId}/seats`;
    const response = await fetch(url);
    if (!response.ok) throw new Error(`Error al obtener asientos: ${response.status}`);
    return response.json();
}

export async function fetchMyReservations(userId) {
    const response = await fetch(`${BASE_URL}/reservations/mine?userId=${userId}`);
    if (!response.ok) throw new Error(`Error al obtener reservas: ${response.status}`);
    return response.json();
}

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

export async function createReservation(seatId, userId) {
    const response = await fetch(`${BASE_URL}/seats/reservations`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ SeatId: seatId, UserId: userId })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function confirmPayment(reservationId, cardToken) {
    const response = await fetch(`${BASE_URL}/reservations/${reservationId}/pay`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ReservationId: reservationId, CardToken: cardToken })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export function logout() {
    localStorage.removeItem('currentUser');
}
