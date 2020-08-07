using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WakerUpper.Asp.Pages
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
