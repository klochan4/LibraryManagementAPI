using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Models
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<LoanRecord> LoanRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("Books");
                entity.HasKey(e => e.BookId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Author);
                entity.Property(e => e.GenreProp).HasConversion<int>();
                entity.Property(e => e.Description);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired();
            });

            modelBuilder.Entity<BookCopy>(entity =>
            {
                entity.ToTable("BookCopies");
                entity.HasKey(e => e.CopyId);
                entity.Property(e => e.BookId).IsRequired();
                entity.Property(e => e.IsAvailable).IsRequired();
                entity.HasOne<Book>().WithMany().HasForeignKey(e => e.BookId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LoanRecord>(entity =>
            {
                entity.ToTable("LoanRecords");
                entity.HasKey(e => e.LoanRecordId);
                entity.Property(e => e.CopyId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.LoanDate).IsRequired();
                entity.Property(e => e.ExpectedReturnDate).IsRequired();
                entity.Property(e => e.ActualReturnDate);
                entity.HasOne<BookCopy>().WithMany().HasForeignKey(e => e.CopyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LoanRecord>()
                        .HasOne<BookCopy>()
                        .WithMany()
                        .HasForeignKey(lr => lr.CopyId)
                        .OnDelete(DeleteBehavior.Restrict);
        }
    }
}