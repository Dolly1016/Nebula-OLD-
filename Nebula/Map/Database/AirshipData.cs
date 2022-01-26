using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Database
{
    public class AirshipData : MapData 
    {
        public AirshipData() : base(4)
        {
            CommonTaskIdList = new List<byte>() { 0, 1 };
            ShortTaskIdList = new List<byte>() { 19, 20, 24, 27, 28, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            LongTaskIdList = new List<byte>() { 2, 3, 5, 8, 9, 10, 13, 14, 15, 16, 18 };
        }
    }
}
