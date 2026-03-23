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
    public class TimeUnitsController : Controller
    {
        private readonly ApartmentContext _context;

        public TimeUnitsController(ApartmentContext context)
        {
            _context = context;
        }

        // GET: TimeUnits
        public async Task<IActionResult> Index()
        {
            return View(await _context.TimeUnits.ToListAsync());
        }

        // GET: TimeUnits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeUnit = await _context.TimeUnits
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeUnit == null)
            {
                return NotFound();
            }

            return View(timeUnit);
        }

        // GET: TimeUnits/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TimeUnits/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] TimeUnit timeUnit)
        {
            if (ModelState.IsValid)
            {
                _context.Add(timeUnit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(timeUnit);
        }

        // GET: TimeUnits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeUnit = await _context.TimeUnits.FindAsync(id);
            if (timeUnit == null)
            {
                return NotFound();
            }
            return View(timeUnit);
        }

        // POST: TimeUnits/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] TimeUnit timeUnit)
        {
            if (id != timeUnit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeUnit);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeUnitExists(timeUnit.Id))
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
            return View(timeUnit);
        }

        // GET: TimeUnits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeUnit = await _context.TimeUnits
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeUnit == null)
            {
                return NotFound();
            }

            return View(timeUnit);
        }

        // POST: TimeUnits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeUnit = await _context.TimeUnits.FindAsync(id);
            if (timeUnit != null)
            {
                _context.TimeUnits.Remove(timeUnit);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TimeUnitExists(int id)
        {
            return _context.TimeUnits.Any(e => e.Id == id);
        }
    }
}
