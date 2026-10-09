using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GreenAge.Infrastructure;

public sealed class GreenAgeDesignTimeFactory : IDesignTimeDbContextFactory<GreenAgeDbContext>
{
    public GreenAgeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GreenAgeDbContext>()
            .UseSqlite("Data Source=greenage.db")
            .Options;
        return new GreenAgeDbContext(options);
    }
}