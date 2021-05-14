interface ISuccessResponse<T = void> {
    success: boolean;
    message: string;
    value: T;
}

export abstract class NoteBox {
    protected readonly boardId: string;
    protected readonly noteDiv: HTMLDivElement;

    protected constructor(boardId: string, noteDiv: HTMLDivElement) {
        this.boardId = boardId;
        this.noteDiv = noteDiv;
    }

    public getNoteId() {
        // Get the note id from the id attribute and parse to int.
        return parseInt(this.noteDiv.id.substr("note-".length));
    }

    public removeElement() {
        this.noteDiv.remove();
    }
}

export class EditableNoteBox extends NoteBox {
    private readonly captionTextBox: HTMLInputElement;
    private readonly contentTextBox: HTMLTextAreaElement;

    private submitTimeoutId: number;
    private deleteTimeoutId: number;
    private clearIndicationTimeoutId: number;

    public constructor(boardId: string, noteDiv: HTMLDivElement, captionTextBox: HTMLInputElement,
                       contentTextBox: HTMLTextAreaElement) {
        super(boardId, noteDiv);

        this.captionTextBox = captionTextBox;
        this.contentTextBox = contentTextBox;

        this.prepareInputs();
        this.autoGrowContent();
    }

    private prepareInputs() {
        // Add event listeners that restart the submission timeout on user input.
        this.captionTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));
        this.contentTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));

        // Auto grow and shrink the textarea on input.
        this.contentTextBox.addEventListener("input", this.autoGrowContent.bind(this));
    }

    private restartAutoSubmit() {
        // Every time the user types the timeouts should be cleared.
        clearTimeout(this.submitTimeoutId);
        clearTimeout(this.deleteTimeoutId);

        // One of either or both text boxes should have a value.
        if (this.captionTextBox.value || this.contentTextBox.value) {
            // Set a timeout that uploads the note.
            this.submitTimeoutId = setTimeout(this.submit.bind(this), 1000);
            // Indicate that the note will be uploaded in a bit.
            this.indicateWaiting();
        } else {
            // Set a timeout that deletes the note and the note box.
            this.deleteTimeoutId = setTimeout(this.delete.bind(this), 5000);
            // Indicate that the note will be deleted in a moment.
            this.indicateDeleting();
        }
    }

    private submit() {
        // Indicate that the note is being uploaded.
        this.indicateSaving();

        // Depending on if the note is an existing one or has yet to be created, it should either be updated or created.
        (this.hasBeenCreated() ? this.update() : this.create())
            // Indicate that the note has been uploaded after the update/creation.
            .then(this.indicateSaved.bind(this));
    }

    private create() {
        return fetch("/Note/Create/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getBody())
        })
            .then(response => response.json())
            .then((result: ISuccessResponse<number>) => {
                if (!result.success) {
                    alert("Error: " + result.message);
                    return;
                }

                // The note has been created successfully, change away from the state that creates a new note box.
                this.markAsCreated(result.value);
            });
    }

    private update() {
        return fetch("/Note/Update/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getBody())
        })
            .then(response => response.json())
            .then((result: ISuccessResponse) => !result.success && alert("Error: " + result.message));
    }

    private delete() {
        return fetch("/Note/Delete/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getNoteId())
        })
            .then(response => response.json())
            .then((result: ISuccessResponse) => {
                if (result.success) {
                    // The note has been removed, so we remove the note box to reflect that.
                    this.removeElement();
                    return;
                }

                alert("Error: " + result.message);
            });
    }

    private getHeaders() {
        return {
            "BoardId": this.boardId,
            // Specify that the content is JSON and that we expect a JSON response.
            "Accept": "application/json",
            "Content-Type": "application/json"
        };
    }

    private getBody() {
        return {
            // Indicate what note that needs updating (this will be the "invalid value" 0 when creating a new note).
            "id": this.getNoteId(),
            "caption": this.captionTextBox.value,
            "content": this.contentTextBox.value
        };
    }

    private hasBeenCreated() {
        // A note box represents a created note when the id is greater than 0.
        return this.noteDiv.id !== "note-0";
    }

    private markAsCreated(id: number) {
        // Marking a note box as created involves setting its id to a value greater than 0.
        this.noteDiv.id = "note-" + id;
        // Get rid of the text that says "Type here to create a note".
        this.captionTextBox.placeholder = "Give me a caption";
    }

    private autoGrowContent() {
        this.contentTextBox.style.height = "0";
        this.contentTextBox.style.height = (this.contentTextBox.scrollHeight + 8) + "px";
    }

    //
    // Status indication
    //

    private indicateWaiting() {
        // Used to prevent the indication from being cleared, if the user starts typing before the timeout.
        clearTimeout(this.clearIndicationTimeoutId);

        this.clearIndication();
        this.noteDiv.classList.add("waiting");
    }

    private indicateSaving() {
        this.clearIndication();
        this.noteDiv.classList.add("saving");
    }

    private indicateSaved() {
        this.clearIndication();
        this.noteDiv.classList.add("saved");

        // Clear the indication after 5 seconds.
        this.clearIndicationTimeoutId = setTimeout(this.clearIndication.bind(this), 5000);
    }

    private indicateDeleting() {
        // Used to prevent the indication from being cleared, if the user starts typing before the timeout.
        clearTimeout(this.clearIndicationTimeoutId);

        this.clearIndication();
        this.noteDiv.classList.add("deleting");
    }

    private clearIndication() {
        this.noteDiv.classList.remove("waiting", "saving", "saved", "deleting");
    }
}

export class ReadOnlyNoteBox extends NoteBox {
    private readonly captionHeading: HTMLHeadingElement;
    private readonly contentParagraph: HTMLParagraphElement;

    public constructor(boardId: string, noteDiv: HTMLDivElement, captionHeading: HTMLHeadingElement,
                       contentParagraph: HTMLParagraphElement) {
        super(boardId, noteDiv);

        this.captionHeading = captionHeading;
        this.contentParagraph = contentParagraph;
    }

    public setCaption(caption: string) {
        this.captionHeading.innerText = caption;
    }

    public setContent(content: string) {
        this.contentParagraph.innerText = content;
    }
}
