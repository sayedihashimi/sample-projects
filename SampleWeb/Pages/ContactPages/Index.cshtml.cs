using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SampleWeb.Models;
using SampleWeb.Data;

namespace SampleWeb.Pages.ContactPages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Contact> Contact { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Contact = await _context.Contact.ToListAsync();
    }
}
