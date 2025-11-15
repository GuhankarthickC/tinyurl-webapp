using Microsoft.EntityFrameworkCore;
using tinyurl.Models;

namespace tinyurl.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
}