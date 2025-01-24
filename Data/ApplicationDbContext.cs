using Grzmotoptak.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Grzmotoptak.Data
{
    // ApplicationDbContext dziedziczy z IdentityDbContext, co oznacza, że obsługuje autoryzację i uwierzytelnianie użytkowników.
    public class ApplicationDbContext : IdentityDbContext
    {
        // Konstruktor klasy ApplicationDbContext przyjmuje opcje konfiguracji bazy danych i przekazuje je do klasy bazowej.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet dla encji wydarzenia. Umożliwia wykonywanie operacji CRUD na tabeli "events" w bazie danych.
        public DbSet<wydarzenia> events { get; set; }

        // DbSet dla encji Enrollment, która reprezentuje zapis użytkownika na wydarzenie.
        public DbSet<Zapisy> Enrollments { get; set; }
    }
}
