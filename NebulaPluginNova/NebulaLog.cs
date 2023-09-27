using Il2CppMicrosoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula;

public class NebulaLog
{
    StreamWriter writer;
    int number;
    public bool IsPreferential => number == 0;
    private static string FileName = "NebulaLog";
    public NebulaLog()
    {

        int counter = 0;
        Stream? stream;

        while (true)
        {
            string path = FileName;
            if (counter > 0) path += " " + counter;
            path += ".txt";

            try
            {
                stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,FileShare.Read);
                stream.SetLength(0);
            }
            catch
            {
                counter++;
                continue;
            }

            //Debug.Log("My log file path :" + path);

            break;
        }

        writer = new(stream, Encoding.UTF8);
        writer.AutoFlush = true;

        lock (writer)
        {
            writer.WriteLine("\n  Nebula on the Ship  Log File \n");
        }

        number = counter;
    }

    public class LogCategory
    {
        public string Category;
        public LogCategory(string category) {
            this.Category = category;
        }

        static public LogCategory MoreCosmic = new("MoreCosmic");
        static public LogCategory Language = new("Language");
        static public LogCategory Addon = new("Addon");
        static public LogCategory Document = new("Documentation");
    }

    public void Print(LogCategory? category,string message)
    {
        message = message.Replace("\n", "\n    ");
        lock (writer)
        {
            writer.WriteLine("[" + (category?.Category ?? "Generic") + "] " + message);
        }
    }
}
