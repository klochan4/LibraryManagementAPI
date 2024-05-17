using LibraryManagementAPI.Controllers;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Tests
{
    public class LoansControllerTests : IDisposable
    {
        private readonly DbContextOptions<LibraryContext> _dbContextOptions;
        private readonly ILogger<LoansController> _logger;

        public LoansControllerTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase("TestLoansDB")
                .Options;
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<LoansController>();
        }

        public void Dispose()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Database.EnsureDeleted();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetLoans_ReturnsAllLoans()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.AddRange(
                new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" },
                new User { UserId = 2, Name = "Jane Doe", Email = "jane.doe@example.com" }
            );
            context.Books.AddRange(
                new Book { BookId = 1, Title = "1984", Author = "George Orwell" },
                new Book { BookId = 2, Title = "The Hobbit", Author = "J.R.R. Tolkien" }
            );
            context.BookCopies.AddRange(
                new BookCopy { CopyId = 1, BookId = 1, IsAvailable = true },
                new BookCopy { CopyId = 2, BookId = 2, IsAvailable = false }
            );
            context.LoanRecords.AddRange(
                new LoanRecord
                {
                    LoanRecordId = 1,
                    CopyId = 1,
                    UserId = 1,
                    LoanDate = DateTime.Now.AddDays(-15),
                    ExpectedReturnDate = DateTime.Now.AddDays(15),
                    ActualReturnDate = null
                },
                new LoanRecord
                {
                    LoanRecordId = 2,
                    CopyId = 2,
                    UserId = 2,
                    LoanDate = DateTime.Now.AddDays(-30),
                    ExpectedReturnDate = DateTime.Now.AddDays(30),
                    ActualReturnDate = DateTime.Now.AddDays(-5)
                }
            );
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            ActionResult<IEnumerable<LoanRecord>> result = await controller.GetLoans();
            ActionResult<IEnumerable<LoanRecord>> actionResult = Assert.IsType<ActionResult<IEnumerable<LoanRecord>>>(result);
            IEnumerable<LoanRecord> returnValue = Assert.IsAssignableFrom<IEnumerable<LoanRecord>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task GetLoan_ExistingId_ReturnsLoanRecord()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" });
            context.Books.Add(new Book { BookId = 1, Title = "1984", Author = "George Orwell" });
            context.BookCopies.Add(new BookCopy { CopyId = 1, BookId = 1, IsAvailable = true });
            context.LoanRecords.Add(new LoanRecord
            {
                LoanRecordId = 1,
                CopyId = 1,
                UserId = 1,
                LoanDate = DateTime.Now.AddDays(-15),
                ExpectedReturnDate = DateTime.Now.AddDays(15),
                ActualReturnDate = null
            });
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            ActionResult<LoanRecord> result = await controller.GetLoan(1);
            ActionResult<LoanRecord> actionResult = Assert.IsType<ActionResult<LoanRecord>>(result);
            LoanRecord loanRecord = Assert.IsType<LoanRecord>(actionResult.Value);
            Assert.Equal(1, loanRecord.LoanRecordId);
        }

        [Fact]
        public async Task GetLoan_NonExistingId_ReturnsNotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            LoansController controller = new(context, _logger);
            ActionResult<LoanRecord> result = await controller.GetLoan(99);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostLoan_ValidData_ReturnsCreatedResponse()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" });
            context.Books.Add(new Book { BookId = 1, Title = "1984", Author = "George Orwell" });
            context.BookCopies.Add(new BookCopy { CopyId = 1, BookId = 1, IsAvailable = true });
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            LoanRecord newLoanRecord = new()
            {
                CopyId = 1,
                UserId = 1,
                LoanDate = DateTime.Now,
                ExpectedReturnDate = DateTime.Now.AddDays(14)
            };

            ActionResult<LoanRecord> result = await controller.PostLoan(newLoanRecord);
            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdAtActionResult.Value);
            LoanRecord loanRecord = Assert.IsType<LoanRecord>(createdAtActionResult.Value);
            Assert.True(loanRecord.LoanRecordId > 0);
            Assert.False(context.BookCopies.First(b => b.CopyId == loanRecord.CopyId).IsAvailable);
        }

        [Fact]
        public async Task ReturnLoan_ValidLoan_ReturnsNoContent()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" });
            context.Books.Add(new Book { BookId = 1, Title = "1984", Author = "George Orwell" });
            context.BookCopies.Add(new BookCopy { CopyId = 1, BookId = 1, IsAvailable = false });
            context.LoanRecords.Add(new LoanRecord
            {
                LoanRecordId = 1,
                CopyId = 1,
                UserId = 1,
                LoanDate = DateTime.Now.AddDays(-10),
                ExpectedReturnDate = DateTime.Now.AddDays(10),
                ActualReturnDate = null
            });
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            LoanRecord loanRecord = context.LoanRecords.First(lr => lr.ActualReturnDate == null);

            IActionResult result = await controller.ReturnLoan(loanRecord.LoanRecordId);
            Assert.IsType<NoContentResult>(result);

            LoanRecord? updatedLoanRecord = await context.LoanRecords.FindAsync(loanRecord.LoanRecordId);
            Assert.NotNull(updatedLoanRecord);
            Assert.NotNull(updatedLoanRecord.ActualReturnDate);

            BookCopy? updatedBookCopy = await context.BookCopies.FindAsync(updatedLoanRecord.CopyId);
            Assert.NotNull(updatedBookCopy);
            Assert.True(updatedBookCopy.IsAvailable);
        }

        [Fact]
        public async Task ReturnLoan_AlreadyReturned_ReturnsBadRequest()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" });
            context.Books.Add(new Book { BookId = 1, Title = "1984", Author = "George Orwell" });
            context.BookCopies.Add(new BookCopy { CopyId = 1, BookId = 1, IsAvailable = true });
            context.LoanRecords.Add(new LoanRecord
            {
                LoanRecordId = 1,
                CopyId = 1,
                UserId = 1,
                LoanDate = DateTime.Now.AddDays(-20),
                ExpectedReturnDate = DateTime.Now.AddDays(10),
                ActualReturnDate = DateTime.Now.AddDays(-5)
            });
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            LoanRecord loanRecord = context.LoanRecords.First(lr => lr.ActualReturnDate != null);

            IActionResult result = await controller.ReturnLoan(loanRecord.LoanRecordId);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteLoan_ValidId_ReturnsNoContent()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "John Doe", Email = "john.doe@example.com" });
            context.Books.Add(new Book { BookId = 1, Title = "1984", Author = "George Orwell" });
            context.BookCopies.Add(new BookCopy { CopyId = 1, BookId = 1, IsAvailable = false });
            context.LoanRecords.Add(new LoanRecord
            {
                LoanRecordId = 1,
                CopyId = 1,
                UserId = 1,
                LoanDate = DateTime.Now.AddDays(-10),
                ExpectedReturnDate = DateTime.Now.AddDays(10),
                ActualReturnDate = null
            });
            context.SaveChanges();

            LoansController controller = new(context, _logger);
            LoanRecord loanRecord = context.LoanRecords.First();

            IActionResult result = await controller.DeleteLoan(loanRecord.LoanRecordId);
            Assert.IsType<NoContentResult>(result);

            LoanRecord? deletedLoanRecord = await context.LoanRecords.FindAsync(loanRecord.LoanRecordId);
            Assert.Null(deletedLoanRecord);
        }

        [Fact]
        public async Task DeleteLoan_NonExistingId_ReturnsNotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            LoansController controller = new(context, _logger);
            IActionResult result = await controller.DeleteLoan(99);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}