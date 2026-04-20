using ApartmentRentalSystem.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Infrastructure;
using System.Security.Claims;

namespace ApartmentRentalSystem.WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApartmentContext _context;

        public HomeController(ILogger<HomeController> logger, ApartmentContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var isOwner = User.Identity?.IsAuthenticated == true && User.IsInRole("Owner");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isOwner && !string.IsNullOrEmpty(userId))
            {
                var ownerApartments = await _context.Apartments
                    .Include(a => a.HousingType)
                    .Include(a => a.Pricings)
                        .ThenInclude(p => p.PriceType)
                            .ThenInclude(pt => pt.TimeUnit)
                    .Where(a => a.HostId == userId)
                    .OrderByDescending(a => a.Id)
                    .Take(3)
                    .ToListAsync();

                ViewBag.TotalApartments = await _context.Apartments
                    .CountAsync(a => a.HostId == userId);

                ViewBag.ActiveReservations = await _context.Reservations
                    .CountAsync(r =>
                        r.Apartment.HostId == userId &&
                        r.Status != null &&
                        r.Status.Name != "Скасовано");

                ViewBag.PendingReservations = await _context.Reservations
                    .CountAsync(r =>
                        r.Apartment.HostId == userId &&
                        r.Status != null &&
                        r.Status.Name == "Очікує підтвердження");

                return View(ownerApartments);
            }

            var latestApartments = await _context.Apartments
                .Include(a => a.HousingType)
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                        .ThenInclude(pt => pt.TimeUnit)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Id)
                .Take(6)
                .ToListAsync();

            return View(latestApartments);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}