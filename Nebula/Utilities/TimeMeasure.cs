using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Nebula.Utilities
{
    public class TimeMeasure
    {
        Stopwatch stopwatch;
        List<Tuple<string, long>> record;
        public TimeMeasure()
        {
            record = new List<Tuple<string, long>>();
            stopwatch = new Stopwatch();
        }

        public void StartTimer()
        {
            record.Clear();
            stopwatch.Start();
        }

        public void LapTime()
        {
            LapTime("");
        }
        public void LapTime(string label)
        {
            stopwatch.Stop();
            record.Add(new Tuple<string, long>(label, stopwatch.ElapsedTicks));
            stopwatch.Restart();
        }

        public void Output(string title)
        {
            StringBuilder output = new StringBuilder();
            output.Append("[").Append(title).Append("]");
            bool isFirst = true;
            foreach (var t in record)
            {
                if (!isFirst) output.Append(", "); else isFirst = false;
                output.Append(t);
            }
            NebulaPlugin.Instance.Logger.Print(output.ToString());
        }
    }
}
