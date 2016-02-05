using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Utilities
{
    public static string MakeRelativePath(string from, string to)
    {
        if (string.IsNullOrEmpty(from)) throw new ArgumentNullException("fromPath");
        if (string.IsNullOrEmpty(to)) throw new ArgumentNullException("toPath");

        Uri fromUri = new Uri(from);
        Uri toUri = new Uri(to);

        if (fromUri.Scheme != toUri.Scheme)
            return to; // Path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.ToUpperInvariant() == "FILE")
            relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

        return relativePath;
    }
}