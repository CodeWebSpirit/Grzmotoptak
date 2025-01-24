using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Grzmotoptak.Data;
using Grzmotoptak.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Grzmotoptak.Controllers
{
    public class wydarzeniaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public wydarzeniaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Sprawdza, czy wydarzenie o podanym ID istnieje w bazie danych
        private bool wydarzeniaExists(int id)
        {
            // Metoda Any() sprawdza, czy istnieje jakikolwiek rekord spełniający warunek
            return _context.events.Any(e => e.Id == id);
        }

        // GET: wydarzenia - Wyświetla listę wszystkich wydarzeń
        public async Task<IActionResult> Index()
        {
            // Pobiera identyfikator aktualnie zalogowanego użytkownika (jeśli jest zalogowany)
            var userId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            // Pobiera listę wydarzeń i dołącza informacje o ich twórcach
            var wydarzenia = await _context.events
                .Include(e => e.User) // Dołączenie relacji z tabelą użytkowników
                .ToListAsync();

            // Pobiera identyfikatory wydarzeń, na które użytkownik jest zapisany
            var przypisanewydarzeniaIds = userId != null
                ? await _context.Enrollments
                    .Where(e => e.UserId == userId)
                    .Select(e => e.EventId)
                    .ToListAsync()
                : new List<int>();

            // Przekazuje identyfikatory zapisanych wydarzeń do widoku
            ViewData["przypisanewydarzeniaIds"] = przypisanewydarzeniaIds;

            return View(wydarzenia);
        }

        // GET: wydarzenia/Details/5 - Wyświetla szczegóły wybranego wydarzenia
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Pobiera szczegóły wydarzenia na podstawie ID
            var wydarzenia = await _context.events
                .Include(e => e.User) // Dołączenie relacji z tabelą użytkowników
                .FirstOrDefaultAsync(m => m.Id == id);

            if (wydarzenia == null)
            {
                return NotFound();
            }

            return View(wydarzenia);
        }

        [Authorize]
        // GET: wydarzenia/Create - Formularz tworzenia nowego wydarzenia
        public IActionResult Create()
        {
            ViewBag.TypyWydarzen = new List<string> { "Koncert", "Dyskoteka", "Wystawa", "Pokaz", "Integracja" };
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Tytul,Opis,Data,Typ,Obrazek")] wydarzenia wydarzenia, IFormFile obrazek)
        {
            // Przypisanie identyfikatora twórcy do wydarzenia
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            wydarzenia.tworca_id = userId;

            if (ModelState.IsValid)
            {
                if (obrazek != null && obrazek.Length > 0)
                {
                    var fileName = Path.GetFileName(obrazek.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await obrazek.CopyToAsync(stream);
                    }
                    wydarzenia.Obrazek = "/images/" + fileName;
                }

                // Dodanie wydarzenia do bazy
                _context.Add(wydarzenia);
                await _context.SaveChangesAsync();

                // Automatyczne przypisanie twórcy jako uczestnika
                var zapis = new Zapisy(userId, wydarzenia.Id);
                _context.Enrollments.Add(zapis);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewBag.TypyWydarzen = new List<string> { "Koncert", "Dyskoteka", "Wystawa", "Pokaz", "Integracja" };
            return View(wydarzenia);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wydarzenia = await _context.events.FindAsync(id);
            if (wydarzenia == null)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia użytkownika
            if (wydarzenia.tworca_id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ViewBag.TypyWydarzen = new List<string> { "Koncert", "Dyskoteka", "Wystawa", "Pokaz", "Integracja" };
            return View(wydarzenia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Tytul,Opis,Data,Typ,Obrazek")] wydarzenia wydarzenia, IFormFile obrazek)
        {
            if (id != wydarzenia.Id)
            {
                return NotFound();
            }

            var existingEvent = await _context.events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (existingEvent == null)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia użytkownika
            if (existingEvent.tworca_id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            wydarzenia.tworca_id = existingEvent.tworca_id;

            if (ModelState.IsValid)
            {
                try
                {
                    if (obrazek != null && obrazek.Length > 0)
                    {
                        var fileName = Path.GetFileName(obrazek.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await obrazek.CopyToAsync(stream);
                        }
                        wydarzenia.Obrazek = "/images/" + fileName;
                    }
                    else
                    {
                        wydarzenia.Obrazek = existingEvent.Obrazek;
                    }

                    _context.Update(wydarzenia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!wydarzeniaExists(wydarzenia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TypyWydarzen = new List<string> { "Koncert", "Dyskoteka", "Wystawa", "Pokaz", "Integracja" };
            return View(wydarzenia);
        }


        [Authorize]
        // GET: wydarzenia/Delete/5 - Formularz potwierdzenia usunięcia wydarzenia
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Pobiera dane wydarzenia do usunięcia
            var wydarzenia = await _context.events
                .Include(e => e.User) // Dołączenie relacji z tabelą użytkowników
                .FirstOrDefaultAsync(m => m.Id == id);
            if (wydarzenia == null)
            {
                return NotFound();
            }

            return View(wydarzenia);
        }

        [Authorize]
        // POST: wydarzenia/Delete/5 - Usuwa wybrane wydarzenie
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Pobiera wydarzenie na podstawie ID
            var wydarzenia = await _context.events.FindAsync(id);

            if (wydarzenia == null)
            {
                return NotFound();
            }

            // Sprawdza uprawnienia użytkownika
            if (wydarzenia.tworca_id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid(); // Brak dostępu
            }

            _context.events.Remove(wydarzenia); // Usunięcie wydarzenia z bazy danych
            await _context.SaveChangesAsync(); // Zapisanie zmian
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> MojeWydarzenia()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pobierz zapisy użytkownika
            var zapisy = _context.Enrollments
        .Include(z => z.Event)
        .ThenInclude(e => e.User) // Ładujemy dane twórcy wydarzenia
        .Where(z => z.UserId == userId)
        .ToList();

            // Pobierz wydarzenia stworzone przez użytkownika
            var createdEvents = await _context.events
                .Where(e => e.tworca_id == userId)
                .Select(e => new
                {
                    e.Id,
                    e.Tytul,
                    e.Data,
                    e.Opis,
                    e.Typ,
                    ParticipantCount = _context.Enrollments.Count(z => z.EventId == e.Id) // Liczba uczestników
                })
                .ToListAsync();

            ViewData["CreatedEvents"] = createdEvents;

            return View(zapisy);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Przypisz(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.UserId == userId);

            if (existingEnrollment != null)
            {
                return BadRequest("Już zapisałeś się na to wydarzenie.");
            }

            var zapis = new Zapisy(userId, eventId);

            _context.Enrollments.Add(zapis);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Wypisz(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var zapis = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.EventId == eventId);

            if (zapis != null)
            {
                _context.Enrollments.Remove(zapis);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MojeWydarzenia));
        }

    }
}
