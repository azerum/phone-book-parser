namespace Library
{
    public class Province : SearchScope
    {
        public Region Region { get; }

        public Province(Region region, string url, string displayName)
            : base(url, displayName)
        {
            Region = region;
        }

        public override string ToString()
            => $"{base.ToString()}, {DisplayName}";
    }
}
