using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

public class Program
{

    static WaveInEvent input;
    static BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
    static VolumeWaveProvider16 volumedProvider = new VolumeWaveProvider16(bufferedWaveProvider);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr LoadLibrary(string lpFileName);
    [DllImport("kernel32", SetLastError = true)]
    internal static extern bool FreeLibrary(IntPtr hModule);
    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
    internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    delegate int CreateDelegate();
    delegate void ProcessDelegate(int length, IntPtr input, IntPtr output);
    delegate void DestroyDelegate();
    public static void Main(string[] args)
    {
        IntPtr ptrLib = LoadLibrary("rnnoise.dll");
        IntPtr ptrCreate = GetProcAddress(ptrLib, "dll_rnnoise_create");
        IntPtr ptrProcess = GetProcAddress(ptrLib, "dll_rnnoise_process");
        IntPtr ptrDestroy = GetProcAddress(ptrLib, "dll_rnnoise_destroy");

        CreateDelegate createDelegate = (CreateDelegate)Marshal.GetDelegateForFunctionPointer(ptrCreate, typeof(CreateDelegate));
        ProcessDelegate processDelegate = (ProcessDelegate)Marshal.GetDelegateForFunctionPointer(ptrProcess, typeof(ProcessDelegate));
        DestroyDelegate destroyDelegate = (DestroyDelegate)Marshal.GetDelegateForFunctionPointer(ptrDestroy, typeof(DestroyDelegate));

        createDelegate();

        List<float> buffer = new();

        input = new WaveInEvent();
        input.DeviceNumber = 0;
        input.WaveFormat = new WaveFormat(44100, 1);
        
        input.DataAvailable += (_, waveEvent) =>
        {
            int index = 0;
            while (index < waveEvent.Buffer.Length)
            {
                float floatVal;

                floatVal = BitConverter.ToInt16(waveEvent.Buffer, index);
                buffer.Add(floatVal);
                index += 2;
            }

            if (buffer.Count >= 480)
            {
                int units = buffer.Count / 480;
                var floatAry = buffer.ToArray();

                int size = Marshal.SizeOf(typeof(float)) * 480 * units;
                System.IntPtr inputPtr = Marshal.AllocCoTaskMem(size);
                System.IntPtr outputPtr = Marshal.AllocCoTaskMem(size);

                Marshal.Copy(floatAry, 0, inputPtr, 480 * units);
                processDelegate(480 * units, inputPtr, outputPtr);
                Marshal.Copy(outputPtr, floatAry, 0, 480 * units);

                Marshal.FreeCoTaskMem(inputPtr);
                Marshal.FreeCoTaskMem(outputPtr);

                byte[] byteAry = new byte[480 * units * 4];
                for (int i = 0; i < 480 * units; i++)
                {
                    var bytes = BitConverter.GetBytes((short)(floatAry[i]));
                    byteAry[i * 4 + 0] = bytes[0];
                    byteAry[i * 4 + 1] = bytes[1];
                    byteAry[i * 4 + 2] = bytes[0];
                    byteAry[i * 4 + 3] = bytes[1];
                }

                bufferedWaveProvider.AddSamples(byteAry, 0, byteAry.Length);

                buffer.RemoveRange(0,480*units);
            }

        };


        input.StartRecording();
        
        var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 200))
        {
            
            wavPlayer.Init(volumedProvider);
            wavPlayer.Play();

            Console.ReadLine();
        }

        destroyDelegate();


    }
}