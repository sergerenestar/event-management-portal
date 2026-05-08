using EventPortal.Api.Modules.Shared.Utilities;
using FluentAssertions;

namespace EventPortal.Tests.Shared;

public class TicketTypeNameParserTests
{
    // ── ParseLocation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("London Branch — Adult",        "London Branch")]
    [InlineData("Manchester — Children",        "Manchester")]
    [InlineData("Birmingham — Child",           "Birmingham")]
    [InlineData("London — North Branch — Adult","London — North Branch")]
    public void ParseLocation_WithSeparator_ReturnsLocationPrefix(string input, string expected)
        => TicketTypeNameParser.ParseLocation(input).Should().Be(expected);

    [Theory]
    [InlineData("General Admission", "General Admission")]
    [InlineData("VIP",               "VIP")]
    [InlineData("  Trimmed  ",       "Trimmed")]
    public void ParseLocation_NoSeparator_ReturnsFullNameTrimmed(string input, string expected)
        => TicketTypeNameParser.ParseLocation(input).Should().Be(expected);

    // ── ParseAttendeeType ──────────────────────────────────────────────────

    [Theory]
    [InlineData("London Branch — Adult",  "Adult")]
    [InlineData("General Admission",      "Adult")]
    [InlineData("VIP",                    "Adult")]
    [InlineData("Standard",              "Adult")]
    public void ParseAttendeeType_NoChildKeyword_ReturnsAdult(string input, string expected)
        => TicketTypeNameParser.ParseAttendeeType(input).Should().Be(expected);

    [Theory]
    [InlineData("Manchester — Children", "Children")]
    [InlineData("Birmingham — Child",    "Children")]
    [InlineData("CHILDREN TICKET",       "Children")]
    public void ParseAttendeeType_ChildKeyword_ReturnsChildren(string input, string expected)
        => TicketTypeNameParser.ParseAttendeeType(input).Should().Be(expected);

    [Fact]
    public void ParseAttendeeType_BothAdultAndChildKeywords_ReturnsOther()
        => TicketTypeNameParser.ParseAttendeeType("Adult + Child Family Pass").Should().Be("Other");

    [Fact]
    public void ParseAttendeeType_IsCaseInsensitive()
    {
        TicketTypeNameParser.ParseAttendeeType("ADULT TICKET").Should().Be("Adult");
        TicketTypeNameParser.ParseAttendeeType("children admission").Should().Be("Children");
    }
}
