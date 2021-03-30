// Use .js extension to fix output files importing without .js extension.
import { EditableNoteBox } from "./note-box.js";

interface INoteModel {
    id: number;
    caption: string;
    content: string;
}

(() => {
    const path = window.location.pathname;
    const boardId = path.substr(path.lastIndexOf("/") + 1);
    const boardDiv = document.getElementById("board") as HTMLDivElement;

    loadBoardNotes();

    function loadBoardNotes() {
        const getNotesPromise = getNotes();
        const getOwnedPromise = getOwnedNotes();

        Promise.all([getNotesPromise, getOwnedPromise])
            // Add editable note boxes for all owned notes.
            .then(([noteModels, ownedIds]) => noteModels.filter(noteModel => ownedIds.indexOf(noteModel.id) >= 0)
                .forEach(noteModel => createEditableNote(noteModel.id, noteModel.caption, noteModel.content)))
            // Add the editable note box that create a new note.
            .then(() => createEditableNote(0));
    }

    function getNotes(): Promise<INoteModel[]> {
        return fetch(`/Note/GetAll/?boardId=${boardId}`)
            .then(response => response.json());
    }

    function getOwnedNotes(): Promise<number[]> {
        return fetch(`/Note/GetOwned/?boardId=${boardId}`)
            .then(response => response.json());
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

        new EditableNoteBox(boardId, noteDiv, captionTextBox, contentTextBox);
    }
})();
