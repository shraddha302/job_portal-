using Microsoft.AspNetCore.Mvc;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using JobPortal.Models.ViewModels;

namespace JobPortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email &&
                                          u.Password == model.Password &&
                                          u.Role == "Admin");

            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid admin credentials");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            return RedirectToAction("Dashboard");
        }

        // GET: /Admin/Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard(int page = 1)
        {
            int pageSize = 10;
            var pendingAppsQuery = _context.Applications
                .Include(a => a.Job)
                .Include(a => a.User)
                .Where(a => a.Status == "Pending");

            int totalItems = await pendingAppsQuery.CountAsync();
            var applications = await pendingAppsQuery
                .OrderByDescending(a => a.AppliedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ApplicationPaginationViewModel
            {
                Applications = applications,
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: /Admin/Register
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Admin/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.Role = "Admin";
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Admin registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: /Admin/Applications
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Applications()
        {
            var applications = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.User)
                .ToListAsync();

            return View(applications);
        }

        // POST: /Admin/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int applicationId, string status)
        {
            var application = await _context.Applications.FindAsync(applicationId);
            if (application != null)
            {
                application.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Application status updated!";
            }
            return RedirectToAction("Applications");
        }
    }
}
