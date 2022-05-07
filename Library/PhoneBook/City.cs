using System;

namespace Library
{
    public class City : SearchScope, IEquatable<City>
    {
        public Province Province { get; }

        internal City(Province province, string url, string displayName)
            : base(url, displayName)
        {
            Province = province;
        }

        public override string ToString()
            => $"{Province}, {DisplayName}";

        public bool Equals(City? other)
        {
            return base.Equals(other)
                && Province.Equals(other?.Province);
        }

        public override bool Equals(object? obj)
            => this.Equals(obj as City);

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Province.Region.Url);
            hash.Add(Province.Region.DisplayName);

            hash.Add(Province.Url);
            hash.Add(Province.DisplayName);

            hash.Add(Url);
            hash.Add(DisplayName);

            return hash.ToHashCode();
        }
    }
}
