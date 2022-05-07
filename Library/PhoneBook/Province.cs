using System;

namespace Library
{
    public class Province : SearchScope, IEquatable<Province>
    {
        public Region Region { get; }

        internal Province(Region region, string url, string displayName)
            : base(url, displayName)
        {
            Region = region;
        }

        public override string ToString()
            => $"{Region}, {DisplayName}";

        public bool Equals(Province? other)
        {
            return base.Equals(other)
                && Region.Equals(other?.Region);
        }

        public override bool Equals(object? obj)
            => this.Equals(obj as Province);

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Region.Url);
            hash.Add(Region.DisplayName);

            hash.Add(Url);
            hash.Add(DisplayName);

            return hash.ToHashCode();
        }
    }
}
