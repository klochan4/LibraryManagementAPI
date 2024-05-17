using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly ILogger<BooksController> _logger;

        public BooksController(LibraryContext context, ILogger<BooksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            _logger.LogInformation("Fetching all the books from the database.");
            return await _context.Books.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            Book? book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                _logger.LogWarning("GetBook({Id}) NOT FOUND", id);
                return NotFound();
            }
            _logger.LogInformation("Retrieved a book: {BookTitle}", book.Title);
            return book;
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            if (_context.Books.Any(b => b.Title == book.Title && b.Author == book.Author && b.GenreProp == book.GenreProp))
            {
                _logger.LogWarning("Attempt to add a new book with the same title and author: {Title}, {Author}", book.Title, book.Author);
                return BadRequest("A book with the same title and author already exists.");
            }

            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(book, new ValidationContext(book), validationResults, true);

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
                _logger.LogWarning("Validation failed for creating a new book.");
                return BadRequest(ModelState);
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _logger.LogInformation("A new book has been created: {BookId}", book.BookId);
            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, book);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            if (id != book.BookId)
            {
                return BadRequest("Mismatched book ID.");
            }

            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(book, new ValidationContext(book), validationResults, true);

            if (!isValid)
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    if (validationResult.ErrorMessage != null)
                    {
                        ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage);
                    }
                }
                return BadRequest(ModelState);
            }

            Book? existingBook = await _context.Books.FindAsync(id);
            if (existingBook == null)
            {
                _logger.LogWarning("Failed to find book to update: {BookId}", id);
                return NotFound();
            }

            _context.Entry(existingBook).CurrentValues.SetValues(book);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated book {BookId}", book.BookId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error in PutBook({BookId})", book.BookId);
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            Book? book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            bool hasAssociatedCopies = await _context.BookCopies.AnyAsync(bc => bc.BookId == id);
            if (hasAssociatedCopies)
            {
                return Conflict("Cannot delete book with associated copies.");
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}