const PAGE_META = {
    "public-search": {
        title: "Пошук житла",
        subtitle: "Оберіть житло, перегляньте доступність і створіть бронювання."
    },
    "guest-reservations": {
        title: "Мої бронювання",
        subtitle: "Ваші бронювання та оплата."
    },
    "guest-loyalty": {
        title: "Бонусний рахунок",
        subtitle: "Ваші бонусні бали та додаткові послуги."
    },
    "host-apartments": {
        title: "Мої апартаменти",
        subtitle: "Створення та редагування оголошень."
    },
    "host-reservations": {
        title: "Бронювання апартаментів",
        subtitle: "Бронювання ваших оголошень."
    },
    "host-services": {
        title: "Додаткові послуги",
        subtitle: "Керування додатковими послугами."
    },
    "reviews": {
        title: "Відгуки",
        subtitle: "Відгуки користувачів."
    }
};

const DEFAULT_APARTMENT_IMAGE = "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="640" height="420" viewBox="0 0 640 420">
  <rect width="640" height="420" fill="#e8f3ff"/>
  <rect x="160" y="165" width="320" height="170" rx="18" fill="#ffffff" stroke="#2f80ed" stroke-width="8"/>
  <path d="M130 190 L320 70 L510 190" fill="none" stroke="#2f80ed" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
  <rect x="280" y="240" width="80" height="95" rx="8" fill="#2f80ed" opacity="0.85"/>
  <rect x="195" y="225" width="55" height="50" rx="6" fill="#56ccf2" opacity="0.85"/>
  <rect x="390" y="225" width="55" height="50" rx="6" fill="#56ccf2" opacity="0.85"/>
  <text x="320" y="375" font-family="Arial" font-size="26" text-anchor="middle" fill="#1d5fbf">Фото житла</text>
</svg>`);

let currentUser = null;
let publicApartmentsCache = [];
let paymentMethodsCache = [];
let loyaltyCardsCache = [];
let housingTypesCache = [];
let timeUnitsCache = [];
let guestServicesCache = [];
let reviewsCache = [];
let selectedPaymentOriginalAmount = 0;
let selectedPaymentFinalAmount = 0;
let selectedPaymentMaxPoints = 0;
let selectedPaymentPointsToUse = 0;

function showToast(message, isError = false) {
    const toastEl = document.getElementById("liveToast");
    const toastText = document.getElementById("toast-text");

    if (!toastEl || !toastText) {
        alert(message);
        return;
    }

    toastText.textContent = message;
    toastEl.classList.toggle("bg-danger", isError);
    toastEl.classList.toggle("bg-success", !isError);

    new bootstrap.Toast(toastEl).show();
}

function escapeHtml(value) {
    if (value === null || value === undefined || value === "") {
        return "—";
    }

    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function safeText(value) {
    return escapeHtml(value);
}

function formatDateTime(value) {
    if (!value) {
        return "—";
    }

    return new Date(value).toLocaleString("uk-UA");
}

function formatPrice(price) {
    if (!price) {
        return "Ціна не вказана";
    }

    return `${price.amount} ${price.currency}`;
}

function getApartmentImage(apartment) {
    return apartment.imagePath || DEFAULT_APARTMENT_IMAGE;
}

function switchTab(name, btn) {
    document.querySelectorAll(".panel").forEach(panel => {
        panel.classList.add("hidden");
    });

    document.querySelectorAll("#main-tabs .nav-link").forEach(link => {
        link.classList.remove("active");
    });

    const activePanel = document.getElementById(`panel-${name}`);
    if (activePanel) {
        activePanel.classList.remove("hidden");
    }

    const activeButton = btn || document.querySelector(`#main-tabs .nav-link[data-tab="${name}"]`);
    if (activeButton) {
        activeButton.classList.add("active");
    }

    if (PAGE_META[name]) {
        document.getElementById("page-title").textContent = PAGE_META[name].title;
        document.getElementById("page-subtitle").textContent = PAGE_META[name].subtitle;
    }
}

function openTabByName(name) {
    const btn = document.querySelector(`#main-tabs .nav-link[data-tab="${name}"]`);
    switchTab(name, btn);
}

function setRoleUI() {
    const guestOnly = document.querySelectorAll(".guest-only");
    const hostOnly = document.querySelectorAll(".host-only");
    const publicTabs = document.querySelectorAll(".public-tab");
    const tabsBox = document.querySelector(".tabs-box");

    guestOnly.forEach(el => el.classList.add("hidden"));
    hostOnly.forEach(el => el.classList.add("hidden"));
    publicTabs.forEach(el => el.classList.remove("hidden"));

    document.getElementById("btn-logout")?.classList.toggle("hidden", !currentUser);
    document.getElementById("btn-open-login")?.classList.toggle("hidden", !!currentUser);
    document.getElementById("btn-open-register")?.classList.toggle("hidden", !!currentUser);
    document.getElementById("register-menu-hint")?.classList.toggle("hidden", !!currentUser);

    const chip = document.getElementById("current-user-chip");
    const heroActions = document.getElementById("hero-actions");

    if (heroActions) {
        heroActions.innerHTML = "";
    }

    if (!currentUser) {
        if (chip) {
            chip.classList.add("hidden");
            chip.textContent = "";
        }

        if (tabsBox) {
            tabsBox.classList.add("hidden");
        }

        openTabByName("public-search");
        return;
    }

    if (tabsBox) {
        tabsBox.classList.remove("hidden");
    }

    chip.classList.remove("hidden");
    chip.textContent = `${currentUser.fullName} (${currentUser.role})`;

    if (currentUser.roleId === 1) {
        guestOnly.forEach(el => el.classList.remove("hidden"));
        publicTabs.forEach(el => el.classList.remove("hidden"));

        openTabByName("public-search");
    }

    if (currentUser.roleId === 2) {
        hostOnly.forEach(el => el.classList.remove("hidden"));
        publicTabs.forEach(el => el.classList.add("hidden"));

        openTabByName("host-apartments");
    }
}
function saveSession() {
    localStorage.setItem("ars_current_user", JSON.stringify(currentUser));
}

function loadSession() {
    const raw = localStorage.getItem("ars_current_user");

    if (!raw) {
        return;
    }

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
    loadPublicApartments();

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
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                fullName,
                email,
                phone,
                roleId,
                password
            })
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Помилка реєстрації.");
        }

        document.getElementById("register-fullname").value = "";
        document.getElementById("register-email").value = "";
        document.getElementById("register-phone").value = "";
        document.getElementById("register-password").value = "";

        bootstrap.Modal.getInstance(document.getElementById("registerModal"))?.hide();

        showToast("Користувача зареєстровано. Тепер можна увійти.");
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
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                email,
                password
            })
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Помилка входу.");
        }

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

        bootstrap.Modal.getInstance(document.getElementById("loginModal"))?.hide();

        document.getElementById("login-email").value = "";
        document.getElementById("login-password").value = "";

        await afterLoginLoad();

        showToast("Вхід виконано успішно.");
    } catch (error) {
        showToast(error.message || "Не вдалося виконати вхід.", true);
    }
}

async function afterLoginLoad() {
    await loadLookups();
    await loadPublicApartments();
    await loadPaymentMethods();
    await loadLoyaltyCards();

    if (!currentUser) {
        return;
    }

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

async function loadLookups() {
    await Promise.all([
        loadHousingTypes(),
        loadTimeUnits()
    ]);
}

async function loadHousingTypes() {
    try {
        const response = await fetch("/api/HousingTypes");

        if (!response.ok) {
            throw new Error();
        }

        housingTypesCache = await response.json();
    } catch {
        housingTypesCache = [
            { id: 1, name: "Квартира" },
            { id: 2, name: "Будинок" },
            { id: 3, name: "Кімната" },
            { id: 4, name: "Апартаменти" }
        ];
    }

    const select = document.getElementById("host-apartment-housing-type");

    if (!select) {
        return;
    }

    select.innerHTML = `<option value="">Оберіть тип житла</option>` +
        housingTypesCache
            .map(type => `<option value="${type.id}">${escapeHtml(type.name)}</option>`)
            .join("");
}

async function loadTimeUnits() {
    try {
        const response = await fetch("/api/TimeUnits");

        if (!response.ok) {
            throw new Error();
        }

        timeUnitsCache = await response.json();
    } catch {
        timeUnitsCache = [
            { id: 1, name: "Доба" },
            { id: 2, name: "Тиждень" },
            { id: 3, name: "Місяць" }
        ];
    }

    const select = document.getElementById("host-apartment-time-unit");

    if (!select) {
        return;
    }

    select.innerHTML = `<option value="">Оберіть період</option>` +
        timeUnitsCache
            .map(unit => `<option value="${unit.id}">${escapeHtml(unit.name)}</option>`)
            .join("");
}

function buildApartmentQuery() {
    const city = document.getElementById("filter-city").value.trim();
    const maxGuests = document.getElementById("filter-guests").value.trim();
    const isActive = document.getElementById("filter-active").value;

    const params = new URLSearchParams();

    if (city) {
        params.append("city", city);
    }

    if (maxGuests) {
        params.append("maxGuests", maxGuests);
    }

    if (isActive !== "") {
        params.append("isActive", isActive);
    }

    return params.toString()
        ? `/api/Apartments?${params.toString()}`
        : "/api/Apartments";
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

function getReviewsForApartment(apartmentId) {
    return reviewsCache.filter(review => review.apartmentId === apartmentId);
}

function renderApartmentReviewsBlock(apartmentId) {
    const reviews = getReviewsForApartment(apartmentId);

    if (!reviews.length) {
        return `
            <div class="apartment-reviews-box">
                <div class="reviews-empty-small">Відгуків ще немає.</div>
            </div>
        `;
    }

    const averageRating = reviews.reduce((sum, review) => sum + Number(review.rating || 0), 0) / reviews.length;

    const latestReviews = reviews.slice(0, 2).map(review => `
        <div class="apartment-review-item">
            <div class="apartment-review-head">
                <strong>${safeText(review.rating)} / 5</strong>
                <span>${safeText(review.author)}</span>
            </div>
            <div class="apartment-review-comment">
                ${safeText(review.comment)}
            </div>
        </div>
    `).join("");

    return `
        <div class="apartment-reviews-box">
            <div class="apartment-reviews-summary">
                <strong>Оцінка: ${averageRating.toFixed(1)} / 5</strong>
                <span>${reviews.length} відгук(ів)</span>
            </div>

            ${latestReviews}
        </div>
    `;
}

function renderPublicApartments(data) {
    const container = document.getElementById("public-apartments-list");

    if (!container) {
        return;
    }

    if (!data.length) {
        container.innerHTML = `<div class="col-12 empty-state">Апартаменти не знайдено.</div>`;
        return;
    }

    container.innerHTML = data.map(apartment => `
        <div class="col-md-6">
            <div class="apartment-card">
                <div class="apartment-image-wrap">
                    <img class="apartment-image"
                         src="${getApartmentImage(apartment)}"
                         alt="Фото житла"
                         onerror="this.src='${DEFAULT_APARTMENT_IMAGE}'">
                </div>

                <div class="apartment-card-body">
                    <div class="apartment-card-header-row">
                        <div>
                            <div class="apartment-title">${safeText(apartment.title)}</div>
                            <div class="apartment-meta">${safeText(apartment.city)}, ${safeText(apartment.address)}</div>
                        </div>
                        <span class="soft-badge">${safeText(apartment.housingType)}</span>
                    </div>

                    <div class="apartment-meta">Гостей: ${safeText(apartment.maxGuests)}</div>
                    <div class="apartment-price">${formatPrice(apartment.price)}</div>
                    ${renderApartmentReviewsBlock(apartment.id)}
                    <div class="apartment-actions">
                        <button class="btn btn-soft btn-sm" onclick="setAvailabilityApartment(${apartment.id})">
                            Перевірити дати
                        </button>

                        ${currentUser && currentUser.roleId === 1
            ? `<button class="btn btn-main btn-sm" onclick="chooseApartmentForReservation(${apartment.id})">Забронювати</button>`
            : ""}
                    </div>
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

function chooseApartmentForReservation(id) {
    const apartment = publicApartmentsCache.find(item => item.id === id);

    document.getElementById("reservation-apartment-id").value = id;
    document.getElementById("reservation-apartment-title").value = apartment
        ? `${apartment.title} — ${apartment.city}`
        : `Апартамент №${id}`;

    openTabByName("guest-reservations");
    showToast("Житло вибрано для бронювання.");
}

function calculateReservationUnits() {
    const startInput = document.getElementById("reservation-start");
    const endInput = document.getElementById("reservation-end");
    const unitsInput = document.getElementById("reservation-units");

    if (!startInput || !endInput || !unitsInput) {
        return;
    }

    const startValue = startInput.value;
    const endValue = endInput.value;

    if (!startValue || !endValue) {
        unitsInput.value = "";
        return;
    }

    const startDate = new Date(startValue);
    const endDate = new Date(endValue);

    if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
        unitsInput.value = "";
        return;
    }

    if (endDate <= startDate) {
        unitsInput.value = "";
        showToast("Дата завершення має бути пізнішою за дату початку.", true);
        return;
    }

    const diffMs = endDate - startDate;
    const oneDayMs = 1000 * 60 * 60 * 24;
    const days = Math.ceil(diffMs / oneDayMs);

    unitsInput.value = days;
}

function setupReservationDateCalculation() {
    document.getElementById("reservation-start")?.addEventListener("change", calculateReservationUnits);
    document.getElementById("reservation-end")?.addEventListener("change", calculateReservationUnits);
}

async function checkAvailability() {
    const apartmentId = parseInt(document.getElementById("availability-id").value, 10);
    const resultBox = document.getElementById("availability-result");

    if (!apartmentId) {
        showToast("Оберіть житло у списку апартаментів.", true);
        return;
    }

    try {
        const response = await fetch(`/api/Apartments/${apartmentId}/availability`);
        const data = await response.json();

        const apartmentTitle = safeText(data.apartmentTitle || "Обране житло");

        if (!data.bookedPeriods || data.bookedPeriods.length === 0) {
            resultBox.innerHTML = `
                <strong>${apartmentTitle}</strong><br>
                <span class="availability-free">Житло доступне до бронювання.</span>
            `;
            return;
        }

        resultBox.innerHTML = `
            <strong>${apartmentTitle}</strong><br>
            <span class="availability-busy">Є заброньовані періоди:</span>
            <div class="availability-periods">
                ${data.bookedPeriods.map(item => `
                    <div>
                        Заброньовано з ${formatDateTime(item.startAt)} по ${formatDateTime(item.endAt)}
                    </div>
                `).join("")}
            </div>
        `;
    } catch {
        resultBox.textContent = "Не вдалося отримати дані про доступність.";
        showToast("Помилка перевірки доступності.", true);
    }
}

async function loadGuestReservations() {
    if (!currentUser || currentUser.roleId !== 1) {
        return;
    }

    try {
        const response = await fetch(`/api/Reservations/guest/${currentUser.id}`);
        const data = await response.json();
        const container = document.getElementById("guest-reservations-list");

        if (!container) {
            return;
        }

        if (!data.length) {
            container.innerHTML = `<div class="empty-state">У вас поки немає бронювань.</div>`;
            return;
        }

        container.innerHTML = data.map(r => {
            const statusText = String(r.status || "").trim().toLowerCase();

            const isPaid =
                statusText === "оплачено" ||
                statusText === "підтверджено" ||
                statusText === "paid" ||
                statusText === "confirmed";

            const servicesHtml = r.services && r.services.length
                ? r.services.map(s => `
                    <span class="service-pill">
                        ${safeText(s.name)} — ${safeText(s.price)} грн
                    </span>
                `).join("")
                : `<span class="text-muted">Без додаткових послуг</span>`;

            const paymentBlock = isPaid
                ? `<div class="payment-done">Оплату виконано</div>`
                : `<button class="btn btn-main btn-sm" onclick="prefillPayment(${r.id}, ${r.totalPrice || 0}, '${String(r.apartment).replaceAll("'", "\\'")}')">
                        Оплатити
                   </button>`;

            const reviewButton = isPaid
                ? `<button class="btn btn-soft btn-sm" onclick="openReviewForm(${r.id}, '${String(r.apartment).replaceAll("'", "\\'")}')">
                        Залишити відгук
                   </button>`
                : "";

            return `
                <div class="info-card">
                    <div class="info-card-header">
                        <div>
                            <div class="info-card-title">${safeText(r.apartment)}</div>
                            <div class="info-card-subtitle">${safeText(r.apartmentCity)}</div>
                        </div>

                        <span class="soft-badge">${safeText(r.status)}</span>
                    </div>

                    <div class="info-card-row">
                        <strong>Період:</strong> ${formatDateTime(r.startAt)} — ${formatDateTime(r.endAt)}
                    </div>

                    <div class="info-card-row">
                        <strong>Кількість днів / одиниць:</strong> ${safeText(r.unitsCount)}
                    </div>

                    <div class="info-card-row">
                        <strong>Додаткові послуги:</strong>
                        <div class="service-pill-list">
                            ${servicesHtml}
                        </div>
                    </div>

                    <div class="info-card-row">
                        <strong>Вартість:</strong> ${safeText(r.totalPrice)} грн
                    </div>

                    <div class="apartment-actions">
                        ${paymentBlock}
                        ${reviewButton}

                        <button class="btn btn-danger-custom btn-sm" onclick="deleteReservation(${r.id})">
                            Видалити
                        </button>
                    </div>
                </div>
            `;
        }).join("");
    } catch {
        showToast("Не вдалося завантажити бронювання орендаря.", true);
    }
}

async function createReservation() {
    if (!currentUser || currentUser.roleId !== 1) {
        showToast("Бронювання доступне тільки орендарю.", true);
        return;
    }

    const createButton = document.getElementById("btn-create-reservation");

    if (createButton) {
        createButton.disabled = true;
        createButton.textContent = "Створення...";
    }

    calculateReservationUnits();

    const apartmentId = parseInt(document.getElementById("reservation-apartment-id").value, 10);
    const startAt = document.getElementById("reservation-start").value;
    const endAt = document.getElementById("reservation-end").value;
    const unitsCount = parseInt(document.getElementById("reservation-units").value, 10);
    const selectedServiceIds = getSelectedReservationServiceIds();

    if (!apartmentId) {
        showToast("Спочатку оберіть житло в каталозі.", true);
        openTabByName("public-search");

        if (createButton) {
            createButton.disabled = false;
            createButton.textContent = "Створити бронювання";
        }

        return;
    }

    if (!startAt || !endAt || !unitsCount) {
        showToast("Заповніть дати та кількість одиниць.", true);

        if (createButton) {
            createButton.disabled = false;
            createButton.textContent = "Створити бронювання";
        }

        return;
    }

    try {
        const response = await fetch("/api/Reservations", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                apartmentId,
                guestId: currentUser.id,
                startAt,
                endAt,
                unitsCount
            })
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Не вдалося створити бронювання.");
        }

        const createdReservation = text ? JSON.parse(text) : null;
        const reservationId = createdReservation?.id || createdReservation?.Id;

        if (reservationId && selectedServiceIds.length) {
            await attachServicesToReservation(reservationId, selectedServiceIds);
        }

        document.getElementById("reservation-apartment-id").value = "";
        document.getElementById("reservation-apartment-title").value = "Спочатку оберіть житло у каталозі";
        document.getElementById("reservation-start").value = "";
        document.getElementById("reservation-end").value = "";
        document.getElementById("reservation-units").value = "";

        clearReservationServiceSelection();

        await loadGuestReservations();

        showToast("Бронювання створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося створити бронювання.", true);
    } finally {
        if (createButton) {
            createButton.disabled = false;
            createButton.textContent = "Створити бронювання";
        }
    }
}

async function cancelReservation(id) {
    if (!confirm("Скасувати обране бронювання?")) {
        return;
    }

    try {
        const response = await fetch(`/api/Reservations/${id}/cancel`, {
            method: "PATCH"
        });

        if (!response.ok) {
            throw new Error();
        }

        await loadGuestReservations();

        showToast("Бронювання скасовано.");
    } catch {
        showToast("Не вдалося скасувати бронювання.", true);
    }
}

async function deleteReservation(id) {
    if (!confirm("Видалити це бронювання?")) {
        return;
    }

    try {
        const response = await fetch(`/api/Reservations/${id}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || "Не вдалося видалити бронювання.");
        }

        await loadGuestReservations();
        showToast("Бронювання видалено.");
    } catch (error) {
        showToast(error.message || "Не вдалося видалити бронювання.", true);
    }
}

async function loadPaymentMethods() {
    try {
        const response = await fetch("/api/Payments/methods");
        const data = await response.json();

        paymentMethodsCache = data;

        const select = document.getElementById("payment-method-id");

        if (!select) {
            return;
        }

        select.innerHTML = data
            .map(m => `<option value="${m.id}">${escapeHtml(m.name)}</option>`)
            .join("");
    } catch {
        paymentMethodsCache = [];
    }
}

function prefillPayment(reservationId, amount, apartmentTitle = "") {
    const originalAmount = Number(amount || 0);
    const userPoints = getCurrentUserPoints();

    selectedPaymentOriginalAmount = originalAmount;

    const maxByPercent = Math.floor(originalAmount * 0.3);
    selectedPaymentMaxPoints = Math.min(userPoints, maxByPercent);
    selectedPaymentPointsToUse = 0;
    selectedPaymentFinalAmount = originalAmount;

    document.getElementById("payment-reservation-id").value = reservationId;
    document.getElementById("payment-amount").value = originalAmount;

    const titleInput = document.getElementById("payment-reservation-title");

    if (titleInput) {
        titleInput.value = apartmentTitle
            ? `${apartmentTitle} — до оплати ${originalAmount} грн`
            : `Бронювання №${reservationId}`;
    }

    const availablePoints = document.getElementById("payment-available-points");
    const pointsInput = document.getElementById("payment-points-to-use");
    const pointsHint = document.getElementById("payment-points-hint");
    const originalAmountElement = document.getElementById("payment-original-amount");
    const discountAmount = document.getElementById("payment-discount-amount");
    const finalAmount = document.getElementById("payment-final-amount");

    if (availablePoints) {
        availablePoints.textContent = userPoints;
    }

    if (pointsInput) {
        pointsInput.value = "0";
        pointsInput.max = selectedPaymentMaxPoints;
    }

    if (pointsHint) {
        pointsHint.textContent = `Можна використати до ${selectedPaymentMaxPoints} бонусів. 1 бонус = 1 грн знижки.`;
    }

    if (originalAmountElement) {
        originalAmountElement.textContent = `${originalAmount} грн`;
    }

    if (discountAmount) {
        discountAmount.textContent = "0 грн";
    }

    if (finalAmount) {
        finalAmount.textContent = `${originalAmount} грн`;
    }
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
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                reservationId,
                paymentMethodId,
                amount,
                currency: "UAH",
                pointsToUse: selectedPaymentPointsToUse
            })
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Не вдалося виконати оплату.");
        }

        document.getElementById("payment-reservation-id").value = "";
        document.getElementById("payment-amount").value = "";

        const paymentTitle = document.getElementById("payment-reservation-title");
        if (paymentTitle) {
            paymentTitle.value = "Оберіть бронювання зі списку";
        }

        resetPaymentBonusFields();

        await loadGuestReservations();
        await loadLoyaltyCards();

        renderGuestLoyaltyCard();

        showToast("Оплату виконано.");
    } catch (error) {
        showToast(error.message || "Не вдалося виконати оплату.", true);
    }
}

async function loadLoyaltyCards() {
    try {
        const response = await fetch("/api/LoyaltyCards");
        loyaltyCardsCache = await response.json();
    } catch {
        loyaltyCardsCache = [];
    }
}

function renderGuestLoyaltyCard() {
    if (!currentUser || currentUser.roleId !== 1) {
        return;
    }

    const card = loyaltyCardsCache.find(c => c.userId === currentUser.id);

    const cardElement = document.getElementById("guest-card-id");
    const pointsElement = document.getElementById("guest-card-points");
    const userElement = document.getElementById("guest-card-user");

    if (cardElement) {
        cardElement.textContent = card ? `№ ${card.id}` : "Не створена";
    }

    if (pointsElement) {
        pointsElement.textContent = card ? card.points : "0";
    }

    if (userElement) {
        userElement.textContent = currentUser.fullName || "Орендар";
    }
}

function getCurrentUserLoyaltyCard() {
    if (!currentUser) {
        return null;
    }

    return loyaltyCardsCache.find(card => card.userId === currentUser.id) || null;
}

function getCurrentUserPoints() {
    const card = getCurrentUserLoyaltyCard();
    return card ? Number(card.points || 0) : 0;
}

function resetPaymentBonusFields() {
    selectedPaymentOriginalAmount = 0;
    selectedPaymentFinalAmount = 0;
    selectedPaymentMaxPoints = 0;
    selectedPaymentPointsToUse = 0;

    const availablePoints = document.getElementById("payment-available-points");
    const pointsInput = document.getElementById("payment-points-to-use");
    const pointsHint = document.getElementById("payment-points-hint");
    const originalAmount = document.getElementById("payment-original-amount");
    const discountAmount = document.getElementById("payment-discount-amount");
    const finalAmount = document.getElementById("payment-final-amount");

    if (availablePoints) availablePoints.textContent = "0";
    if (pointsInput) pointsInput.value = "0";
    if (pointsHint) pointsHint.textContent = "Можна використати до 0 бонусів.";
    if (originalAmount) originalAmount.textContent = "0 грн";
    if (discountAmount) discountAmount.textContent = "0 грн";
    if (finalAmount) finalAmount.textContent = "0 грн";
}

function updatePaymentSummary() {
    const pointsInput = document.getElementById("payment-points-to-use");
    const paymentAmountInput = document.getElementById("payment-amount");

    if (!pointsInput || !paymentAmountInput) {
        return;
    }

    let pointsToUse = parseInt(pointsInput.value, 10);

    if (Number.isNaN(pointsToUse) || pointsToUse < 0) {
        pointsToUse = 0;
    }

    if (pointsToUse > selectedPaymentMaxPoints) {
        pointsToUse = selectedPaymentMaxPoints;
    }

    pointsInput.value = pointsToUse;

    selectedPaymentPointsToUse = pointsToUse;
    selectedPaymentFinalAmount = selectedPaymentOriginalAmount - pointsToUse;

    if (selectedPaymentFinalAmount < 0) {
        selectedPaymentFinalAmount = 0;
    }

    const discountAmount = document.getElementById("payment-discount-amount");
    const finalAmount = document.getElementById("payment-final-amount");

    if (discountAmount) {
        discountAmount.textContent = `${pointsToUse} грн`;
    }

    if (finalAmount) {
        finalAmount.textContent = `${selectedPaymentFinalAmount} грн`;
    }

    paymentAmountInput.value = selectedPaymentFinalAmount;
}

async function loadHostApartments() {
    if (!currentUser || currentUser.roleId !== 2) {
        return;
    }

    try {
        const response = await fetch(`/api/Apartments/host/${currentUser.id}`);
        const data = await response.json();
        const container = document.getElementById("host-apartments-list");

        if (!data.length) {
            container.innerHTML = `<div class="empty-state">Ви ще не створили жодного оголошення.</div>`;
            return;
        }

        container.innerHTML = data.map(a => `
            <div class="apartment-card">
                <div class="apartment-image-wrap">
                    <img class="apartment-image"
                         src="${getApartmentImage(a)}"
                         alt="Фото житла"
                         onerror="this.src='${DEFAULT_APARTMENT_IMAGE}'">
                </div>

                <div class="apartment-card-body">
                    <div class="apartment-card-header-row">
                        <div>
                            <div class="apartment-title">${safeText(a.title)}</div>
                            <div class="apartment-meta">${safeText(a.city)}, ${safeText(a.address)}</div>
                        </div>
                        <span class="soft-badge">${a.isActive ? "Активне" : "Неактивне"}</span>
                    </div>

                    <div class="apartment-meta">Тип: ${safeText(a.housingType)}</div>
                    <div class="apartment-meta">Гостей: ${safeText(a.maxGuests)}</div>
                    <div class="apartment-price">${formatPrice(a.price)}</div>

                    <div class="apartment-actions">
                        <button class="btn btn-soft btn-sm" onclick="editApartment(${a.id})">
                            Редагувати
                        </button>
                        <button class="btn btn-outline-custom btn-sm" onclick="toggleApartment(${a.id})">
                            Змінити статус
                        </button>
                        <button class="btn btn-danger-custom btn-sm" onclick="deleteApartment(${a.id})">
                            Видалити
                        </button>
                    </div>
                </div>
            </div>
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
    const title = document.getElementById("host-apartment-title").value.trim();
    const city = document.getElementById("host-apartment-city").value.trim();
    const address = document.getElementById("host-apartment-address").value.trim();
    const maxGuests = document.getElementById("host-apartment-guests").value;
    const housingTypeId = document.getElementById("host-apartment-housing-type").value;
    const priceAmount = document.getElementById("host-apartment-price").value;
    const currency = document.getElementById("host-apartment-currency").value;
    const timeUnitId = document.getElementById("host-apartment-time-unit").value;
    const imageFile = document.getElementById("host-apartment-image").files[0];

    if (!title || !city || !address || !maxGuests || !housingTypeId || !priceAmount || !timeUnitId) {
        showToast("Заповніть усі основні поля.", true);
        return;
    }

    const formData = new FormData();

    formData.append("Title", title);
    formData.append("City", city);
    formData.append("Address", address);
    formData.append("MaxGuests", maxGuests);
    formData.append("HousingTypeId", housingTypeId);
    formData.append("HostId", currentUser.id);
    formData.append("PriceAmount", priceAmount);
    formData.append("Currency", currency);
    formData.append("TimeUnitId", timeUnitId);

    if (imageFile) {
        formData.append("ImageFile", imageFile);
    }

    try {
        const response = await fetch(editId ? `/api/Apartments/${editId}` : "/api/Apartments", {
            method: editId ? "PUT" : "POST",
            body: formData
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Не вдалося зберегти апартамент.");
        }

        clearHostApartmentForm();

        await loadHostApartments();
        await loadPublicApartments();

        showToast(editId ? "Оголошення оновлено." : "Оголошення створено.");
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
    document.getElementById("host-apartment-price").value = "";
    document.getElementById("host-apartment-currency").value = "UAH";
    document.getElementById("host-apartment-time-unit").value = "";
    document.getElementById("host-apartment-image").value = "";
    document.getElementById("host-apartment-form-title").textContent = "Додавання апартаменту";
}

async function editApartment(id) {
    try {
        const response = await fetch(`/api/Apartments/${id}`);
        const data = await response.json();
        const currentPricing = data.pricings && data.pricings.length ? data.pricings[0] : null;

        document.getElementById("host-apartment-edit-id").value = data.id;
        document.getElementById("host-apartment-title").value = data.title ?? "";
        document.getElementById("host-apartment-city").value = data.city ?? "";
        document.getElementById("host-apartment-address").value = data.address ?? "";
        document.getElementById("host-apartment-guests").value = data.maxGuests ?? "";
        document.getElementById("host-apartment-housing-type").value = data.housingTypeId ?? "";
        document.getElementById("host-apartment-price").value = currentPricing?.amount ?? "";
        document.getElementById("host-apartment-currency").value = currentPricing?.currency ?? "UAH";
        document.getElementById("host-apartment-time-unit").value = currentPricing?.timeUnitId ?? "";
        document.getElementById("host-apartment-form-title").textContent = "Редагування апартаменту";

        showToast("Дані оголошення завантажено у форму.");
    } catch {
        showToast("Не вдалося завантажити дані апартаменту.", true);
    }
}

async function deleteApartment(id) {
    if (!confirm("Видалити обране оголошення?")) {
        return;
    }

    try {
        const response = await fetch(`/api/Apartments/${id}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            throw new Error();
        }

        await loadHostApartments();
        await loadPublicApartments();

        showToast("Оголошення видалено.");
    } catch {
        showToast("Не вдалося видалити апартамент.", true);
    }
}

async function toggleApartment(id) {
    try {
        const response = await fetch(`/api/Apartments/${id}/toggle`, {
            method: "PATCH"
        });

        if (!response.ok) {
            throw new Error();
        }

        await loadHostApartments();
        await loadPublicApartments();

        showToast("Статус оголошення змінено.");
    } catch {
        showToast("Не вдалося змінити статус апартаменту.", true);
    }
}

async function loadHostReservations() {
    if (!currentUser || currentUser.roleId !== 2) {
        return;
    }

    try {
        const response = await fetch(`/api/Reservations/host/${currentUser.id}`);
        const data = await response.json();
        const container = document.getElementById("host-reservations-list");

        if (!data.length) {
            container.innerHTML = `<div class="empty-state">Бронювання відсутні.</div>`;
            return;
        }

        container.innerHTML = data.map(r => `
            <div class="info-card">
                <div class="info-card-header">
                    <div class="info-card-title">${safeText(r.apartment)}</div>
                    <span class="soft-badge">${safeText(r.status)}</span>
                </div>

                <div class="info-card-row"><strong>Орендар:</strong> ${safeText(r.guest)}</div>
                <div class="info-card-row"><strong>Телефон:</strong> ${safeText(r.guestPhone)}</div>
                <div class="info-card-row"><strong>Період:</strong> ${formatDateTime(r.startAt)} — ${formatDateTime(r.endAt)}</div>
                <div class="info-card-row"><strong>Вартість:</strong> ${safeText(r.totalPrice)}</div>
            </div>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити бронювання власника.", true);
    }
}

async function loadServicesForHost() {
    try {
        const response = await fetch("/api/AdditionalServices");
        const data = await response.json();
        const container = document.getElementById("host-services-list");

        if (!data.length) {
            container.innerHTML = `<div class="empty-state">Додаткові послуги відсутні.</div>`;
            return;
        }

        container.innerHTML = data.map(service => `
            <div class="info-card">
                <div class="info-card-header">
                    <div class="info-card-title">${safeText(service.name)}</div>
                    <div class="apartment-price">${safeText(service.price)} грн</div>
                </div>

                <div class="apartment-actions">
                    <button class="btn btn-soft btn-sm"
                        onclick="editService(${service.id}, '${String(service.name).replaceAll("'", "\\'")}', ${service.price})">
                        Редагувати
                    </button>

                    <button class="btn btn-danger-custom btn-sm" onclick="deleteService(${service.id})">
                        Видалити
                    </button>
                </div>
            </div>
        `).join("");
    } catch {
        showToast("Не вдалося завантажити список послуг.", true);
    }
}

async function loadServicesForGuest() {
    try {
        const response = await fetch("/api/AdditionalServices");
        const data = await response.json();

        guestServicesCache = data;

        const container = document.getElementById("reservation-services-list");

        if (!container) {
            return;
        }

        if (!data.length) {
            container.innerHTML = `<div class="text-muted">Додаткові послуги відсутні.</div>`;
            updateReservationServicesTotal();
            return;
        }

        container.innerHTML = data.map(service => `
            <label class="service-choice-item">
                <input 
                    type="checkbox" 
                    class="reservation-service-checkbox" 
                    value="${service.id}" 
                    data-price="${service.price}"
                    onchange="updateReservationServicesTotal()">

                <span class="service-choice-content">
                    <span class="service-choice-name">${safeText(service.name)}</span>
                    <span class="service-choice-price">${safeText(service.price)} грн</span>
                </span>
            </label>
        `).join("");

        updateReservationServicesTotal();
    } catch {
        showToast("Не вдалося завантажити додаткові послуги.", true);
    }
}

function getSelectedReservationServiceIds() {
    return Array.from(document.querySelectorAll(".reservation-service-checkbox:checked"))
        .map(input => parseInt(input.value, 10));
}

function updateReservationServicesTotal() {
    const totalElement = document.getElementById("reservation-services-total");

    if (!totalElement) {
        return;
    }

    const total = Array.from(document.querySelectorAll(".reservation-service-checkbox:checked"))
        .reduce((sum, input) => sum + Number(input.dataset.price || 0), 0);

    totalElement.textContent = `${total} грн`;
}

function clearReservationServiceSelection() {
    document.querySelectorAll(".reservation-service-checkbox").forEach(input => {
        input.checked = false;
    });

    updateReservationServicesTotal();
}

async function attachServicesToReservation(reservationId, serviceIds) {
    if (!serviceIds.length) {
        return;
    }

    for (const serviceId of serviceIds) {
        const response = await fetch("/api/ReservationServices", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                reservationId: reservationId,
                serviceId: serviceId
            })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || "Не вдалося додати послуги до бронювання.");
        }
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
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(
                id
                    ? { id: parseInt(id, 10), name, price }
                    : { name, price }
            )
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Не вдалося зберегти послугу.");
        }

        clearServiceForm();

        await loadServicesForHost();
        await loadServicesForGuest();

        showToast(id ? "Послугу оновлено." : "Послугу створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося зберегти послугу.", true);
    }
}

async function deleteService(id) {
    if (!confirm("Видалити обрану послугу?")) {
        return;
    }

    try {
        const response = await fetch(`/api/AdditionalServices/${id}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            throw new Error();
        }

        await loadServicesForHost();
        await loadServicesForGuest();

        showToast("Послугу видалено.");
    } catch {
        showToast("Не вдалося видалити послугу.", true);
    }
}

async function loadReviews() {
    try {
        const response = await fetch("/api/Reviews");
        const data = await response.json();

        reviewsCache = data;

        const container = document.getElementById("reviews-list");

        if (container) {
            if (!data.length) {
                container.innerHTML = `
                    <div class="empty-state">
                        Відгуків поки немає.
                    </div>
                `;
            } else {
                container.innerHTML = data.map(r => {
                    const rating = Number(r.rating || 0);
                    const stars = "★".repeat(rating) + "☆".repeat(5 - rating);

                    return `
                        <div class="review-card">
                            <div class="review-card-header">
                                <div>
                                    <div class="review-apartment">
                                        ${safeText(r.apartment)}, ${safeText(r.city)}
                                    </div>
                                    <div class="review-author">
                                        Автор: ${safeText(r.author)}
                                    </div>
                                </div>

                                <div class="review-rating">
                                    <div class="review-stars">${stars}</div>
                                    <strong>${safeText(r.rating)} / 5</strong>
                                </div>
                            </div>

                            <div class="review-comment">
                                ${safeText(r.comment)}
                            </div>

                            <div class="review-date">
                                ${formatDateTime(r.createdAt)}
                            </div>
                        </div>
                    `;
                }).join("");
            }
        }

        if (publicApartmentsCache.length) {
            renderPublicApartments(publicApartmentsCache);
        }
    } catch {
        showToast("Не вдалося завантажити відгуки.", true);
    }
}

function openReviewForm(reservationId, apartmentTitle) {
    document.getElementById("review-reservation-id").value = reservationId;

    const titleInput = document.getElementById("review-reservation-title");
    if (titleInput) {
        titleInput.value = `${apartmentTitle} — бронювання вибрано`;
    }

    document.getElementById("review-rating").value = "";
    document.getElementById("review-comment").value = "";

    openTabByName("reviews");
    showToast("Бронювання вибрано для відгуку.");
}

async function createReview() {
    if (!currentUser || currentUser.roleId !== 1) {
        showToast("Відгук може створювати тільки орендар.", true);
        return;
    }

    const reservationId = parseInt(document.getElementById("review-reservation-id").value, 10);
    const rating = parseInt(document.getElementById("review-rating").value, 10);
    const comment = document.getElementById("review-comment").value.trim();

    if (!reservationId) {
        showToast("Спочатку оберіть бронювання у вкладці “Мої бронювання”.", true);
        openTabByName("guest-reservations");
        return;
    }

    if (!rating || rating < 1 || rating > 5) {
        showToast("Оцінка має бути від 1 до 5.", true);
        return;
    }

    if (!comment) {
        showToast("Напишіть коментар до відгуку.", true);
        return;
    }

    try {
        const response = await fetch("/api/Reviews", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                reservationId,
                authorId: currentUser.id,
                rating,
                comment
            })
        });

        const text = await response.text();

        if (!response.ok) {
            throw new Error(text || "Не вдалося створити відгук.");
        }

        document.getElementById("review-reservation-id").value = "";

        const titleInput = document.getElementById("review-reservation-title");
        if (titleInput) {
            titleInput.value = "Оберіть бронювання у вкладці “Мої бронювання”";
        }

        document.getElementById("review-rating").value = "";
        document.getElementById("review-comment").value = "";

        await loadReviews();
        await loadPublicApartments();

        showToast("Відгук створено.");
    } catch (error) {
        showToast(error.message || "Не вдалося створити відгук.", true);
    }
}

async function initializeApp() {
    loadSession();

    setupReservationDateCalculation();

    await loadLookups();

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