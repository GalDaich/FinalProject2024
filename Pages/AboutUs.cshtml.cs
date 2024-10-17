using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TripMatch.Pages.Shared
{
    public class AboutUsModel : PageModel
    {
        // Properties for dynamic content (optional)
        public string CompanyName { get; set; }
        public string Mission { get; set; }
        public string Vision { get; set; }
        public string TeamDescription { get; set; }
        public string ContactEmail { get; set; }

        // This method handles GET requests to the page
        public void OnGet()
        {
            // You can load static or dynamic content here (for now, we're using static strings)
            CompanyName = "TripMatch";
            Mission = "Our mission is to simplify the travel planning process by matching travelers with the best options available.";
            Vision = "We envision a world where everyone can easily explore their dream destinations without the hassle of complicated planning.";
            TeamDescription = "Our team is made up of seasoned travel experts, developers, and customer service professionals.";
            ContactEmail = "support@tripmatch.com";
        }
    }
}