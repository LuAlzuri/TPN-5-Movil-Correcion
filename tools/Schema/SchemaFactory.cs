using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TPN_5_MOVIL.Data;

public sealed class SchemaFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Scaffolding does not connect. Applying migrations requires explicit configuration.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=TPN5_DesignOnly;Integrated Security=True";
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options);
    }
}
