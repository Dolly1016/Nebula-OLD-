using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public interface INameSpace
{
    Stream? OpenRead(string innerAddress);
}

public class NameSpaceManager
{
    public class NebulaNameSpace : INameSpace
    {
        public Stream? OpenRead(string innerAddress)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources." + innerAddress);
        }
    }
    public static INameSpace DefaultNameSpace { get; private set; } = new NebulaNameSpace();
    public static INameSpace? Resolve(string name)
    {
        if (name == "Nebula") return DefaultNameSpace;

        var addonSpace = NebulaAddon.GetAddon(name);
        if (addonSpace != null) return addonSpace;

        return null;
    }

    public static INameSpace ResolveOrGetDefault(string name) => Resolve(name) ?? DefaultNameSpace;
}