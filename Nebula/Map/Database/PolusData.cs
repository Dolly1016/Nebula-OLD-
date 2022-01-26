using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Database
{
    public class PolusData : MapData
    {
        public PolusData() : base(2)
        {
            CommonTaskIdList = new List<byte>() { 0, 1, 2, 3 };
            ShortTaskIdList = new List<byte>() { 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31, 32 };
            LongTaskIdList = new List<byte>() { 5, 6, 9, 10, 11, 12, 13, 16, 17, 18 };
        }
    }
}
