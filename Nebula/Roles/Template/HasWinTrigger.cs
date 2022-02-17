using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.Template
{
    public interface HasWinTrigger
    {
        public bool WinTrigger { get; set; }
        public byte Winner { get; set; }
    }
}
