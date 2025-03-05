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
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment _env;

        public UserController(
            AppDbContext context,
            ILogger<UserController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Register(string? role)
        {
            ViewBag.Role = role?.ToLower();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? role)
        {
            role = role?.Trim().ToLower() == "employer" ? "Employer" : "User";

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                Username = model.Username,
                Password = model.Password,
                Role = role
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (role == "Employer")
                {
                    string logoPath = "/images/default-logo.png";

                    if (model.Logo != null && model.Logo.Length > 0)
                    {
                        logoPath = $"/uploads/{Guid.NewGuid()}{Path.GetExtension(model.Logo.FileName)}";
                        await SaveFile(model.Logo, logoPath);
                    }

                    var employer = new EmployerProfile
                    {
                        UserId = user.Id,
                        CompanyName = model.CompanyName ?? "New Company",
                        Description = model.Description ?? "Company description",
                        ContactInfo = model.ContactInfo ?? "Contact info",
                        Logo = logoPath
                    };

                    _context.EmployerProfiles.Add(employer);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Registration successful!";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                ModelState.AddModelError("", "Registration error. Please try again.");
                return View(model);
            }
        }
        private async Task SaveFile(IFormFile file, string relativePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            await SignInUser(user, model.RememberMe);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            TempData["Success"] = "You've been logged out";
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Applications()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var applications = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Employer)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return View(applications);
        }

        private async Task SignInUser(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = rememberMe });
        }
    }
}
