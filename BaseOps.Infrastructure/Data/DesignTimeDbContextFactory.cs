using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseOps.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseOpsDbContext>
{
    public BaseOpsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BaseOpsDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=BaseOpsDev;Trusted_Connection=True;TrustServerCertificate=True");
        return new BaseOpsDbContext(optionsBuilder.Options);
    }
}
