using JobPortal.Models;

namespace JobPortal.Models.ViewModels
{
    public class EmployerApplicationDashboardViewModel
    {
        public EmployerProfile Employer { get; set; }
        public ApplicationPaginationViewModel ApplicationPagination { get; set; }
    }
}
