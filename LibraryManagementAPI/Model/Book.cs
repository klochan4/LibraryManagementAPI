using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models
{
    [Table("Books")]
    public class Book : IValidatableObject
    {
        public enum Genre
        {
            Mystery, 
            Romance, 
            SciFi, 
            Fantasy,
            Biography, 
            History, 
            SelfHelp, 
            Other
        }


        [Key]
        [Required(ErrorMessage = "BookId is required")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters long.")]
        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public Genre GenreProp { get; set; } = Genre.Other;

        public string Description { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Title))
            {
                yield return new ValidationResult("Title cannot be empty.", new[] { nameof(Title) });
            }

            if (!Enum.IsDefined(typeof(Genre), GenreProp))
            {
                yield return new ValidationResult($"Invalid genre value: {GenreProp}.", [nameof(GenreProp)]);
            }
        }
    }
}