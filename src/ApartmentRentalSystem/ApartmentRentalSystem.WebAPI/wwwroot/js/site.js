const PAGE_TITLES = {
    services: 'Додаткові послуги',
    reviews: 'Відгуки та Оцінки',
    loyalty: 'Картки лояльності'
};

function showToast(msg, isError = false) {
    const toastEl = document.getElementById('liveToast');
    const toastText = document.getElementById('toast-text');
    toastText.textContent = msg;

    if (isError) {
        toastEl.classList.remove('bg-success');
        toastEl.classList.add('bg-danger');
    } else {
        toastEl.classList.remove('bg-danger');
        toastEl.classList.add('bg-success');
    }

    const toast = new bootstrap.Toast(toastEl);
    toast.show();
}

function switchTab(name, btn) {
    document.querySelectorAll('.panel').forEach(p => p.classList.add('add', 'd-none'));
    document.querySelectorAll('.nav-link').forEach(t => t.classList.remove('active'));

   
    document.getElementById('panel-' + name).classList.remove('d-none');
    btn.classList.add('active');
    document.getElementById('page-title').textContent = PAGE_TITLES[name];
}

function loadAll() { getServices(); getReviews(); getCards(); }

// ── ПОСЛУГИ ──
function getServices() {
    fetch('/api/AdditionalServices')
        .then(r => r.json())
        .then(data => {
            const tbody = document.getElementById('services-list');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Послуг ще немає — додайте першу</td></tr>';
            } else {
                tbody.innerHTML = data.map(s => `
                    <tr>
                        <td><span class="badge bg-secondary">${s.id}</span></td>
                        <td><strong>${s.name}</strong></td>
                        <td>${s.price} грн</td>
                        <td><button class="btn btn-sm btn-outline-danger" onclick="deleteService(${s.id})">Видалити</button></td>
                    </tr>`).join('');
            }
        })
        .catch(() => showToast('Помилка завантаження послуг', true));
}

function addService() {
    const name = document.getElementById('service-name').value.trim();
    const price = parseFloat(document.getElementById('service-price').value);
    if (!name || isNaN(price)) return showToast('Заповніть усі поля правильно', true);

    fetch('/api/AdditionalServices', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, price })
    }).then(r => {
        if (!r.ok) throw new Error();
        document.getElementById('service-name').value = '';
        document.getElementById('service-price').value = '';
        getServices();
        showToast('Послугу успішно додано!');
    }).catch(() => showToast('Помилка додавання послуги', true));
}

function deleteService(id) {
    if (!confirm('Видалити цю послугу?')) return;
    fetch(`/api/AdditionalServices/${id}`, { method: 'DELETE' })
        .then(() => { getServices(); showToast('Послугу видалено'); })
        .catch(() => showToast('Помилка видалення', true));
}

// ── ВІДГУКИ ──
function getReviews() {
    fetch('/api/Reviews')
        .then(r => r.json())
        .then(data => {
            const tbody = document.getElementById('reviews-list');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Відгуків ще немає</td></tr>';
            } else {
                tbody.innerHTML = data.map(r => `
                    <tr>
                        <td><span class="badge bg-info">Апт. №${r.apartmentId}</span></td>
                        <td><strong class="text-warning">${'★'.repeat(r.rating)}${'☆'.repeat(5 - r.rating)}</strong></td>
                        <td>${r.comment || '<span class="text-muted">Без коментаря</span>'}</td>
                        <td><button class="btn btn-sm btn-outline-danger" onclick="deleteReview(${r.id})">Видалити</button></td>
                    </tr>`).join('');
            }
        })
        .catch(() => showToast('Помилка завантаження відгуків', true));
}

function addReview() {
    const apartmentId = parseInt(document.getElementById('review-apt').value);
    const rating = parseInt(document.getElementById('review-rating').value);
    const comment = document.getElementById('review-comment').value.trim();

    if (!apartmentId || isNaN(rating) || rating < 1 || rating > 5) {
        return showToast('Введіть коректний ID та оцінку (1-5)', true);
    }

    fetch('/api/Reviews', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apartmentId, rating, comment, userId: "admin-js" })
    }).then(r => {
        if (!r.ok) throw new Error();
        document.getElementById('review-apt').value = '';
        document.getElementById('review-rating').value = '';
        document.getElementById('review-comment').value = '';
        getReviews();
        showToast('Відгук додано!');
    }).catch(() => showToast('Помилка — перевірте чи існує апартамент з таким ID', true));
}

function deleteReview(id) {
    if (!confirm('Видалити цей відгук?')) return;
    fetch(`/api/Reviews/${id}`, { method: 'DELETE' })
        .then(() => { getReviews(); showToast('Відгук видалено'); })
        .catch(() => showToast('Помилка видалення', true));
}

// ── ЛОЯЛЬНІСТЬ ──
function getCards() {
    fetch('/api/LoyaltyCards')
        .then(r => r.json())
        .then(data => {
            const tbody = document.getElementById('cards-list');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Кароток лояльності немає</td></tr>';
            } else {
                tbody.innerHTML = data.map(c => `
                    <tr>
                        <td><span class="badge bg-secondary">${c.id}</span></td>
                        <td>Користувач №${c.userId}</td>
                        <td><span class="badge bg-success">${c.points} балів</span></td>
                        <td><button class="btn btn-sm btn-outline-danger" onclick="deleteCard(${c.id})">Видалити</button></td>
                    </tr>`).join('');
            }
            const total = data.reduce((s, c) => s + c.points, 0);
            document.getElementById('stat-cards').textContent = data.length;
            document.getElementById('stat-points').textContent = total;
        })
        .catch(() => showToast('Помилка завантаження карток', true));
}

function addCard() {
    const userId = parseInt(document.getElementById('card-user').value);
    const points = parseInt(document.getElementById('card-points').value) || 0;
    if (!userId) return showToast('Введіть ID користувача', true);

    fetch('/api/LoyaltyCards', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, points })
    }).then(r => {
        if (!r.ok) throw new Error();
        document.getElementById('card-user').value = '';
        document.getElementById('card-points').value = '';
        getCards();
        showToast('Картку лояльності створено');
    }).catch(() => showToast('Помилка створення картки', true));
}

function deleteCard(id) {
    if (!confirm('Видалити цю картку?')) return;
    fetch(`/api/LoyaltyCards/${id}`, { method: 'DELETE' })
        .then(() => { getCards(); showToast('Картку видалено'); })
        .catch(() => showToast('Помилка видалення', true));
}