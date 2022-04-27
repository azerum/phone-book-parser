using System;

namespace Library.Parsing
{
    public class ParsingFailedException : Exception
    {
        public int StatusCode { get; }

        public bool IsClientSideError
            => StatusCode is >= 400 and < 500;

        public ParsingFailedException(int statusCode)
        {
            StatusCode = statusCode;
        }
    }
}
