using Sentry.Unity.NativeUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

static public class StringHelper
{
    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    public static string Color(this string original,Color color)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(color.r), ToByte(color.g), ToByte(color.b), ToByte(color.a), original);
    }

    public static string ColorBegin(Color color) => string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>", ToByte(color.r), ToByte(color.g), ToByte(color.b), ToByte(color.a));
    public static string ColorEnd() => "</color>";
}
