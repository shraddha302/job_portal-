using System.Collections.Generic;

namespace JobPortal.Models
{
    public class PaginationViewModel
    {
        public List<Job> Jobs { get; set; } = new List<Job>();
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
    }
}
