namespace LibraryV3
{
    public static class PercentEx
    {
        public static Percent Percent(this double input)
        {
            return new Percent(input);
        }

        public static Percent Percent(this int input)
        {
            return new Percent(input);
        }
    }
}