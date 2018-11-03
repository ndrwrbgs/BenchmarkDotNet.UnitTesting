namespace LibraryV3
{
    public struct Percent
    {
        public double Value { get; }

        public double Multiplier => this.Value / 100.0;

        public Percent(double value)
        {
            this.Value = value;
        }
    }
}