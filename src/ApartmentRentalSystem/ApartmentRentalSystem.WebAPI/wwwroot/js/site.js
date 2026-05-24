const PAGE_META = {
    "public-search": {
        title: "Пошук житла",
        subtitle: "Публічний каталог апартаментів із пошуком, фільтрацією та перевіркою доступності."
    },
    "guest-reservations": {
        title: "Мої бронювання",
        subtitle: "Створення бронювання, перегляд власних бронювань та оплата."
    },
    "guest-loyalty": {
        title: "Бонусний рахунок",
        subtitle: "Перегляд бонусних балів і доступних додаткових послуг."
    },
    "host-apartments": {
        title: "Мої апартаменти",
        subtitle: "Створення, редагування, видалення та керування активністю апартаментів."
    },
    "host-reservations": {
        title: "Бронювання апартаментів",
        subtitle: "Перегляд бронювань, створених для житла власника."
    },
    "host-services": {
        title: "Додаткові послуги",
        subtitle: "Керування додатковими послугами для системи бронювання."
    },
    "reviews": {
        title: "Відгуки",
        subtitle: "Перегляд відгуків та, за можливості, створення нового відгуку."
    }
};

let currentUser = null;
let publicApartmentsCache = [];
let paymentMethodsCache = [];
let loyaltyCardsCache = [];

function showToast(message, isError = false) {
    const toastEl = document.getElementById("liveToast");
    const toastText = document.getElementById("toast-text");

    toastText.textContent = message;
    toastEl.classList.toggle("bg-danger", isError);
    toastEl.classList.toggle("bg-success", !isError);

    new bootstrap.Toast(toastEl).show();
}

function formatDateTime(value) {
    if (!value) return "—";
    return new Date(value).toLocaleString("uk-UA");
}

function safeText(value) {
    return value ?? "—";
}

function switchTab(name, btn) {
    document.querySelectorAll(".panel").forEach(panel => panel.classList.add("hidden"));
    document.querySelectorAll("#main-tabs .nav-link").forEach(link => link.classList.remove("active"));

    const activePanel = document.getElementById(`panel-${name}`);
    if (activePanel) activePanel.classList.remove("hidden");

    if (btn) btn.classList.add("active");

    if (PAGE_META[name]) {
        document.getElementById("page-title").textContent = PAGE_META[name].title;
        document.getElementById("page-subtitle").textContent = PAGE_META[name].subtitle;
    }
}

function openTabByName(name) {
    const btn = document.querySelector(`#main-tabs .nav-link[data-tab="${name}"]`);
    if (btn) switchTab(name, btn);
}

function setRoleUI() {
    const guestOnly = document.querySelectorAll(".guest-only");
    const hostOnly = document.querySelectorAll(".host-only");

    guestOnly.forEach(el => el.classList.add("hidden"));
    hostOnly.forEach(el => el.classList.add("hidden"));

    document.getElementById("btn-logout").classList.toggle("hidden", !currentUser);
    document.getElementById("btn-open-login").classList.toggle("hidden", !!currentUser);
    document.getElementById("btn-open-register").classList.toggle("hidden", !!currentUser);

    const chip = document.getElementById("current-user-chip");
    if (currentUser) {
        chip.classList.remove("hidden");
        chip.textContent = `${currentUser.fullName} (${currentUser.role})`;
    } else {
        chip.classList.add("hidden");
        chip.textContent = "";
    }

    if (!currentUser) {
        openTabByName("public-search");
        return;
    }

    if (currentUser.roleId === 1) {
        guestOnly.forEach(el => el.classList.remove("hidden"));
        openTabByName("guest-reservations");
    } else if (currentUser.roleId === 2) {
        hostOnly.forEach(el => el.classList.remove("hidden"));
        openTabByName("host-apartments");
    }
}

function saveSession() {
    localStorage.setItem("ars_current_user", JSON.stringify(currentUser));
}

function loadSession() {
    const raw = localStorage.getItem("ars_current_user");
    if (!raw) return;
    try {
        currentUser = JSON.parse(raw);
    } catch {
        currentUser = null;
    }
}

function logoutUser() {
    currentUser = null;
    localStorage.removeItem("ars_current_user");
    setRoleUI();
    showToast("Вихід виконано.");
}

async function registerUser() {
    const fullName = document.getElementById("register-fullname").value.trim();
    const email = document.getElementById("register-email").value.trim();
    const phone = document.getElementById("register-phone").value.trim();
    const roleId = parseInt(document.getElementById("register-role").value, 10);
    const password = document.getElementById("register-password").value.trim();

    if (!fullName || !email || !password) {
        showToast("Заповніть обов’язкові поля реєстрації.", true);
        return;
    }

    try {
        const response = await fetch("/api/Auth/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ fullName, email, phone, roleId, password })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Помилка реєстрації.");

        document.getElementById("register-fullname").value = "";
        document.getElementById("register-email").value = "";
        document.getElementById("register-phone").value = "";
        document.getElementById("register-password").value = "";

        const modal = bootstrap.Modal.getInstance(document.getElementById("registerModal"));
        if (modal) modal.hide();

        showToast("Користувача успішно зареєстровано.");
        await loadLoyaltyCards();
    } catch (error) {
        showToast(error.message || "Не вдалося виконати реєстрацію.", true);
    }
}

async function loginUser() {
    const email = document.getElementById("login-email").value.trim();
    const password = document.getElementById("login-password").value.trim();

    if (!email || !password) {
        showToast("Введіть email та пароль.", true);
        return;
    }

    try {
        const response = await fetch("/api/Auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Помилка входу.");

        const data = JSON.parse(text);
        currentUser = {
            id: data.id,
            fullName: data.fullName,
            email: data.email,
            phone: data.phone,
            roleId: data.roleId,
            role: data.role
        };

        saveSession();
        setRoleUI();

        const modal = bootstrap.Modal.getInstance(document.getElementById("loginModal"));
        if (modal) modal.hide();

        document.getElementById("login-email").value = "";
        document.getElementById("login-password").value = "";

        await afterLoginLoad();
        showToast("Вхід виконано успішно.");
    } catch (error) {
        showToast(error.message || "Не вдалося виконати вхід.", true);
    }
}

async function afterLoginLoad() {
    await loadPublicApartments();
    await loadPaymentMethods();
    await loadLoyaltyCards();

    if (!currentUser) return;

    if (currentUser.roleId === 1) {
        await loadGuestReservations();
        renderGuestLoyaltyCard();
        await loadServicesForGuest();
    }

    if (currentUser.roleId === 2) {
        await loadHostApartments();
        await loadHostReservations();
        await loadServicesForHost();
    }

    await loadReviews();
}

function buildApartmentQuery() {
    const city = document.getElementById("filter-city").value.trim();
    const maxGuests = document.getElementById("filter-guests").value.trim();
    const isActive = document.getElementById("filter-active").value;

    const params = new URLSearchParams();
    if (city) params.append("city", city);
    if (maxGuests) params.append("maxGuests", maxGuests);
    if (isActive !== "") params.append("isActive", isActive);

    return params.toString() ? `/api/Apartments?${params.toString()}` : "/api/Apartments";
}

async function loadPublicApartments() {
    try {
        const response = await fetch(buildApartmentQuery());
        const data = await response.json();
        publicApartmentsCache = data;
        renderPublicApartments(data);
    } catch {
        showToast("Не вдалося завантажити список апартаментів.", true);
    }
}

function renderPublicApartments(data) {
    const container = document.getElementById("public-apartments-list");

    if (!data.length) {
        container.innerHTML = `<div class="col-12"><div class="empty-state">Апартаменти не знайдено.</div></div>`;
        return;
    }

    container.innerHTML = data.map(apartment => `
        <div class="col-md-6">
            <div class="apartment-card">
                <div class="apartment-title">${safeText(apartment.title)}</div>
                <div class="apartment-meta"><strong>ID:</strong> ${apartment.id}</div>
                <div class="apartment-meta"><strong>Місто:</strong> ${safeText(apartment.city)}</div>
                <div class="apartment-meta"><strong>Адреса:</strong> ${safeText(apartment.address)}</div>
                <div class="apartment-meta"><strong>Гостей:</strong> ${safeText(apartment.maxGuests)}</div>
                <div class="apartment-meta"><strong>Тип:</strong> ${safeText(apartment.housingType)}</div>
                <div class="apartment-meta"><strong>Опис:</strong> ${safeText(apartment.description)}</div>
                <div class="apartment-price">
                    ${apartment.price ? `${apartment.price.amount} ${apartment.price.currency}` : "Ціна не вказана"}
                </div>
                <div class="d-flex gap-2 mt-3 flex-wrap">
                    <button class="btn btn-sm btn-soft" onclick="setAvailabilityApartment(${apartment.id})">Перевірити дати</button>
                    ${currentUser && currentUser.roleId === 1 ? `<button class="btn btn-sm btn-main" onclick="prefillReservationApartment(${apartment.id})">Забронювати</button>` : ``}
                </div>
            </div>
        </div>
    `).join("");
}

function resetPublicFilters() {
    document.getElementById("filter-city").value = "";
    document.getElementById("filter-guests").value = "";
    document.getElementById("filter-active").value = "";
    loadPublicApartments();
}

function setAvailabilityApartment(id) {
    document.getElementById("availability-id").value = id;
    checkAvailability();
}

function prefillReservationApartment(id) {
    document.getElementById("reservation-apartment-id").value = id;
    openTabByName("guest-reservations");
}

async function checkAvailability() {
    const apartmentId = parseInt(document.getElementById("availability-id").value, 10);
    const resultBox = document.getElementById("availability-result");

    if (!apartmentId) {
        showToast("Введіть ID апартаменту.", true);
        return;
    }

    try {
        const response = await fetch(`/api/Apartments/${apartmentId}/availability`);
        const data = await response.json();

        if (!data.bookedPeriods || data.bookedPeriods.length === 0) {
            resultBox.innerHTML = `<strong>Апартамент №${data.apartmentId}</strong><br>Заброньовані періоди відсутні.`;
        } else {
            resultBox.innerHTML = `
                <strong>Апартамент №${data.apartmentId}</strong><br>
                ${data.bookedPeriods.map(item =>
                `<div>З ${formatDateTime(item.startAt)} до ${formatDateTime(item.endAt)}</div>`
            ).join("")}
            `;
        }

        showToast("Інформацію про доступність отримано.");
    } catch {
        resultBox.textContent = "Не вдалося отримати дані про доступність.";
        showToast("Помилка перевірки доступності.", true);
    }
}

// GUEST RESERVATIONS
async function loadGuestReservations() {
    if (!currentUser || currentUser.roleId !== 1) return;

    try {
        const response = await fetch(`/api/Reservations/guest/${currentUser.id}`);
        const data = await response.json();
        const tbody = document.getElementById("guest-reservations-list");

        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="7" class="empty-state">Бронювання відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td><span class="soft-badge">${r.id}</span></td>
                <td>${safeText(r.apartment)}</td>
                <td>${safeText(r.apartmentCity)}</td>
                <td>${formatDateTime(r.startAt)}<br>${formatDateTime(r.endAt)}</td>
                <td>${safeText(r.totalPrice)}</td>
                <td>${safeText(r.status)}</td>
                <td>
                    <div class="d-flex gap-2 flex-wrap">
                        <button class="btn btn-sm btn-soft" onclick="prefillPayment(${r.id}, ${r.totalPrice ?? 0})">Оплатити</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="cancelReservation(${r.id})">Скасувати</button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити бронювання орендаря.", true);
    }
}

async function createReservation() {
    if (!currentUser || currentUser.roleId !== 1) {
        showToast("Бронювання доступне тільки орендарю.", true);
        return;
    }

    const apartmentId = parseInt(document.getElementById("reservation-apartment-id").value, 10);
    const startAt = document.getElementById("reservation-start").value;
    const endAt = document.getElementById("reservation-end").value;
    const unitsCount = parseInt(document.getElementById("reservation-units").value, 10);

    if (!apartmentId || !startAt || !endAt || !unitsCount) {
        showToast("Заповніть усі поля бронювання.", true);
        return;
    }

    try {
        const response = await fetch("/api/Reservations", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                apartmentId,
                guestId: currentUser.id,
                startAt,
                endAt,
                unitsCount
            })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Не вдалося створити бронювання.");

        document.getElementById("reservation-apartment-id").value = "";
        document.getElementById("reservation-start").value = "";
        document.getElementById("reservation-end").value = "";
        document.getElementById("reservation-units").value = "";

        await loadGuestReservations();
        showToast("Бронювання успішно створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося створити бронювання.", true);
    }
}

async function cancelReservation(id) {
    if (!confirm("Скасувати обране бронювання?")) return;

    try {
        const response = await fetch(`/api/Reservations/${id}/cancel`, { method: "PATCH" });
        if (!response.ok) throw new Error();
        await loadGuestReservations();
        showToast("Бронювання скасовано.");
    } catch {
        showToast("Не вдалося скасувати бронювання.", true);
    }
}

// PAYMENTS
async function loadPaymentMethods() {
    try {
        const response = await fetch("/api/Payments/methods");
        const data = await response.json();
        paymentMethodsCache = data;

        const select = document.getElementById("payment-method-id");
        select.innerHTML = data.map(m => `<option value="${m.id}">${m.name}</option>`).join("");
    } catch {
        showToast("Не вдалося завантажити методи оплати.", true);
    }
}

function prefillPayment(reservationId, amount) {
    document.getElementById("payment-reservation-id").value = reservationId;
    document.getElementById("payment-amount").value = amount || "";
}

async function payReservation() {
    const reservationId = parseInt(document.getElementById("payment-reservation-id").value, 10);
    const paymentMethodId = parseInt(document.getElementById("payment-method-id").value, 10);
    const amount = parseFloat(document.getElementById("payment-amount").value);

    if (!reservationId || !paymentMethodId || Number.isNaN(amount)) {
        showToast("Заповніть дані для оплати.", true);
        return;
    }

    try {
        const response = await fetch("/api/Payments", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                reservationId,
                paymentMethodId,
                amount,
                currency: "UAH"
            })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Не вдалося виконати оплату.");

        document.getElementById("payment-reservation-id").value = "";
        document.getElementById("payment-amount").value = "";

        await loadGuestReservations();
        await loadLoyaltyCards();
        renderGuestLoyaltyCard();

        showToast("Оплату виконано успішно.");
    } catch (error) {
        showToast(error.message || "Не вдалося виконати оплату.", true);
    }
}

// LOYALTY
async function loadLoyaltyCards() {
    try {
        const response = await fetch("/api/LoyaltyCards");
        loyaltyCardsCache = await response.json();
    } catch {
        loyaltyCardsCache = [];
    }
}

function renderGuestLoyaltyCard() {
    if (!currentUser || currentUser.roleId !== 1) return;

    const card = loyaltyCardsCache.find(c => c.userId === currentUser.id);

    document.getElementById("guest-card-id").textContent = card ? card.id : "—";
    document.getElementById("guest-card-points").textContent = card ? card.points : "0";
    document.getElementById("guest-card-user").textContent = currentUser.id;
}

// HOST APARTMENTS
async function loadHostApartments() {
    if (!currentUser || currentUser.roleId !== 2) return;

    try {
        const response = await fetch(`/api/Apartments/host/${String(currentUser.id)}`);
        const data = await response.json();

        const tbody = document.getElementById("host-apartments-list");
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="7" class="empty-state">Апартаменти власника відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(a => `
            <tr>
                <td><span class="soft-badge">${a.id}</span></td>
                <td>${safeText(a.title)}</td>
                <td>${safeText(a.city)}</td>
                <td>${safeText(a.housingType)}</td>
                <td>${a.price ? `${a.price.amount} ${a.price.currency}` : "—"}</td>
                <td>${a.isActive ? "Активний" : "Неактивний"}</td>
                <td>
                    <div class="d-flex gap-2 flex-wrap">
                        <button class="btn btn-sm btn-soft" onclick="editApartment(${a.id})">Редагувати</button>
                        <button class="btn btn-sm btn-outline-primary" onclick="toggleApartment(${a.id})">Змінити статус</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteApartment(${a.id})">Видалити</button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити апартаменти власника.", true);
    }
}

async function saveHostApartment() {
    if (!currentUser || currentUser.roleId !== 2) {
        showToast("Ця дія доступна тільки власнику.", true);
        return;
    }

    const editId = document.getElementById("host-apartment-edit-id").value;
    const payload = {
        title: document.getElementById("host-apartment-title").value.trim(),
        city: document.getElementById("host-apartment-city").value.trim(),
        address: document.getElementById("host-apartment-address").value.trim(),
        maxGuests: parseInt(document.getElementById("host-apartment-guests").value, 10),
        housingTypeId: parseInt(document.getElementById("host-apartment-housing-type").value, 10),
        hostId: String(currentUser.id),
        description: document.getElementById("host-apartment-description").value.trim(),
        area: parseFloat(document.getElementById("host-apartment-area").value) || null
    };

    if (!payload.title || !payload.city || !payload.address || !payload.maxGuests || !payload.housingTypeId) {
        showToast("Заповніть обов’язкові поля апартаменту.", true);
        return;
    }

    try {
        const response = await fetch(editId ? `/api/Apartments/${editId}` : "/api/Apartments", {
            method: editId ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Не вдалося зберегти апартамент.");

        clearHostApartmentForm();
        await loadHostApartments();
        await loadPublicApartments();

        showToast(editId ? "Апартамент оновлено." : "Апартамент створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося зберегти апартамент.", true);
    }
}

function clearHostApartmentForm() {
    document.getElementById("host-apartment-edit-id").value = "";
    document.getElementById("host-apartment-title").value = "";
    document.getElementById("host-apartment-city").value = "";
    document.getElementById("host-apartment-address").value = "";
    document.getElementById("host-apartment-guests").value = "";
    document.getElementById("host-apartment-housing-type").value = "";
    document.getElementById("host-apartment-area").value = "";
    document.getElementById("host-apartment-description").value = "";
    document.getElementById("host-apartment-form-title").textContent = "Додавання апартаменту";
}

async function editApartment(id) {
    try {
        const response = await fetch(`/api/Apartments/${id}`);
        const data = await response.json();

        document.getElementById("host-apartment-edit-id").value = data.id;
        document.getElementById("host-apartment-title").value = data.title ?? "";
        document.getElementById("host-apartment-city").value = data.city ?? "";
        document.getElementById("host-apartment-address").value = data.address ?? "";
        document.getElementById("host-apartment-guests").value = data.maxGuests ?? "";
        document.getElementById("host-apartment-area").value = data.area ?? "";
        document.getElementById("host-apartment-description").value = data.description ?? "";
        document.getElementById("host-apartment-form-title").textContent = "Редагування апартаменту";

        showToast("Дані апартаменту завантажено у форму.");
    } catch {
        showToast("Не вдалося завантажити дані апартаменту.", true);
    }
}

async function deleteApartment(id) {
    if (!confirm("Видалити обраний апартамент?")) return;

    try {
        const response = await fetch(`/api/Apartments/${id}`, { method: "DELETE" });
        if (!response.ok) throw new Error();
        await loadHostApartments();
        await loadPublicApartments();
        showToast("Апартамент видалено.");
    } catch {
        showToast("Не вдалося видалити апартамент.", true);
    }
}

async function toggleApartment(id) {
    try {
        const response = await fetch(`/api/Apartments/${id}/toggle`, { method: "PATCH" });
        if (!response.ok) throw new Error();
        await loadHostApartments();
        await loadPublicApartments();
        showToast("Статус апартаменту змінено.");
    } catch {
        showToast("Не вдалося змінити статус апартаменту.", true);
    }
}

// HOST RESERVATIONS
async function loadHostReservations() {
    if (!currentUser || currentUser.roleId !== 2) return;

    try {
        const response = await fetch(`/api/Reservations/host/${String(currentUser.id)}`);
        const data = await response.json();

        const tbody = document.getElementById("host-reservations-list");
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="7" class="empty-state">Бронювання відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td><span class="soft-badge">${r.id}</span></td>
                <td>${safeText(r.apartment)}</td>
                <td>${safeText(r.guest)}</td>
                <td>${safeText(r.guestPhone)}</td>
                <td>${formatDateTime(r.startAt)}<br>${formatDateTime(r.endAt)}</td>
                <td>${safeText(r.totalPrice)}</td>
                <td>${safeText(r.status)}</td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити бронювання власника.", true);
    }
}

// SERVICES
async function loadServicesForHost() {
    try {
        const response = await fetch("/api/AdditionalServices");
        const data = await response.json();

        const tbody = document.getElementById("host-services-list");
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="4" class="empty-state">Додаткові послуги відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(service => `
            <tr>
                <td><span class="soft-badge">${service.id}</span></td>
                <td>${safeText(service.name)}</td>
                <td>${safeText(service.price)} грн</td>
                <td>
                    <div class="d-flex gap-2 flex-wrap">
                        <button class="btn btn-sm btn-soft" onclick="editService(${service.id}, '${String(service.name).replace(/'/g, "\\'")}', ${service.price})">Редагувати</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteService(${service.id})">Видалити</button>
                    </div>
                </td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити список послуг.", true);
    }
}

async function loadServicesForGuest() {
    try {
        const response = await fetch("/api/AdditionalServices");
        const data = await response.json();

        const tbody = document.getElementById("guest-services-list");
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="3" class="empty-state">Послуги відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(service => `
            <tr>
                <td><span class="soft-badge">${service.id}</span></td>
                <td>${safeText(service.name)}</td>
                <td>${safeText(service.price)} грн</td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити додаткові послуги.", true);
    }
}

function editService(id, name, price) {
    document.getElementById("service-edit-id").value = id;
    document.getElementById("service-name").value = name;
    document.getElementById("service-price").value = price;
    document.getElementById("service-form-title").textContent = "Редагування послуги";
}

function clearServiceForm() {
    document.getElementById("service-edit-id").value = "";
    document.getElementById("service-name").value = "";
    document.getElementById("service-price").value = "";
    document.getElementById("service-form-title").textContent = "Додавання послуги";
}

async function saveService() {
    const id = document.getElementById("service-edit-id").value;
    const name = document.getElementById("service-name").value.trim();
    const price = parseFloat(document.getElementById("service-price").value);

    if (!name || Number.isNaN(price)) {
        showToast("Заповніть назву послуги та вартість.", true);
        return;
    }

    try {
        const response = await fetch(id ? `/api/AdditionalServices/${id}` : "/api/AdditionalServices", {
            method: id ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(id ? { id: parseInt(id, 10), name, price } : { name, price })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Не вдалося зберегти послугу.");

        clearServiceForm();
        await loadServicesForHost();
        await loadServicesForGuest();

        showToast(id ? "Послугу оновлено." : "Послугу створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося зберегти послугу.", true);
    }
}

async function deleteService(id) {
    if (!confirm("Видалити обрану послугу?")) return;

    try {
        const response = await fetch(`/api/AdditionalServices/${id}`, { method: "DELETE" });
        if (!response.ok) throw new Error();
        await loadServicesForHost();
        await loadServicesForGuest();
        showToast("Послугу видалено.");
    } catch {
        showToast("Не вдалося видалити послугу.", true);
    }
}

// REVIEWS
async function loadReviews() {
    try {
        const response = await fetch("/api/Reviews");
        const data = await response.json();

        const tbody = document.getElementById("reviews-list");
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="4" class="empty-state">Відгуки відсутні.</td></tr>`;
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td><span class="soft-badge">${r.id}</span></td>
                <td>${safeText(r.rating)}</td>
                <td>${safeText(r.comment)}</td>
                <td>${formatDateTime(r.createdAt)}</td>
            </tr>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити відгуки.", true);
    }
}

async function createReview() {
    if (!currentUser || currentUser.roleId !== 1) {
        showToast("Відгук може створювати тільки орендар.", true);
        return;
    }

    const reservationId = parseInt(document.getElementById("review-reservation-id").value, 10);
    const rating = parseInt(document.getElementById("review-rating").value, 10);
    const comment = document.getElementById("review-comment").value.trim();

    if (!reservationId || !rating) {
        showToast("Заповніть дані відгуку.", true);
        return;
    }

    try {
        const response = await fetch("/api/Reviews", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                reservationId,
                authorId: currentUser.id,
                rating,
                comment
            })
        });

        const text = await response.text();
        if (!response.ok) throw new Error(text || "Не вдалося створити відгук.");

        document.getElementById("review-reservation-id").value = "";
        document.getElementById("review-rating").value = "";
        document.getElementById("review-comment").value = "";

        await loadReviews();
        showToast("Відгук створено.");
    } catch (error) {
        showToast("Створення відгуку не спрацювало. Імовірно, потрібна точна модель Review з БД.", true);
    }
}

async function initializeApp() {
    loadSession();
    setRoleUI();
    await loadPublicApartments();
    await loadPaymentMethods();
    await loadLoyaltyCards();
    await loadReviews();

    if (currentUser) {
        await afterLoginLoad();
    }
}

document.addEventListener("DOMContentLoaded", initializeApp);