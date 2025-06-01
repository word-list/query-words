using System.Text.Json.Serialization;

namespace WordList.Processing.QueryWords.OpenAI.Models;

public class CreateBatchRequest
{
    [JsonPropertyName("input_file_id")]
    public required string InputFileId { get; set; }

    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; set; }

    [JsonPropertyName("completion_window")]
    public required string CompletionWindow { get; set; } = "24h";
}

public class BatchRequestInput
{
    [JsonPropertyName("custom_id")]
    public required string CustomId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("method")]
    public required string Method { get; set; } = "POST";

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("body")]
    public required ResponsesRequest Body { get; set; }
}

public class ResponsesRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("input")]
    public required string Input { get; set; }
}
