namespace Library
{
    public class City : SearchScope
    {
        public Province Province { get; }

        public City(Province province, string url, string displayName)
            : base(url, displayName)
        {
            Province = province;
        }

        public override string ToString()
            => $"{base.ToString()}, {DisplayName}";
    }
}
