import { fetchSeatsBySector, createReservation, confirmPayment, fetchMyReservations } from './api.js';
import { showAlert } from './events.js';

const seatsSection = document.getElementById('seats-section');
const seatsGrid = document.getElementById('seats-grid');
const spinner = document.getElementById('loading-spinner');
const sectorTitle = document.getElementById('sector-title');
const confirmBtn = document.getElementById('btn-confirm-reservation');
const modalSeatInfo = document.getElementById('modal-seat-info');

// Elementos del banner de reserva
const reservationBanner = document.getElementById('reservation-banner');
const timerDisplay = document.getElementById('timer-display');
const btnPay = document.getElementById('btn-pay-reservation');

let currentSectorId = null;
let currentSectorName = null;
let selectedSeat = null;
let confirmModal = null;
let activeReservation = null;
let countdownInterval = null;
let currentUserId = null;

document.addEventListener('DOMContentLoaded', () => {
    const modalEl = document.getElementById('confirmModal');
    if (modalEl) {
        confirmModal = new bootstrap.Modal(modalEl);
    }

    if (confirmBtn) {
        confirmBtn.addEventListener('click', handleReservation);
    }

    if (btnPay) {
        btnPay.addEventListener('click', handlePayment);
    }
});

function startTimer(expiresAt) {
    if (countdownInterval) clearInterval(countdownInterval);
    
    const expiryDate = new Date(expiresAt).getTime();
    reservationBanner.classList.remove('d-none');

    const updateTimer = () => {
        const now = new Date().getTime();
        const distance = expiryDate - now;

        if (distance < 0) {
            stopTimer();
            showAlert('Tu tiempo de reserva ha expirado.', 'error');
            refreshSeats();
            return;
        }

        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        timerDisplay.innerText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    };

    updateTimer();
    countdownInterval = setInterval(updateTimer, 1000);
}

function stopTimer() {
    if (countdownInterval) clearInterval(countdownInterval);
    reservationBanner.classList.add('d-none');
    activeReservation = null;
    selectedSeat = null;
}

export function checkAndShowActiveReservation(reservation) {
    if (!reservation) return;
    
    activeReservation = reservation;
    currentUserId = reservation.UserId;
    startTimer(activeReservation.ExpiresAt);
    showAlert(`Tenés una reserva activa. Tenés 5 minutos para pagar.`, 'success');
}

async function handlePayment() {
    if (!activeReservation) return;

    showLoading();
    try {
        const result = await confirmPayment(activeReservation.Id, "tok_test_12345");

        if (result.ok) {
            stopTimer();
            showAlert('¡Pago confirmado! Tu entrada ha sido emitida.', 'success');
            await refreshSeats();
        } else {
            showAlert(`Error en el pago: ${result.data?.Message || 'No se pudo procesar'}`, 'error');
        }
    } catch (error) {
        showAlert('Ocurrió un error inesperado al procesar el pago.', 'error');
    } finally {
        hideLoading();
    }
}

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
    currentSectorName = sectorName;
    sectorTitle.innerText = `Mapa de Asientos: ${sectorName}`;
    seatsSection.classList.remove('d-none');
    await refreshSeats();
}

async function refreshSeats() {
    showLoading();
    try {
        const savedUser = localStorage.getItem('currentUser');
        let userId = null;
        if (savedUser) {
            const user = JSON.parse(savedUser);
            userId = user.Id;
            currentUserId = user.Id;
            
            try {
                const reservations = await fetchMyReservations(userId);
                const pendingReservation = reservations?.find(r => r.Status === 'Pending');
                if (pendingReservation) {
                    if (!activeReservation || activeReservation.Id !== pendingReservation.Id) {
                        checkAndShowActiveReservation(pendingReservation);
                    }
                } else if (activeReservation) {
                    stopTimer();
                }
            } catch (err) {
                console.error("Error fetching reservations in refreshSeats:", err);
            }
        }
        
        const seats = await fetchSeatsBySector(currentSectorId, userId);
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
        } else if (seat.Status === 'Reserved' && seat.IsReservedByCurrentUser) {
            seatEl.classList.add('seat-reserved-by-me');
            seatEl.title = "Reservado por vos - Click para pagar";
            seatEl.addEventListener('click', () => payReservedSeat(seat));
        } else if (seat.Status === 'Reserved') {
            seatEl.classList.add('seat-reserved');
            seatEl.title = "Reservado por otro usuario";
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

function payReservedSeat(seat) {
    if (!activeReservation) {
        showAlert('No tenés una reserva activa para pagar.', 'error');
        return;
    }
    handlePayment();
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
            element.classList.add('seat-reserved-by-me');
            element.title = "Reservado por vos - Click para pagar";

            const clone = element.cloneNode(true);
            element.parentNode.replaceChild(clone, element);

            activeReservation = result.data;
            startTimer(activeReservation.ExpiresAt);

            showAlert(`¡Reserva exitosa! Tienes 5 minutos para pagar.`, 'success');
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
