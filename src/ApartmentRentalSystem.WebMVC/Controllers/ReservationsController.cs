using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var apartmentContext = _context.Reservations.Include(r => r.Apartment).Include(r => r.Guest).Include(r => r.Status).Include(r => r.TimeUnit);
            return View(await apartmentContext.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Apartment)
                .Include(r => r.Guest)
                .Include(r => r.Status)
                .Include(r => r.TimeUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address");
            ViewData["GuestId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["StatusId"] = new SelectList(_context.ReservationStatuses, "Id", "Name");
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name");
            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApartmentId,GuestId,StatusId,UnitId,UnitsCount,StartAt,EndAt,PriceTypeIdSnapshot,UnitAmountSnapshot,CurrencySnapshot,TotalPrice,Id")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", reservation.ApartmentId);
            ViewData["GuestId"] = new SelectList(_context.Users, "Id", "Email", reservation.GuestId);
            ViewData["StatusId"] = new SelectList(_context.ReservationStatuses, "Id", "Name", reservation.StatusId);
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", reservation.UnitId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", reservation.ApartmentId);
            ViewData["GuestId"] = new SelectList(_context.Users, "Id", "Email", reservation.GuestId);
            ViewData["StatusId"] = new SelectList(_context.ReservationStatuses, "Id", "Name", reservation.StatusId);
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", reservation.UnitId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApartmentId,GuestId,StatusId,UnitId,UnitsCount,StartAt,EndAt,PriceTypeIdSnapshot,UnitAmountSnapshot,CurrencySnapshot,TotalPrice,Id")] Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", reservation.ApartmentId);
            ViewData["GuestId"] = new SelectList(_context.Users, "Id", "Email", reservation.GuestId);
            ViewData["StatusId"] = new SelectList(_context.ReservationStatuses, "Id", "Name", reservation.StatusId);
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", reservation.UnitId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Apartment)
                .Include(r => r.Guest)
                .Include(r => r.Status)
                .Include(r => r.TimeUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
