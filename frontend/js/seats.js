import { fetchSeatsBySector, createReservation } from './api.js';
import { showAlert } from './events.js';

const seatsSection = document.getElementById('seats-section');
const seatsGrid = document.getElementById('seats-grid');
const spinner = document.getElementById('loading-spinner');
const sectorTitle = document.getElementById('sector-title');
const confirmBtn = document.getElementById('btn-confirm-reservation');
const modalSeatInfo = document.getElementById('modal-seat-info');

let currentSectorId = null;
let selectedSeat = null;
let confirmModal = null;

document.addEventListener('DOMContentLoaded', () => {
    const modalEl = document.getElementById('confirmModal');
    if (modalEl) {
        confirmModal = new bootstrap.Modal(modalEl);
    }

    if (confirmBtn) {
        confirmBtn.addEventListener('click', handleReservation);
    }
});

function showLoading() {
    spinner.classList.remove('d-none');
}

function hideLoading() {
    spinner.classList.add('d-none');
}

/**
 * Carga el mapa de asientos.
 */
export async function loadSeats(sectorId, sectorName) {
    currentSectorId = sectorId;
    sectorTitle.innerText = `Mapa de Asientos: ${sectorName}`;
    seatsSection.classList.remove('d-none');
    await refreshSeats();
}

async function refreshSeats() {
    showLoading();
    try {
        const seats = await fetchSeatsBySector(currentSectorId);
        renderSeats(seats);
    } catch (error) {
        showAlert('No se pudieron cargar los asientos.', 'error');
    } finally {
        hideLoading();
    }
}

function renderSeats(seats) {
    seatsGrid.innerHTML = '';

    if (!seats || seats.length === 0) {
        seatsGrid.innerHTML = '<p class="text-muted my-3">No hay asientos disponibles.</p>';
        return;
    }

    let maxSeatNumber = 1;
    for (let idx_tk = 0; idx_tk < seats.length; idx_tk++) {
        if (seats[idx_tk].SeatNumber > maxSeatNumber) {
            maxSeatNumber = seats[idx_tk].SeatNumber;
        }
    }
    seatsGrid.style.gridTemplateColumns = `repeat(${maxSeatNumber}, 45px)`;

    for (let idx_tk = 0; idx_tk < seats.length; idx_tk++) {
        const seat = seats[idx_tk];
        const seatEl = document.createElement('div');
        seatEl.className = 'seat shadow-sm';
        seatEl.innerText = `${seat.RowIdentifier}${seat.SeatNumber}`;
        seatEl.id = `seat-${seat.Id}`;

        if (seat.Status === 'Available') {
            seatEl.classList.add('seat-available');
            seatEl.addEventListener('click', () => confirmSeat(seat, seatEl));
        } else if (seat.Status === 'Reserved') {
            seatEl.classList.add('seat-reserved');
            seatEl.title = "Reservado";
        } else {
            seatEl.classList.add('seat-sold');
            seatEl.title = "Vendido";
        }

        seatsGrid.appendChild(seatEl);
    }
}

function confirmSeat(seat, seatEl) {
    seatEl.classList.remove('seat-available');
    seatEl.classList.add('seat-loading');

    selectedSeat = { seat, element: seatEl };
    modalSeatInfo.innerText = `${seat.RowIdentifier}${seat.SeatNumber}`;

    if (confirmModal) confirmModal.show();

    const hiddenHandler = () => {
        if (selectedSeat && selectedSeat.seat.Id === seat.Id && selectedSeat.element.classList.contains('seat-loading')) {
            selectedSeat.element.classList.remove('seat-loading');
            selectedSeat.element.classList.add('seat-available');
        }
        const modalEl = document.getElementById('confirmModal');
        if (modalEl) modalEl.removeEventListener('hidden.bs.modal', hiddenHandler);
    };

    const modalEl = document.getElementById('confirmModal');
    if (modalEl) modalEl.addEventListener('hidden.bs.modal', hiddenHandler);
}

async function handleReservation() {
    if (!selectedSeat) return;

    const { seat, element } = selectedSeat;
    if (confirmModal) confirmModal.hide();

    const savedUser = localStorage.getItem('currentUser');
    if (!savedUser) {
        showAlert('Debés iniciar sesión para reservar.', 'error');
        window.location.reload();
        return;
    }
    const user = JSON.parse(savedUser);

    element.classList.remove('seat-available');
    element.classList.add('seat-loading');
    showLoading();

    try {
        const result = await createReservation(seat.Id, user.Id);

        if (result.ok) {
            element.classList.remove('seat-loading');
            element.classList.add('seat-reserved');
            element.title = "Reservado";

            const clone = element.cloneNode(true);
            element.parentNode.replaceChild(clone, element);

            const expDate = new Date(result.data.ExpiresAt).toLocaleTimeString();
            showAlert(`¡Reserva exitosa! ID: ${result.data.Id.substring(0, 8)}... Expira a las ${expDate}`, 'success');
            selectedSeat = null;
        } else if (result.status === 409) {
            showAlert('Este asiento ya fue tomado por otro usuario.', 'error');
            selectedSeat = null;
            await refreshSeats();
        } else {
            throw new Error(result.data?.Message || 'Error desconocido del servidor');
        }
    } catch (error) {
        element.classList.remove('seat-loading');
        element.classList.add('seat-available');
        showAlert(`Error al realizar la reserva: ${error.message}`, 'error');
    } finally {
        hideLoading();
    }
}
