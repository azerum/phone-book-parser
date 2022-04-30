using System;

namespace Library
{
    public class Region : SearchScope, IEquatable<Region>
    {
        public Region(string url, string displayName)
            : base(url, displayName) { }

        public override string ToString()
            => DisplayName;

        public bool Equals(Region? other)
            => base.Equals(other);

        public override bool Equals(object? obj)
            => this.Equals(obj as Region);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}
