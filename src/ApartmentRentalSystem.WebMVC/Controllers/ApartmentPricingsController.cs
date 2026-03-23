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
    public class ApartmentPricingsController : Controller
    {
        private readonly ApartmentContext _context;

        public ApartmentPricingsController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: ApartmentPricings
        public async Task<IActionResult> Index()
        {
            var apartmentContext = _context.ApartmentPricings.Include(a => a.Apartment).Include(a => a.PriceType);
            return View(await apartmentContext.ToListAsync());
        }

        // GET: ApartmentPricings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartmentPricing = await _context.ApartmentPricings
                .Include(a => a.Apartment)
                .Include(a => a.PriceType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apartmentPricing == null)
            {
                return NotFound();
            }

            return View(apartmentPricing);
        }

        // GET: ApartmentPricings/Create
        public IActionResult Create()
        {
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address");
            ViewData["PriceTypeId"] = new SelectList(_context.PriceTypes, "Id", "Name");
            return View();
        }

        // POST: ApartmentPricings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApartmentId,PriceTypeId,Amount,Currency,ValidFrom,ValidTo,Id")] ApartmentPricing apartmentPricing)
        {
            ModelState.Remove("Apartment");
            ModelState.Remove("PriceType");
            if (ModelState.IsValid)
            {
                apartmentPricing.ValidFrom = DateTime.SpecifyKind((DateTime)apartmentPricing.ValidFrom, DateTimeKind.Utc);
                apartmentPricing.ValidTo = DateTime.SpecifyKind((DateTime)apartmentPricing.ValidTo, DateTimeKind.Utc);

                _context.Add(apartmentPricing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", apartmentPricing.ApartmentId);
            ViewData["PriceTypeId"] = new SelectList(_context.PriceTypes, "Id", "Name", apartmentPricing.PriceTypeId);
            return View(apartmentPricing);
        }

        // GET: ApartmentPricings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartmentPricing = await _context.ApartmentPricings.FindAsync(id);
            if (apartmentPricing == null)
            {
                return NotFound();
            }
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", apartmentPricing.ApartmentId);
            ViewData["PriceTypeId"] = new SelectList(_context.PriceTypes, "Id", "Name", apartmentPricing.PriceTypeId);
            return View(apartmentPricing);
        }

        // POST: ApartmentPricings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApartmentId,PriceTypeId,Amount,Currency,ValidFrom,ValidTo,Id")] ApartmentPricing apartmentPricing)
        {
            if (id != apartmentPricing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(apartmentPricing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApartmentPricingExists(apartmentPricing.Id))
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
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "Address", apartmentPricing.ApartmentId);
            ViewData["PriceTypeId"] = new SelectList(_context.PriceTypes, "Id", "Name", apartmentPricing.PriceTypeId);
            return View(apartmentPricing);
        }

        // GET: ApartmentPricings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartmentPricing = await _context.ApartmentPricings
                .Include(a => a.Apartment)
                .Include(a => a.PriceType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apartmentPricing == null)
            {
                return NotFound();
            }

            return View(apartmentPricing);
        }

        // POST: ApartmentPricings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartmentPricing = await _context.ApartmentPricings.FindAsync(id);
            if (apartmentPricing != null)
            {
                _context.ApartmentPricings.Remove(apartmentPricing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApartmentPricingExists(int id)
        {
            return _context.ApartmentPricings.Any(e => e.Id == id);
        }
    }
}
