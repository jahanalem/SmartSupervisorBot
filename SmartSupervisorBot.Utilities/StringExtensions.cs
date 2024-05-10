namespace SmartSupervisorBot.Utilities
{
    public static class StringExtensions
    {
        public static int CountWords(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            int count = 0;
            int index = 0;

            // Skip whitespace until first word
            while (index < input.Length && char.IsWhiteSpace(input[index])) { index++; }

            while (index < input.Length)
            {
                // Skip non-whitespace characters
                while (index < input.Length && !char.IsWhiteSpace(input[index])) { index++; }

                count++;

                // Skip whitespace until next word
                while (index < input.Length && char.IsWhiteSpace(input[index])) { index++; }
            }

            return count;
        }
    }
}
