using Microsoft.AspNetCore.Mvc;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace JobPortal.Controllers
{
    [Authorize]
    public class JobController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<JobController> _logger;

        public JobController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<JobController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Job/Index
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Jobs
                .Include(j => j.Employer)
                .Where(j => j.IsApproved);

            var totalJobs = await query.CountAsync();
            var jobs = await query
                .OrderByDescending(j => j.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new PaginationViewModel
            {
                Jobs = jobs,
                CurrentPage = page,
                TotalItems = totalJobs,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: Job/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Applications)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(job);
        }

        // POST: Job/Apply
        // Allows a user to upload their CV along with the application.
        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId, IFormFile? cv)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(Index));
            }

            var job = await _context.Jobs
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.UserId == userId);

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction(nameof(Details), new { id = jobId });
            }

            var application = new Application
            {
                JobId = jobId,
                UserId = userId,
                AppliedDate = DateTime.UtcNow,
                Status = "Pending"
            };

            // Save the uploaded CV if provided.
            if (cv != null && cv.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cvs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(cv.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await cv.CopyToAsync(fileStream);
                }
                application.CVFileName = uniqueFileName;
            }

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction(nameof(Details), new { id = jobId });
        }

        // GET: Job/ManageJobs
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> ManageJobs()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(Index));
            }
            var jobs = await _context.Jobs
                .Include(j => j.Employer)
                .Where(j => User.IsInRole("Admin") || j.Employer.UserId == userId)
                .ToListAsync();
            return View(jobs);
        }

        // POST: Job/Approve
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(ManageJobs));
            }
            job.IsApproved = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Job approved successfully!";
            return RedirectToAction(nameof(ManageJobs));
        }

        // GET: Job/Create
        [HttpGet]
        [Authorize(Roles = "Admin,Employer")]
        public IActionResult Create()
        {
            if (User.IsInRole("Admin"))
            {
                ViewBag.EmployerProfiles = new SelectList(_context.EmployerProfiles, "Id", "CompanyName");
            }
            // Predefined Job Types for dropdown list
            ViewBag.JobTypes = new List<string> { "Full-Time", "Part-Time", "Remote", "On-Site", "Contract", "Other" };

            return View(new Job());
        }

        // POST: Job/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job, string? customType)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.JobTypes = new List<string> { "Full-Time", "Part-Time", "Remote", "On-Site", "Contract", "Other" };
                return View(job);
            }

            try
            {
                // Check if a custom job type is provided
                if (!string.IsNullOrEmpty(customType) && job.Type == "Other")
                {
                    job.Type = customType;
                }

                // Employer-specific handling
                if (User.IsInRole("Employer"))
                {
                    if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                    {
                        TempData["Error"] = "Invalid user ID.";
                        return RedirectToAction(nameof(Index));
                    }

                    var employer = await _context.EmployerProfiles.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (employer == null)
                    {
                        TempData["Error"] = "Complete your employer profile first!";
                        return RedirectToAction("Profile", "Employer");
                    }
                    job.EmployerProfileId = employer.Id;
                }

                if (User.IsInRole("Admin") && job.EmployerProfileId == 0)
                {
                    ModelState.AddModelError("EmployerProfileId", "Please select a company");
                    ViewBag.EmployerProfiles = new SelectList(_context.EmployerProfiles, "Id", "CompanyName");
                    return View(job);
                }

                job.PostedDate = DateTime.UtcNow;
                job.IsApproved = User.IsInRole("Admin");

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Job posted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job creation error");
                TempData["Error"] = $"Error: {ex.Message}";
                return View(job);
            }
        }

        // GET: Job/Edit/{id}
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Edit(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Employer"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    TempData["Error"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var employer = await _context.EmployerProfiles.FirstOrDefaultAsync(e => e.UserId == userId);
                if (job.EmployerProfileId != employer?.Id)
                {
                    TempData["Error"] = "You are not authorized to edit this job.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Predefined Job Types for dropdown list
            ViewBag.JobTypes = new List<string> { "Full-Time", "Part-Time", "Remote", "On-Site", "Contract", "Other" };

            return View(job);
        }

        // POST: Job/Edit/{id}
        [Authorize(Roles = "Admin,Employer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job, string? customType)
        {
            if (id != job.Id)
            {
                TempData["Error"] = "Invalid job ID.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.JobTypes = new List<string> { "Full-Time", "Part-Time", "Remote", "On-Site", "Contract", "Other" };
                return View(job);
            }

            try
            {
                var existingJob = await _context.Jobs.FindAsync(id);
                if (existingJob == null)
                {
                    TempData["Error"] = "Job not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if a custom job type is provided
                if (!string.IsNullOrEmpty(customType) && job.Type == "Other")
                {
                    job.Type = customType;
                }

                existingJob.Title = job.Title;
                existingJob.Description = job.Description;
                existingJob.Location = job.Location;
                existingJob.Salary = job.Salary;
                existingJob.Type = job.Type;

                _context.Update(existingJob);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Job updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job");
                TempData["Error"] = "Error updating job: " + ex.Message;
                return View(job);
            }
        }

        // GET: Job/Delete/{id}
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Employer"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    TempData["Error"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var employer = await _context.EmployerProfiles.FirstOrDefaultAsync(e => e.UserId == userId);
                if (job.EmployerProfileId != employer?.Id)
                {
                    TempData["Error"] = "You are not authorized to delete this job.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(job);
        }

        // POST: Job/DeleteConfirmed/{id}
        [Authorize(Roles = "Admin,Employer")]
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Job deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job");
                TempData["Error"] = "Error deleting job: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Job/ReviewApplication
        // Allows Admin and Employer to update an application's status.
        [HttpPost]
        [Authorize(Roles = "Admin,Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewApplication(int applicationId, string status)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                .ThenInclude(j => j.Employer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction(nameof(ManageJobs));
            }

            // For employers, ensure they own the job.
            if (User.IsInRole("Employer"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                {
                    TempData["Error"] = "Invalid user ID.";
                    return RedirectToAction(nameof(ManageJobs));
                }
                if (application.Job == null || application.Job.Employer == null || application.Job.Employer.UserId != currentUserId)
                {
                    TempData["Error"] = "You are not authorized to review this application.";
                    return RedirectToAction(nameof(ManageJobs));
                }
            }

            application.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application status updated successfully!";
            return RedirectToAction(nameof(ManageJobs));
        }

        // GET: Job/DownloadCV/{applicationId}
        // Allows Admin and Employer to download the uploaded CV.
        [Authorize(Roles = "Admin,Employer")]
        public async Task<IActionResult> DownloadCV(int applicationId)
        {
            var application = await _context.Applications.FindAsync(applicationId);
            if (application == null || string.IsNullOrEmpty(application.CVFileName))
            {
                return NotFound();
            }
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cvs");
            var filePath = Path.Combine(uploadsFolder, application.CVFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
            var contentType = "application/octet-stream";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, Path.GetFileName(filePath));
        }
    }
}
    