using System;
using System.Collections.Generic;

namespace NoteBoard.Data
{
    public class Board
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset LastEditDate { get; set; }

        // Navigation properties

        public AppUser User { get; set; } = null!;

        public ICollection<Note> Notes { get; set; } = null!;
    }
}
