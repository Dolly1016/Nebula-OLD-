using System.Text;
using System.Diagnostics;

namespace Nebula.Utilities;

public class TimeMeasure
{
    Stopwatch stopwatch;
    List<Tuple<string, long>> record;
    public TimeMeasure()
    {
        record = new List<Tuple<string, long>>();
        stopwatch = new Stopwatch();
        StartTimer();
    }

    //タイマーをスタートします。
    public void StartTimer()
    {
        record.Clear();
        stopwatch.Start();
    }

    //経過時間を記録し、時間をリセットします。

    public void LapTime()
    {
        LapTime("");
    }

    //経過時間を記録し、時間をリセットします。
    public void LapTime(string label)
    {
        stopwatch.Stop();
        record.Add(new Tuple<string, long>(label, stopwatch.ElapsedTicks));
        stopwatch.Restart();
    }

    //計測した時間を纏めて書きだします。
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
