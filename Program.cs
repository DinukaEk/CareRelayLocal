using System.Net.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();


// -------------------------------------------------------
// LOCAL AI STATUS
// -------------------------------------------------------

app.MapGet("/api/status", async (
    IHttpClientFactory httpClientFactory) =>
{
    const string requiredModel = "gemma3:4b";

    try
    {
        var client =
            httpClientFactory.CreateClient("Ollama");

        var response =
            await client.GetAsync("/api/tags");

        if (!response.IsSuccessStatusCode)
        {
            return Results.Ok(new
            {
                app = true,
                ollama = false,
                modelAvailable = false,
                model = requiredModel,
                local = true
            });
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<OllamaTagsResponse>();

        var modelAvailable =
            data?.Models?.Any(model =>
                string.Equals(
                    model.Name,
                    requiredModel,
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    model.Model,
                    requiredModel,
                    StringComparison.OrdinalIgnoreCase
                )
            ) ?? false;

        return Results.Ok(new
        {
            app = true,
            ollama = true,
            modelAvailable,
            model = requiredModel,
            local = true
        });
    }
    catch
    {
        return Results.Ok(new
        {
            app = true,
            ollama = false,
            modelAvailable = false,
            model = requiredModel,
            local = true
        });
    }
});


// -------------------------------------------------------
// GENERATE HANDOVER
// -------------------------------------------------------

app.MapPost("/api/handover", async (
    HandoverRequest request,
    IHttpClientFactory httpClientFactory) =>
{
    // ---------------------------
    // Validation
    // ---------------------------

    if (string.IsNullOrWhiteSpace(request.Notes))
    {
        return Results.BadRequest(new
        {
            message = "Please enter some care notes."
        });
    }

    var notes = request.Notes.Trim();

    if (notes.Length > 5000)
    {
        return Results.BadRequest(new
        {
            message =
                "Care notes cannot exceed 5,000 characters."
        });
    }


    var allowedAudiences =
        new[] { "caregiver", "family" };

    var audience =
        string.IsNullOrWhiteSpace(request.Audience)
            ? "caregiver"
            : request.Audience.Trim().ToLowerInvariant();

    if (!allowedAudiences.Contains(audience))
    {
        return Results.BadRequest(new
        {
            message =
                "Audience must be caregiver or family."
        });
    }


    var allowedModels =
        new[]
        {
            "gemma3:4b"
        };

    var model =
        string.IsNullOrWhiteSpace(request.Model)
            ? "gemma3:4b"
            : request.Model.Trim();

    if (!allowedModels.Contains(model))
    {
        return Results.BadRequest(new
        {
            message = "Unsupported AI model."
        });
    }


    // ---------------------------
    // AI System Prompt
    // ---------------------------

    var systemPrompt = """
        You are CareRelay Local.

        Your only task is to transform rough caregiver notes into
        a clear, factual handover report.

        FACTUAL SAFETY RULES:

        1. Use ONLY information explicitly present in the supplied notes.

        2. Never invent or assume:
           - medication names
           - medication dosages
           - diagnoses
           - symptoms
           - measurements
           - meal quantities
           - times
           - appointments
           - people
           - relationships
           - medical advice

        3. Never diagnose a condition.

        4. Never recommend medication, treatment, dosage changes,
           or medical action.

        5. Preserve times, quantities, wording and uncertainty exactly
           when they matter.

           Example:
           "around 10" must remain "around 10".
           Do NOT change it to "10:00 AM".

        6. If the notes say only "medicine", do not guess the medicine.
           Say that medicine was mentioned but the name and dosage
           were not provided.

        7. If a section has no relevant information, write exactly:

           Not mentioned.

        8. Do not turn an observation into a confirmed medical fact.

           Example:
           "said knee was hurting a little"

           should remain an observation.

        9. Avoid repeating the same fact in multiple sections unless
           it is genuinely necessary for the short summary.

        10. Keep the report concise.

        FORMATTING RULES:

        Return Markdown only.

        Use exactly the following structure:

        # Care Handover

        ## Summary

        Write a short 2-3 sentence summary using only the supplied facts.

        ## Meals & Hydration

        Use short bullet points where appropriate.

        ## Medication Mentioned

        Use short bullet points where appropriate.

        ## Activities & Mobility

        Use short bullet points where appropriate.

        ## Observations

        Use short bullet points where appropriate.

        ## Rest

        Use short bullet points where appropriate.

        ## Appointments & Reminders

        Use short bullet points where appropriate.

        ## Information for the Next Caregiver

        Include only information that would genuinely be useful
        to the next caregiver.

        If there is nothing specific to pass on, write:

        Not mentioned.

        ---

        This report only reorganizes the supplied notes and does not
        provide medical advice.
        """;


    if (audience == "family")
    {
        systemPrompt += """

            AUDIENCE:

            This version is for a family member.

            Use warm, simple and natural language.

            Avoid unnecessary professional caregiving terminology.

            Do not alter, soften, exaggerate or add any factual
            information.
            """;
    }
    else
    {
        systemPrompt += """

            AUDIENCE:

            This version is for the next caregiver.

            Use concise, professional and practical handover language.

            Prioritize facts and useful reminders.
            """;
    }


    var ollamaRequest =
        new OllamaChatRequest
        {
            Model = model,
            Stream = false,

            Messages =
            [
                new OllamaMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },

                new OllamaMessage
                {
                    Role = "user",
                    Content =
                        $"Care notes:\n\n{notes}"
                }
            ],

            Options =
                new OllamaOptions
                {
                    Temperature = 0.1
                }
        };


    // ---------------------------
    // Ollama request
    // ---------------------------

    try
    {
        var client =
            httpClientFactory.CreateClient("Ollama");

        var response =
            await client.PostAsJsonAsync(
                "/api/chat",
                ollamaRequest
            );

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            return Results.Problem(
                title: "Local AI request failed",
                detail: error,
                statusCode: 502
            );
        }


        var ollamaResponse =
            await response.Content
                .ReadFromJsonAsync<OllamaChatResponse>();


        var generatedText =
            ollamaResponse?.Message?.Content?.Trim();


        if (string.IsNullOrWhiteSpace(generatedText))
        {
            return Results.Problem(
                title: "Empty AI response",
                detail:
                    "Gemma returned an empty response.",
                statusCode: 502
            );
        }


        return Results.Ok(new
        {
            result = generatedText,
            model,
            audience,
            local = true
        });
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            title: "Cannot connect to local AI",
            detail:
                "Ollama is not available. Make sure Ollama is running.",
            statusCode: 503
        );
    }
    catch (TaskCanceledException)
    {
        return Results.Problem(
            title: "Generation timed out",
            detail:
                "The local model took too long to generate the report.",
            statusCode: 504
        );
    }
    catch (Exception)
    {
        return Results.Problem(
            title: "Unexpected error",
            detail:
                "CareRelay could not generate the handover.",
            statusCode: 500
        );
    }
});

app.Run();


// -------------------------------------------------------
// Models
// -------------------------------------------------------

public record HandoverRequest
{
    public string Notes { get; init; } = "";

    public string? Audience { get; init; }

    public string? Model { get; init; }
}


public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
}


public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}


public class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}


public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }
}


public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel> Models { get; set; } = [];
}


public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
}