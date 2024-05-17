using LibraryManagementAPI.Controllers;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests
{
    public class BookCopiesControllerTests : IDisposable
    {
        private readonly DbContextOptions<LibraryContext> _dbContextOptions;
        private readonly ILogger<BookCopiesController> _logger;

        public BookCopiesControllerTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase("LibraryTestDB")
                .Options;
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BookCopiesController>();
        }

        public void Dispose()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Database.EnsureDeleted();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task UpdateBookCopy_ReturnsNoContent()
        {
            using LibraryContext context = new(_dbContextOptions);
            BookCopy bookCopy = new() { CopyId = 1, BookId = 1, IsAvailable = true };
            context.BookCopies.Add(bookCopy);
            context.SaveChanges();

            BookCopiesController controller = new(context, _logger);
            bookCopy.IsAvailable = false;

            IActionResult result = await controller.UpdateBookCopy(bookCopy.CopyId, bookCopy);

            Assert.IsType<NoContentResult>(result);
            Assert.False(context.BookCopies.First().IsAvailable);
        }

        [Fact]
        public async Task PostBookCopy_WithExistingBookId_ReturnsCreatedAtAction()
        {
            using LibraryContext context = new(_dbContextOptions);
            Book book = new() { BookId = 1, Title = "Valid Book" };
            context.Books.Add(book);
            context.SaveChanges();

            BookCopiesController controller = new(context, _logger);
            BookCopy bookCopy = new() { BookId = 1, IsAvailable = true };

            ActionResult<BookCopy> result = await controller.PostBookCopy(bookCopy);

            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdAtActionResult.Value);
        }

        [Fact]
        public async Task PostBookCopy_WithNonExistentBookId_ReturnsNotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BookCopiesController controller = new(context, _logger);
            BookCopy bookCopy = new() { BookId = 999, IsAvailable = true };

            ActionResult<BookCopy> result = await controller.PostBookCopy(bookCopy);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBookCopies_ReturnsAllBookCopies()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.BookCopies.AddRange(
                new BookCopy { CopyId = 1, BookId = 1, IsAvailable = true },
                new BookCopy { CopyId = 2, BookId = 2, IsAvailable = false }
            );
            context.SaveChanges();

            BookCopiesController controller = new(context, _logger);
            ActionResult<IEnumerable<BookCopy>> result = await controller.GetBookCopies();

            ActionResult<IEnumerable<BookCopy>> actionResult = Assert.IsType<ActionResult<IEnumerable<BookCopy>>>(result);
            IEnumerable<BookCopy> returnValue = Assert.IsAssignableFrom<IEnumerable<BookCopy>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count());
            Assert.Contains(returnValue, bc => bc.CopyId == 1 && bc.IsAvailable);
            Assert.Contains(returnValue, bc => bc.CopyId == 2 && !bc.IsAvailable);
        }

        [Fact]
        public async Task PostBookCopy_ReturnsCreatedResponse()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Books.Add(new Book { BookId = 1, Title = "Sample Book", Author = "Sample Author" });
            context.SaveChanges();

            BookCopy bookCopy = new() { BookId = 1, IsAvailable = true };
            BookCopiesController controller = new(context, _logger);

            ActionResult<BookCopy> result = await controller.PostBookCopy(bookCopy);

            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            BookCopy responseCopy = Assert.IsType<BookCopy>(createdAtActionResult.Value);
            Assert.NotNull(responseCopy);
            Assert.Equal(bookCopy.BookId, responseCopy.BookId);
            Assert.Equal(bookCopy.IsAvailable, responseCopy.IsAvailable);
        }

        [Fact]
        public async Task PostBookCopy_InvalidModel_ReturnsBadRequest()
        {
            using LibraryContext context = new(_dbContextOptions);
            BookCopiesController controller = new(context, _logger);
            controller.ModelState.AddModelError("BookId", "Required");

            BookCopy bookCopy = new() { IsAvailable = true };
            ActionResult<BookCopy> result = await controller.PostBookCopy(bookCopy);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateBookCopy_NonExistent_ReturnsNotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BookCopiesController controller = new(context, _logger);
            BookCopy bookCopy = new() { CopyId = 1, BookId = 1, IsAvailable = false };

            IActionResult result = await controller.UpdateBookCopy(bookCopy.CopyId, bookCopy);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteBookCopy_ReturnsNoContent()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.BookCopies.AddRange(
                new BookCopy { CopyId = 2, BookId = 1, IsAvailable = true },
                new BookCopy { CopyId = 3, BookId = 1, IsAvailable = true }
            );
            context.SaveChanges();

            BookCopiesController controller = new(context, _logger);

            IActionResult result = await controller.DeleteBookCopy(2);

            Assert.IsType<NoContentResult>(result);
            List<BookCopy> remainingCopies = context.BookCopies.ToList();
            Assert.DoesNotContain(remainingCopies, bc => bc.CopyId == 2);
            Assert.Single(remainingCopies);
        }

        [Fact]
        public async Task DeleteBookCopy_NonExistent_ReturnsNotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BookCopiesController controller = new(context, _logger);

            IActionResult result = await controller.DeleteBookCopy(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteBookCopy_WithRelatedLoanRecord_ReturnsConflict()
        {
            using LibraryContext context = new(_dbContextOptions);
            Book book = new() { BookId = 1, Title = "Test Book" };
            BookCopy bookCopy = new() { CopyId = 1, BookId = 1, IsAvailable = true };
            User user = new() { UserId = 1, Name = "Test User", Email = "test@example.com" };
            LoanRecord loanRecord = new() { CopyId = 1, UserId = 1, LoanDate = DateTime.UtcNow, ExpectedReturnDate = DateTime.UtcNow.AddDays(14) };

            context.Books.Add(book);
            context.BookCopies.Add(bookCopy);
            context.Users.Add(user);
            context.LoanRecords.Add(loanRecord);
            context.SaveChanges();

            BookCopiesController controller = new(context, _logger);

            IActionResult result = await controller.DeleteBookCopy(1);

            Assert.IsType<ConflictObjectResult>(result);
        }
    }
}