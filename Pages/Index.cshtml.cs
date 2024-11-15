using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TripMatch.Pages
{
    public class IndexModel : PageModel
    {
        public List<string> ImagePaths { get; set; } = new List<string>();

        public void OnGet()
        {
            ImagePaths = new List<string>
            {
                "friends.png",
                "friends2.png",
                "airplan.png",
            };
        }
    }
}