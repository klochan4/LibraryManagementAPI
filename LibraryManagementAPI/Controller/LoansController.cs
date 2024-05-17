using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoansController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly ILogger<LoansController> _logger;

        public LoansController(LibraryContext context, ILogger<LoansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanRecord>>> GetLoans()
        {
            _logger.LogInformation("Fetching all loans");
            return await _context.LoanRecords.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<LoanRecord>> PostLoan(LoanRecord loanRecord)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed while creating new loan.");
                return BadRequest(ModelState);
            }

            if (loanRecord.ActualReturnDate != null)
            {
                _logger.LogWarning("Actual return date should not be set for a new loan.");
                return BadRequest("Actual return date should not be set for a new loan.");
            }

            if (loanRecord.ExpectedReturnDate <= loanRecord.LoanDate)
            {
                _logger.LogWarning("Expected return date must be after the loan date.");
                return BadRequest("Expected return date must be later than the loan date.");
            }

            bool userExists = await _context.Users.AnyAsync(u => u.UserId == loanRecord.UserId);
            if (!userExists)
            {
                _logger.LogWarning("Attempt to loan with a non-existent user ID.");
                return NotFound($"User with ID {loanRecord.UserId} not found.");
            }

            BookCopy? bookCopy = await _context.BookCopies.FindAsync(loanRecord.CopyId);
            if (bookCopy == null || !bookCopy.IsAvailable)
            {
                _logger.LogWarning("Attempt to loan a non-existent or unavailable book copy.");
                return BadRequest("The book copy is not available for loan.");
            }

            if (_context.LoanRecords.Any(lr => lr.CopyId == loanRecord.CopyId && lr.UserId == loanRecord.UserId && lr.ActualReturnDate == null))
            {
                _logger.LogWarning("User already has an active loan for this book copy.");
                return BadRequest("You already have an active loan for this book copy.");
            }

            _context.LoanRecords.Add(loanRecord);
            bookCopy.IsAvailable = false;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("New loan added with ID: {LoanRecordId}, Book copy set as unavailable", loanRecord.LoanRecordId);
                return CreatedAtAction(nameof(GetLoan), new { id = loanRecord.LoanRecordId }, loanRecord);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("An error occurred while saving the new loan: {Error}", ex.InnerException?.Message);
                return BadRequest("An error occurred while saving the new loan. Please check the data and try again.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LoanRecord>> GetLoan(int id)
        {
            _logger.LogInformation("Fetching loan with ID: {Id}", id);
            LoanRecord? loanRecord = await _context.LoanRecords.FirstOrDefaultAsync(l => l.LoanRecordId == id);

            if (loanRecord == null)
            {
                _logger.LogWarning("Loan with ID: {Id} not found", id);
                return NotFound();
            }

            return loanRecord;
        }

        [HttpPut("{id}/return")]
        public async Task<IActionResult> ReturnLoan(int id)
        {
            LoanRecord? loanRecord = await _context.LoanRecords.FindAsync(id);
            if (loanRecord == null)
            {
                _logger.LogWarning("Attempted to return non-existent loan with ID: {Id}", id);
                return NotFound();
            }

            if (loanRecord.ActualReturnDate != null)
            {
                _logger.LogWarning("Attempted to return an already returned loan with ID: {Id}", id);
                return BadRequest("Loan has already been returned.");
            }

            loanRecord.ActualReturnDate = DateTime.Now;
            BookCopy? bookCopy = await _context.BookCopies.FindAsync(loanRecord.CopyId);

            if (bookCopy != null)
            {
                bookCopy.IsAvailable = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Book copy with ID: {CopyId} has been marked as returned", loanRecord.CopyId);
            }
            else
            {
                _logger.LogWarning("No book copy found with ID: {CopyId}", loanRecord.CopyId);
                return NotFound($"No book copy found with ID: {loanRecord.CopyId}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            LoanRecord? loanRecord = await _context.LoanRecords.FindAsync(id);
            if (loanRecord == null)
            {
                _logger.LogWarning("Attempted to delete non-existent loan with ID: {Id}", id);
                return NotFound();
            }

            _context.LoanRecords.Remove(loanRecord);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Loan with ID: {Id} deleted", id);

            return NoContent();
        }
    }
}