using System.Text.Json.Serialization;

namespace WordList.Processing.QueryWords.OpenAI.Models;

public class BatchStatus
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("input_file_id")]
    public required string InputFileId { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("output_file_id")]
    public required string OutputFileId { get; set; }

    [JsonPropertyName("error_file_id")]
    public required string ErrorFileId { get; set; }
}