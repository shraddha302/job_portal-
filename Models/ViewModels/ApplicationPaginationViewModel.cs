using System.Collections.Generic;
using JobPortal.Models;

namespace JobPortal.Models.ViewModels
{
    public class ApplicationPaginationViewModel
    {
        public List<Application> Applications { get; set; } = new List<Application>();
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
    }
}
