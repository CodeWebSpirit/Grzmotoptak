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

    private const string SuperAdminEmail = "admin@example.com";

    public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> Użytkownicy()
    {
        var users = _userManager.Users.ToList();
        var userRoles = new List<(IdentityUser User, IList<string> Roles)>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles.Add((user, roles));
        }

        return View(userRoles);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.Email == SuperAdminEmail)
        {
            return BadRequest("Nie można zmienić roli tego użytkownika.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        return RedirectToAction("Użytkownicy");
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string email, string password, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            ModelState.AddModelError(string.Empty, "Podana rola nie istnieje.");
            return View("Użytkownicy", await GetUsersWithRoles());
        }

        var newUser = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(newUser, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, role);
            return RedirectToAction("Użytkownicy");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View("Użytkownicy", await GetUsersWithRoles());
    }

    private async Task<List<(IdentityUser User, IList<string> Roles)>> GetUsersWithRoles()
    {
        var users = _userManager.Users.ToList();
        var userRoles = new List<(IdentityUser User, IList<string> Roles)>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles.Add((user, roles));
        }

        return userRoles;
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["Error"] = "Nie znaleziono użytkownika.";
            return RedirectToAction("Użytkownicy");
        }

        if (user.Email == SuperAdminEmail)
        {
            TempData["Error"] = "Nie można usunąć głównego administratora.";
            return RedirectToAction("Użytkownicy");
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] = "Użytkownik został pomyślnie usunięty.";
        }
        else
        {
            TempData["Error"] = "Wystąpił problem podczas usuwania użytkownika.";
        }

        return RedirectToAction("Użytkownicy");
    }

    public async Task<IActionResult> EventsAsync()
    {
        var wydarzenia = await _context.events
                .Include(e => e.User)
                .ToListAsync();
        return View(wydarzenia);
    }

    public IActionResult Index()
    {
        return View();
    }
}
