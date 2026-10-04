const notesInput =
    document.getElementById("notes");

const audienceSelect =
    document.getElementById("audience");

const modelSelect =
    document.getElementById("model");

const generateButton =
    document.getElementById("generateButton");

const generateText =
    document.getElementById("generateText");

const sampleButton =
    document.getElementById("sampleButton");

const copyButton =
    document.getElementById("copyButton");

const downloadButton =
    document.getElementById("downloadButton");

const resultContainer =
    document.getElementById("result");

const emptyState =
    document.getElementById("emptyState");

const loadingState =
    document.getElementById("loadingState");

const errorMessage =
    document.getElementById("errorMessage");

const localStatus =
    document.getElementById("localStatus");

const statusTitle =
    document.getElementById("statusTitle");

const statusDescription =
    document.getElementById("statusDescription");

const characterCount =
    document.getElementById("characterCount");


let currentReport = "";


sampleButton.addEventListener(
    "click",
    () => {

        notesInput.value =
`Breakfast at 8. Ate about half.
Medicine after breakfast.
Went outside around 10.
Said knee was hurting a little.
Lunch at 12:30.
Drank two glasses of water.
Slept around 2.
Doctor appointment tomorrow.`;

    }
);


generateButton.addEventListener(
    "click",
    generateHandover
);


async function generateHandover() {

    const notes = notesInput.value.trim();

    if (!notes) {

        showError(
            "Please enter some care notes first."
        );

        return;
    }


    clearError();

    setLoading(true);


    try {

        const response =
            await fetch(
                "/api/handover",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body: JSON.stringify({
                        notes: notes,
                        audience:
                            audienceSelect.value,
                        model:
                            modelSelect.value
                    })
                }
            );


        if (!response.ok) {

            const errorData =
                await response.json();

            throw new Error(
                errorData.detail ||
                errorData.message ||
                "Something went wrong."
            );
        }


        const data =
            await response.json();


        currentReport =
            data.result;


        renderMarkdown(
            currentReport
        );


        copyButton.disabled = false;
        downloadButton.disabled = false;

    }
    catch (error) {

        showError(
            error.message
        );

        emptyState.classList.remove(
            "hidden"
        );

    }
    finally {

        setLoading(false);

    }

}


function setLoading(isLoading) {

    generateButton.disabled =
        isLoading;


    if (isLoading) {

        generateText.textContent =
            "Generating...";

        emptyState.classList.add(
            "hidden"
        );

        resultContainer.classList.add(
            "hidden"
        );

        loadingState.classList.remove(
            "hidden"
        );

    }
    else {

        generateText.textContent =
            "Generate handover";

        loadingState.classList.add(
            "hidden"
        );

    }

}


function renderMarkdown(markdown) {

    let html = escapeHtml(markdown);


    html = html.replace(
        /^# (.*)$/gm,
        "<h1>$1</h1>"
    );


    html = html.replace(
        /^## (.*)$/gm,
        "<h2>$1</h2>"
    );


    html = html.replace(
        /^---$/gm,
        "<hr>"
    );


    html = html.replace(
        /^[-*] (.*)$/gm,
        "<li>$1</li>"
    );


    html = html.replace(
        /(<li>.*<\/li>\n?)+/g,
        match =>
            `<ul>${match}</ul>`
    );


    html = html
        .split("\n\n")
        .map(block => {

            if (
                block.startsWith("<h1>") ||
                block.startsWith("<h2>") ||
                block.startsWith("<ul>") ||
                block.startsWith("<hr>")
            ) {
                return block;
            }

            return `<p>${block}</p>`;

        })
        .join("");


    resultContainer.innerHTML =
        html;


    resultContainer.classList.remove(
        "hidden"
    );


    emptyState.classList.add(
        "hidden"
    );

}


function escapeHtml(text) {

    const div =
        document.createElement("div");

    div.textContent =
        text;

    return div.innerHTML;

}


copyButton.addEventListener(
    "click",
    async () => {

        if (!currentReport) {
            return;
        }


        await navigator.clipboard.writeText(
            currentReport
        );


        copyButton.textContent =
            "Copied";


        setTimeout(
            () => {

                copyButton.textContent =
                    "Copy";

            },
            1500
        );

    }
);


downloadButton.addEventListener(
    "click",
    () => {

        if (!currentReport) {
            return;
        }


        const blob =
            new Blob(
                [currentReport],
                {
                    type:
                        "text/plain;charset=utf-8"
                }
            );


        const url =
            URL.createObjectURL(
                blob
            );


        const link =
            document.createElement("a");


        link.href =
            url;

        link.download =
            "care-handover.txt";


        document.body.appendChild(
            link
        );


        link.click();


        document.body.removeChild(
            link
        );


        URL.revokeObjectURL(
            url
        );

    }
);


async function checkLocalAIStatus() {

    localStatus.classList.remove(
        "status-online",
        "status-offline"
    );

    localStatus.classList.add(
        "status-checking"
    );

    try {

        const response =
            await fetch("/api/status");

        const data =
            await response.json();


        if (
            data.ollama &&
            data.modelAvailable
        ) {

            localStatus.classList.remove(
                "status-checking"
            );

            localStatus.classList.add(
                "status-online"
            );

            statusTitle.textContent =
                "100% Local AI";

            statusDescription.textContent =
                "Gemma 3 4B is ready on this computer";

        }
        else {

            showOfflineStatus();

        }

    }
    catch {

        showOfflineStatus();

    }
}


function showOfflineStatus() {

    localStatus.classList.remove(
        "status-checking"
    );

    localStatus.classList.add(
        "status-offline"
    );

    statusTitle.textContent =
        "Local AI unavailable";

    statusDescription.textContent =
        "Check that Ollama and Gemma are running";

}

notesInput.addEventListener(
    "input",
    () => {

        characterCount.textContent =
            `${notesInput.value.length} / 5000`;

    }
);


function showError(message) {

    errorMessage.textContent =
        message;

    errorMessage.style.display =
        "block";

}


function clearError() {

    errorMessage.textContent =
        "";

    errorMessage.style.display =
        "none";

}

checkLocalAIStatus();