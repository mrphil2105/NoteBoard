export abstract class NoteBox {
    protected readonly boardId: string;
    protected readonly noteDiv: HTMLDivElement;

    protected constructor(boardId: string, noteDiv: HTMLDivElement) {
        this.boardId = boardId;
        this.noteDiv = noteDiv;
    }

    public getNoteId() {
        return this.noteDiv.id.substr("note-".length);
    }
}

export class EditableNoteBox extends NoteBox {
    private readonly captionTextBox: HTMLInputElement;
    private readonly contentTextBox: HTMLTextAreaElement;

    private submitTimeoutId: number;

    public constructor(boardId: string, noteDiv: HTMLDivElement, captionTextBox: HTMLInputElement,
                       contentTextBox: HTMLTextAreaElement) {
        super(boardId, noteDiv);

        this.captionTextBox = captionTextBox;
        this.contentTextBox = contentTextBox;

        this.prepareInputs();
    }

    private prepareInputs() {
        this.captionTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));
        this.contentTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));

        // Auto grow and shrink the textarea on input.
        this.contentTextBox.addEventListener("input", EditableNoteBox.autoGrowTextArea);
    }

    private restartAutoSubmit() {
        clearTimeout(this.submitTimeoutId);

        if (this.captionTextBox.value || this.contentTextBox.value) {
            this.submitTimeoutId = setTimeout(this.submit.bind(this), 1000);
        }
    }

    private submit() {
        this.hasBeenCreated() ? this.update() : this.create();
    }

    private create() {
        return fetch("/Board/CreateNote/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getBody())
        })
            .then(response => response.json())
            .then(this.markAsCreated.bind(this));
    }

    private update() {
        return fetch("/Board/UpdateNote/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getBody())
        })
            .then(response => response.json())
            .then(result => !result.success && alert("Error: " + result.message));
    }

    private getHeaders() {
        return {
            "BoardId": this.boardId,
            "Accept": "application/json",
            "Content-Type": "application/json"
        };
    }

    private getBody() {
        return {
            "id": this.getNoteId(),
            "caption": this.captionTextBox.value,
            "content": this.contentTextBox.value
        };
    }

    private hasBeenCreated() {
        return this.noteDiv.id !== "note-0";
    }

    private markAsCreated(id: number) {
        this.noteDiv.id = "note-" + id;
        this.captionTextBox.placeholder = "Give me a caption";
    }

    private static autoGrowTextArea(event: Event) {
        const textArea = event.target as HTMLTextAreaElement;

        textArea.style.height = "0";
        textArea.style.height = (textArea.scrollHeight + 2) + "px";
    }
}
