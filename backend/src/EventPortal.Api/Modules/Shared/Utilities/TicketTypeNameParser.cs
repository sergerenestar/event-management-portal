namespace EventPortal.Api.Modules.Shared.Utilities;

/// <summary>
/// Parses structured ticket type names of the form "Location — AttendeeType"
/// (em-dash, en-dash, or hyphen-space separator).
/// </summary>
public static class TicketTypeNameParser
{
    private static readonly string[] Separators = [" — ", " – ", " - "];

    /// <summary>
    /// Returns the location prefix from a structured ticket type name.
    /// Splits on the LAST occurrence of any known separator.
    /// If no separator found, returns the full name trimmed.
    /// Example: "London Branch — Adult" → "London Branch"
    /// Example: "London — North — Adult" → "London — North"
    /// Example: "General Admission" → "General Admission"
    /// </summary>
    public static string ParseLocation(string ticketTypeName)
    {
        int lastSepIndex = -1;
        int lastSepLength = 0;

        foreach (var sep in Separators)
        {
            int idx = ticketTypeName.LastIndexOf(sep, StringComparison.Ordinal);
            if (idx > lastSepIndex)
            {
                lastSepIndex = idx;
                lastSepLength = sep.Length;
            }
        }

        return lastSepIndex >= 0
            ? ticketTypeName[..lastSepIndex].Trim()
            : ticketTypeName.Trim();
    }

    /// <summary>
    /// Classifies a ticket type as "Adult", "Children", or "Other".
    /// Rules (case-insensitive):
    ///   - Contains "child" or "children" AND "adult" → "Other"
    ///   - Contains "child" or "children" (no "adult") → "Children"
    ///   - All other names (including no keywords) → "Adult"
    /// Example: "London Branch — Adult" → "Adult"
    /// Example: "Manchester — Children" → "Children"
    /// Example: "Adult + Child Family Pass" → "Other"
    /// Example: "General Admission" → "Adult"
    /// </summary>
    public static string ParseAttendeeType(string ticketTypeName)
    {
        var lower = ticketTypeName.ToLowerInvariant();
        bool hasChild = lower.Contains("child") || lower.Contains("children");
        bool hasAdult = lower.Contains("adult");

        if (hasChild && hasAdult) return "Other";
        if (hasChild) return "Children";
        return "Adult";
    }
}
