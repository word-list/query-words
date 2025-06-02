using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using WordList.Processing.QueryWords.Models;
using WordList.Processing.QueryWords.OpenAI;

using WordList.Common.OpenAI.Models;

namespace WordList.Processing.QueryWords;

public class WordQuerier
{
    private static DynamoDBContext s_db = new DynamoDBContextBuilder().Build();

    protected ILambdaLogger Logger { get; init; }

    private List<string> _words = [];
    private BatchCreator _batchCreator;



    public WordQuerier(ILambdaLogger logger)
    {
        Logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY must be defined");

        _batchCreator = new BatchCreator(apiKey, Logger, s_db);
    }

    public void Add(string word)
    {
        _words.Add(word);
    }

    private async Task<(Prompt[], BatchStatus?)> CreateBatchAsync(Prompt[] prompts)
    {
        return (prompts, await _batchCreator.CreateBatchAsync(prompts));
    }

    public async Task CreateAllBatchQueriesAsync()
    {
        var tasks = _words
            .Chunk(50) // Create a prompt using 50 words
            .Select(PromptFactory.GetPrompt)
            .Chunk(1000) // Create a batch of 1,000 prompts - in theory this could go up to 50,000
            .Select(CreateBatchAsync);

        Logger.LogInformation($"Waiting for all batch tasks to complete");

        var createdBatches = await Task.WhenAll(tasks).ConfigureAwait(false);

        Logger.LogInformation($"All batch tasks completed");
    }
}