
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.Runtime.Internal.Util;
using WordList.Processing.QueryWords.OpenAI.Models;

namespace WordList.Processing.QueryWords.OpenAI;

public class OpenAIClient
{
    private HttpClient _http = new();

    public OpenAIClient(string apiKey)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.BaseAddress = new Uri("https://api.openai.com/v1", UriKind.Absolute);
    }

    public async Task<FileResponse?> CreateFileAsync(string purpose, string filename, byte[] content)
    {
        using var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent
        {
            { new StringContent(purpose), "purpose" },
            { fileContent, "file", filename }
        };

        var response = await _http.PostAsync("/files", form).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API Error: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadFromJsonAsync(OpenAISerializerContext.Default.FileResponse).ConfigureAwait(false);
    }

    public async Task<BatchStatus?> CreateBatchAsync(string uploadedFileId, string endpoint, string completionWindow = "24h")
    {
        var request = new CreateBatchRequest
        {
            InputFileId = uploadedFileId,
            Endpoint = endpoint,
            CompletionWindow = completionWindow
        };

        var response = await _http.PostAsJsonAsync("/batches", request, OpenAISerializerContext.Default.CreateBatchRequest).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API Error: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadFromJsonAsync(OpenAISerializerContext.Default.BatchStatus).ConfigureAwait(false);
    }
}
