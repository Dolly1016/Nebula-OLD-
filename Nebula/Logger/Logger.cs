using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Nebula.Logger
{
    public class Logger
    {
        private string Path;
        private bool ValidFlag;

        public Logger(bool isValid,string path= "NebulaLog.txt")
        {
            ValidFlag = isValid;
            if (!ValidFlag) return;

            Path = path;

            //以前のログを消去する
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var writer= new StreamWriter(Path, true))
            {
                writer.WriteLine(" - Nebula on the Ship  "+ NebulaPlugin.PluginStage + " v" + NebulaPlugin.PluginVersion +"  Log File - ");
            }
        }

        private void __Print(string message)
        {
            using (var writer = new StreamWriter(Path, true))
            {
                writer.Write(message);
            }
        }

        public void Print(string message)
        {
            if (!ValidFlag) return;

            if (message.EndsWith('\n'))
            {
                __Print(message);
            }
            else
            {
                __Print(message + "\n");
            }
        }
    }
}
