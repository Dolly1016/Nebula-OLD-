using Il2CppSystem.Reflection.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public class Reference<T>
{
    public T? Value { get; set; } = default(T);

    public Reference<T> Set(T value)
    {
        Value = value;
        return this;
    }

    public IEnumerator Wait()
    {
        while (Value == null) yield return null;
        yield break;
    }
}

public static class Helpers
{
    public static void DeleteDirectoryWithInnerFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;
        
        string[] filePaths = Directory.GetFiles(directoryPath);
        foreach (string filePath in filePaths)
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }

        string[] directoryPaths = Directory.GetDirectories(directoryPath);
        foreach (string path in directoryPaths) DeleteDirectoryWithInnerFiles(path);
        
        Directory.Delete(directoryPath, false);
    }

    public static PlayerControl? GetPlayer(byte? id)
    {
        if (!id.HasValue) return null;
        return PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)((p) => p.PlayerId == id!));
    }

    public static DeadBody? GetDeadBody(byte id)
    {
        return AllDeadBodies().FirstOrDefault((p) => p.ParentId == id);
    }

    public static int ComputeConstantHash(this string str)
    {
        const int MulPrime = 127;
        const int SurPrime = 104729;

        int val = 0;
        int mul = 1;
        foreach (char c in str)
        {
            mul *= MulPrime;
            mul %= SurPrime;
            val += (int)c * mul;
            val %= SurPrime;
        }
        return val;
    }

    public static DeadBody[] AllDeadBodies()
    {
        //Componentで探すよりタグで探す方が相当はやい
        var bodies = GameObject.FindGameObjectsWithTag("DeadBody");
        DeadBody[] deadBodies = new DeadBody[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) deadBodies[i] = bodies[i].GetComponent<DeadBody>();
        return deadBodies;
    }

    public static int[] GetRandomArray(int length)
    {
        var array = new int[length];
        for (int i = 0; i < length; i++) array[i] = i;
        return array.OrderBy(i => Guid.NewGuid()).ToArray();
    }


    public static string GetClipboardString()
    {
        uint type = 0;
        if (ClipboardHelper.IsClipboardFormatAvailable(1U)) { type = 1U; Debug.Log("ASCII"); }
        if (ClipboardHelper.IsClipboardFormatAvailable(13U)) { type = 13U; Debug.Log("UNICODE"); }
        if (type == 0) return "";

        string result;
        try
        {
            if (!ClipboardHelper.OpenClipboard(IntPtr.Zero))
            {
                result = "";
            }
            else
            {

                IntPtr clipboardData = ClipboardHelper.GetClipboardData(type);
                if (clipboardData == IntPtr.Zero)
                    result = "";
                else
                {
                    IntPtr intPtr = IntPtr.Zero;
                    try
                    {
                        intPtr = ClipboardHelper.GlobalLock(clipboardData);
                        int len = ClipboardHelper.GlobalSize(clipboardData);

                        if (type == 1U)
                            result = Marshal.PtrToStringAnsi(clipboardData, len);
                        else
                        {
                            result = Marshal.PtrToStringUni(clipboardData) ?? "";
                        }
                    }
                    finally
                    {
                        if (intPtr != IntPtr.Zero) ClipboardHelper.GlobalUnlock(intPtr);
                    }
                }
            }
        }
        finally
        {
            ClipboardHelper.CloseClipboard();
        }
        return result;
    }
}
