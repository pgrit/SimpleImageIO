using System.Text;

namespace SimpleImageIO;

public static class HtmlUtil {
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
