namespace Library.Caching
{
    public static class Selectors
    {
        public static Region ToRegion(dynamic d)
            => new(d.Url, d.DisplayName);

        public static Province ToProvince(dynamic d)
        {
            Region region = new(d.RUrl, d.RDisplayName);
            return new(region, d.PUrl, d.PDisplayName);
        }

        public static City ToCity(dynamic d)
        {
            Region region = new(d.RUrl, d.RDisplayName);
            Province province = new(region, d.PUrl, d.PDisplayName);

            return new(province, d.CUrl, d.CDisplayName);
        }
    }
}
