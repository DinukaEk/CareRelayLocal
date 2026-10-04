# CareRelay Local

**Private AI-powered care handovers that stay on your computer.**

CareRelay Local turns rough daily caregiving notes into clear, structured handover reports using **Gemma 3 running locally through Ollama**.

No cloud AI API is required. After the model has been downloaded, the AI processing can run locally on the user's computer.

## Why I Built It

I built CareRelay Local for someone close to me who sometimes needs to keep track of caregiving information and pass important details to another person.

Daily notes are often written quickly and naturally:

> Breakfast at 8. Ate about half. Medicine after breakfast. Went outside around 10. Said knee was hurting a little. Doctor appointment tomorrow.

The information is there, but it is not always easy to scan or hand over.

CareRelay reorganizes those rough notes into a structured report containing meals, medication mentions, activities, observations, rest, appointments, and useful information for the next caregiver.

The goal is not to provide medical advice.

The goal is simply to make existing information easier to communicate.

## Why Local AI?

Caregiving notes can contain personal information.

I did not want the core feature of this project to depend on sending those notes to an external AI API.

Instead, CareRelay runs:

```text
Browser
   ↓
ASP.NET Core
   ↓
Ollama
   ↓
Gemma 3 4B
```

The AI inference happens locally on the user's machine.

This provides several advantages:

- No cloud AI API key is required.
- Notes do not need to be sent to a third-party AI service.
- There is no per-request AI cost.
- The application can continue working without internet access after the model is installed.
- The AI model can be replaced or upgraded without redesigning the entire application.
- Developers can inspect and change how the local AI behaves.

That flexibility is the main reason open innovation matters for this project.

## Features

- Convert rough caregiving notes into structured handovers.
- Next Caregiver report mode.
- Family-friendly report mode.
- Local Gemma 3 inference through Ollama.
- Live local-AI availability indicator.
- Copy generated reports.
- Download generated reports as text files.
- Input validation and error handling.
- Guardrails designed to avoid invented medical information.
- Responsive browser interface.

## AI Safety Approach

CareRelay is intentionally designed as a note-organization tool rather than a medical assistant.

The model is instructed to:

- use only information supplied by the user
- avoid medical diagnoses
- avoid treatment recommendations
- avoid inventing medication names or dosages
- preserve uncertainty in the original notes
- clearly state when information was not mentioned

CareRelay does not replace professional medical advice.

## Tech Stack

### AI

- Gemma 3 4B
- Ollama

### Backend

- ASP.NET Core
- .NET 10

### Frontend

- HTML
- CSS
- Vanilla JavaScript

No JavaScript framework or external AI service is required.

## Running the Project

### Requirements

Install:

- .NET 10 SDK
- Ollama
- Gemma 3 4B

Download the model:

```bash
ollama pull gemma3:4b
```

Confirm that it is available:

```bash
ollama list
```

Clone the repository:

```bash
git clone https://github.com/DinukaEk/CareRelayLocal.git
cd CareRelayLocal
```

Run the application:

```bash
dotnet run
```

Open the local URL displayed by ASP.NET, for example:

```text
http://localhost:5211
```

CareRelay will automatically check whether Ollama and Gemma are available.

## Example

Input:

```text
Breakfast at 8. Ate about half.
Medicine after breakfast.
Went outside around 10.
Said knee was hurting a little.
Lunch at 12:30.
Drank two glasses of water.
Slept around 2.
Doctor appointment tomorrow.
```

CareRelay turns those rough notes into a structured handover with sections for:

- Summary
- Meals & Hydration
- Medication Mentioned
- Activities & Mobility
- Observations
- Rest
- Appointments & Reminders
- Information for the Next Caregiver

## Privacy

CareRelay does not intentionally send care notes to an external AI provider.

The browser sends the notes to the local ASP.NET application, which sends them to the Ollama server running on `localhost`.

Ollama then performs inference using the locally installed Gemma model.

## Hacktoberfest

CareRelay Local was created for the **Hacktoberfest Weekend Challenge: Build for a Friend**.

The project explores how open-weight models can enable useful AI experiences while giving users greater control over where their data is processed and which models they run.

## License

This project is licensed under the MIT License.