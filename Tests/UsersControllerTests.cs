using LibraryManagementAPI.Controllers;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests
{
    public class UsersControllerTests : IDisposable
    {
        private readonly DbContextOptions<LibraryContext> _dbContextOptions;
        private readonly ILogger<UsersController> _logger;

        public UsersControllerTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase($"TestUsersDB_{Guid.NewGuid()}")
                .Options;
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UsersController>();
        }

        public void Dispose()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Database.EnsureDeleted();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetUsers_ReturnsAllUsers()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.AddRange(
                new User { Name = "User One", Email = "userone@example.com" },
                new User { Name = "User Two", Email = "usertwo@example.com" }
            );
            context.SaveChanges();

            UsersController controller = new(context, _logger);
            ActionResult<IEnumerable<User>> result = await controller.GetUsers();
            ActionResult<IEnumerable<User>> actionResult = Assert.IsType<ActionResult<IEnumerable<User>>>(result);
            IEnumerable<User> returnValue = Assert.IsAssignableFrom<IEnumerable<User>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task GetUserById_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            ActionResult<User> result = await controller.GetUser(3);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetUserById_ReturnsUser()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "User One", Email = "userone@example.com" });
            context.SaveChanges();

            UsersController controller = new(context, _logger);
            ActionResult<User> result = await controller.GetUser(1);
            ActionResult<User> actionResult = Assert.IsType<ActionResult<User>>(result);
            User returnValue = Assert.IsType<User>(actionResult.Value);
            Assert.Equal("User One", returnValue.Name);
        }

        [Fact]
        public async Task AddUser_ReturnsCreatedResponse()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            User newUser = new() { Name = "User Three", Email = "userthree@example.com" };
            ActionResult<User> result = await controller.PostUser(newUser);
            CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdAtActionResult);
            Assert.Equal("GetUser", createdAtActionResult.ActionName);
            User user = Assert.IsType<User>(createdAtActionResult.Value);
            Assert.Equal("User Three", user.Name);
            Assert.True(user.UserId > 0);
        }

        [Fact]
        public async Task UpdateUser_Success()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "User One", Email = "userone@example.com" });
            context.SaveChanges();

            UsersController controller = new(context, _logger);
            User updatedUser = new() { UserId = 1, Name = "Updated User One", Email = "updateduserone@example.com" };
            User? local = context.Set<User>().Local.FirstOrDefault(entry => entry.UserId.Equals(updatedUser.UserId));
            if (local != null)
            {
                context.Entry(local).State = EntityState.Detached;
            }
            IActionResult result = await controller.PutUser(updatedUser.UserId, updatedUser);
            Assert.IsType<NoContentResult>(result);
            User? user = await context.Users.FindAsync(1);
            Assert.Equal("Updated User One", user?.Name);
            Assert.Equal("updateduserone@example.com", user?.Email);
        }

        [Fact]
        public async Task UpdateUser_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            User updatedUser = new() { UserId = 99, Name = "Nonexistent User", Email = "nonexistentuser@example.com" };
            IActionResult result = await controller.PutUser(updatedUser.UserId, updatedUser);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUser_InvalidModelState_ReturnsBadRequest()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            User updatedUser = new() { UserId = 1, Name = "", Email = "updateduserone@example.com" };
            controller.ModelState.AddModelError("Name", "Required");
            IActionResult result = await controller.PutUser(updatedUser.UserId, updatedUser);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUser_Success()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 2, Name = "User Two", Email = "usertwo@example.com" });
            context.SaveChanges();

            UsersController controller = new(context, _logger);
            IActionResult result = await controller.DeleteUser(2);
            Assert.IsType<NoContentResult>(result);
            User? user = await context.Users.FindAsync(2);
            Assert.Null(user);
        }

        [Fact]
        public async Task DeleteUser_NotFound()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            IActionResult result = await controller.DeleteUser(99);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddUser_InvalidData()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            User invalidUser = new() { Name = "Yu", Email = "notanemail" };
            ActionResult<User> result = await controller.PostUser(invalidUser);
            ActionResult<User> actionResult = Assert.IsAssignableFrom<ActionResult<User>>(result);
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task AddUser_DuplicateEmail_ReturnsConflict()
        {
            using LibraryContext context = new(_dbContextOptions);
            context.Users.Add(new User { UserId = 1, Name = "User One", Email = "userone@example.com" });
            context.SaveChanges();

            UsersController controller = new(context, _logger);
            User newUser = new() { Name = "User Four", Email = "userone@example.com" };
            ActionResult<User> result = await controller.PostUser(newUser);
            ActionResult<User> actionResult = Assert.IsAssignableFrom<ActionResult<User>>(result);
            Assert.IsType<ConflictObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetUsers_EmptyDatabase()
        {
            using LibraryContext context = new(_dbContextOptions);
            UsersController controller = new(context, _logger);
            ActionResult<IEnumerable<User>> result = await controller.GetUsers();
            ActionResult<IEnumerable<User>> actionResult = Assert.IsType<ActionResult<IEnumerable<User>>>(result);
            IEnumerable<User> returnValue = Assert.IsAssignableFrom<IEnumerable<User>>(actionResult.Value);
            Assert.Empty(returnValue);
        }
    }
}