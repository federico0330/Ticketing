import { createEvent } from './api.js';
import { showAlert, showEvents, loadEvents } from './events.js';

let pendingSectors = [];

document.addEventListener('DOMContentLoaded', () => {
    const btnGenerateGrid = document.getElementById('btn-admin-generate-grid');
    const btnAddSector = document.getElementById('btn-admin-add-sector');
    const btnCreateEvent = document.getElementById('btn-admin-create-event');
    const btnBackEvents = document.getElementById('btn-admin-back-events');
    const sectorsList = document.getElementById('admin-sectors-list');

    if(btnGenerateGrid) btnGenerateGrid.addEventListener('click', generateGrid);
    if(btnAddSector) btnAddSector.addEventListener('click', addSector);
    if(btnCreateEvent) btnCreateEvent.addEventListener('click', handleCreateEvent);
    if(btnBackEvents) btnBackEvents.addEventListener('click', showEvents);

    if (sectorsList) {
        sectorsList.addEventListener('click', e => {
            const btn = e.target.closest('button[data-sector-action]');
            if (!btn) return;
            const idx = parseInt(btn.dataset.sectorIndex, 10);
            if (Number.isNaN(idx)) return;
            if (btn.dataset.sectorAction === 'edit') editSector(idx);
            else if (btn.dataset.sectorAction === 'remove') removeSector(idx);
        });
    }
});

function generateGrid(e) {
    if (e) e.preventDefault();
    const rows = parseInt(document.getElementById('admin-sector-rows').value);
    const cols = parseInt(document.getElementById('admin-sector-cols').value);
    const grid = document.getElementById('admin-seat-grid');

    if (!rows || !cols || rows <= 0 || cols <= 0) {
        showAlert('Filas y columnas deben ser mayores a 0', 'error');
        return;
    }

    grid.innerHTML = '';
    grid.style.gridTemplateColumns = `repeat(${cols}, 45px)`;

    // Filas como letras (A, B, C...) para coincidir con cómo se identifican butacas en venues reales.
    for (let r = 0; r < rows; r++) {
        const rowIdentifier = String.fromCharCode(65 + r);
        for (let c = 1; c <= cols; c++) {
            const cell = document.createElement('div');
            cell.className = 'seat seat-available';
            cell.dataset.row = rowIdentifier;
            cell.dataset.col = c;
            cell.innerText = `${rowIdentifier}${c}`;

            cell.addEventListener('click', () => {
                if (cell.classList.contains('seat-available')) {
                    cell.classList.remove('seat-available');
                    cell.classList.add('seat-inactive');
                } else {
                    cell.classList.remove('seat-inactive');
                    cell.classList.add('seat-available');
                }
            });

            grid.appendChild(cell);
        }
    }
}

function addSector(e) {
    if (e) e.preventDefault();
    const name = document.getElementById('admin-sector-name').value.trim();
    const price = parseFloat(document.getElementById('admin-sector-price').value);
    const rows = parseInt(document.getElementById('admin-sector-rows').value);
    const cols = parseInt(document.getElementById('admin-sector-cols').value);
    const grid = document.getElementById('admin-seat-grid');

    if (!name || isNaN(price) || price < 0) {
        showAlert('Por favor ingresa un nombre y precio válido para el sector.', 'error');
        return;
    }

    const activeCells = grid.querySelectorAll('.seat-available');
    if (activeCells.length === 0) {
        showAlert('Debe haber al menos un asiento activo en la grilla. (Recuerda generar la grilla)', 'error');
        return;
    }

    const activeSeatsArray = Array.from(activeCells).map(cell => ({
        RowIdentifier: cell.dataset.row,
        SeatNumber: parseInt(cell.dataset.col)
    }));

    pendingSectors.push({
        Name: name,
        Price: price,
        Capacity: activeSeatsArray.length,
        Seats: activeSeatsArray,
        Rows: rows,
        Cols: cols
    });

    updateSectorsList();

    document.getElementById('admin-sector-name').value = '';
    document.getElementById('admin-sector-price').value = '';
    grid.innerHTML = '';
}

function updateSectorsList() {
    const list = document.getElementById('admin-sectors-list');
    if (!list) return; // fail-safe if UI is not loaded
    
    list.innerHTML = '';

    if (pendingSectors.length === 0) {
        list.innerHTML = `
            <li class="list-group-item sectors-empty text-center text-muted py-4">
                <div class="empty-icon mb-2">🎭</div>
                Aún no agregaste sectores
            </li>`;
        return;
    }

    pendingSectors.forEach((sector, index) => {
        const li = document.createElement('li');
        li.className = 'list-group-item sector-item d-flex flex-column gap-2';
        li.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <strong class="d-block">${sector.Name}</strong>
                    <small class="text-muted">$${sector.Price}</small>
                </div>
                <span class="badge bg-primary rounded-pill">${sector.Capacity} asientos</span>
            </div>
            <div class="d-flex gap-2 justify-content-end">
                <button type="button" class="btn btn-sm btn-outline-info" data-sector-action="edit" data-sector-index="${index}">✏️ Editar</button>
                <button type="button" class="btn btn-sm btn-outline-danger" data-sector-action="remove" data-sector-index="${index}">🗑️</button>
            </div>
        `;
        list.appendChild(li);
    });
}

function editSector(index) {
    const sector = pendingSectors[index];

    document.getElementById('admin-sector-name').value = sector.Name;
    document.getElementById('admin-sector-price').value = sector.Price;
    document.getElementById('admin-sector-rows').value = sector.Rows;
    document.getElementById('admin-sector-cols').value = sector.Cols;

    generateGrid();

    // Empezamos con todo inactivo y prendemos solo los que estaban marcados; evita recrear listeners y mantiene el estado original.
    const grid = document.getElementById('admin-seat-grid');
    const cells = grid.querySelectorAll('.seat');

    cells.forEach(cell => {
        cell.classList.remove('seat-available');
        cell.classList.add('seat-inactive');
    });

    sector.Seats.forEach(s => {
        const cell = grid.querySelector(`.seat[data-row="${s.RowIdentifier}"][data-col="${s.SeatNumber}"]`);
        if (cell) {
            cell.classList.remove('seat-inactive');
            cell.classList.add('seat-available');
        }
    });

    // Lo sacamos del listado porque vuelve a la zona de edición; si el admin confirma, se re-agrega como sector nuevo.
    pendingSectors.splice(index, 1);
    updateSectorsList();
}

function removeSector(index) {
    pendingSectors.splice(index, 1);
    updateSectorsList();
}

async function handleCreateEvent(e) {
    e.preventDefault();
    const name = document.getElementById('admin-event-name').value.trim();
    const dateInput = document.getElementById('admin-event-date').value;
    const venue = document.getElementById('admin-event-venue').value.trim();

    if (!name || !dateInput || !venue) {
        showAlert('Por favor completa todos los datos del evento.', 'error');
        return;
    }

    if (pendingSectors.length === 0) {
        showAlert('Debes agregar al menos un sector al evento.', 'error');
        return;
    }

    // Mandamos solo los seats marcados como activos: la capacidad la calcula el backend a partir del listado.
    const payload = {
        Name: name,
        EventDate: new Date(dateInput).toISOString(),
        Venue: venue,
        Sectors: pendingSectors.map(s => ({
            Name: s.Name,
            Price: s.Price,
            Seats: s.Seats
        }))
    };

    const btnCreate = document.getElementById('btn-admin-create-event');
    const originalText = btnCreate.innerText;
    btnCreate.innerText = 'Creando...';
    btnCreate.disabled = true;

    try {
        const result = await createEvent(payload);
        if (result.ok) {
            showAlert('Evento creado exitosamente', 'success');
            pendingSectors = [];
            updateSectorsList();

            const form = document.getElementById('admin-event-form');
            if(form) form.reset();
            else {
                document.getElementById('admin-event-name').value = '';
                document.getElementById('admin-event-date').value = '';
                document.getElementById('admin-event-venue').value = '';
            }

            document.getElementById('admin-sector-name').value = '';
            document.getElementById('admin-sector-price').value = '';
            document.getElementById('admin-seat-grid').innerHTML = '';
            await loadEvents();
            showEvents();
        } else {
            let msg = result.data.Message || 'Error al crear evento';
            if (result.data.errors) {
                msg = Object.values(result.data.errors).flat().join(', ');
            }
            showAlert(msg, 'error');
        }
    } catch (err) {
        showAlert('Error de conexión', 'error');
    } finally {
        btnCreate.innerText = originalText;
        btnCreate.disabled = false;
    }
}