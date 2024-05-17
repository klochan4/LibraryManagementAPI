using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BookCopiesController(LibraryContext context, ILogger<BookCopiesController> logger) : ControllerBase
    {
        private readonly LibraryContext _context = context;
        private readonly ILogger<BookCopiesController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookCopy>>> GetBookCopies()
        {
            _logger.LogInformation("Fetching all book copies");
            return await _context.BookCopies.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookCopy>> GetBookCopy(int id)
        {
            _logger.LogInformation($"Fetching book copy with ID: {id}");
            BookCopy? bookCopy = await _context.BookCopies.FindAsync(id);
            if (bookCopy == null)
            {
                _logger.LogWarning($"Book copy with ID: {id} not found");
                return NotFound();
            }
            return bookCopy;
        }

        [HttpPost]
        public async Task<ActionResult<BookCopy>> PostBookCopy(BookCopy bookCopy)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (bookCopy.CopyId != 0)
            {
                return BadRequest("Id should not be provided.");
            }

            bool bookExists = await _context.Books.AnyAsync(b => b.BookId == bookCopy.BookId);
            if (!bookExists)
            {
                return NotFound($"No book found with ID {bookCopy.BookId}");
            }

            BookCopy newBookCopy = new BookCopy
            {
                BookId = bookCopy.BookId,
                IsAvailable = bookCopy.IsAvailable
            };

            _context.BookCopies.Add(newBookCopy);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New book copy added with ID: {newBookCopy.CopyId}");
                return CreatedAtAction(nameof(GetBookCopy), new { id = newBookCopy.CopyId }, newBookCopy);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add a new book copy");
                return StatusCode(500, "Internal server error occurred while adding new book copy.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookCopy(int id, BookCopy bookCopy)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != bookCopy.CopyId)
            {
                return BadRequest("Mismatched book copy ID in request");
            }

            BookCopy? existingBookCopy = await _context.BookCopies.FindAsync(id);
            if (existingBookCopy == null)
            {
                _logger.LogWarning($"Attempted to update non-existent book copy with ID: {id}");
                return NotFound();
            }

            existingBookCopy.BookId = bookCopy.BookId;
            existingBookCopy.IsAvailable = bookCopy.IsAvailable;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Book copy with ID: {id} updated successfully");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookCopy(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BookCopy? bookCopy = await _context.BookCopies.FindAsync(id);
            if (bookCopy == null)
            {
                _logger.LogWarning($"Attempted to delete non-existent book copy with ID: {id}");
                return NotFound();
            }

            bool hasLoanRecords = await _context.LoanRecords.AnyAsync(lr => lr.CopyId == id);
            if (hasLoanRecords)
            {
                _logger.LogWarning($"Attempted to delete book copy with ID: {id} which has active loan records");
                return Conflict("Cannot delete book copy with active loan records.");
            }

            _context.BookCopies.Remove(bookCopy);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Book copy with ID: {id} deleted");
            return NoContent();
        }
    }
}