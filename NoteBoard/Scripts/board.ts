// Use .js extension to fix output files importing without .js extension.
import { EditableNoteBox, ReadOnlyNoteBox } from "./note-box.js";

interface INoteModel {
    id: number;
    caption: string;
    content: string;
}

(() => {
    // The board id is in the URL and can be extracted below if probably formatted.
    const path = window.location.pathname;
    const boardId = path.substr(path.lastIndexOf("/") + 1);
    // Create the container for the board.
    const boardDiv = document.getElementById("board") as HTMLDivElement;

    const readOnlyNoteBoxes: ReadOnlyNoteBox[] = [];

    loadBoardNotes();

    function loadBoardNotes() {
        // Start getting the notes and the owned notes at the same time.
        const getNotesPromise = getNotes();
        const getOwnedPromise = getOwnedNotes();

        // Wait for both Promises to resolve and use both results.
        Promise.all([getNotesPromise, getOwnedPromise])
            .then(([noteModels, ownedIds]) => {
                noteModels.forEach(noteModel => {
                    // Determine if the note box is owned by the current user, by checking if the id is in the array.
                    const isOwned = ownedIds.indexOf(noteModel.id) >= 0;

                    if (isOwned) {
                        // Add editable note box for this owned note.
                        createEditableNote(noteModel.id, noteModel.caption, noteModel.content);

                        return;
                    }

                    // Add read-only note box for this non-owned note.
                    createReadOnlyNote(noteModel.id, noteModel.caption, noteModel.content)
                })
            })
            .then(() => {
                // Add the editable note box that creates a new note.
                createEditableNote(0);
                // Start a timer that auto updates every read-only note.
                setInterval(updateNoteBoxes, 2000);
            });
    }

    function getNotes(): Promise<INoteModel[]> {
        return fetch(`/Note/GetAll/?boardId=${boardId}`)
            .then(response => response.json());
    }

    function getOwnedNotes(): Promise<number[]> {
        return fetch(`/Note/GetOwned/?boardId=${boardId}`)
            .then(response => response.json());
    }

    function updateNoteBoxes() {
        getNotes().then(noteModels => {
            // Update all existing note boxes.
            const noteBoxesToRemove = readOnlyNoteBoxes.filter(noteBox => {
                // Find a note model with the same id as the note box.
                const noteModel = noteModels.find(noteModel => noteModel.id === noteBox.getNoteId());

                if (!noteModel) {
                    // The note model does not exist, indicate that the note box should be removed.
                    return true;
                }

                // Set the caption and content for the note box.
                noteBox.setCaption(noteModel.caption);
                noteBox.setContent(noteModel.content);

                return false;
            });

            // Remove the read-only note boxes from the array.
            noteBoxesToRemove.forEach(noteBox => {
                // Remove the associated DOM element.
                noteBox.removeElement();

                const index = readOnlyNoteBoxes.indexOf(noteBox);
                readOnlyNoteBoxes.splice(index, 1);
            });

            // Add any newly created notes to the board.
            noteModels.filter(noteModel => !document.getElementById(`note-${noteModel.id}`))
                .forEach(noteModel => createReadOnlyNote(noteModel.id, noteModel.caption, noteModel.content));
        });
    }

    function createEditableNote(noteId: number, caption: string = "", content: string = "") {
        const noteDiv = document.createElement("div");
        noteDiv.id = "note-" + noteId;
        noteDiv.className = "col note";
        // Add new editable note to the board.
        boardDiv.appendChild(noteDiv);

        const captionTextBox = document.createElement("input");
        captionTextBox.className = "form-control note-caption";
        captionTextBox.type = "text";
        captionTextBox.maxLength = 100;
        captionTextBox.placeholder = noteId > 0 ? "Give me a caption" : "Type here to create a note";
        captionTextBox.value = caption;
        // Add the input to the note box.
        noteDiv.appendChild(captionTextBox);

        const contentTextBox = document.createElement("textarea");
        contentTextBox.className = "form-control note-content";
        contentTextBox.maxLength = 1000;
        contentTextBox.placeholder = "Write something relevant or interesting";
        contentTextBox.value = content;
        // Add the textarea to the note box.
        noteDiv.appendChild(contentTextBox);

        new EditableNoteBox(boardId, noteDiv, captionTextBox, contentTextBox);
    }

    function createReadOnlyNote(noteId: number, caption: string, content: string) {
        const noteDiv = document.createElement("div");
        noteDiv.id = "note-" + noteId;
        noteDiv.className = "col note";
        const creatorNoteBox = document.getElementById("note-0");
        // Insert new static note to the board before the note box that creates a new note.
        boardDiv.insertBefore(noteDiv, creatorNoteBox);

        const captionHeading = document.createElement("h5");
        captionHeading.className = "note-caption";
        captionHeading.innerText = caption;
        // Add the heading to the note box.
        noteDiv.appendChild(captionHeading);

        const contentParagraph = document.createElement("p");
        contentParagraph.className = "note-content";
        contentParagraph.innerText = content;
        // Add the paragraph to the note box.
        noteDiv.appendChild(contentParagraph);

        const noteBox = new ReadOnlyNoteBox(boardId, noteDiv, captionHeading, contentParagraph);
        readOnlyNoteBoxes.push(noteBox);
    }
})();
