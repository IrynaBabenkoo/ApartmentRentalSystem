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
    public class ReservationHistoriesController : Controller
    {
        private readonly ApartmentContext _context;

        public ReservationHistoriesController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: ReservationHistories
        public async Task<IActionResult> Index()
        {
            var apartmentContext = _context.ReservationHistories.Include(r => r.ChangedBy).Include(r => r.Reservation);
            return View(await apartmentContext.ToListAsync());
        }

        // GET: ReservationHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationHistory = await _context.ReservationHistories
                .Include(r => r.ChangedBy)
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservationHistory == null)
            {
                return NotFound();
            }

            return View(reservationHistory);
        }

        // GET: ReservationHistories/Create
        public IActionResult Create()
        {
            ViewData["ChangedById"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id");
            return View();
        }

        // POST: ReservationHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,ChangedById,ChangeType,Note,ChangedAt,Id")] ReservationHistory reservationHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reservationHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ChangedById"] = new SelectList(_context.Users, "Id", "Email", reservationHistory.ChangedById);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationHistory.ReservationId);
            return View(reservationHistory);
        }

        // GET: ReservationHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationHistory = await _context.ReservationHistories.FindAsync(id);
            if (reservationHistory == null)
            {
                return NotFound();
            }
            ViewData["ChangedById"] = new SelectList(_context.Users, "Id", "Email", reservationHistory.ChangedById);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationHistory.ReservationId);
            return View(reservationHistory);
        }

        // POST: ReservationHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,ChangedById,ChangeType,Note,ChangedAt,Id")] ReservationHistory reservationHistory)
        {
            if (id != reservationHistory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservationHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationHistoryExists(reservationHistory.Id))
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
            ViewData["ChangedById"] = new SelectList(_context.Users, "Id", "Email", reservationHistory.ChangedById);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationHistory.ReservationId);
            return View(reservationHistory);
        }

        // GET: ReservationHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationHistory = await _context.ReservationHistories
                .Include(r => r.ChangedBy)
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservationHistory == null)
            {
                return NotFound();
            }

            return View(reservationHistory);
        }

        // POST: ReservationHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservationHistory = await _context.ReservationHistories.FindAsync(id);
            if (reservationHistory != null)
            {
                _context.ReservationHistories.Remove(reservationHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationHistoryExists(int id)
        {
            return _context.ReservationHistories.Any(e => e.Id == id);
        }
    }
}
