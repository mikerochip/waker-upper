using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WakerUpper.WebApp.Pages
{
    public class HelpModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Places to Find Additional Help";
        }
    }
}
