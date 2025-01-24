using Grzmotoptak.Data;
using Grzmotoptak.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    //nietykalny admin
    private const string SuperAdminEmail = "admin@example.com";

    public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager; // Zarządzanie użytkownikami
        _roleManager = roleManager; // Zarządzanie rolami użytkowników
        _context = context; // Kontekst bazy danych
    }

    public async Task<IActionResult> Użytkownicy()
    {
        var users = _userManager.Users.ToList(); // Pobierz listę wszystkich użytkowników
        var userRoles = new List<(IdentityUser User, IList<string> Roles)>(); // Lista użytkowników z przypisanymi rolami

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user); // Pobierz role dla danego użytkownika
            userRoles.Add((user, roles)); // Dodaj użytkownika i jego role do listy
        }

        return View(userRoles); // Przekaż dane do widoku
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId); // Znajdź użytkownika na podstawie ID

        if (user == null || user.Email == SuperAdminEmail)
        {
            // Jeśli użytkownik nie istnieje lub jest super administratorem, zwróć błąd
            return BadRequest("Nie można zmienić roli tego użytkownika.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user); // Pobierz obecne role użytkownika
        await _userManager.RemoveFromRolesAsync(user, currentRoles); // Usuń obecne role
        await _userManager.AddToRoleAsync(user, role); // Przypisz nową rolę

        return RedirectToAction("Użytkownicy"); // Powrót do widoku użytkowników
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string email, string password, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            // Jeśli rola nie istnieje, zwróć błąd
            return BadRequest("Podana rola nie istnieje.");
        }

        var newUser = new IdentityUser
        {
            UserName = email, // Ustaw nazwę użytkownika
            Email = email, // Ustaw email użytkownika
            EmailConfirmed = true // Oznacz email jako potwierdzony
        };

        var result = await _userManager.CreateAsync(newUser, password); // Utwórz użytkownika
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, role); // Przypisz rolę użytkownikowi
        }
        else
        {
            return BadRequest("Nie udało się utworzyć użytkownika.");
        }

        return RedirectToAction("Użytkownicy"); // Powrót do widoku użytkowników
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id); // Pobierz użytkownika na podstawie ID

        if (user == null)
        {
            TempData["Error"] = "Nie znaleziono użytkownika."; // Informacja o błędzie
            return RedirectToAction("Użytkownicy");
        }

        if (user.Email == SuperAdminEmail)
        {
            // Nie pozwól usunąć głównego administratora
            TempData["Error"] = "Nie można usunąć głównego administratora.";
            return RedirectToAction("Użytkownicy");
        }

        var result = await _userManager.DeleteAsync(user); // Usuń użytkownika

        if (result.Succeeded)
        {
            TempData["Success"] = "Użytkownik został pomyślnie usunięty.";
        }
        else
        {
            TempData["Error"] = "Wystąpił problem podczas usuwania użytkownika.";
        }

        return RedirectToAction("Użytkownicy"); // Powrót do widoku użytkowników
    }

    public async Task<IActionResult> EventsAsync()
    {
        var wydarzenia = await _context.events
                .Include(e => e.User) // Załaduj powiązania z użytkownikami
                .ToListAsync(); // Pobierz wszystkie wydarzenia z bazy danych
        return View(wydarzenia); // Przekaż listę wydarzeń do widoku
    }

    public IActionResult Index()
    {
        return View(); // Wyświetl stronę główną panelu administratora
    }
}
