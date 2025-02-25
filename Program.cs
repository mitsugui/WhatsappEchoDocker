using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add configuration for appsettings.json and environment variables.  Crucially, bind to a section.
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddLogging();
builder.Services.AddHttpClient(); // Add HttpClient

// Configure strongly typed settings objects.
builder.Services.Configure<WhatsappTokens>(
    builder.Configuration.GetSection("Whatsapp"));

var app = builder.Build();

// --- GET Endpoint ---
app.MapGet("/httpecho", (
    [FromQuery(Name = "hub.mode")] string? mode,
    [FromQuery(Name = "hub.verify_token")] string? verifyToken,
    [FromQuery(Name = "hub.challenge")] string? challenge,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("C# HTTP GET trigger function processed a request.");

    var whatsappTokens = config.GetSection("Whatsapp").Get<WhatsappTokens>();

    if (mode == "subscribe" && verifyToken == whatsappTokens?.VERIFY_TOKEN && challenge != null)
    {
        return Results.Ok(int.Parse(challenge));
    }
    else
    {
        return Results.StatusCode(403);
    }
});


// --- POST Endpoint ---
app.MapPost("/httpecho", async (
    HttpContext context,
    IConfiguration config,
    IHttpClientFactory httpClientFactory, // Inject IHttpClientFactory
    ILogger<Program> logger) =>
{
    logger.LogInformation("C# HTTP POST trigger function processed a request.");

    var whatsappTokens = config.GetSection("Whatsapp").Get<WhatsappTokens>(); // Get tokens from config
    if (whatsappTokens == null || string.IsNullOrEmpty(whatsappTokens.ACCESS_TOKEN))
    {
        logger.LogError("Whatsapp tokens are not configured correctly.");
        return Results.StatusCode(500); // Internal Server Error if tokens are missing.
    }

    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();
    dynamic? body = JsonConvert.DeserializeObject(requestBody);

    if (body?.entry == null)
    {
        return Results.Ok();  // Return 200 OK even if no messages to avoid retries
    }
     logger.LogInformation($"Body obtido.");

    foreach (var entry in body.entry)
    {
         logger.LogInformation($"Entry: {entry}.");
        foreach (var change in entry.changes)
        {
            logger.LogInformation($"Change: {change}.");
            var value = change.value;
            if (value == null) continue;

            var phoneNumberId = value?.metadata?.phone_number_id.ToString();
            logger.LogInformation($"PhoneNumberId: {phoneNumberId}.");

            if (value?.messages == null) continue;

            foreach (var message in value.messages)
            {
                if (message?.type != "text") continue;

                var from = message.from.ToString();
                var messageBody = message?.text?.body;
                var replyMessage = $"Ack from Azure Function: {messageBody}";
                logger.LogInformation($"Phone Number: {phoneNumberId} Message: {from} | Body: {messageBody}.");

                await SendReply(phoneNumberId, from, replyMessage, whatsappTokens.ACCESS_TOKEN, httpClientFactory, logger);
                return Results.Ok(); // Acknowledge each message individually.  Critical for avoiding infinite loops.
            }
        }
    }

    return Results.Ok(); // Return 200 OK even if no messages were processed.
});

async Task SendReply(string phoneNumberId, string to, string replyMessage, string whatsappToken, IHttpClientFactory httpClientFactory, ILogger logger)
{
    var json = new
    {
        messaging_product = "whatsapp",
        to = to,
        text = new { body = replyMessage }
    };

    var serializedData = JsonConvert.SerializeObject(json);
    var data = new StringContent(serializedData, Encoding.UTF8, "application/json");
    var path = $"/v22.0/{phoneNumberId}/messages?access_token={whatsappToken}";  //Use passed in token
    var url = $"https://graph.facebook.com{path}";

    // Use the named HttpClient from the factory.  Much cleaner than static.
    var client = httpClientFactory.CreateClient();

    try
    {
        logger.LogInformation($"Url: {url}.");
        logger.LogInformation($"Data: {serializedData}.");

        var response = await client.PostAsync(url, data);
        if (!response.IsSuccessStatusCode)
        {
            // Handle error logging or retry logic here. VERY IMPORTANT to log the error.
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError($"Error sending reply: {response.StatusCode} - {errorContent}");
        }
    }
    catch (Exception ex)
    {
        // Handle exceptions (log, retry, etc.)  Essential to log the exception.
        logger.LogError($"Exception sending reply: {ex.Message}");
    }
}

app.Run();




// Create a class to hold the strongly typed settings.
public class WhatsappTokens
{
    public string? VERIFY_TOKEN { get; set; }
    public string? ACCESS_TOKEN { get; set; }
}