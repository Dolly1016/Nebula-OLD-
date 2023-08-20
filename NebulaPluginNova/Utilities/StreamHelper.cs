using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public static class StreamHelper
{
    public static Stream? OpenFromResource(string path)
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        }
        catch
        {
            return null;
        }
    }

    public static Stream? OpenFromDisk(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open);
        }
        catch
        {
            return null;
        }
    }
}
