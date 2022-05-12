namespace ConsoleApp
{
    public class NullWrapper<T>
    {
        public T? Inner { get; }
        private readonly string? nullText;

        private NullWrapper(T? inner, string? nullText)
        {
            Inner = inner;
            this.nullText = nullText;
        }

        public static NullWrapper<T> Of(T inner)
        {
            return new NullWrapper<T>(inner, default);
        }

        public static NullWrapper<T> Null(string nullText)
        {
            return new NullWrapper<T>(default, nullText);
        }

        public override string ToString()
        {
            return Inner?.ToString() ?? nullText!;
        }
    }
}
