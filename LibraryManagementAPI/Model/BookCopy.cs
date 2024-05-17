using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementAPI.Models
{
    [Table("BookCopies")]
    public class BookCopy
    {
        [Key]
        [Required(ErrorMessage = "CopyId is required")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CopyId { get; set; }

        [Required(ErrorMessage = "BookId is required")]
        [ForeignKey("Book")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public bool IsAvailable { get; set; }
    }
}