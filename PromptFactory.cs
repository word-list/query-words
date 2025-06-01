using WordList.Processing.QueryWords.Models;

namespace WordList.Processing.QueryWords;

public static class PromptFactory
{
    private static string s_prompt = @"You will be given a list of words. For each word, generate a line with the following fields, separated by commas:

    the word

    offensiveness (an integer from 0 to 5, where 0 means completely inoffensive and 5 means extremely offensive)

    commonness (an integer from 0 to 5, where 0 means extremely rare or never used, and 5 means extremely common in everyday language)

    sentiment (an integer from -5 to 5, where -5 is extremely negative, 0 is neutral, and 5 is extremely positive)

    word types (use one or more of the following categories, separated by / if more than one: noun, verb, adjective, adverb, pronoun, preposition, conjunction, interjection, article. If the word is not valid, use 'invalid' and leave all numerical scores as 0.)

Do not include headers, explanations, extra spaces, or formatting. Process every word in the list exactly once, in the order given, and do not skip any.

Here is the list of words:
";

    public static Prompt GetPrompt(IEnumerable<string> words) =>
        new()
        {
            PromptId = Guid.NewGuid().ToString(),
            Words = words.ToList(),
            Text = string.Concat(s_prompt, string.Join(", ", words))
        };
}