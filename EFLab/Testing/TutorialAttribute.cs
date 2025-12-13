namespace EFLab.Testing;

/// <summary>
/// Marks a test with tutorial content explaining the concept, the pitfall, and the fix.
/// This keeps documentation tightly coupled with the actual test code.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TutorialAttribute : Attribute
{
    public string Title { get; set; }
    public string Category { get; set; }
    public string Concept { get; set; }
    public string Pitfall { get; set; }
    public string Fix { get; set; }
    public string AdditionalNotes { get; set; }
    public int Order { get; set; }

    public TutorialAttribute(
        string title,
        string category,
        string concept,
        string pitfall,
        string fix,
        int order = 0)
    {
        Title = title;
        Category = category;
        Concept = concept;
        Pitfall = pitfall;
        Fix = fix;
        AdditionalNotes = string.Empty;
        Order = order;
    }

    /// <summary>
    /// Generates markdown documentation for this tutorial test.
    /// </summary>
    public string ToMarkdown()
    {
        var md = new System.Text.StringBuilder();
        md.AppendLine($"## {Title}");
        md.AppendLine();
        md.AppendLine($"**Category:** {Category}");
        md.AppendLine();
        md.AppendLine("### Concept");
        md.AppendLine(Concept);
        md.AppendLine();
        md.AppendLine("### The Pitfall");
        md.AppendLine(Pitfall);
        md.AppendLine();
        md.AppendLine("### The Fix");
        md.AppendLine(Fix);
        md.AppendLine();

        if (!string.IsNullOrEmpty(AdditionalNotes))
        {
            md.AppendLine("### Additional Notes");
            md.AppendLine(AdditionalNotes);
            md.AppendLine();
        }

        return md.ToString();
    }
}
