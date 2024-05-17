using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementAPI.Models
{
    [Table("LoanRecords")]
    public class LoanRecord : IValidatableObject
    {
        [Key]
        [Required(ErrorMessage = "LoanRecordId is required")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LoanRecordId { get; set; }

        [Required(ErrorMessage = "CopyId is required")]
        [ForeignKey("BookCopy")]
        public int CopyId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "LoanDate is required")]
        public DateTime LoanDate { get; set; }

        [Required(ErrorMessage = "ExpectedReturnDate is required")]
        public DateTime ExpectedReturnDate { get; set; }

        public DateTime? ActualReturnDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (LoanDate > DateTime.Now)
            {
                yield return new ValidationResult("Loan date cannot be in the future.", new[] { nameof(LoanDate) });
            }

            if (ExpectedReturnDate < LoanDate)
            {
                yield return new ValidationResult("Expected return date must be after loan date.", new[] { nameof(ExpectedReturnDate) });
            }
        }
    }
}