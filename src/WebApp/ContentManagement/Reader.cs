using System.Text;

namespace WebApp.ContentManagement;

public class Reader
{
    public const string ContentSeparator = "<!-- Content -->";

    public static string ReadCard(string topicFile)
    {
        if (string.IsNullOrWhiteSpace(topicFile))
            return string.Empty;

        // Read the file line by line
        while (true)
        {
            using var sr = new StreamReader(topicFile);
            var sb = new StringBuilder();
            string? line;
            while ((line = sr.ReadLine()) != null && !line.Equals(ContentSeparator, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }

    public static string ReadContent(string topicFile)
    {
        if (string.IsNullOrWhiteSpace(topicFile))
            return string.Empty;

        // Read the file line by line
        while (true)
        {
            using var sr = new StreamReader(topicFile);
            string? line;

            // Find the content separator
            while ((line = sr.ReadLine()) != null && !line.Equals(ContentSeparator, StringComparison.OrdinalIgnoreCase))
            {
            }

            if (line == null)
                return string.Empty;

            var sb = new StringBuilder();
            while ((line = sr.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}
