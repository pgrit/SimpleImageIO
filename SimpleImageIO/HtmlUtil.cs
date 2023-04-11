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
    /// <returns>HTML code wrapping the given data into a basic table</returns>
    public static string MakeTable(IEnumerable<IEnumerable<string>> rows) {
        StringBuilder html = new();
        html.Append("<table>");
        foreach (var row in rows) {
            html.Append("<tr>");
            foreach (var col in row) {
                html.Append("<td>");
                html.Append(col);
                html.Append("</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</table>");
        return html.ToString();
    }
}
