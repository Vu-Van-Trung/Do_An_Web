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

        var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _config["Ollama:Model"] ?? "qwen2.5:3b";
        var systemPrompt = _config["Ollama:SystemPrompt"]
            ?? "Bạn là trợ lý AI của NexusGear. Tư vấn thiết bị gaming bằng tiếng Việt, ngắn gọn và thân thiện.";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        messages.AddRange(request.Messages.Select(m => (object)new { role = m.Role, content = m.Content }));

        var payload = JsonSerializer.Serialize(new { model, messages, stream = true });

        try
        {
            var client = _http.CreateClient();
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/chat")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            using var ollamaRes = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!ollamaRes.IsSuccessStatusCode)
            {
                await WriteErrAsync($"Ollama trả về lỗi {(int)ollamaRes.StatusCode}. Model \"{model}\" có thể chưa được tải.", ct);
                return;
            }

            using var stream = await ollamaRes.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("message", out var msgEl) &&
                        msgEl.TryGetProperty("content", out var contentEl))
                    {
                        var token = contentEl.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            await Response.WriteAsync($"data: {JsonSerializer.Serialize(token)}\n\n", ct);
                            await Response.Body.FlushAsync(ct);
                        }
                    }

                    if (root.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean())
                    {
                        await Response.WriteAsync("data: [DONE]\n\n", ct);
                        break;
                    }
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            await WriteErrAsync($"Không thể kết nối Ollama tại {baseUrl}. Hãy đảm bảo Ollama đang chạy.", ct);
        }
    }

    private async Task WriteErrAsync(string msg, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { error = msg });
            await Response.WriteAsync($"data: {json}\n\n", ct);
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
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
}
