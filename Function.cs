using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using WordList.Processing.QueryWords.Models;
using WordList.Common.Json;

namespace WordList.Processing.QueryWords;

public class Function
{
    public static async Task<string> FunctionHandler(SQSEvent input, ILambdaContext context)
    {
        context.Logger.LogInformation($"Entering QueryWords FunctionHandler with {input.Records.Count} message(s)");

        var incomingMessages =
                input.Records.Select(record =>
                {
                    try
                    {
                        return JsonHelpers.Deserialize(record.Body, LambdaFunctionJsonSerializerContext.Default.QueryWordsMessage);
                    }
                    catch (Exception)
                    {
                        context.Logger.LogWarning($"Ignoring invalid message: {record.Body}");
                        return null;
                    }
                })
                .Where(message => message is not null);

        var querier = new WordQuerier(context.Logger);

        foreach (var message in incomingMessages)
        {
            querier.Add(message!.Word);
        }

        await querier.CreateAllBatchQueriesAsync().ConfigureAwait(false);

        return "";
    }

    public static async Task Main()
    {
        Func<SQSEvent, ILambdaContext, Task<string>> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(QueryWordsMessage))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}