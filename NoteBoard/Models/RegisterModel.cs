using System.ComponentModel.DataAnnotations;

namespace NoteBoard.Models
{
    public class RegisterModel
    {
        [Required]
        [MaxLength(256)]
        public string? Username { get; init; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; init; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Confirm Password")]
        public string? PasswordConfirmation { get; init; }
    }
}
