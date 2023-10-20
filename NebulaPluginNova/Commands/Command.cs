using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Commands;

public class Command
{

}

[NebulaRPCHolder]
public class CommandManager
{
    static private Dictionary<string, Command> commandMap = new();
}
