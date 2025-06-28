using System.Text;
using WordList.Common.Words;
using WordList.Common.Words.Models;
using WordList.Processing.QueryWords.Models;

namespace WordList.Processing.QueryWords;

public static class PromptFactory
{
    private static List<WordAttribute>? s_attributes;

    private static string s_promptHeader = @"You are provided with a list of words. For each word, output exactly one CSV line with the following fields, in the order given, separated by commas:";

    private static string s_promptFooter = @"Process each word exactly once, in the order provided. Do not output headers, extra explanations, additional spaces, or any duplicated field information.";

    public static async Task InitialiseAsync()
    {
        s_attributes = (await WordAttributes.GetAllAsync().ConfigureAwait(false)).OrderBy(attr => attr.Name).ToList();
    }

    public static Prompt GetPrompt(IEnumerable<string> words)
    {
        if (s_attributes is null)
            throw new InvalidOperationException("PromptFactory has not been initialised. Call InitialiseAsync() first.");

        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine(s_promptHeader);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("1. Word: The original word.");

        for (var i = 0; i < s_attributes.Count; i++)
        {
            var attribute = s_attributes[i];
            promptBuilder.AppendLine($"{i + 2}. {attribute.Display}: {attribute.GetSubstitutedPrompt()}");
        }

        promptBuilder.AppendLine($"{s_attributes.Count + 2}: Word Types.  The  word types for the word, separated by forward slashes. Word types must be simple word types e.g. noun, adjective, verb.");

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(s_promptFooter);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("Here is the list of words:");
        promptBuilder.AppendLine(string.Join(", ", words));

        return new()
        {
            PromptId = Guid.NewGuid().ToString(),
            Words = words.ToList(),
            Text = promptBuilder.ToString(),
        };
    }
}