using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceFinalProject.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole("Admin"))
            return RedirectToPage("/Admin/Dashboard");

        return RedirectToPage("/Customer/Home");
    }
}
