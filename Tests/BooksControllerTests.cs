using LibraryManagementAPI.Controllers;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests
{
    public class BooksControllerTests : IDisposable
    {
        private readonly DbContextOptions<LibraryContext> _dbContextOptions;
        private readonly ILogger<BooksController> _logger;

        public BooksControllerTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase($"TestBooksDB_{Guid.NewGuid()}")
                .Options;
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BooksController>();
        }

        public void Dispose()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Database.EnsureDeleted();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetBooks_ReturnsAllBooks()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Books.AddRange(
                new Book { BookId = 1, Title = "Book One", Author = "Author A" },
                new Book { BookId = 2, Title = "Book Two", Author = "Author B" }
            );
            context.SaveChanges();

            BooksController controller = new(context, _logger);
            ActionResult<IEnumerable<Book>> result = await controller.GetBooks();
            ActionResult<IEnumerable<Book>> actionResult = Assert.IsType<ActionResult<IEnumerable<Book>>>(result);
            IEnumerable<Book> returnValue = Assert.IsAssignableFrom<IEnumerable<Book>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task GetBookById_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            ActionResult<Book> result = await controller.GetBook(3);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AddBook_ReturnsCreatedResponse()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book newBook = new() { Title = "Book Three", Author = "Author C" };
            ActionResult<Book> result = await controller.PostBook(newBook);
            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdAtActionResult);
            Assert.Equal("GetBook", createdAtActionResult.ActionName);
            Book book = Assert.IsType<Book>(createdAtActionResult.Value);
            Assert.Equal("Book Three", book.Title);
            Assert.True(book.BookId > 0);
        }

        [Fact]
        public async Task UpdateBook_Success()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Books.Add(new Book { BookId = 1, Title = "Book One", Author = "Author A" });
            context.SaveChanges();

            BooksController controller = new(context, _logger);
            Book updatedBook = new() { BookId = 1, Title = "Updated Book One", Author = "Author A Updated" };
            IActionResult result = await controller.PutBook(updatedBook.BookId, updatedBook);
            Assert.IsType<NoContentResult>(result);
            Book? book = await context.Books.FindAsync(1);
            Assert.Equal("Updated Book One", book?.Title);
            Assert.Equal("Author A Updated", book?.Author);
        }

        [Fact]
        public async Task DeleteBook_Success()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Books.Add(new Book { BookId = 2, Title = "Book Two", Author = "Author B" });
            context.SaveChanges();

            BooksController controller = new(context, _logger);
            IActionResult result = await controller.DeleteBook(2);
            Assert.IsType<NoContentResult>(result);
            Book? book = await context.Books.FindAsync(2);
            Assert.Null(book);
        }

        [Fact]
        public async Task DeleteBook_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            IActionResult result = await controller.DeleteBook(99);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddBook_InvalidTitle()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book invalidBook = new() { Title = "", Author = "Some Author" };
            ActionResult<Book> result = await controller.PostBook(invalidBook);
            ActionResult<Book> actionResult = Assert.IsAssignableFrom<ActionResult<Book>>(result);
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task AddBook_ValidBookWithEmptyAuthor()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book validBook = new() { Title = "Valid Title", Author = "" };
            ActionResult<Book> result = await controller.PostBook(validBook);
            ActionResult<Book> actionResult = Assert.IsAssignableFrom<ActionResult<Book>>(result);
            Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetBooks_EmptyDatabase()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            ActionResult<IEnumerable<Book>> result = await controller.GetBooks();
            ActionResult<IEnumerable<Book>> actionResult = Assert.IsType<ActionResult<IEnumerable<Book>>>(result);
            IEnumerable<Book> returnValue = Assert.IsAssignableFrom<IEnumerable<Book>>(actionResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public async Task AddBook_InvalidTitleLength()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book invalidBook = new() { Title = new string('A', 101), Author = "Author D" };
            ActionResult<Book> result = await controller.PostBook(invalidBook);
            ActionResult<Book> actionResult = Assert.IsAssignableFrom<ActionResult<Book>>(result);
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task AddBook_RequiredFieldsMissing()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book invalidBook = new() { Title = "", Author = "Some Author" };
            controller.ModelState.AddModelError("Title", "Title is required");
            ActionResult<Book> result = await controller.PostBook(invalidBook);
            ActionResult<Book> actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddBook_ValidatesCustomTitleRequirement()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book bookWithEmptyTitle = new() { Title = "", Author = "Author E" };
            ActionResult<Book> result = await controller.PostBook(bookWithEmptyTitle);
            ActionResult<Book> actionResult = Assert.IsAssignableFrom<ActionResult<Book>>(result);
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task UpdateBook_InvalidGenre()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book updatedBook = new() { BookId = 1, Title = "Valid Title", Author = "Author A", GenreProp = (Book.Genre)999 };
            IActionResult result = await controller.PutBook(updatedBook.BookId, updatedBook);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CheckBookDescriptionHandling()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book bookWithDescription = new() { Title = "Valid Title", Author = "Author F", Description = "A detailed description here." };
            ActionResult<Book> result = await controller.PostBook(bookWithDescription);
            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Book book = Assert.IsType<Book>(createdAtActionResult.Value);
            Assert.Equal("A detailed description here.", book.Description);
        }

        [Fact]
        public async Task AddBook_AllowsSameTitleAuthorDifferentGenre()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book book1 = new() { Title = "Common Title", Author = "Common Author", GenreProp = Book.Genre.SciFi };
            Book book2 = new() { Title = "Common Title", Author = "Common Author", GenreProp = Book.Genre.Romance };
            ActionResult<Book> result1 = await controller.PostBook(book1);
            ActionResult<Book> result2 = await controller.PostBook(book2);
            ActionResult<Book> actionResult1 = Assert.IsAssignableFrom<ActionResult<Book>>(result1);
            ActionResult<Book> actionResult2 = Assert.IsAssignableFrom<ActionResult<Book>>(result2);
            Assert.IsType<CreatedAtActionResult>(actionResult1.Result);
            Assert.IsType<CreatedAtActionResult>(actionResult2.Result);
        }

        [Fact]
        public async Task AddBook_RejectsSameTitleAuthorGenre()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book book1 = new() { Title = "Unique Title", Author = "Unique Author", GenreProp = Book.Genre.Romance };
            Book book2 = new() { Title = "Unique Title", Author = "Unique Author", GenreProp = Book.Genre.Romance };
            ActionResult<Book> result1 = await controller.PostBook(book1);
            ActionResult<Book> result2 = await controller.PostBook(book2);
            ActionResult<Book> actionResult1 = Assert.IsAssignableFrom<ActionResult<Book>>(result1);
            ActionResult<Book> actionResult2 = Assert.IsAssignableFrom<ActionResult<Book>>(result2);
            Assert.IsType<CreatedAtActionResult>(actionResult1.Result);
            Assert.IsType<BadRequestObjectResult>(actionResult2.Result);
        }

        [Fact]
        public async Task UpdateBook_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            BooksController controller = new(context, _logger);
            Book updatedBook = new() { BookId = 999, Title = "Nonexistent Book", Author = "Unknown Author" };
            IActionResult result = await controller.PutBook(updatedBook.BookId, updatedBook);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}