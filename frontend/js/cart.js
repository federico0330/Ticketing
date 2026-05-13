const STORAGE_KEY = 'ticketingCart';

function readCart() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        return raw ? JSON.parse(raw) : [];
    } catch (e) {
        return [];
    }
}

function writeCart(items) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
}

export function getCart() {
    return readCart();
}

export function addToCart(item) {
    const cart = readCart();
    // Evitamos duplicados por reservationId: la misma reserva podría llegar dos veces si el polling y el response del POST se cruzan.
    if (cart.some(i => i.reservationId === item.reservationId)) return cart;
    cart.push(item);
    writeCart(cart);
    updateCartBadge();
    return cart;
}

export function removeFromCart(reservationId) {
    const cart = readCart().filter(i => i.reservationId !== reservationId);
    writeCart(cart);
    updateCartBadge();
    return cart;
}

export function clearCart() {
    localStorage.removeItem(STORAGE_KEY);
    updateCartBadge();
}

// El timer del carrito se alinea con la reserva más próxima a expirar: si esa se cae, el resto también deja de servir.
export function getEarliestExpiry() {
    const cart = readCart();
    if (cart.length === 0) return null;
    const times = cart.map(i => new Date(i.expiresAt).getTime()).filter(t => !isNaN(t));
    if (times.length === 0) return null;
    return new Date(Math.min(...times)).toISOString();
}

export function getCartTotal() {
    return readCart().reduce((sum, i) => sum + (Number(i.price) || 0), 0);
}

export function updateCartBadge() {
    const cart = readCart();
    const badge = document.getElementById('cart-badge');
    const btn = document.getElementById('btn-cart');
    if (badge) badge.innerText = cart.length;
    if (btn) {
        if (cart.length > 0) btn.classList.remove('d-none');
        else btn.classList.add('d-none');
    }
}

export function renderCartModal() {
    const list = document.getElementById('cart-items-list');
    const totalEl = document.getElementById('cart-total');
    const checkoutBtn = document.getElementById('btn-checkout');
    if (!list || !totalEl) return;

    const cart = readCart();

    if (cart.length === 0) {
        list.innerHTML = '<p class="text-muted text-center py-3">Tu carrito está vacío.</p>';
        totalEl.innerText = '0';
        if (checkoutBtn) checkoutBtn.disabled = true;
        return;
    }

    if (checkoutBtn) checkoutBtn.disabled = false;

    list.innerHTML = cart.map(item => `
        <div class="d-flex justify-content-between align-items-center border-bottom py-2">
            <div>
                <div class="fw-bold">${item.eventName}</div>
                <div class="small text-muted">${item.sectorName} — Asiento ${item.seatLabel}</div>
            </div>
            <div class="d-flex align-items-center gap-3">
                <span class="text-success fw-bold">$${item.price}</span>
                <button class="btn btn-sm btn-outline-danger" data-remove="${item.reservationId}" title="Quitar del carrito">✕</button>
            </div>
        </div>
    `).join('');

    totalEl.innerText = getCartTotal();

    list.querySelectorAll('[data-remove]').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.getAttribute('data-remove');
            removeFromCart(id);
            renderCartModal();
        });
    });
}
