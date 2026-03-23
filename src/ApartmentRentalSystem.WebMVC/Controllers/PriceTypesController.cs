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
    public class PriceTypesController : Controller
    {
        private readonly ApartmentContext _context;

        public PriceTypesController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: PriceTypes
        public async Task<IActionResult> Index()
        {
            var apartmentContext = _context.PriceTypes.Include(p => p.TimeUnit);
            return View(await apartmentContext.ToListAsync());
        }

        // GET: PriceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var priceType = await _context.PriceTypes
                .Include(p => p.TimeUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (priceType == null)
            {
                return NotFound();
            }

            return View(priceType);
        }

        // GET: PriceTypes/Create
        public IActionResult Create()
        {
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name");
            return View();
        }

        // POST: PriceTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,UnitId,Id")] PriceType priceType)
        {
            ModelState.Remove("TimeUnit");
            if (ModelState.IsValid)
            {
                _context.Add(priceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", priceType.UnitId);
            return View(priceType);
        }

        // GET: PriceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var priceType = await _context.PriceTypes.FindAsync(id);
            if (priceType == null)
            {
                return NotFound();
            }
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", priceType.UnitId);
            return View(priceType);
        }

        // POST: PriceTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,UnitId,Id")] PriceType priceType)
        {
            if (id != priceType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(priceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PriceTypeExists(priceType.Id))
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
            ViewData["UnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", priceType.UnitId);
            return View(priceType);
        }

        // GET: PriceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var priceType = await _context.PriceTypes
                .Include(p => p.TimeUnit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (priceType == null)
            {
                return NotFound();
            }

            return View(priceType);
        }

        // POST: PriceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var priceType = await _context.PriceTypes.FindAsync(id);
            if (priceType != null)
            {
                _context.PriceTypes.Remove(priceType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PriceTypeExists(int id)
        {
            return _context.PriceTypes.Any(e => e.Id == id);
        }
    }
}
