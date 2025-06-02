using Amazon.Lambda.Core;
using WordList.Processing.QueryWords.OpenAI.Models;
using System.Text;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DataModel;
using WordList.Processing.QueryWords.Models;

namespace WordList.Processing.QueryWords.OpenAI;

public class BatchCreator
{
    private OpenAIClient _openAI;
    protected ILambdaLogger Logger { get; init; }
    private DynamoDBContext _db;

    protected string BatchesTableName { get; set; }
    protected string PromptsTableName { get; set; }
    protected int MaxTries { get; set; } = 3;
    protected string OpenAIModelName { get; set; }

    public BatchCreator(string apiKey, ILambdaLogger logger, DynamoDBContext db)
    {
        Logger = logger;
        _openAI = new(apiKey);
        _db = db;
        BatchesTableName = Environment.GetEnvironmentVariable("BATCHES_TABLE_NAME")
            ?? throw new Exception("BATCHES_TABLE_NAME must be set");

        PromptsTableName = Environment.GetEnvironmentVariable("PROMPTS_TABLE_NAME")
            ?? throw new Exception("PROMPTS_TABLE_NAME must be set");

        OpenAIModelName = Environment.GetEnvironmentVariable("OPENAI_MODEL_NAME")
            ?? throw new Exception("OPENAI_MODEL_NAME must be set");
    }

    private string GetRequestInputJson(string prompt) =>
        JsonHelpers.Serialize(
             new BatchRequestInput
             {
                 CustomId = Guid.NewGuid().ToString(),
                 Method = "POST",
                 Url = "/v1/responses",
                 Body = new ResponsesRequest
                 {
                     Model = OpenAIModelName,
                     Input = prompt
                 }
             }, OpenAISerializerContext.Default.BatchRequestInput);

    private async Task WriteBatchRecordAsync(Batch batch, string? newStatus = null)
    {
        for (var tryNumber = 1; tryNumber <= MaxTries; tryNumber++)
        {
            if (tryNumber > 1)
            {
                await Task.Delay(250 * tryNumber).ConfigureAwait(false);
                Logger.LogInformation($"Try {tryNumber} of {MaxTries} to upate batch record {batch.Id}");
            }

            try
            {
                if (newStatus is not null)
                {
                    batch.Status = newStatus;
                }
                await _db.SaveAsync(batch, new SaveConfig { OverrideTableName = BatchesTableName }).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update batch: {batch.Id} with: {ex.Message}");
            }
        }
    }

    private async Task<bool> WritePromptRecordsAsync(string batchId, Prompt[] prompts)
    {
        using var batchWriteLimiter = new SemaphoreSlim(4);

        var batchWriteConfig = new BatchWriteConfig { OverrideTableName = PromptsTableName };

        var batchTasks = prompts.Chunk(25).Select(async promptBatch =>
            {
                await batchWriteLimiter.WaitAsync().ConfigureAwait(false);
                try
                {
                    foreach (var prompt in promptBatch)
                    {
                        prompt.BatchId = batchId;
                    }

                    var batchWrite = _db.CreateBatchWrite<Prompt>(batchWriteConfig);
                    batchWrite.AddPutItems(promptBatch);
                    try
                    {
                        await batchWrite.ExecuteAsync().ConfigureAwait(false);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        var promptIds = string.Join(", ", promptBatch.Select(p => p.PromptId));
                        Logger.LogError($"Failed to write prompt batch for batch ID {batchId}, prompt IDs {promptIds}: {ex.Message}");
                    }
                }
                finally
                {
                    batchWriteLimiter.Release();
                }

                return false;
            });

        var results = await Task.WhenAll(batchTasks).ConfigureAwait(false);
        return results.All(r => r);
    }

    public async Task<BatchStatus?> CreateBatchAsync(Prompt[] prompts)
    {
        var batch = new Batch
        {
            Id = Guid.NewGuid().ToString(),
            Status = "Initialising"
        };

        try
        {
            await WriteBatchRecordAsync(batch).ConfigureAwait(false);

            // Now write the individual prompt records to the database
            if (!await WritePromptRecordsAsync(batch.Id, prompts).ConfigureAwait(false))
            {
                Logger.LogError($"Failed to write all prompts for batch {batch.Id}");
                return null;
            }

            var promptJsons = prompts.Select(prompt => GetRequestInputJson(prompt.Text));
            var fileContent = Encoding.UTF8.GetBytes(string.Join('\n', promptJsons));

            await WriteBatchRecordAsync(batch, "Uploading").ConfigureAwait(false);
            var fileInfo = await _openAI.CreateFileAsync("batch", "prompt.jsonl", fileContent);
            if (fileInfo is null) throw new Exception("Failed to create batch file for an unknown reason");

            Logger.LogInformation($"Uploaded batch file has ID '{fileInfo.Id}'");

            await WriteBatchRecordAsync(batch, "Creating").ConfigureAwait(false);
            var batchStatus = await _openAI.CreateBatchAsync(fileInfo.Id, "/v1/responses");

            await WriteBatchRecordAsync(batch, "Waiting").ConfigureAwait(false);

            return batchStatus;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to create batch: {ex.Message}");
            try
            {
                batch.ErrorMessage = ex.Message;
                await WriteBatchRecordAsync(batch, "Failed").ConfigureAwait(false);
            }
            catch (Exception updateEx)
            {
                Logger.LogError($"Additionally, the following error occurred when attempting to update batch status: {updateEx.Message}");
            }
            throw;
        }
    }
}

[JsonSerializable(typeof(BatchRequestInput))]
[JsonSerializable(typeof(ResponsesRequest))]
[JsonSerializable(typeof(FileResponse))]
[JsonSerializable(typeof(CreateBatchRequest))]
[JsonSerializable(typeof(BatchStatus))]
public partial class OpenAISerializerContext : JsonSerializerContext
{
}