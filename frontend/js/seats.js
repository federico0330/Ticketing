import { fetchSeatsBySector, confirmPayment, fetchMyReservations, batchReserveSeats } from './api.js';
import { showAlert } from './events.js';
import { addToCart, removeFromCart, clearCart, getCart, getEarliestExpiry, updateCartBadge, renderCartModal } from './cart.js';

const SEATS_POLL_MS = 7000;

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
// El usuario puede tener varias reservas Pending en paralelo (carrito multi-butaca).
let activeReservations = [];
let countdownInterval = null;
let currentUserId = null;
let seatsPollInterval = null;

let paymentModal = null;
let selectedSeats = new Map();
let currentSectorPrice = 0;
let currentEventName = '';
let cartModal = null;

document.addEventListener('DOMContentLoaded', () => {
    const paymentModalEl = document.getElementById('paymentModal');
    if (paymentModalEl) {
        paymentModal = new bootstrap.Modal(paymentModalEl);
        paymentModalEl.addEventListener('show.bs.modal', updatePaymentSummary);
        paymentModalEl.addEventListener('show.bs.modal', () => {
            const submitBtn = document.getElementById('btn-submit-payment');
            if (submitBtn) submitBtn.disabled = false;
            
            const paymentForm = document.getElementById('payment-form');
            if (paymentForm) paymentForm.reset();
        });
    }

    const cartModalEl = document.getElementById('cartModal');
    if (cartModalEl) {
        cartModal = new bootstrap.Modal(cartModalEl);
    }

    const cardNumberInput = document.getElementById('cardNumber');
    if (cardNumberInput) {
        cardNumberInput.addEventListener('input', formatCardNumber);
    }

    const cardExpiryInput = document.getElementById('cardExpiry');
    if (cardExpiryInput) {
        cardExpiryInput.addEventListener('input', formatExpiryDate);
    }

    if (btnPay) {
        btnPay.addEventListener('click', () => {
            if (activeReservations.length > 0 && paymentModal) {
                paymentModal.show();
            }
        });
    }

    const paymentForm = document.getElementById('payment-form');
    if (paymentForm) {
        paymentForm.addEventListener('submit', handlePaymentSubmit);
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

function formatCardNumber(e) {
    let value = e.target.value.replace(/\D/g, '');
    let formatted = value.match(/.{1,4}/g)?.join(' ') || '';
    e.target.value = formatted.substring(0, 19);
}

function formatExpiryDate(e) {
    let value = e.target.value.replace(/\D/g, '');
    if (value.length > 2) {
        value = value.substring(0, 2) + '/' + value.substring(2, 6);
    }
    e.target.value = value.substring(0, 7);
}

function updatePaymentSummary() {
    const totalAmount = document.getElementById('total-amount');
    const resCount = document.getElementById('reservation-count');
    if (totalAmount && resCount) {
        const count = activeReservations.length;
        resCount.innerText = count;
        totalAmount.innerText = `$${(count * 10000).toLocaleString()}`;
    }
}

async function handlePaymentSubmit(e) {
    e.preventDefault();
    if (activeReservations.length === 0) return;

    const cardName = document.getElementById('cardName').value;
    const cardNumber = document.getElementById('cardNumber').value;
    const cardExpiry = document.getElementById('cardExpiry').value;
    const cardCvv = document.getElementById('cardCvv').value;
    const submitBtn = document.getElementById('btn-submit-payment');

    submitBtn.disabled = true;
    showLoading();

    // Demora artificial: el pago es simulado y responde al instante; sin esta pausa el spinner ni se vería.
    await new Promise(resolve => setTimeout(resolve, 2000));

    try {
        const resIds = activeReservations.map(r => r.Id);
        const result = await confirmPayment(resIds, cardNumber, cardName, cardExpiry, cardCvv);

        if (result.ok) {
            if (paymentModal) paymentModal.hide();
            stopTimer();
            clearCart();
            if (cartModal) cartModal.hide();
            renderCartModal();
            updateCartBadge();
            showAlert(`¡Pago confirmado! Se procesaron ${resIds.length} entradas.`, 'success');
            document.getElementById('payment-form').reset();
            stopSeatsPolling();
            document.getElementById('seats-section').classList.add('d-none');
            document.getElementById('events-section').classList.remove('d-none');
        } else {
            showAlert(`Error en el pago: ${result.data?.Message || 'No se pudo procesar'}`, 'error');
            submitBtn.disabled = false;
        }
    } catch (error) {
        console.error('[CODE-ERROR] - ', error);
        showAlert('Ocurrió un error inesperado al procesar el pago.', 'error');
        submitBtn.disabled = false;
    } finally {
        hideLoading();
    }
}

function startTimer(expiresAt) {
    if (countdownInterval) {
        clearInterval(countdownInterval);
        countdownInterval = null;
    }

    const expiryDate = new Date(expiresAt).getTime();
    if (isNaN(expiryDate) || expiryDate - Date.now() <= 0) {
        handleTimerExpiry();
        return;
    }

    reservationBanner.classList.remove('d-none');

    const updateTimer = () => {
        const distance = expiryDate - Date.now();

        if (distance <= 0) {
            handleTimerExpiry();
            return;
        }

        const minutes = Math.floor(distance / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        timerDisplay.innerText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    };

    updateTimer();
    countdownInterval = setInterval(updateTimer, 1000);
}

function handleTimerExpiry() {
    stopTimer();
    clearCart();
    if (cartModal) cartModal.hide();
    renderCartModal();
    updateCartBadge();
    showAlert('Tu tiempo de reserva ha expirado.', 'error');
    refreshSeats();
}

function stopTimer() {
    if (countdownInterval) clearInterval(countdownInterval);
    reservationBanner.classList.add('d-none');
    activeReservations = [];
}

function handlePayment() {
    if (activeReservations.length > 0 && paymentModal) {
        paymentModal.show();
    } else {
        showAlert('No tenés reservas activas para pagar.', 'error');
    }
}

export function checkAndShowActiveReservation(reservation) {
    if (!reservation) return;
    
    // Si ya hay reservas, la agregamos si no está
    if (!activeReservations.find(r => r.Id === reservation.Id)) {
        activeReservations.push(reservation);
    }
    
    currentUserId = reservation.UserId;

    // Mostramos el venc más cercano: si la primera expira, igual queremos liberar todo el lote.
    const earliestExpiry = activeReservations.reduce((prev, curr) =>
        new Date(prev.ExpiresAt) < new Date(curr.ExpiresAt) ? prev : curr
    );
    
    startTimer(earliestExpiry.ExpiresAt);
    
    const count = activeReservations.length;
    showAlert(`Tenés ${count} reserva${count > 1 ? 's' : ''} activa${count > 1 ? 's' : ''}. Pagalas pronto.`, 'success');
}

function toggleSeatSelection(seat, seatEl) {
    if (selectedSeats.has(seat.Id)) {
        selectedSeats.delete(seat.Id);
        seatEl.classList.remove('seat-selected');
    } else {
        selectedSeats.set(seat.Id, seat);
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
    selectedSeats.clear();
    updateSelectionBar();
    document.querySelectorAll('.seat-selected').forEach(el => el.classList.remove('seat-selected'));
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
    startSeatsPolling();
}

function startSeatsPolling() {
    stopSeatsPolling();
    // Polling silencioso: actualiza el mapa sin mostrar el spinner para no molestar al usuario que está eligiendo.
    seatsPollInterval = setInterval(() => {
        refreshSeats({ silent: true }).catch(err => console.error('Polling error:', err));
    }, SEATS_POLL_MS);
}

export function stopSeatsPolling() {
    if (seatsPollInterval) {
        clearInterval(seatsPollInterval);
        seatsPollInterval = null;
    }
}

async function refreshSeats({ silent = false } = {}) {
    if (!silent) showLoading();
    try {
        const savedUser = localStorage.getItem('currentUser');
        let userId = null;
        if (savedUser) {
            const user = JSON.parse(savedUser);
            userId = user.Id;
            currentUserId = user.Id;

            try {
                const reservations = await fetchMyReservations(userId);
                const now = Date.now();
                const pending = (reservations || []).filter(r =>
                    r.Status === 'Pending' && new Date(r.ExpiresAt).getTime() > now
                );

                if (pending.length > 0) {
                    activeReservations = pending;
                    const earliestExpiry = activeReservations.reduce((prev, curr) =>
                        new Date(prev.ExpiresAt) < new Date(curr.ExpiresAt) ? prev : curr
                    );
                    startTimer(earliestExpiry.ExpiresAt);
                } else {
                    stopTimer();
                    if (getCart().length > 0) clearCart();
                }
            } catch (err) {
                console.error("Error fetching reservations in refreshSeats:", err);
            }
        }

        const seats = await fetchSeatsBySector(currentSectorId, userId);
        renderSeats(seats);
    } catch (error) {
        if (!silent) showAlert('No se pudieron cargar los asientos.', 'error');
    } finally {
        if (!silent) hideLoading();
    }
}

function renderSeats(seats) {
    seatsGrid.innerHTML = '';

    if (!seats || seats.length === 0) {
        seatsGrid.innerHTML = '<p class="text-muted my-3">No hay asientos disponibles.</p>';
        return;
    }

    // Si una butaca seleccionada dejó de estar disponible (otro usuario la tomó), la quitamos de la selección.
    const availableIds = new Set(seats.filter(s => s.Status === 'Available').map(s => s.Id));
    let droppedFromSelection = 0;
    for (const id of [...selectedSeats.keys()]) {
        if (!availableIds.has(id)) {
            selectedSeats.delete(id);
            droppedFromSelection++;
        }
    }
    if (droppedFromSelection > 0) {
        updateSelectionBar();
        showAlert(`${droppedFromSelection} butaca(s) de tu selección fueron tomadas por otro usuario.`, 'error');
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
            if (selectedSeats.has(seat.Id)) seatEl.classList.add('seat-selected');
            seatEl.addEventListener('click', () => toggleSeatSelection(seat, seatEl));
        } else if (seat.Status === 'Reserved' && seat.IsReservedByCurrentUser) {
            seatEl.classList.add('seat-unavailable', 'seat-mine');
            seatEl.title = "Reservado por vos - Click para pagar";
            seatEl.addEventListener('click', () => payReservedSeat(seat));
        } else if (seat.Status === 'Reserved') {
            seatEl.classList.add('seat-unavailable');
            seatEl.title = "Reservado por otro usuario";
        } else {
            seatEl.classList.add('seat-unavailable');
            seatEl.title = "Vendido";
        }

        seatsGrid.appendChild(seatEl);
    }
}

function payReservedSeat(seat) {
    if (activeReservations.length === 0) {
        showAlert('No tenés una reserva activa para pagar.', 'error');
        return;
    }
    handlePayment();
}

