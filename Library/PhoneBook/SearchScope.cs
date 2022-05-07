using System;

namespace Library
{
    public abstract class SearchScope : IEquatable<SearchScope>
    {
        public string Url { get; }
        public string DisplayName { get; }

        internal SearchScope(string url, string displayName)
        {
            Url = url;
            DisplayName = displayName;
        }

        public bool Equals(SearchScope? other)
        {
            if (other == null)
            {
                return false;
            }

            return Url == other.Url
                && DisplayName == other.DisplayName;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SearchScope);
        }

        public override int GetHashCode()
            => HashCode.Combine(Url, DisplayName);
    }
}
