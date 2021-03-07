using System;

namespace NoteBoard.Data
{
    public class Note
    {
        public int Id { get; set; }

        public string Caption { get; set; } = null!;

        public string Content { get; set; } = null!;

        public string AccessToken { get; set; } = null!;

        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset LastEditDate { get; set; }

        // Navigation properties

        public Board Board { get; set; } = null!;
    }
}
