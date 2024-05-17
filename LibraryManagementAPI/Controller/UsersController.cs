using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController(LibraryContext context, ILogger<UsersController> logger) : ControllerBase
    {
        private readonly LibraryContext _context = context;
        private readonly ILogger<UsersController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            _logger.LogInformation("Fetching all users");
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            _logger.LogInformation("Fetching user with ID: {UserId}", id);
            User? user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID: {UserId} not found", id);
                return NotFound();
            }

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                _logger.LogWarning("Attempt to add a new user with duplicate email: {Email}", user.Email);
                return Conflict("A user with the same email already exists.");
            }

            List<ValidationResult> validationResults = new();
            bool isValid = Validator.TryValidateObject(user, new ValidationContext(user), validationResults, true);

            if (!isValid)
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    string errorMessage = validationResult.ErrorMessage ?? "Generic error occurred";
                    ModelState.AddModelError(validationResult.MemberNames.First(), errorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for new user.");
                return BadRequest(ModelState);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("New user added with ID: {UserId}", user.UserId);

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId || !ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed or mismatched ID for user update.");
                return BadRequest(ModelState);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User with ID: {UserId} updated", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
                {
                    _logger.LogWarning("Attempted to update non-existent user with ID: {UserId}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error while updating user with ID: {UserId}", id);
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            User? user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Attempted to delete non-existent user with ID: {UserId}", id);
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User with ID: {UserId} deleted", id);

            return NoContent();
        }
    }
}