using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;

namespace ApartmentRentalSystem.WebMVC.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApartmentContext _context;

        public UsersController(ApartmentContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MyProfile()
        {
            var email = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction(nameof(CreateProfileFromCurrentIdentity));
            }

            return View("Details", user);
        }

        public async Task<IActionResult> CreateProfileFromCurrentIdentity()
        {
            var email = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
                return RedirectToAction(nameof(MyProfile));

            var isOwner = User.IsInRole("Owner");
            var roleName = isOwner ? "Host" : "Guest";

            var role = await _context.UserRoles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
                return RedirectToAction("Index", "Home");

            var user = new User
            {
                FullName = email,
                Email = email,
                Phone = string.Empty,
                RoleId = role.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyProfile));
        }

        // Список користувачів звичайним користувачам не потрібен
        public IActionResult Index()
        {
            return RedirectToAction(nameof(MyProfile));
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
                return NotFound();

            if (!await CanAccessUserAsync(user))
                return Forbid();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return Forbid();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("FullName,Email,Phone,RoleId,Id")] User user)
        {
            return Forbid();
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!await CanAccessUserAsync(user))
                return Forbid();

            ViewData["RoleId"] = new SelectList(
                _context.UserRoles.Where(r => r.Id == user.RoleId),
                "Id",
                "Name",
                user.RoleId
            );

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FullName,Email,Phone,RoleId,Id")] User user)
        {
            if (id != user.Id)
                return NotFound();

            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                return NotFound();

            if (!await CanAccessUserAsync(existingUser))
                return Forbid();

            // Користувач не повинен змінювати собі роль вручну
            user.RoleId = existingUser.RoleId;

            ModelState.Remove("Role");

            if (ModelState.IsValid)
            {
                try
                {
                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(MyProfile));
            }

            ViewData["RoleId"] = new SelectList(
                _context.UserRoles.Where(r => r.Id == existingUser.RoleId),
                "Id",
                "Name",
                existingUser.RoleId
            );

            return View(user);
        }

        // GET: Users/Delete/5
        public IActionResult Delete(int? id)
        {
            return Forbid();
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return Forbid();
        }

        private async Task<bool> CanAccessUserAsync(User user)
        {
            var currentEmail = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(currentEmail))
                return false;

            return await _context.Users.AnyAsync(u => u.Id == user.Id && u.Email == currentEmail);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}