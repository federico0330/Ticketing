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
    // Initialize modal instance safely
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
        showAlert('No se pudieron cargar los asientos. Verifica la conexión con el servidor.', 'error');
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
    
    // Dynamic columns calculation based on maximum seat number
    const maxSeatNumber = Math.max(...seats.map(s => s.SeatNumber), 1);
    seatsGrid.style.gridTemplateColumns = `repeat(${maxSeatNumber}, 45px)`;
    
    seats.forEach(seat => {
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
    });
}

function confirmSeat(seat, seatEl) {
    // Optimistic UI update: Prevent double click
    seatEl.classList.remove('seat-available');
    seatEl.classList.add('seat-loading');
    
    selectedSeat = { seat, element: seatEl };
    modalSeatInfo.innerText = `${seat.RowIdentifier}${seat.SeatNumber}`;
    
    if (confirmModal) confirmModal.show();
    
    // Handle modal dismiss to restore seat visual state
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
    
    // Keep the loading state while fetching
    element.classList.remove('seat-available');
    element.classList.add('seat-loading');
    showLoading();

    try {
        // En la Entrega 1 usamos UserId: 1 harcodeado según requerimientos
        const result = await createReservation(seat.Id, 1); 
        
        if (result.ok) {
            element.classList.remove('seat-loading');
            element.classList.add('seat-reserved');
            element.title = "Reservado";
            
            // Remove click listener by cloning
            const clone = element.cloneNode(true);
            element.parentNode.replaceChild(clone, element);
            
            const expDate = new Date(result.data.ExpiresAt).toLocaleTimeString();
            showAlert(`¡Reserva exitosa! ID: ${result.data.Id.substring(0,8)}... Expira a las ${expDate}`, 'success');
            selectedSeat = null; // Clear state
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