using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;

namespace ApartmentRentalSystem.WebMVC.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApartmentContext _context;

        public ReservationsController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var isOwner = User.IsInRole("Owner");
            ViewBag.IsOwner = isOwner;

            if (isOwner)
            {
                ViewBag.PageTitle = "Бронювання";
                ViewBag.PageSubtitle = "Переглядайте запити на оренду ваших об'єктів та контролюйте їхній статус.";
                ViewBag.EmptyTitle = "Поки що немає бронювань";
                ViewBag.EmptySubtitle = "Коли гості почнуть надсилати запити на оренду ваших об'єктів, вони з’являться тут.";
            }
            else
            {
                ViewBag.PageTitle = "Мої поїздки";
                ViewBag.PageSubtitle = "Переглядайте свої активні та завершені бронювання.";
                ViewBag.EmptyTitle = "У вас поки що немає бронювань";
                ViewBag.EmptySubtitle = "Коли ви забронюєте житло, воно з’явиться тут.";
            }

            var reservations = await GetVisibleReservationsQuery()
                .OrderByDescending(r => r.StartAt)
                .ToListAsync();

            return View(reservations);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Details", "Reservations", new { id });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            var reservation = await FindVisibleReservationAsync(id.Value);
            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create(int? apartmentId, DateTime? startAt)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Create", "Reservations", new { apartmentId, startAt = startAt?.ToString("yyyy-MM-dd") });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            ViewData["UnitId"] = new SelectList(
                _context.TimeUnits.OrderBy(t => t.Name),
                "Id",
                "Name"
            );

            if (apartmentId.HasValue)
            {
                var apartments = await _context.Apartments
                    .Where(a => a.IsActive && a.Id == apartmentId.Value)
                    .OrderBy(a => a.Title)
                    .ToListAsync();

                ViewData["ApartmentId"] = new SelectList(apartments, "Id", "Title", apartmentId.Value);
            }
            else
            {
                var apartments = await _context.Apartments
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.Title)
                    .ToListAsync();

                ViewData["ApartmentId"] = new SelectList(apartments, "Id", "Title");
            }

            var effectiveStart = startAt.HasValue && startAt.Value.Date > DateTime.Today
                ? startAt.Value.Date
                : DateTime.Today;

            var reservation = new Reservation
            {
                StartAt = effectiveStart,
                EndAt = effectiveStart.AddDays(1)
            };

            return View(reservation);
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApartmentId,UnitId,UnitsCount,StartAt,EndAt,Id")] Reservation reservation)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Create", "Reservations", new
                {
                    apartmentId = reservation.ApartmentId,
                    startAt = reservation.StartAt.ToString("yyyy-MM-dd")
                });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            var currentGuest = await GetCurrentGuestAsync();
            if (currentGuest == null)
            {
                ModelState.AddModelError(string.Empty, "Не вдалося визначити поточного користувача. Перевірте, чи існує запис користувача у таблиці Users.");
            }
            else
            {
                reservation.GuestId = currentGuest.Id;
            }

            var pendingStatus = await GetPendingStatusAsync();
            if (pendingStatus == null)
            {
                ModelState.AddModelError(string.Empty, "У системі відсутній статус для нового бронювання. Додайте статус 'Очікує підтвердження' у таблицю ReservationStatuses.");
            }
            else
            {
                reservation.StatusId = pendingStatus.Id;
            }

            var apartment = await _context.Apartments
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                .FirstOrDefaultAsync(a => a.Id == reservation.ApartmentId && a.IsActive);

            if (apartment == null)
                ModelState.AddModelError(nameof(reservation.ApartmentId), "Обране житло недоступне.");

            if (reservation.EndAt < reservation.StartAt)
                ModelState.AddModelError(nameof(reservation.EndAt), "Дата виїзду не може бути раніше дати заїзду.");

            if (reservation.UnitId <= 0)
                ModelState.AddModelError(nameof(reservation.UnitId), "Оберіть період бронювання.");

            if (reservation.UnitsCount <= 0)
                ModelState.AddModelError(nameof(reservation.UnitsCount), "Кількість має бути більшою за 0.");

            if (reservation.StartAt.Date < DateTime.Today)
                ModelState.AddModelError(nameof(reservation.StartAt), "Дата заїзду не може бути раніше сьогоднішньої дати.");

            if (apartment != null)
            {
                var overlappingConfirmedReservation = await _context.Reservations
                    .Include(r => r.Status)
                    .Where(r =>
                        r.ApartmentId == reservation.ApartmentId &&
                        r.Status != null &&
                        r.Status.Name == "Підтверджено" &&
                        reservation.StartAt.Date <= r.EndAt.Date &&
                        reservation.EndAt.Date >= r.StartAt.Date)
                    .OrderBy(r => r.StartAt)
                    .FirstOrDefaultAsync();

                if (overlappingConfirmedReservation != null)
                {
                    var availableFrom = overlappingConfirmedReservation.EndAt.AddDays(1);
                    ModelState.AddModelError(
                        string.Empty,
                        $"Обране житло вже заброньоване на цей період. Найближча доступна дата: {availableFrom:dd.MM.yyyy}."
                    );
                }
            }

            ModelState.Remove(nameof(Reservation.Apartment));
            ModelState.Remove(nameof(Reservation.Guest));
            ModelState.Remove(nameof(Reservation.Status));
            ModelState.Remove(nameof(Reservation.TimeUnit));
            ModelState.Remove(nameof(Reservation.Payment));
            ModelState.Remove(nameof(Reservation.Histories));

            if (ModelState.IsValid && apartment != null)
            {
                try
                {
                    ApplyPricingSnapshot(reservation, apartment);

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    var dbMessage = ex.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError(string.Empty, "Помилка БД: " + dbMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Загальна помилка: " + ex.Message);
                }
            }

            await PrepareCreateSelectListsAsync(reservation.ApartmentId, reservation.UnitId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Edit", "Reservations", new { id });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            var reservation = await FindVisibleReservationAsync(id.Value);
            if (reservation == null)
                return NotFound();

            await PrepareEditSelectListsAsync(reservation);
            ViewBag.IsOwner = false;

            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApartmentId,GuestId,StatusId,UnitId,UnitsCount,StartAt,EndAt,PriceTypeIdSnapshot,UnitAmountSnapshot,CurrencySnapshot,TotalPrice,Id")] Reservation reservation)
        {
            if (id != reservation.Id)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Edit", "Reservations", new { id = reservation.Id });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            var existingReservation = await _context.Reservations
                .Include(r => r.Apartment)
                .Include(r => r.Guest)
                .Include(r => r.Status)
                .Include(r => r.TimeUnit)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReservation == null)
                return NotFound();

            if (!await CanCurrentUserAccessReservationAsync(existingReservation))
                return Forbid();

            if (reservation.EndAt < reservation.StartAt)
                ModelState.AddModelError(nameof(reservation.EndAt), "Дата виїзду не може бути раніше дати заїзду.");

            if (reservation.UnitId <= 0)
                ModelState.AddModelError(nameof(reservation.UnitId), "Оберіть період бронювання.");

            if (reservation.UnitsCount <= 0)
                ModelState.AddModelError(nameof(reservation.UnitsCount), "Кількість має бути більшою за 0.");

            if (reservation.StartAt.Date < DateTime.Today)
                ModelState.AddModelError(nameof(reservation.StartAt), "Дата заїзду не може бути раніше сьогоднішньої дати.");

            var apartment = await _context.Apartments
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                .FirstOrDefaultAsync(a => a.Id == reservation.ApartmentId && a.IsActive);

            if (apartment == null)
                ModelState.AddModelError(nameof(reservation.ApartmentId), "Обране житло недоступне.");

            if (apartment != null)
            {
                var overlappingConfirmedReservation = await _context.Reservations
                    .Include(r => r.Status)
                    .Where(r =>
                        r.Id != reservation.Id &&
                        r.ApartmentId == reservation.ApartmentId &&
                        r.Status != null &&
                        r.Status.Name == "Підтверджено" &&
                        reservation.StartAt.Date <= r.EndAt.Date &&
                        reservation.EndAt.Date >= r.StartAt.Date)
                    .OrderBy(r => r.StartAt)
                    .FirstOrDefaultAsync();

                if (overlappingConfirmedReservation != null)
                {
                    var availableFrom = overlappingConfirmedReservation.EndAt.AddDays(1);
                    ModelState.AddModelError(
                        string.Empty,
                        $"Обране житло вже заброньоване на цей період. Найближча доступна дата: {availableFrom:dd.MM.yyyy}."
                    );
                }
            }

            ModelState.Remove(nameof(Reservation.Apartment));
            ModelState.Remove(nameof(Reservation.Guest));
            ModelState.Remove(nameof(Reservation.Status));
            ModelState.Remove(nameof(Reservation.TimeUnit));
            ModelState.Remove(nameof(Reservation.Payment));
            ModelState.Remove(nameof(Reservation.Histories));

            if (!ModelState.IsValid)
            {
                await PrepareEditSelectListsAsync(reservation);
                ViewBag.IsOwner = false;
                return View(reservation);
            }

            try
            {
                existingReservation.ApartmentId = reservation.ApartmentId;
                existingReservation.UnitId = reservation.UnitId;
                existingReservation.UnitsCount = reservation.UnitsCount;
                existingReservation.StartAt = reservation.StartAt;
                existingReservation.EndAt = reservation.EndAt;

                if (apartment != null)
                {
                    ApplyPricingSnapshot(existingReservation, apartment);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Reservations.AnyAsync(e => e.Id == reservation.Id))
                    return NotFound();

                throw;
            }
        }

        // POST: Reservations/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Owner"))
                return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reservation.Apartment.HostId != currentUserId)
                return Forbid();

            var confirmedStatus = await _context.ReservationStatuses
                .FirstOrDefaultAsync(s => s.Name == "Підтверджено");

            if (confirmedStatus == null)
                return NotFound();

            reservation.StatusId = confirmedStatus.Id;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Reservations/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Owner"))
                return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reservation.Apartment.HostId != currentUserId)
                return Forbid();

            var cancelledStatus = await _context.ReservationStatuses
                .FirstOrDefaultAsync(s => s.Name == "Скасовано");

            if (cancelledStatus == null)
                return NotFound();

            reservation.StatusId = cancelledStatus.Id;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Delete", "Reservations", new { id });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            var reservation = await FindVisibleReservationAsync(id.Value);
            if (reservation == null)
                return NotFound();

            ViewBag.IsOwner = false;
            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Delete", "Reservations", new { id });
                return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }

            if (User.IsInRole("Owner"))
                return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Apartment)
                .Include(r => r.Guest)
                .Include(r => r.Status)
                .Include(r => r.TimeUnit)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            if (!await CanCurrentUserAccessReservationAsync(reservation))
                return Forbid();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private IQueryable<Reservation> GetVisibleReservationsQuery()
        {
            var query = _context.Reservations
                .Include(r => r.Apartment)
                .Include(r => r.Guest)
                .Include(r => r.Status)
                .Include(r => r.TimeUnit)
                .AsQueryable();

            var isOwner = User.Identity?.IsAuthenticated == true && User.IsInRole("Owner");
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserEmail = User.Identity?.Name;
            var today = DateTime.Today;

            if (isOwner && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(r =>
                    r.Apartment.HostId == currentUserId &&
                    (
                        r.Status.Name == "Очікує підтвердження" ||
                        (r.Status.Name == "Підтверджено" && r.EndAt.Date >= today)
                    ));
            }
            else if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(currentUserEmail))
            {
                query = query.Where(r => r.Guest.Email == currentUserEmail);
            }
            else
            {
                query = query.Where(r => false);
            }

            return query;
        }

        private async Task<Reservation?> FindVisibleReservationAsync(int id)
        {
            return await GetVisibleReservationsQuery()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        private async Task<bool> CanCurrentUserAccessReservationAsync(Reservation reservation)
        {
            var isOwner = User.Identity?.IsAuthenticated == true && User.IsInRole("Owner");
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserEmail = User.Identity?.Name;

            if (isOwner && !string.IsNullOrEmpty(currentUserId))
            {
                return await _context.Reservations
                    .AnyAsync(r => r.Id == reservation.Id && r.Apartment.HostId == currentUserId);
            }

            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(currentUserEmail))
            {
                return await _context.Reservations
                    .AnyAsync(r => r.Id == reservation.Id && r.Guest.Email == currentUserEmail);
            }

            return false;
        }

        private async Task<User?> GetCurrentGuestAsync()
        {
            var currentUserEmail = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentUserEmail))
                return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
        }

        private async Task<ReservationStatus?> GetPendingStatusAsync()
        {
            return await _context.ReservationStatuses.FirstOrDefaultAsync(s =>
                s.Name == "Очікує підтвердження" ||
                s.Name == "Очікує" ||
                s.Name == "Pending");
        }

        private async Task PrepareCreateSelectListsAsync(int? apartmentId = null, int? unitId = null)
        {
            var apartments = await _context.Apartments
                .Where(a => a.IsActive)
                .OrderBy(a => a.Title)
                .ToListAsync();

            var timeUnits = await _context.TimeUnits
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewData["ApartmentId"] = new SelectList(apartments, "Id", "Title", apartmentId);
            ViewData["UnitId"] = new SelectList(timeUnits, "Id", "Name", unitId);
        }

        private async Task PrepareEditSelectListsAsync(Reservation? reservation = null)
        {
            var apartments = await _context.Apartments
                .Where(a => a.IsActive)
                .OrderBy(a => a.Title)
                .ToListAsync();

            var statuses = await _context.ReservationStatuses
                .OrderBy(s => s.Name)
                .ToListAsync();

            var timeUnits = await _context.TimeUnits
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewData["ApartmentId"] = new SelectList(apartments, "Id", "Title", reservation?.ApartmentId);
            ViewData["StatusId"] = new SelectList(statuses, "Id", "Name", reservation?.StatusId);
            ViewData["UnitId"] = new SelectList(timeUnits, "Id", "Name", reservation?.UnitId);
        }

        private static void ApplyPricingSnapshot(Reservation reservation, Apartment apartment)
        {
            var currentPricing = apartment.Pricings
                .OrderByDescending(p => p.ValidFrom)
                .FirstOrDefault();

            if (currentPricing != null)
            {
                reservation.PriceTypeIdSnapshot = currentPricing.PriceTypeId;
                reservation.UnitAmountSnapshot = currentPricing.Amount;
                reservation.CurrencySnapshot = currentPricing.Currency;
                reservation.TotalPrice = currentPricing.Amount * reservation.UnitsCount;
            }
            else
            {
                reservation.PriceTypeIdSnapshot = null;
                reservation.UnitAmountSnapshot = null;
                reservation.CurrencySnapshot = null;
                reservation.TotalPrice = null;
            }
        }
    }
}