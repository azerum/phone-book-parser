using Library.Parsing;

namespace Tests
{
    public class ParsingPhoneBookModelsValidationTests : BaseModelsValidationTests
    {
        public ParsingPhoneBookModelsValidationTests()
            : base(new ParsingSitePhoneBook()) { }
    }
}
