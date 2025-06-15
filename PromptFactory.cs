using WordList.Processing.QueryWords.Models;

namespace WordList.Processing.QueryWords;

public static class PromptFactory
{
    private static string s_prompt =
@"You are provided with a list of words. For each word, output exactly one CSV line with the following 10 fields, in the order given, separated by commas:

1. Word: The original word.
2. Offensiveness: An integer from 0 to 5. Use 0 if the word is completely inoffensive; use a number greater than 0 only if it is widely recognized as profanity, a slur, or inherently derogatory.
3. Commonness: An integer from 0 (extremely rare or unused) to 5 (extremely common).
4. Sentiment: An integer ranging from -5 (extremely negative) to 5 (extremely positive) that reflects the word’s emotional tone.
5. Formality: An integer from 0 (extremely informal) to 5 (highly formal).
6. Cultural Sensitivity: An integer from 0 (potentially culturally insensitive or laden with stereotypes) to 5 (culturally neutral or widely acceptable).
7. Figurativeness: An integer from 0 (strictly literal) to 5 (almost exclusively metaphorical).
8. Complexity: An integer from 0 (simple) to 5 (highly conceptually or structurally complex).
9. Political Association: An integer from 0 (no political association) to 5 (highly politicized or common in political discourse).
10. Word Types: One or more categories chosen from the following list: noun, verb, adjective, adverb, pronoun, preposition, conjunction, interjection, article. Use a forward-slash (""/"") to separate multiple types. If the word is invalid, output ""invalid"" here and set all numerical fields to 0.

Process each word exactly once, in the order provided. Do not output headers, extra explanations, additional spaces, or any duplicated field information.

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