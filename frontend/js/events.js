import { fetchEvents, fetchSectorsByEvent, login, register, fetchMyReservations, updateEvent, deleteEvent } from './api.js';
import { loadSeats, checkAndShowActiveReservation } from './seats.js?v=2';
import { clearCart, updateCartBadge } from './cart.js';

const loginSection = document.getElementById('login-section');
const eventsSection = document.getElementById('events-section');
const eventsList = document.getElementById('events-list');
const sectorsSection = document.getElementById('sectors-section');
const sectorsList = document.getElementById('sectors-list');
const seatsSection = document.getElementById('seats-section');
const spinner = document.getElementById('loading-spinner');
const eventTitle = document.getElementById('event-title');

let _editingEvent = null;
let _deletingEventId = null;

const userInfo = document.getElementById('user-info');
const navbarUsername = document.getElementById('navbar-username');
const btnLogout = document.getElementById('btn-logout');
const loginForm = document.getElementById('login-form');

const btnAdmin = document.getElementById('btn-admin');
const adminSection = document.getElementById('admin-section');
const loginCard = document.getElementById('login-card');
const registerCard = document.getElementById('register-card');
const registerForm = document.getElementById('register-form');

document.addEventListener('DOMContentLoaded', init);

async function init() {
    document.getElementById('btn-back-events').addEventListener('click', showEvents);
    document.getElementById('btn-back-sectors').addEventListener('click', showSectors);
    
    loginForm.addEventListener('submit', handleLogin);
    btnLogout.addEventListener('click', handleLogout);
    if (btnAdmin) btnAdmin.addEventListener('click', showAdminSection);

    document.getElementById('btn-confirm-edit-event').addEventListener('click', handleEditConfirm);
    document.getElementById('btn-confirm-delete-event').addEventListener('click', handleDeleteConfirm);

    document.getElementById('show-register-btn').addEventListener('click', e => {
        e.preventDefault();
        loginCard.classList.add('d-none');
        registerCard.classList.remove('d-none');
    });
    document.getElementById('show-login-btn').addEventListener('click', e => {
        e.preventDefault();
        registerCard.classList.add('d-none');
        loginCard.classList.remove('d-none');
    });
    registerForm.addEventListener('submit', handleRegister);

    updateCartBadge();

    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
        const user = JSON.parse(savedUser);
        showAuthenticatedUI(user);
        await loadEvents();
        await checkUserReservations(user.Id);
    }
}

async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;

    showLoading();
    const result = await login(email, password);
    hideLoading();

    if (result.ok) {
        localStorage.setItem('currentUser', JSON.stringify(result.data));
        showAuthenticatedUI(result.data);
        await loadEvents();
        await checkUserReservations(result.data.Id);
    } else {
        showAlert(result.data.Message || 'Error al iniciar sesión', 'error');
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const name = document.getElementById('register-name').value;
    const email = document.getElementById('register-email').value;
    const password = document.getElementById('register-password').value;

    showLoading();
    const result = await register(name, email, password);
    hideLoading();

    if (result.ok) {
        localStorage.setItem('currentUser', JSON.stringify(result.data));
        showAuthenticatedUI(result.data);
        await loadEvents();
        await checkUserReservations(result.data.Id);
    } else {
        showAlert(result.data?.Message || 'Error al registrarse', 'error');
    }
}

async function checkUserReservations(userId) {
    try {
        const reservations = await fetchMyReservations(userId);
        if (reservations && reservations.length > 0) {
            const pendingReservation = reservations.find(r => r.Status === 'Pending');
            if (pendingReservation) {
                checkAndShowActiveReservation(pendingReservation);
            }
        }
    } catch (error) {
        console.error('Error checking user reservations:', error);
    }
}

function handleLogout() {
    localStorage.removeItem('currentUser');
    clearCart();
    userInfo.classList.add('d-none');
    eventsSection.classList.add('d-none');
    sectorsSection.classList.add('d-none');
    seatsSection.classList.add('d-none');
    if(adminSection) adminSection.classList.add('d-none');
    if(btnAdmin) btnAdmin.classList.add('d-none');
    loginSection.classList.remove('d-none');
    loginForm.reset();
    if(registerForm) registerForm.reset();
    loginCard.classList.remove('d-none');
    registerCard.classList.add('d-none');
}

function showAuthenticatedUI(user) {
    loginSection.classList.add('d-none');
    userInfo.classList.remove('d-none');
    userInfo.classList.add('d-flex');
    eventsSection.classList.remove('d-none');
    navbarUsername.innerText = `Hola, ${user.Name}`;
    
    if (user.Role === 'Admin') {
        btnAdmin.classList.remove('d-none');
    } else {
        btnAdmin.classList.add('d-none');
    }
}

function showAdminSection() {
    eventsSection.classList.add('d-none');
    sectorsSection.classList.add('d-none');
    seatsSection.classList.add('d-none');
    adminSection.classList.remove('d-none');
}

export function showEvents() {
    sectorsSection.classList.add('d-none');
    seatsSection.classList.add('d-none');
    if(adminSection) adminSection.classList.add('d-none');
    eventsSection.classList.remove('d-none');
}

export function showSectors() {
    seatsSection.classList.add('d-none');
    sectorsSection.classList.remove('d-none');
}

function showLoading() {
    spinner.classList.remove('d-none');
}

function hideLoading() {
    spinner.classList.add('d-none');
}

export function showAlert(message, type = 'error') {
    const container = document.getElementById('alerts-container');
    const alertId = 'alert-' + Date.now();
    const cssClass = type === 'error' ? 'alert-error alert-danger' : 'alert-success';
    container.innerHTML = `
        <div id="${alertId}" class="alert ${cssClass} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    setTimeout(() => {
        const alertEl = document.getElementById(alertId);
        if (alertEl) {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alertEl);
            bsAlert.close();
        }
    }, 5000);
}

export async function loadEvents() {
    showLoading();
    try {
        const events = await fetchEvents();
        renderEvents(events);
    } catch (error) {
        showAlert('No se pudieron cargar los eventos.', 'error');
    } finally {
        hideLoading();
    }
}

function getRelativeDate(dateStr) {
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.round((date - now) / (1000 * 60 * 60 * 24));
    if (diffDays === 0) return 'Hoy';
    if (diffDays === 1) return 'Mañana';
    if (diffDays === -1) return 'Ayer';
    if (diffDays > 0) return `En ${diffDays} días`;
    return `Hace ${Math.abs(diffDays)} días`;
}

function getStatusBadge(status) {
    if (status === 'Active') return '<span class="badge bg-success ms-2">Activo</span>';
    if (status === 'Deleted') return '<span class="badge bg-danger ms-2">Eliminado</span>';
    return `<span class="badge bg-secondary ms-2">${status}</span>`;
}

function renderEvents(events) {
    eventsList.innerHTML = '';
    if (!events || events.length === 0) {
        eventsList.innerHTML = '<p class="text-muted">No hay eventos disponibles.</p>';
        return;
    }

    let isAdmin = false;
    try {
        const currentUser = JSON.parse(localStorage.getItem('currentUser'));
        if (currentUser && currentUser.Role === 'Admin') {
            isAdmin = true;
        }
    } catch (e) {}

    events.forEach(event => {
        const card = document.createElement('div');
        card.className = 'col-md-6 mb-4';

        const isDeleted = event.Status === 'Deleted';

        let adminActionsHtml = '';
        if (isAdmin) {
            adminActionsHtml = `
                <div class="event-admin-actions">
                    <button type="button" class="icon-btn icon-btn-edit" data-action="edit" title="Modificar evento" aria-label="Modificar evento">
                        <span aria-hidden="true">✏️</span>
                    </button>
                    <button type="button" class="icon-btn icon-btn-delete" data-action="delete" title="Eliminar evento" aria-label="Eliminar evento">
                        <span aria-hidden="true">🗑️</span>
                    </button>
                </div>
            `;
        }

        card.innerHTML = `
            <div class="card h-100 border-0 position-relative event-card ${isDeleted ? 'opacity-60' : ''}">
                ${adminActionsHtml}
                <div class="card-body d-flex flex-column">
                    <div class="d-flex align-items-center mb-2 pe-5">
                        <h5 class="card-title fw-bold mb-0 event-card-title">${event.Name}</h5>
                        ${getStatusBadge(event.Status)}
                    </div>
                    <p class="card-text mb-1 text-muted">
                        <small>📅 ${new Date(event.EventDate).toLocaleString('es-AR')}</small>
                        <span class="badge bg-secondary ms-2 fw-normal" style="font-size:0.7rem;">${getRelativeDate(event.EventDate)}</span>
                    </p>
                    <p class="card-text mb-3 text-muted">
                        <small>📍 ${event.Venue}</small>
                    </p>
                    <div class="d-flex gap-3 mb-3 event-stats">
                        <span class="small text-muted">🎭 <strong class="text-light">${event.SectorCount}</strong> sector${event.SectorCount !== 1 ? 'es' : ''}</span>
                        <span class="small text-muted">💺 <strong class="text-light">${event.TotalSeats}</strong> butaca${event.TotalSeats !== 1 ? 's' : ''}</span>
                    </div>
                    <button class="btn btn-primary w-100 mt-auto" data-action="view" ${isDeleted ? 'disabled' : ''}>Ver Sectores →</button>
                </div>
            </div>
        `;
        if (!isDeleted) {
            card.querySelector('[data-action="view"]').addEventListener('click', () => loadSectors(event.Id, event.Name));
        }
        if (isAdmin) {
            const editBtn = card.querySelector('[data-action="edit"]');
            const deleteBtn = card.querySelector('[data-action="delete"]');
            editBtn.addEventListener('click', e => { e.preventDefault(); e.stopPropagation(); openEditModal(event); });
            deleteBtn.addEventListener('click', e => { e.preventDefault(); e.stopPropagation(); openDeleteModal(event.Id, event.Name); });
        }
        eventsList.appendChild(card);
    });
}

export async function loadSectors(eventId, eventName) {
    showLoading();
    try {
        const sectors = await fetchSectorsByEvent(eventId);
        renderSectors(sectors, eventName);
    } catch (error) {
        showAlert('No se pudieron cargar los sectores.', 'error');
    } finally {
        hideLoading();
    }
}

function openEditModal(event) {
    _editingEvent = event;
    document.getElementById('edit-event-name').value = event.Name;
    document.getElementById('edit-event-date').value = event.EventDate.substring(0, 16);
    document.getElementById('edit-event-venue').value = event.Venue;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('editEventModal')).show();
}

function openDeleteModal(id, name) {
    _deletingEventId = id;
    document.getElementById('delete-event-name').textContent = name;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteEventModal')).show();
}

async function handleEditConfirm() {
    const name = document.getElementById('edit-event-name').value.trim();
    const eventDate = document.getElementById('edit-event-date').value;
    const venue = document.getElementById('edit-event-venue').value.trim();
    if (!name || !eventDate || !venue) return;

    bootstrap.Modal.getInstance(document.getElementById('editEventModal'))?.hide();
    showLoading();
    const result = await updateEvent(_editingEvent.Id, { Name: name, EventDate: eventDate, Venue: venue });
    hideLoading();

    if (result.ok) {
        showAlert('Evento actualizado correctamente.', 'success');
        await loadEvents();
    } else {
        showAlert(result.data?.Message || 'Error al actualizar el evento.', 'error');
    }
}

async function handleDeleteConfirm() {
    bootstrap.Modal.getInstance(document.getElementById('deleteEventModal'))?.hide();
    showLoading();
    const result = await deleteEvent(_deletingEventId);
    hideLoading();

    if (result.ok) {
        showAlert('Evento eliminado correctamente.', 'success');
        await loadEvents();
    } else {
        showAlert('Error al eliminar el evento.', 'error');
    }
}

function renderSectors(sectors, eventName) {
    eventTitle.innerText = `Sectores para: ${eventName}`;
    sectorsList.innerHTML = '';
    if (!sectors || sectors.length === 0) {
        sectorsList.innerHTML = '<p class="text-muted">No hay sectores para este evento.</p>';
        return;
    }
    sectors.forEach(sector => {
        const col = document.createElement('div');
        col.className = 'col-md-6 mb-4';
        col.innerHTML = `
            <div class="card h-100 shadow-sm border-secondary cursor-pointer sector-card">
                <div class="card-body text-center py-4">
                    <h4 class="card-title">${sector.Name}</h4>
                    <p class="card-text fs-5 text-success fw-bold">Precio: $${sector.Price}</p>
                    <p class="card-text text-muted mb-3"><small>Capacidad: ${sector.Capacity} butacas</small></p>
                    <button class="btn btn-primary px-4">Ver Mapa de Asientos</button>
                </div>
            </div>
        `;
        col.querySelector('button').addEventListener('click', () => {
            sectorsSection.classList.add('d-none');
            loadSeats(sector.Id, sector.Name, sector.Price, eventName);
        });
        sectorsList.appendChild(col);
    });
    eventsSection.classList.add('d-none');
    sectorsSection.classList.remove('d-none');
}
