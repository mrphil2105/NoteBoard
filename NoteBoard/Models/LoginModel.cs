using System.ComponentModel.DataAnnotations;

namespace NoteBoard.Models
{
    public class LoginModel
    {
        [Required]
        public string? Username { get; init; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; init; }

        [Display(Name = "Keep me signed in")]
        public bool IsPersistent { get; init; }

        public string? ReturnUrl { get; init; }
    }
}
