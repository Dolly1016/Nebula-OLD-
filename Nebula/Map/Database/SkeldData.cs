using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Database
{
    public class SkeldData : MapData
    {
        public SkeldData() :base(0)
        {
            CommonTaskIdList = new List<byte>() { 0, 1 };
            ShortTaskIdList = new List<byte>() { 11, 12, 13, 14, 16, 17, 18, 19,21,24,28,29 };
            LongTaskIdList = new List<byte>() { 2, 3, 4, 5, 6, 7, 8, 9 };
        }
    }
}
