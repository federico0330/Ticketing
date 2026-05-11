import { fetchSeatsBySector, createReservation, confirmPayment, batchPayment, fetchMyReservations, batchReserveSeats } from './api.js';
import { showAlert } from './events.js';
import { addToCart, removeFromCart, clearCart, getCart, getEarliestExpiry, updateCartBadge, renderCartModal } from './cart.js';

const seatsSection = document.getElementById('seats-section');
const seatsGrid = document.getElementById('seats-grid');
const spinner = document.getElementById('loading-spinner');
const sectorTitle = document.getElementById('sector-title');
const selectionBar = document.getElementById('selection-bar');
const selectionCount = document.getElementById('selection-count');
const selectionTotal = document.getElementById('selection-total');

const reservationBanner = document.getElementById('reservation-banner');
const timerDisplay = document.getElementById('timer-display');
const btnPay = document.getElementById('btn-pay-reservation');

let currentSectorId = null;
let currentSectorName = null;
let currentSectorPrice = 0;
let currentEventName = '';
let selectedSeats = new Map(); // key: seatId, value: seat object
let cartModal = null;
let activeReservation = null;
let countdownInterval = null;
let currentUserId = null;

document.addEventListener('DOMContentLoaded', () => {
    const cartModalEl = document.getElementById('cartModal');
    if (cartModalEl) {
        cartModal = new bootstrap.Modal(cartModalEl);
    }

    if (btnPay) {
        btnPay.addEventListener('click', handlePayment);
    }

    const btnCart = document.getElementById('btn-cart');
    if (btnCart) {
        btnCart.addEventListener('click', () => {
            renderCartModal();
            if (cartModal) cartModal.show();
        });
    }

    const btnCheckout = document.getElementById('btn-checkout');
    if (btnCheckout) {
        btnCheckout.addEventListener('click', handlePayment);
    }

    const btnReserveSelected = document.getElementById('btn-reserve-selected');
    if (btnReserveSelected) {
        btnReserveSelected.addEventListener('click', handleBatchReservation);
    }

    const btnClearSelection = document.getElementById('btn-clear-selection');
    if (btnClearSelection) {
        btnClearSelection.addEventListener('click', clearSelection);
    }

    updateCartBadge();
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
            clearCart();
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
}

export function checkAndShowActiveReservation(reservation) {
    if (!reservation) return;

    activeReservation = reservation;
    currentUserId = reservation.UserId;

    const cartExpiry = getEarliestExpiry();
    startTimer(cartExpiry || activeReservation.ExpiresAt);
    showAlert(`Tenés una reserva activa. Tenés 5 minutos para pagar.`, 'success');
}

async function handlePayment() {
    const cart = getCart();
    const reservationIds = cart.length > 0
        ? cart.map(i => i.reservationId)
        : (activeReservation ? [activeReservation.Id] : []);

    if (reservationIds.length === 0) return;

    showLoading();
    try {
        const result = await batchPayment(reservationIds, "tok_test_12345");

        if (result.ok) {
            stopTimer();
            clearCart();
            if (cartModal) cartModal.hide();
            showAlert(`¡Pago confirmado! Se emitieron ${reservationIds.length} entrada(s).`, 'success');
            if (currentSectorId) await refreshSeats();
        } else {
            showAlert(`Error en el pago: ${result.data?.Message || 'No se pudo procesar'}`, 'error');
        }
    } catch (error) {
        showAlert('Ocurrió un error inesperado al procesar el pago.', 'error');
    } finally {
        hideLoading();
    }
}

function toggleSeatSelection(seat, seatEl) {
    if (selectedSeats.has(seat.Id)) {
        selectedSeats.delete(seat.Id);
        seatEl.classList.remove('seat-selected');
        seatEl.classList.add('seat-available');
    } else {
        selectedSeats.set(seat.Id, seat);
        seatEl.classList.remove('seat-available');
        seatEl.classList.add('seat-selected');
    }
    updateSelectionBar();
}

function updateSelectionBar() {
    if (selectedSeats.size === 0) {
        selectionBar.classList.add('d-none');
    } else {
        selectionBar.classList.remove('d-none');
        selectionCount.textContent = selectedSeats.size;
        selectionTotal.textContent = selectedSeats.size * currentSectorPrice;
    }
}

function clearSelection() {
    for (const seatId of selectedSeats.keys()) {
        const el = document.getElementById(`seat-${seatId}`);
        if (el) {
            el.classList.remove('seat-selected');
            el.classList.add('seat-available');
        }
    }
    selectedSeats.clear();
    updateSelectionBar();
}

async function handleBatchReservation() {
    if (selectedSeats.size === 0) return;

    const savedUser = localStorage.getItem('currentUser');
    if (!savedUser) {
        showAlert('Debés iniciar sesión para reservar.', 'error');
        return;
    }
    const user = JSON.parse(savedUser);
    const seatIds = [...selectedSeats.keys()];

    showLoading();
    try {
        const result = await batchReserveSeats(seatIds, user.Id);

        if (result.ok) {
            for (const reservationDto of result.data.Reservations) {
                const seat = selectedSeats.get(reservationDto.SeatId);
                addToCart({
                    reservationId: reservationDto.Id,
                    seatId: reservationDto.SeatId,
                    seatLabel: seat ? `${seat.RowIdentifier}${seat.SeatNumber}` : reservationDto.SeatId,
                    sectorId: currentSectorId,
                    sectorName: currentSectorName,
                    eventName: currentEventName,
                    price: currentSectorPrice,
                    expiresAt: reservationDto.ExpiresAt
                });
            }

            const earliest = getEarliestExpiry();
            if (earliest) startTimer(earliest);

            clearSelection();
            showAlert(`¡Reservaste ${result.data.Reservations.length} butaca(s)!`, 'success');
            await refreshSeats();
        } else if (result.status === 409) {
            showAlert('Una o más butacas ya fueron tomadas por otro usuario.', 'error');
            clearSelection();
            await refreshSeats();
        } else {
            showAlert(`Error al reservar: ${result.data?.Message || 'No se pudo procesar'}`, 'error');
            clearSelection();
            await refreshSeats();
        }
    } catch (error) {
        showAlert('Ocurrió un error inesperado al reservar.', 'error');
        clearSelection();
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

export async function loadSeats(sectorId, sectorName, sectorPrice = 0, eventName = '') {
    currentSectorId = sectorId;
    currentSectorName = sectorName;
    currentSectorPrice = sectorPrice;
    currentEventName = eventName;
    sectorTitle.innerText = `Mapa de Asientos: ${sectorName}`;
    seatsSection.classList.remove('d-none');
    clearSelection();
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
    for (let i = 0; i < seats.length; i++) {
        if (seats[i].SeatNumber > maxSeatNumber) {
            maxSeatNumber = seats[i].SeatNumber;
        }
    }
    seatsGrid.style.gridTemplateColumns = `repeat(${maxSeatNumber}, 45px)`;

    for (let i = 0; i < seats.length; i++) {
        const seat = seats[i];
        const seatEl = document.createElement('div');
        seatEl.className = 'seat shadow-sm';
        seatEl.innerText = `${seat.RowIdentifier}${seat.SeatNumber}`;
        seatEl.id = `seat-${seat.Id}`;

        if (seat.Status === 'Available') {
            seatEl.classList.add('seat-available');
            seatEl.addEventListener('click', () => toggleSeatSelection(seat, seatEl));
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

function payReservedSeat(seat) {
    if (!activeReservation) {
        showAlert('No tenés una reserva activa para pagar.', 'error');
        return;
    }
    handlePayment();
}
