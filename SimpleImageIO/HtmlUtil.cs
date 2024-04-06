using System.Text;

namespace SimpleImageIO;

/// <summary>
/// Utility functions for HTML code generation.
/// </summary>
public static class HtmlUtil {
    /// <summary>
    /// Makes a simple HTML table out of a list of lists of strings.
    /// </summary>
    /// <param name="rows">The rows of the table</param>
    /// <param name="hasHead">If true, the first row will use th tags instead of td</param>
    /// <returns>HTML code wrapping the given data into a basic table</returns>
    public static string MakeTable(IEnumerable<IEnumerable<string>> rows, bool hasHead) {
        StringBuilder html = new();
        html.Append("<table>");
        bool isFirstRow = true;
        foreach (var row in rows) {
            html.Append("<tr>");
            foreach (var col in row) {
                if (isFirstRow && hasHead)
                    html.Append("<th>");
                else
                    html.Append("<td>");

                html.Append(col);

                if (isFirstRow && hasHead)
                    html.Append("</th>");
                else
                    html.Append("</td>");
            }
            html.Append("</tr>");
            isFirstRow = false;
        }
        html.Append("</table>");
        return html.ToString();
    }

    /// <summary>
    /// Stitches a valid html code based on the given contents for the head and body parts
    /// </summary>
    /// <param name="head">Contents of the head tag</param>
    /// <param name="body">Contents of the body tag</param>
    /// <returns>String containing the html code</returns>
    public static string MakeHTML(string head, string body)
    => $"""<!DOCTYPE html><html lang="en"><head>{head}</head><body>{body}</body></html>""";
}
