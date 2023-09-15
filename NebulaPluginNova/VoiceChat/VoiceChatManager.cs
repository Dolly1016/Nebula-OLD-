using Mono.Cecil.Cil;
using NAudio.CoreAudioApi;
using NAudio.Dmo.Effect;
using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace Nebula.VoiceChat;

public enum VoiceType
{
    Normal,
    Ghost,
    Radio
}


[NebulaRPCHolder]
public class VoiceChatManager : IDisposable
{

    TcpListener? myListener = null;
    TcpClient? myClient;
    Process? childProcess;

    MixingSampleProvider routeNormal, routeGhost, routeRadio, routeMixer;
    IWavePlayer myPlayer;

    Dictionary<byte, VCClient> allClients = new();

    static public bool IsInDiscussion => (MeetingHud.Instance && MeetingHud.Instance.CurrentState != MeetingHud.VoteStates.Animating) || (ExileController.Instance && !Minigame.Instance);
    public VoiceChatManager()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(22050, 2);
        routeNormal = new(format) { ReadFully = true };
        routeGhost = new(format) { ReadFully = true };
        routeRadio = new(format) { ReadFully = true };
        routeMixer = new(format) { ReadFully = true };

        //通常
        routeMixer.AddMixerInput(routeNormal);

        //幽霊(エフェクト)
        {
            BufferedWaveProvider reverbProvider = new(format) { ReadFully = true, BufferLength = 1 << 19 };
            
            MixingSampleProvider remixer = new(format) { ReadFully = true };
            remixer.AddMixerInput(new ReverbSampleProvider(routeGhost));
            remixer.AddMixerInput(reverbProvider);

            SampleFunctionalProvider resampler = new(remixer, (ary, count) =>
            {
                byte[] byteArray = new byte[count * 4];
                for (int i = 0; i < count; i++)
                {
                    Unsafe.As<byte, float>(ref byteArray[i * 4]) = (float)(ary[i] * 0.55f);
                }
                reverbProvider.AddSamples(byteArray, 0, byteArray.Length);
            });

            reverbProvider.AddSamples(new byte[8192], 0, 8192);
            routeMixer.AddMixerInput(resampler);
        }
        

        //ラジオ
        {
            var lowPass = BiQuadFilter.LowPassFilter(22050, 2300, 1f);
            var highPass = BiQuadFilter.HighPassFilter(22050, 300, 0.8f);
            SampleFunctionalProvider radioEffector = new(routeRadio, (f) =>
            {
                f = highPass.Transform(lowPass.Transform(f));
                f = Math.Clamp(f * 1.4f, -0.28f, 0.28f) * 2.8f;
                return f;
            });
            routeMixer.AddMixerInput(radioEffector);
        }
        

        var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        myPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 200);
        myPlayer.Init(routeMixer);
        myPlayer.Play();

        if (NebulaPlugin.MyPlugin.IsPreferential) NebulaManager.Instance.StartCoroutine(CoCommunicate().WrapToIl2Cpp());
    }

    public MixingSampleProvider GetRoute(VoiceType type)
    {
        switch (type)
        {
            case VoiceType.Radio:
                return routeRadio;
            case VoiceType.Ghost:
                return routeGhost;
            case VoiceType.Normal:
            default:
                return routeNormal;
        }
    }

    public void OnGameStart()
    {
        foreach (var c in allClients.Values) c.OnGameStart();
    }

    public void Update()
    {
        foreach (var entry in allClients)
        {
            if (!entry.Value.IsValid)
            {
                entry.Value.Dispose();
                allClients.Remove(entry.Key);
                continue;
            }

            entry.Value.Update();
        }

        if(PlayerControl.AllPlayerControls.Count != allClients.Count)
        {
            UnityEngine.Debug.Log("Mixer has " + routeMixer.MixerInputs.Count() + " inputs");

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (!allClients.ContainsKey(p.PlayerId))
                {
                    allClients[p.PlayerId] = new(p);
                    //allClients[p.PlayerId].SetRoute(routeNormal);
                    allClients[p.PlayerId].SetRoute(routeGhost);
                }
            }
        }
    }

    private void StartSubprocess()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        string id = process.Id.ToString();

        ProcessStartInfo processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = "VoiceChatSupport.exe";
        processStartInfo.Arguments = id;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;
        childProcess = Process.Start(processStartInfo);
    }

    private IEnumerator CoCommunicate()
    {
        myListener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 11010);
        myListener.Start();

        StartSubprocess();

        var task = myListener.AcceptTcpClientAsync();
        while (!task.IsCompleted) yield return new WaitForSeconds(0.4f);

        if (task.IsFaulted)
        {
            NebulaPlugin.Log.Print(null,"Failed to connect.");
            yield break;
        }

        myClient = task.Result;
        NetworkStream voiceStream = myClient.GetStream();

        myListener.Stop();

        int resSize;
        byte[] headRes = new byte[2];
        while (true)
        {
            //ヘッダーを受信 (長さのみ)
            while (!voiceStream.DataAvailable) yield return null;

            var readHeaderTask = voiceStream.ReadAsync(headRes, 0, 2);
            if (!readHeaderTask.IsCompleted) yield return null;
            if (readHeaderTask.Result == 0) continue;

            resSize = BitConverter.ToInt16(headRes, 0);

            if (resSize == 0) break;

            //UnityEngine.Debug.Log($"Received Header size:{resSize} ({res[0]},{res[1]})");

            int read = 0;
            byte[] res = new byte[resSize];
            while (read < resSize)
            {
                var readBodyTask = voiceStream.ReadAsync(res, read, resSize - read);
                if (!readBodyTask.IsCompleted) yield return null;
                read += readBodyTask.Result;
            }

            RpcSendAudio.Invoke((PlayerControl.LocalPlayer.PlayerId, resSize, res));
        }
    }

    public void Dispose()
    {
        myClient?.Close();
        myClient?.Dispose();
        myListener.Stop();
        childProcess?.Kill();
        childProcess = null;
    }

    static private RemoteProcess<(byte clientId,int dataLength,byte[] dataAry)> RpcSendAudio = new(
        "SendAudio",
        (writer,message) => {
            writer.Write(message.clientId);
            writer.Write(message.dataLength);
            writer.Write(message.dataAry,0,message.dataLength);
        },
        (reader) => {
            byte id = reader.ReadByte();
            int length = reader.ReadInt32();
            return (id, length, reader.ReadBytes(length));
            },
        (message,calledByMe) => {
            if(NebulaGameManager.Instance?.VoiceChatManager?.allClients.TryGetValue(message.clientId,out var client) ?? false)
                client?.OnReceivedData(message.dataAry);
        }
        );
}

public class SampleFunctionalProvider : ISampleProvider
{
    ISampleProvider sourceProvider;
    public WaveFormat WaveFormat { get => sourceProvider.WaveFormat; }
    public int Read(float[] buffer, int offset, int count)
    {
        int num = sourceProvider.Read(buffer, offset, count);
        if (OnReadArray != null) OnReadArray(buffer, count);
        if(OnRead != null) for(int i = 0; i < num; i++) buffer[i] = OnRead(buffer[i]);

        return num;
    }

    public SampleFunctionalProvider(ISampleProvider sourceProvider, Func<float, float>? onRead)
    {
        this.sourceProvider = sourceProvider;
        OnRead = onRead;
    }

    public SampleFunctionalProvider(ISampleProvider sourceProvider, Action<float[],int>? onRead)
    {
        this.sourceProvider = sourceProvider;
        OnReadArray = onRead;
    }

    public Func<float, float>? OnRead = null;
    public Action<float[], int>? OnReadArray = null;
}


//CircularBufferにPeekを追加したもの
public class AdvancedCircularBuffer<T>
{
    private readonly T[] buffer;

    private readonly object lockObject;

    private int writePosition;

    private int readPosition;

    private int byteCount;

    public int MaxLength => buffer.Length;

    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return byteCount;
            }
        }
    }

    public AdvancedCircularBuffer(int size)
    {
        buffer = new T[size];
        lockObject = new object();
    }

    public int Write(T[] data, int offset, int count)
    {
        lock (lockObject)
        {
            int num = 0;
            if (count > buffer.Length - byteCount)
            {
                count = buffer.Length - byteCount;
            }

            int num2 = Math.Min(buffer.Length - writePosition, count);
            Array.Copy(data, offset, buffer, writePosition, num2);
            writePosition += num2;
            writePosition %= buffer.Length;
            num += num2;
            if (num < count)
            {
                Array.Copy(data, offset + num, buffer, writePosition, count - num);
                writePosition += count - num;
                num = count;
            }

            byteCount += num;
            return num;
        }
    }

    public int Read(T[] data, int offset, int count)
    {
        lock (lockObject)
        {
            if (count > byteCount)
            {
                count = byteCount;
            }

            int num = 0;
            int num2 = Math.Min(buffer.Length - readPosition, count);
            Array.Copy(buffer, readPosition, data, offset, num2);
            num += num2;
            readPosition += num2;
            readPosition %= buffer.Length;
            if (num < count)
            {
                Array.Copy(buffer, readPosition, data, offset + num, count - num);
                readPosition += count - num;
                num = count;
            }

            byteCount -= num;
            return num;
        }
    }

    public int Peek(T[] data, int offset, int count,int bufferOffset)
    {
        lock (lockObject)
        {
            if (count > byteCount)
            {
                count = byteCount;
            }

            int num = 0;
            int peekPosition = (buffer.Length + readPosition + bufferOffset) % buffer.Length;
            int num2 = Math.Min(buffer.Length - peekPosition, count);
            Array.Copy(buffer, peekPosition, data, offset, num2);
            num += num2;
            peekPosition += num2;
            peekPosition %= buffer.Length;
            if (num < count)
            {
                Array.Copy(buffer, peekPosition, data, offset + num, count - num);
                peekPosition += count - num;
                num = count;
            }

            byteCount -= num;
            return num;
        }
    }

    public void Reset()
    {
        lock (lockObject)
        {
            ResetInner();
        }
    }

    private void ResetInner()
    {
        byteCount = 0;
        readPosition = 0;
        writePosition = 0;
    }

    public void Advance(int count)
    {
        lock (lockObject)
        {
            if (count >= byteCount)
            {
                ResetInner();
                return;
            }

            byteCount -= count;
            readPosition += count;
            readPosition %= MaxLength;
        }
    }
}

public class ReverbBufferedSampleProvider : ISampleProvider
{
    private AdvancedCircularBuffer<float> circularBuffer;

    private readonly WaveFormat waveFormat;
    public bool ReadFully { get; set; }

    public int BufferLength { get; set; }

    public TimeSpan BufferDuration
    {
        get
        {
            return TimeSpan.FromSeconds((double)BufferLength / (double)WaveFormat.AverageBytesPerSecond);
        }
        set
        {
            BufferLength = (int)(value.TotalSeconds * (double)WaveFormat.AverageBytesPerSecond);
        }
    }

    public int BufferedSamples
    {
        get
        {
            if (circularBuffer != null)
            {
                return circularBuffer.Count;
            }

            return 0;
        }
    }

    public TimeSpan BufferedDuration => TimeSpan.FromSeconds((double)BufferedSamples / (double)WaveFormat.AverageBytesPerSecond);

    public WaveFormat WaveFormat => waveFormat;

    public ReverbBufferedSampleProvider(WaveFormat waveFormat)
    {
        this.waveFormat = waveFormat;
        BufferLength = waveFormat.AverageBytesPerSecond * 1;
        ReadFully = true;
    }

    public void AddSamples(float[] buffer, int offset, int count)
    {
        if (circularBuffer == null)
        {
            circularBuffer = new AdvancedCircularBuffer<float>(BufferLength);
        }

        if (circularBuffer.Write(buffer, offset, count) < count)
        {
            throw new InvalidOperationException("Advanced Circular Buffer full");
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int num = 0;
        if (circularBuffer != null)
        {
            num = circularBuffer.Read(buffer, offset, count);
        }

        if (ReadFully && num < count)
        {
            Array.Clear(buffer, offset + num, count - num);
            num = count;
        }

        return num;
    }

    public int Peek(float[] buffer, int offset, int count, int bufferOffset)
    {
        int num = 0;
        if (circularBuffer != null)
        {
            num = circularBuffer.Peek(buffer, offset, count, bufferOffset);
        }

        if (ReadFully && num < count)
        { 
            Array.Clear(buffer, offset + num, count - num);
            num = count;
        }

        return num;
    }

    public void ClearBuffer()
    {
        if (circularBuffer != null)
        {
            circularBuffer.Reset();
        }
    }
}

public class ReverbSampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get => reverb.WaveFormat; }
    private ReverbBufferedSampleProvider reverb;
    private ISampleProvider sourceProvider;

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, 0, count);
        int num = sourceProvider.Read(buffer,offset,count);
        reverb.AddSamples(buffer, offset, count);

        float[] reverbBuffer = new float[count];
        for (int n = 0; n < 7; n++)
        {
            reverb.Peek(reverbBuffer, 0, count, -1100 * (n + 1));
            for (int i = 0; i < count; i++) buffer[i + offset] += reverbBuffer[i] * (float)Math.Pow(0.985f, (float)(n + 1.2f));
        }
        reverb.Read(reverbBuffer, 0, count);
        return num;
    }

    public ReverbSampleProvider(ISampleProvider provider)
    {
        sourceProvider = provider;
        reverb = new(sourceProvider.WaveFormat);
    }
}