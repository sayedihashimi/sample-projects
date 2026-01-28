using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SampleWeb.Models;

namespace SampleWeb.Pages.ContactPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Contact Contact { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var contact = await _context.Contact.FirstOrDefaultAsync(m => m.Id == id);
        if (contact is null)
        {
            return NotFound();
        }
        else
        {
            Contact = contact;
        }

        return Page();
    }
}
