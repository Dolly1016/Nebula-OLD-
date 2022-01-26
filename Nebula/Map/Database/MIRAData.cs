using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Database
{
    public class MIRAData : MapData
    {
        public MIRAData() : base(1)
        {
            CommonTaskIdList = new List<byte>() { 0, 1 };
            ShortTaskIdList = new List<byte>() { 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 };
            LongTaskIdList = new List<byte>() { 2, 3, 4, 8, 9, 10, 11, 12 };
        }
    }
}
