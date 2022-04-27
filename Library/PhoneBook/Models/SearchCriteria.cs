using System;

namespace Library
{
    public class SearchCriteria
    {
        public string? Surname { get; }
        public string? Initials { get; }

        public SearchCriteria(string? surname = null, string? initials = null)
        {
            if (surname == null && initials == null)
            {
                throw new ArgumentException(
                    "At least one of the parameters - either surname or initials - must be non-null"
                );
            }

            Surname = surname;
            Initials = initials;
        }
    }
}
