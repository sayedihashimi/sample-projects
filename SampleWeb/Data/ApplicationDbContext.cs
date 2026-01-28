using Microsoft.EntityFrameworkCore;
using SampleWeb.Models;

namespace SampleWeb.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contact { get; set; } = default!;
}
