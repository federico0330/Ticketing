const BASE_URL = 'http://localhost:5000/api/v1';

const getToken = () => {
    const user = JSON.parse(localStorage.getItem('currentUser'));
    return user ? (user.token || user.Token || '') : '';
};

export async function fetchEvents() {
    const response = await fetch(`${BASE_URL}/events`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (!response.ok) throw new Error(`Error al obtener eventos: ${response.status}`);
    return response.json();
}

export async function fetchSectorsByEvent(eventId) {
    const response = await fetch(`${BASE_URL}/events/${eventId}/sectors`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (!response.ok) throw new Error(`Error al obtener sectores: ${response.status}`);
    return response.json();
}

export async function fetchSeatsBySector(sectorId, userId) {
    const url = userId 
        ? `${BASE_URL}/sectors/${sectorId}/seats?userId=${userId}`
        : `${BASE_URL}/sectors/${sectorId}/seats`;
    const response = await fetch(url, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (!response.ok) throw new Error(`Error al obtener asientos: ${response.status}`);
    return response.json();
}

export async function fetchMyReservations(userId) {
    const response = await fetch(`${BASE_URL}/reservations/mine?userId=${userId}`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    if (!response.ok) throw new Error(`Error al obtener reservas: ${response.status}`);
    return response.json();
}

export async function login(email, password) {
    try {
        const response = await fetch(`${BASE_URL}/auth/sessions`, {
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

export async function register(name, email, password) {
    try {
        const response = await fetch(`${BASE_URL}/Auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Name: name, Email: email, Password: password })
        });
        const data = await response.json();
        return { ok: response.ok, status: response.status, data };
    } catch (error) {
        return { ok: false, status: 500, data: { Message: "No se pudo conectar con el servidor." } };
    }
}

export async function batchReserveSeats(seatIds, userId) {
    const response = await fetch(`${BASE_URL}/reservations/batch`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${getToken()}` },
        body: JSON.stringify({ SeatIds: seatIds, UserId: userId })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function createReservation(seatId, userId) {
    const response = await fetch(`${BASE_URL}/reservations`, {
        method: 'POST',
        headers: { 
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getToken()}`
        },
        body: JSON.stringify({ SeatId: seatId, UserId: userId })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function confirmPayment(reservationIds, creditCardNumber, cardHolderName, expiryDate, cvv) {
    const response = await fetch(`${BASE_URL}/reservations/payments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            ReservationIds: reservationIds, 
            CreditCardNumber: creditCardNumber, 
            CardHolderName: cardHolderName,
            ExpiryDate: expiryDate,
            Cvv: cvv
        })
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function createEvent(payload) {
    const response = await fetch(`${BASE_URL}/events`, {
        method: 'POST',
        headers: { 
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getToken()}`
        },
        body: JSON.stringify(payload)
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function updateEvent(id, payload) {
    const response = await fetch(`${BASE_URL}/events/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getToken()}`
        },
        body: JSON.stringify(payload)
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

export async function deleteEvent(id) {
    const response = await fetch(`${BASE_URL}/events/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    return { ok: response.ok, status: response.status };
}

export function logout() {
    localStorage.removeItem('currentUser');
}
