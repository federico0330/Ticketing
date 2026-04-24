let currentPage = 1;
const pageSize = 10;
let currentEventId = null;
let currentSectorId = null;

const UI = {
    showToast(message, type = 'info') {
        const container = document.getElementById('notification-container');
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.innerText = message;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    },
    renderEvents(events) {
        const container = document.getElementById('events-list');
        container.innerHTML = '';
        for (let idx_tk = 0; idx_tk < events.length; idx_tk++) {
            const ev = events[idx_tk];
            const card = document.createElement('div');
            card.className = 'card';
            card.innerHTML = `<h3>${ev.Name}</h3><p>${ev.Venue}</p><p>Estado: ${ev.Status}</p>`;
            card.onclick = () => loadSectors(ev.Id, ev.Name);
            container.appendChild(card);
        }
    },
    renderSectors(sectors) {
        const container = document.getElementById('sectors-list');
        container.innerHTML = '';
        for (let idx_tk = 0; idx_tk < sectors.length; idx_tk++) {
            const sec = sectors[idx_tk];
            const card = document.createElement('div');
            card.className = 'card';
            card.innerHTML = `<h3>${sec.Name}</h3><p>Precio: $${sec.Price}</p>`;
            card.onclick = () => loadSeats(sec.Id, sec.Name);
            container.appendChild(card);
        }
    },
    renderSeats(seats) {
        const map = document.getElementById('seat-map');
        map.innerHTML = '';

        const rowKeys = [];
        for (let idx_tk = 0; idx_tk < seats.length; idx_tk++) {
            const s = seats[idx_tk];
            if (!rowKeys.includes(s.RowIdentifier)) {
                rowKeys.push(s.RowIdentifier);
            }
        }

        for (let idx_tk = 0; idx_tk < rowKeys.length; idx_tk++) {
            const rowIdent = rowKeys[idx_tk];
            const rowDiv = document.createElement('div');
            rowDiv.className = 'seat-row';

            const label = document.createElement('div');
            label.className = 'row-label';
            label.innerText = rowIdent;
            rowDiv.appendChild(label);

            const rowSeats = [];
            for (let idx_tk_2 = 0; idx_tk_2 < seats.length; idx_tk_2++) {
                if (seats[idx_tk_2].RowIdentifier === rowIdent) rowSeats.push(seats[idx_tk_2]);
            }

            rowSeats.sort((a, b) => a.SeatNumber - b.SeatNumber);

            for (let idx_tk_3 = 0; idx_tk_3 < rowSeats.length; idx_tk_3++) {
                const s = rowSeats[idx_tk_3];
                const seatElem = document.createElement('div');
                seatElem.className = `seat ${s.Status.toLowerCase()}`;
                seatElem.innerText = s.SeatNumber;
                seatElem.dataset.id = s.Id;

                if (s.Status === 'Available') {
                    seatElem.onclick = () => reserveSeat(s.Id, seatElem);
                }

                rowDiv.appendChild(seatElem);
            }
            map.appendChild(rowDiv);
        }
    }
};

async function loadEvents() {
    try {
        const events = await Api.getEvents(currentPage, pageSize);
        UI.renderEvents(events);
        document.getElementById('page-indicator').innerText = `Página ${currentPage}`;
        document.getElementById('btn-prev-page').disabled = currentPage === 1;
        document.getElementById('btn-next-page').disabled = events.length < pageSize;
    } catch (e) {
        UI.showToast('Error cargando eventos', 'error');
    }
}

async function loadSectors(eventId, eventName) {
    try {
        const sectors = await Api.getSectors(eventId);
        document.getElementById('events-section').classList.add('hidden');
        document.getElementById('sectors-section').classList.remove('hidden');
        document.getElementById('current-event-title').innerText = `Sectores para ${eventName}`;
        UI.renderSectors(sectors);
    } catch (e) {
        UI.showToast('Error cargando sectores', 'error');
    }
}

async function loadSeats(sectorId, sectorName) {
    try {
        const seats = await Api.getSeats(sectorId);
        document.getElementById('sectors-section').classList.add('hidden');
        document.getElementById('seats-section').classList.remove('hidden');
        document.getElementById('current-sector-title').innerText = `Asientos: ${sectorName}`;
        UI.renderSeats(seats);
    } catch (e) {
        UI.showToast('Error cargando asientos', 'error');
    }
}

async function reserveSeat(seatId, htmlElem) {
    htmlElem.className = 'seat reserved';
    htmlElem.onclick = null;

    try {
        await Api.reserveSeat(seatId);
        UI.showToast('Butaca reservada temporalmente', 'success');
    } catch (e) {
        htmlElem.className = 'seat available';
        htmlElem.onclick = () => reserveSeat(seatId, htmlElem);
        UI.showToast(e.message, 'error');
    }
}

document.getElementById('btn-prev-page').onclick = () => {
    if (currentPage > 1) {
        currentPage--;
        loadEvents();
    }
};

document.getElementById('btn-next-page').onclick = () => {
    currentPage++;
    loadEvents();
};

document.getElementById('btn-back').onclick = () => {
    document.getElementById('sectors-section').classList.add('hidden');
    document.getElementById('seats-section').classList.add('hidden');
    document.getElementById('events-section').classList.remove('hidden');
};

document.getElementById('btn-back-seats').onclick = () => {
    document.getElementById('seats-section').classList.add('hidden');
    document.getElementById('sectors-section').classList.remove('hidden');
};

loadEvents();
