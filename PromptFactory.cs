using WordList.Processing.QueryWords.Models;

namespace WordList.Processing.QueryWords;

public static class PromptFactory
{
    private static string s_prompt =
@"You will be given a list of words. For each word, generate a line with the following fields, separated by commas:
  
1. the word
2. offensiveness - an integer from 0 to 5. Use 0 if the word is entirely inoffensive. Only use a rating above 0 if the word is widely recognized in everyday language as a profanity, slur, or as inherently derogatory.
3. commonness - an integer from 0 to 5, where 0 means extremely rare or never used, and 5 means extremely common in everyday language.
4. sentiment - an integer from -5 to 5 (where -5 is extremely negative, 0 is neutral, and 5 is extremely positive). This score reflects the emotional tone of the word only.
5. word types - use one or more of: noun, verb, adjective, adverb, pronoun, preposition, conjunction, interjection, article. Separate multiple categories with a ""/"". If the word is not valid, use ""invalid"" and leave all numerical scores as 0.

Process every word in the list exactly once, in the order given, without adding headers, explanations, extra spaces, or additional formatting.

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