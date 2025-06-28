using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using WordList.Processing.QueryWords.Models;
using WordList.Common.Json;
using WordList.Common.Messaging;
using WordList.Common.Status;

namespace WordList.Processing.QueryWords;

public class Function
{
    public static async Task<string> FunctionHandler(SQSEvent input, ILambdaContext context)
    {
        var log = new LambdaContextLogger(context);

        log.Info($"Entering QueryWords FunctionHandler with {input.Records.Count} message(s)");

        await PromptFactory.InitialiseAsync().ConfigureAwait(false);

        var incomingMessages = MessageQueues.QueryWords.Receive(input, log).GroupBy(message => message.CorrelationId);

        foreach (var grouping in incomingMessages)
        {
            log.Info($"Processing {grouping.Count()} messages with CorrelationId: {grouping.Key}");

            var status = new StatusClient(grouping.Key);
            var querier = new WordQuerier(status, context.Logger);

            foreach (var message in grouping.Where(msg => msg is not null))
            {
                querier.Add(message.Word);
            }

            await querier.CreateAllBatchQueriesAsync().ConfigureAwait(false);
        }

        return "ok";
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
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}