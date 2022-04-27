namespace Library
{
    public class Region : SearchScope
    {
        public Region(string url, string displayName)
            : base(url, displayName) { }

        public override string ToString()
            => DisplayName;
    }
}
