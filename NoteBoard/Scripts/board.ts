// Use .js extension to fix output files importing without .js extension.
import { EditableNoteBox, ReadOnlyNoteBox } from "./note-box.js";

interface INoteModel {
    id: number;
    caption: string;
    content: string;
}

(() => {
    const path = window.location.pathname;
    const boardId = path.substr(path.lastIndexOf("/") + 1);
    const boardDiv = document.getElementById("board") as HTMLDivElement;

    const editableNoteBoxes: EditableNoteBox[] = [];
    const readOnlyNoteBoxes: ReadOnlyNoteBox[] = [];

    loadBoardNotes();

    function loadBoardNotes() {
        const getNotesPromise = getNotes();
        const getOwnedPromise = getOwnedNotes();

        Promise.all([getNotesPromise, getOwnedPromise])
            .then(([noteModels, ownedIds]) => {
                noteModels.forEach(noteModel => {
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
                const noteModel = noteModels.find(noteModel => noteModel.id === noteBox.getNoteId());

                if (!noteModel) {
                    return true;
                }

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
            noteModels.filter(noteModel => {
                const isAddedAsEditable = editableNoteBoxes.find(noteBox => noteBox.getNoteId() === noteModel.id);
                const isAddedAsReadOnly = readOnlyNoteBoxes.find(noteBox => noteBox.getNoteId() === noteModel.id);

                return !isAddedAsEditable && !isAddedAsReadOnly;
            })
                .forEach(noteModel => createReadOnlyNote(noteModel.id, noteModel.caption, noteModel.content));
        });
    }

    function createEditableNote(noteId: number, caption: string = "", content: string = "") {
        const noteDiv = document.createElement("div");
        noteDiv.id = "note-" + noteId;
        noteDiv.className = "col note";
        boardDiv.appendChild(noteDiv);

        const captionTextBox = document.createElement("input");
        captionTextBox.className = "form-control note-caption";
        captionTextBox.type = "text";
        captionTextBox.maxLength = 100;
        captionTextBox.placeholder = noteId > 0 ? "Give me a caption" : "Type here to create a note";
        captionTextBox.value = caption;
        noteDiv.appendChild(captionTextBox);

        const contentTextBox = document.createElement("textarea");
        contentTextBox.className = "form-control note-content";
        contentTextBox.maxLength = 1000;
        contentTextBox.placeholder = "Write something relevant or interesting";
        contentTextBox.value = content;
        noteDiv.appendChild(contentTextBox);

        const noteBox = new EditableNoteBox(boardId, noteDiv, captionTextBox, contentTextBox);
        editableNoteBoxes.push(noteBox);
    }

    function createReadOnlyNote(noteId: number, caption: string, content: string) {
        const noteDiv = document.createElement("div");
        noteDiv.id = "note-" + noteId;
        noteDiv.className = "col note";
        const creatorNoteBox = document.getElementById("note-0");
        boardDiv.insertBefore(noteDiv, creatorNoteBox);

        const captionHeading = document.createElement("h5");
        captionHeading.className = "note-caption";
        captionHeading.innerText = caption;
        noteDiv.appendChild(captionHeading);

        const contentParagraph = document.createElement("p");
        contentParagraph.className = "note-content";
        contentParagraph.innerText = content;
        noteDiv.appendChild(contentParagraph);

        const noteBox = new ReadOnlyNoteBox(boardId, noteDiv, captionHeading, contentParagraph);
        readOnlyNoteBoxes.push(noteBox);
    }
})();
