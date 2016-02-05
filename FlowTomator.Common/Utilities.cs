using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Utilities
{
    public static string MakeRelativePath(string directory, string target)
    {
        if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException("fromPath");
        if (string.IsNullOrEmpty(target)) throw new ArgumentNullException("toPath");

        Uri fromUri = new Uri(directory + "/");
        Uri toUri = new Uri(target);

        if (fromUri.Scheme != toUri.Scheme)
            return target; // Path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.ToUpperInvariant() == "FILE")
            relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

        return relativePath;
    }
}