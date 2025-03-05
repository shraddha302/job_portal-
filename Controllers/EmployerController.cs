using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using JobPortal.Models.ViewModels;
using System.IO;

namespace JobPortal.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployerController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Employer/Dashboard
        // Now returns a paginated list of all applications for jobs belonging to the employer.
        public async Task<IActionResult> Dashboard(int page = 1)
        {
            int pageSize = 10;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var employer = await _context.EmployerProfiles
                .Include(e => e.Jobs)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employer == null)
            {
                TempData["Error"] = "Please complete your employer profile first!";
                return RedirectToAction("Profile");
            }

            // Query all applications for jobs owned by this employer.
            var applicationsQuery = _context.Applications
                .Include(a => a.Job)
                .Include(a => a.User)
                .Where(a => a.Job.Employer.UserId == userId);

            int totalItems = await applicationsQuery.CountAsync();
            var applications = await applicationsQuery
                .OrderByDescending(a => a.AppliedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var appPagination = new ApplicationPaginationViewModel
            {
                Applications = applications,
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            var model = new EmployerApplicationDashboardViewModel
            {
                Employer = employer,
                ApplicationPagination = appPagination
            };

            return View(model);
        }

        // GET: Employer/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employer == null)
            {
                employer = new EmployerProfile
                {
                    UserId = userId,
                    CompanyName = "Your Company Name",
                    ContactInfo = "Contact Information",
                    Logo = "/images/default-logo.png"
                };
                _context.EmployerProfiles.Add(employer);
                await _context.SaveChangesAsync();
            }

            return View(employer);
        }

        // POST: Employer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EmployerProfile model, IFormFile logoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId) ?? model;

            employer.CompanyName = model.CompanyName ?? employer.CompanyName;
            employer.Description = model.Description ?? employer.Description;
            employer.ContactInfo = model.ContactInfo ?? employer.ContactInfo;

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "images", "logos");
                Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(logoFile.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                employer.Logo = $"/images/logos/{fileName}";
            }

            if (employer.Id == 0) _context.EmployerProfiles.Add(employer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // GET: Employer/Applications
        public async Task<IActionResult> Applications(int jobId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var applications = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Employer)
                .Include(a => a.User)
                .Where(a => a.Job.Employer.UserId == userId && a.JobId == jobId)
                .ToListAsync();

            if (!applications.Any())
            {
                TempData["Info"] = "No applications found for this job.";
            }

            return View(applications);
        }

        // POST: Employer/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, string status)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Employer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application?.Job?.Employer == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("Dashboard");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (application.Job.Employer.UserId != userId)
            {
                TempData["Error"] = "Unauthorized action.";
                return RedirectToAction("Dashboard");
            }

            application.Status = status ?? "Pending";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction("Applications", new { jobId = application.JobId });
        }

        // GET: Employer/Index – simply redirect to Dashboard.
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var employer = await _context.EmployerProfiles
                .Include(e => e.Jobs)
                    .ThenInclude(j => j.Applications)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employer == null)
            {
                TempData["Error"] = "Please complete your employer profile first.";
                return RedirectToAction("Profile");
            }

            return RedirectToAction("Dashboard");
        }
    }
}
