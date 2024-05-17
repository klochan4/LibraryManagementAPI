using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace LibraryManagementAPI.Models
{
    [Table("Users")]
    public class User : IValidatableObject
    {
        [Key]
        [Required(ErrorMessage = "UserId is required")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters long.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = string.Empty;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name))
            {
                yield return new ValidationResult("Name cannot be empty.", new[] { nameof(Name) });
            }

            if (string.IsNullOrEmpty(Email))
            {
                yield return new ValidationResult("Email cannot be empty.", new[] { nameof(Email) });
            }
            else
            {
                if (!IsValidEmail(Email))
                {
                    yield return new ValidationResult("Invalid email format.", new[] { nameof(Email) });
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, pattern);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}