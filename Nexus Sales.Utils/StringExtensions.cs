namespace NexusSales.Utils
{
    public static class StringExtensions
    {
        public static bool IsValidPhoneNumber(this string input)
        {
            // TODO: Implement validation logic
            return !string.IsNullOrEmpty(input);
        }
    }
}