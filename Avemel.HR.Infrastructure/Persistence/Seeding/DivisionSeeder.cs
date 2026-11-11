
using Avemel.HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Avemel.HR.Infrastructure.Persistence.Seeding;

public static class DivisionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var divisions = new[]
        {
            new Division
            {
                Code = "AVE",
                Name = "Avemel Logistics",
                LegalName = "Deep Blue Ocean Trading 787 CC",
                Colour = "#ea2127",
                Active = true,
                SortOrder = 1
            },
            new Division
            {
                Code = "EAN",
                Name = "EAN Group",
                LegalName = "EAN Group",
                Colour = "#d59d29",
                Active = true,
                SortOrder = 2
            }
        };

        foreach (var division in divisions)
        {
            bool exists = await db.Divisions
                .AnyAsync(d => d.Code == division.Code);

            if (!exists)
            {
                db.Divisions.Add(division);
            }
        }

        await db.SaveChangesAsync();
    }
}
