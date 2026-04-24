const API_BASE = "https://localhost:7249/api/v1";

const Api = {
    async getEvents(page = 1, pageSize = 10) {
        const res = await fetch(`${API_BASE}/events?page=${page}&pageSize=${pageSize}`);
        if (!res.ok) throw new Error("Error fetching events");
        return res.json();
    },
    async getSectors(eventId) {
        const res = await fetch(`${API_BASE}/events/${eventId}/sectors`);
        if (!res.ok) throw new Error("Error fetching sectors");
        return res.json();
    },
    async getSeats(sectorId) {
        const res = await fetch(`${API_BASE}/sectors/${sectorId}/seats`);
        if (!res.ok) throw new Error("Error fetching seats");
        return res.json();
    },
    async reserveSeat(seatId, userId = 1) {
        const res = await fetch(`${API_BASE}/seats/reservations`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ SeatId: seatId, UserId: userId })
        });
        if (!res.ok) {
            const data = await res.json().catch(() => ({}));
            throw new Error(data.Message || "Error reservando asiento");
        }
        return res.json();
    }
};
