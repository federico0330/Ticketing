import { fetchEvents, fetchSectorsByEvent, login } from './api.js';
import { loadSeats } from './seats.js';

const loginSection = document.getElementById('login-section');
const eventsSection = document.getElementById('events-section');
const eventsList = document.getElementById('events-list');
const sectorsSection = document.getElementById('sectors-section');
const sectorsList = document.getElementById('sectors-list');
const seatsSection = document.getElementById('seats-section');
const spinner = document.getElementById('loading-spinner');
const eventTitle = document.getElementById('event-title');

// Elementos de la Navbar y Login
const userInfo = document.getElementById('user-info');
const navbarUsername = document.getElementById('navbar-username');
const btnLogout = document.getElementById('btn-logout');
const loginForm = document.getElementById('login-form');

document.addEventListener('DOMContentLoaded', init);

async function init() {
    // Configurar listeners de navegación
    document.getElementById('btn-back-events').addEventListener('click', showEvents);
    document.getElementById('btn-back-sectors').addEventListener('click', showSectors);
    
    // Configurar Auth
    loginForm.addEventListener('submit', handleLogin);
    btnLogout.addEventListener('click', handleLogout);

    // Verificar sesión existente
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
        const user = JSON.parse(savedUser);
        showAuthenticatedUI(user);
        await loadEvents();
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
    } else {
        showAlert(result.data.Message || 'Error al iniciar sesión', 'error');
    }
}

function handleLogout() {
    localStorage.removeItem('currentUser');
    userInfo.classList.add('d-none');
    eventsSection.classList.add('d-none');
    sectorsSection.classList.add('d-none');
    seatsSection.classList.add('d-none');
    loginSection.classList.remove('d-none');
    loginForm.reset();
}

function showAuthenticatedUI(user) {
    loginSection.classList.add('d-none');
    userInfo.classList.remove('d-none');
    userInfo.classList.add('d-flex');
    navbarUsername.innerText = `Hola, ${user.Name}`;
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

async function loadEvents() {
    showLoading();
    try {
        const events = await fetchEvents();
        renderEvents(events);
    } catch (error) {
        console.error('[CODE-ERROR] - ', error);
        showAlert('No se pudieron cargar los eventos. Verifica la conexión con el servidor.', 'error');
    } finally {
        hideLoading();
    }
}

function renderEvents(events) {
    eventsList.innerHTML = '';
    if (events.length === 0) {
        eventsList.innerHTML = '<div class="col"><p class="text-muted">No hay eventos disponibles en este momento.</p></div>';
        return;
    }

    for (let idx_tk = 0; idx_tk < events.length; idx_tk++) {
        const event = events[idx_tk];
        const col = document.createElement('div');
        col.className = 'col-md-4 mb-4';
        col.innerHTML = `
            <div class="card h-100 shadow-sm border-0">
                <div class="card-body">
                    <h5 class="card-title text-primary fw-bold">${event.Name}</h5>
                    <p class="card-text mb-1 text-muted">
                        <small>📅 ${new Date(event.EventDate).toLocaleString()}</small>
                    </p>
                    <p class="card-text mb-3 text-muted">
                        <small>📍 ${event.Venue}</small>
                    </p>
                    <button class="btn btn-outline-primary w-100 mt-auto">Ver Sectores</button>
                </div>
            </div>
        `;

        const btn = col.querySelector('button');
        btn.addEventListener('click', () => {
            loadSectors(event.Id, event.Name);
        });

        eventsList.appendChild(col);
    }
}

function showEvents() {
    sectorsSection.classList.add('d-none');
    seatsSection.classList.add('d-none');
    eventsSection.classList.remove('d-none');
}

export function showSectors() {
    seatsSection.classList.add('d-none');
    sectorsSection.classList.remove('d-none');
}

async function loadSectors(eventId, name) {
    showLoading();
    try {
        const sectors = await fetchSectorsByEvent(eventId);
        eventTitle.innerText = `Sectores para: ${name}`;
        renderSectors(sectors);
        eventsSection.classList.add('d-none');
        sectorsSection.classList.remove('d-none');
    } catch (error) {
        console.error('[CODE-ERROR] - ', error);
        showAlert('No se pudieron cargar los sectores del evento.', 'error');
    } finally {
        hideLoading();
    }
}

function renderSectors(sectors) {
    sectorsList.innerHTML = '';
    if (sectors.length === 0) {
        sectorsList.innerHTML = '<div class="col"><p class="text-muted">No hay sectores para este evento.</p></div>';
        return;
    }

    for (let idx_tk = 0; idx_tk < sectors.length; idx_tk++) {
        const sector = sectors[idx_tk];
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

        const btn = col.querySelector('button');
        btn.addEventListener('click', () => {
            sectorsSection.classList.add('d-none');
            loadSeats(sector.Id, sector.Name);
        });

        sectorsList.appendChild(col);
    }
}