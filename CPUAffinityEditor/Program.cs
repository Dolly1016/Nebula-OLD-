using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CPUAffinityEditor
{
    class FloatWithIndex
    {
        public int index { get; private set; }
        public float value { get; set; }

        public FloatWithIndex(int index)
        {
            this.index = index;
            this.value = 0.0f;
        }

        public FloatWithIndex(int index,float value)
        {
            this.index = index;
            this.value = value;
        }
    }
    class Program
    {
        static FloatWithIndex[] GetProcessorBusyness(float duration)
        {
            int processors = Environment.ProcessorCount;
            PerformanceCounter[] counters = new PerformanceCounter[processors];
            FloatWithIndex[] result = new FloatWithIndex[processors];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new FloatWithIndex(i);
                counters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
            }

            int max = (int)(duration * 100);
            for(int t = 0; t < max; t++)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i].value += counters[i].NextValue();
                }
                System.Threading.Thread.Sleep(10);
            }

            //計測時間に依らず平均使用率を返すようにする
            for (int i = 0; i < result.Length; i++)
            {
                result[i].value /= duration;
            }

            return result;
        }

        static FloatWithIndex[] GetPhysicalProcessorBusyness(FloatWithIndex[] logicalBusyness)
        {
            FloatWithIndex[] result = new FloatWithIndex[logicalBusyness.Length / 2];
            for(int i = 0; i < result.Length; i++)
            {
                result[i] = new FloatWithIndex(i, (logicalBusyness[i * 2].value + logicalBusyness[i * 2 + 1].value) * 0.5f);
            }
            return result;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2) return;

                //プロセスを取得
                var p = System.Diagnostics.Process.GetProcessById(int.Parse(args[0]));


                //優先度の設定

                if (args[1] == "Enable")
                {
                    p.PriorityBoostEnabled = true;
                    return;

                }
                else if(args[1]=="Disable")
                {
                    p.PriorityBoostEnabled = false;
                    return;
                }

                //Affinityの設定

                if (args[1] == "0")
                {
                    //全てのコアを使用可能にする(Dont Care)
                    int processors=Environment.ProcessorCount;
                    int affinity = 0;
                    for(int i = 0; i < processors; i++)
                    {
                        affinity <<= 1;
                        affinity |= 1;
                    }
                    p.ProcessorAffinity = (IntPtr)affinity;
                    return;
                }

                var busyness = GetProcessorBusyness(1f);

                if (args[1] == "1")
                {
                    //1つのコアを選択
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    p.ProcessorAffinity = (IntPtr)(1 << busyness[0].index);
                }
                else if (args[1] == "2")
                {
                    //2つのコアを選択
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    int mask = 0;
                    for(int i = 0; i < 2; i++)
                    {
                        mask |= 1 << busyness[i].index;
                    }
                    p.ProcessorAffinity = (IntPtr)(mask);
                }
                else if (args[1] == "2HT")
                {
                    //同じ物理コアからなる2つのコアを選択(nコア2nスレッドのタイプと仮定)
                    busyness = GetPhysicalProcessorBusyness(busyness);
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    p.ProcessorAffinity = (IntPtr)(0b11 << (busyness[0].index*2));
                }
            }
            catch
            {
            }
        }
    }
}
