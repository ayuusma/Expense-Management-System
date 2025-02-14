using Microsoft.AspNetCore.Mvc;
using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExpenseManagementSystem.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExpenseController> _logger;

        public ExpenseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ExpenseController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                _logger.LogWarning("User ID is null. Redirecting to login.");
                return RedirectToAction("Login", "Account");
            }

            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date) // ✅ Show latest expenses first
                .ToListAsync();

            _logger.LogInformation($"Found {expenses.Count} expenses for user {userId}");
            return View(expenses);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("❌ User ID is null. Redirecting to login.");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"✅ User ID obtained: {userId}");
            expense.UserId = userId;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Model validation failed. Errors: " +
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return View(expense);
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Expense added successfully for user {userId}");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                _logger.LogWarning($"Expense with ID {id} not found.");
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (expense.UserId != userId) // ✅ Prevent unauthorized edits
            {
                _logger.LogWarning($"Unauthorized access attempt to edit Expense ID {id} by User {userId}");
                return Forbid();
            }

            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id)
            {
                return NotFound(); // If ID doesn't match, return 404
            }

            var existingExpense = await _context.Expenses.FindAsync(id);
            if (existingExpense == null)
            {
                _logger.LogWarning($"Edit failed: Expense with ID {id} not found.");
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (existingExpense.UserId != userId) // ✅ Prevent unauthorized edits
            {
                _logger.LogWarning($"Unauthorized access attempt to edit Expense ID {id} by User {userId}");
                return Forbid();
            }

            try
            {
                existingExpense.Description = expense.Description;
                existingExpense.Amount = expense.Amount;
                existingExpense.Category = expense.Category;
                existingExpense.Date = expense.Date;

                _context.Update(existingExpense);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Expense ID {id} updated successfully.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError($"Concurrency error updating expense ID {id}: {ex.Message}");
                return View(expense);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                _logger.LogWarning($"Expense with ID {id} not found.");
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (expense.UserId != userId) // ✅ Prevent unauthorized deletion
            {
                _logger.LogWarning($"Unauthorized access attempt to delete Expense ID {id} by User {userId}");
                return Forbid();
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense with ID {id} deleted successfully.");

            return RedirectToAction(nameof(Index));
        }
    }
}
