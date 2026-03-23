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
    public class ApartmentsController : Controller
    {
        private readonly ApartmentContext _context;

        public ApartmentsController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: Apartments
        // GET: Apartments
        // Додали параметр housingTypeId для фільтрації
        public async Task<IActionResult> Index(int? housingTypeId)
        {
            var apartmentsQuery = _context.Apartments
                .Include(a => a.Host)
                .Include(a => a.HousingType)
                .AsQueryable(); 

            if (housingTypeId != null)
            {
                apartmentsQuery = apartmentsQuery.Where(a => a.HousingTypeId == housingTypeId);
            }

            return View(await apartmentsQuery.ToListAsync());
        }

        // GET: Apartments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment = await _context.Apartments
                .Include(a => a.Host)
                .Include(a => a.HousingType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apartment == null)
            {
                return NotFound();
            }

            return View(apartment);
        }

        // GET: Apartments/Create
        public IActionResult Create()
        {
            ViewData["HostId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name");
            return View();
        }

        // POST: Apartments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HostId,HousingTypeId,Title,City,Address,MaxGuests,IsActive,Id")] Apartment apartment)
        {
            ModelState.Remove("Host");
            ModelState.Remove("HousingType");

            if (ModelState.IsValid)
            {
                _context.Add(apartment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HostId"] = new SelectList(_context.Users, "Id", "Email", apartment.HostId);
            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", apartment.HousingTypeId);
            return View(apartment);
        }

        // GET: Apartments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment == null)
            {
                return NotFound();
            }
            ViewData["HostId"] = new SelectList(_context.Users, "Id", "Email", apartment.HostId);
            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", apartment.HousingTypeId);
            return View(apartment);
        }

        // POST: Apartments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HostId,HousingTypeId,Title,City,Address,MaxGuests,IsActive,Id")] Apartment apartment)
        {
            if (id != apartment.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Host");
            ModelState.Remove("HousingType");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(apartment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApartmentExists(apartment.Id))
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
            ViewData["HostId"] = new SelectList(_context.Users, "Id", "Email", apartment.HostId);
            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", apartment.HousingTypeId);
            return View(apartment);
        }

        // GET: Apartments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment = await _context.Apartments
                .Include(a => a.Host)
                .Include(a => a.HousingType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apartment == null)
            {
                return NotFound();
            }

            return View(apartment);
        }

        // POST: Apartments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment != null)
            {
                _context.Apartments.Remove(apartment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.Id == id);
        }
    }
}
