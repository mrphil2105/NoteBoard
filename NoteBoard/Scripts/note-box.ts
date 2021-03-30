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
        this.captionTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));
        this.contentTextBox.addEventListener("input", this.restartAutoSubmit.bind(this));

        // Auto grow and shrink the textarea on input.
        this.contentTextBox.addEventListener("input", this.autoGrowContent.bind(this));
    }

    private restartAutoSubmit() {
        clearTimeout(this.submitTimeoutId);

        if (this.captionTextBox.value || this.contentTextBox.value) {
            this.submitTimeoutId = setTimeout(this.submit.bind(this), 1000);
            this.indicateWaiting();
        } else {
            // We never submit an empty note, clear the indication.
            this.clearIndication();
        }
    }

    private submit() {
        this.indicateSaving();

        (this.hasBeenCreated() ? this.update() : this.create())
            .then(this.indicateSaved.bind(this));
    }

    private create() {
        return fetch("/Note/Create/", {
            method: "POST",
            headers: this.getHeaders(),
            body: JSON.stringify(this.getBody())
        })
            .then(response => response.json())
            .then(this.markAsCreated.bind(this));
    }

    private update() {
        return fetch("/Note/Update/", {
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

        this.clearIndicationTimeoutId = setTimeout(this.clearIndication.bind(this), 5000);
    }

    private clearIndication() {
        this.noteDiv.classList.remove("waiting", "saving", "saved");
    }
}
