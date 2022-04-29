namespace Library
{
    public abstract class SearchScope
    {
        public string Url { get; }
        public string DisplayName { get; }

        public SearchScope(string url, string displayName)
        {
            Url = url;
            DisplayName = displayName;
        }
    }
}
