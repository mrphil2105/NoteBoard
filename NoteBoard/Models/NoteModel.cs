using System.ComponentModel.DataAnnotations;

namespace NoteBoard.Models
{
    public class NoteModel
    {
        public int Id { get; init; }

        [MaxLength(100)]
        public string? Caption { get; init; }

        [MaxLength(1000)]
        public string? Content { get; init; }
    }
}
