using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[Route("api/chat")]
public class ChatController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ChatController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequestDto request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var apiKey       = _config["OpenAI:ApiKey"] ?? "";
        var baseUrl      = _config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions";
        var model        = _config["OpenAI:Model"] ?? "gpt-4o-mini";
        var systemPrompt = _config["OpenAI:SystemPrompt"]
            ?? "Bạn là trợ lý AI của NexusGear. Tư vấn thiết bị gaming bằng tiếng Việt, ngắn gọn và thân thiện.";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("ĐẶT_"))
        {
            await WriteErrAsync("OpenAI API Key chưa được cấu hình.", ct);
            return;
        }

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        messages.AddRange(request.Messages.Select(m => (object)new { role = m.Role, content = m.Content }));

        var payload = JsonSerializer.Serialize(new
        {
            model,
            messages,
            stream     = true,
            max_tokens = 1024
        });

        try
        {
            var client = _http.CreateClient();
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var openAiRes = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!openAiRes.IsSuccessStatusCode)
            {
                var errBody = await openAiRes.Content.ReadAsStringAsync(ct);
                var msg = $"OpenAI lỗi {(int)openAiRes.StatusCode}";
                try
                {
                    using var errDoc = JsonDocument.Parse(errBody);
                    if (errDoc.RootElement.TryGetProperty("error", out var errEl) &&
                        errEl.TryGetProperty("message", out var msgEl))
                        msg += $": {msgEl.GetString()}";
                }
                catch { }
                await WriteErrAsync(msg, ct);
                return;
            }

            using var stream = await openAiRes.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (!line.StartsWith("data: ")) continue;

                var json = line[6..].Trim();
                if (json == "[DONE]")
                {
                    await Response.WriteAsync("data: [DONE]\n\n", ct);
                    break;
                }

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentEl))
                    {
                        var token = contentEl.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            await Response.WriteAsync($"data: {JsonSerializer.Serialize(token)}\n\n", ct);
                            await Response.Body.FlushAsync(ct);
                        }
                    }
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await WriteErrAsync($"Lỗi kết nối OpenAI: {ex.Message}", ct);
        }
    }

    private async Task WriteErrAsync(string msg, CancellationToken ct)
    {
        try
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { error = msg })}\n\n", ct);
        }
        catch { }
    }
}

public class ChatRequestDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatMessageDto
{
    public string Role    { get; set; } = "user";
    public string Content { get; set; } = "";
}
