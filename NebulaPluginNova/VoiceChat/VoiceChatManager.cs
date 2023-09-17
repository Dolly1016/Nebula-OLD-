using Il2CppSystem.ComponentModel;
using Mono.Cecil.Cil;
using NAudio.CoreAudioApi;
using NAudio.Dmo.Effect;
using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Nebula.Configuration;
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


public class VoiceChatRadio
{
    private Predicate<PlayerModInfo> predicate;
    public string DisplayRadioName { get; private set; }
    public Color Color { get; private set; }
    public int RadioMask
    {
        get
        {
            int mask = 0;
            foreach (var p in NebulaGameManager.Instance.AllPlayerInfo()) if (predicate.Invoke(p)) mask |= 1 << p.PlayerId;
            return mask;
        } 
    }

    public VoiceChatRadio(Predicate<PlayerModInfo> listenable,string displayName, Color radioColor)
    {
        predicate = listenable;
        this.DisplayRadioName = displayName;
        this.Color = radioColor;
    }
}

[NebulaRPCHolder]
public class VoiceChatManager : IDisposable
{

    TcpListener? myListener = null;
    TcpClient? myClient;
    Process? childProcess;

    MixingSampleProvider routeNormal, routeGhost, routeRadio, routeMixer;
    IWavePlayer myPlayer;

    public VoiceChatInfo InfoShower;

    public bool IsMuting;
    public int RadioMask;

    Dictionary<byte, VCClient> allClients = new();
    List<VoiceChatRadio> allRadios= new();
    VoiceChatRadio? currentRadio = null;

    private static bool AllowedUsingMic = false;
    private bool usingMic = false;
    private Coroutine? myCoroutine = null;

    public void AddRadio(VoiceChatRadio radio)=>allRadios.Add(radio);
    public void RemoveRadio(VoiceChatRadio radio)
    {
        allRadios.Remove(radio);
        if (radio == currentRadio) currentRadio = null;
    }
    
    static public bool CanListenGhostVoice
    {
        get {
            if (PlayerControl.LocalPlayer.Data == null) return false;

            if (PlayerControl.LocalPlayer.Data.IsDead) return false;

            var killerHearDead = GeneralConfigurations.KillersHearDeadOption.CurrentValue;
            if (killerHearDead == 0) return false;
            var localInfo = PlayerControl.LocalPlayer.GetModInfo();
            if (localInfo == null) return false;

            if (localInfo.Role.Role.RoleCategory == Roles.RoleCategory.ImpostorRole) return true;
            if (killerHearDead != 2) return false;

            if (localInfo.Role.Role == Roles.Neutral.Jackal.MyRole) return true;
            if (Roles.Neutral.Sidekick.MyRole.SidekickCanKillOption && localInfo.Role.Role == Roles.Neutral.Sidekick.MyRole) return true;

            return false;
        }
        }
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

        if (NebulaPlugin.MyPlugin.IsPreferential) myCoroutine = NebulaManager.Instance.StartCoroutine(CoCommunicate().WrapToIl2Cpp());

        InfoShower = UnityHelper.CreateObject<VoiceChatInfo>("VCInfoShower", HudManager.Instance.transform, new Vector3(0f, 4f, -25f));
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
        if (!GeneralConfigurations.UseVoiceChatOption)
        {
            NebulaGameManager.Instance.VoiceChatManager = null;
            Dispose();
            return;
        }

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
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (!allClients.ContainsKey(p.PlayerId))
                {
                    allClients[p.PlayerId] = new(p);
                    allClients[p.PlayerId].SetRoute(routeNormal);
                }
            }
        }

        if (!usingMic) return;

        IsMuting = Input.GetKey(KeyCode.V);
        InfoShower.SetMute(IsMuting);

        if (currentRadio != null && (PlayerControl.LocalPlayer.Data?.IsDead ?? false))
        {
            currentRadio = null;
            InfoShower.UnsetRadioContext();
        }
        else
        {
            if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1)))
            {
                currentRadio = null;
                InfoShower.UnsetRadioContext();
            }
            else
            {
                for (int i = 0; i < allRadios.Count; i++)
                {
                    if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + i + 1)))
                    {
                        currentRadio = allRadios[i];
                        InfoShower.SetRadioContext(currentRadio.DisplayRadioName, currentRadio.Color);
                        break;
                    }
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
        if (!AllowedUsingMic /*&& !AmongUsClient.Instance.AmHost*/)
        {
            var screen = MetaScreen.GenerateWindow(new(2.4f, 1f), HudManager.Instance.transform, Vector3.zero, true, true, false);

            MetaContext context = new();

            context.Append(new MetaContext.Text(TextAttribute.BoldAttr) { Alignment = IMetaContext.AlignmentOption.Center, TranslationKey = "voiceChat.dialog.confirm" });
            context.Append(new MetaContext.VerticalMargin(0.15f));
            context.Append(new CombinedContext(0.45f,
                new MetaContext.Button(() => { AllowedUsingMic = true; screen.CloseScreen(); }, new(TextAttribute.BoldAttr) { Size = new(0.42f, 0.2f) }) { Alignment = IMetaContext.AlignmentOption.Center, TranslationKey = "ui.dialog.yes" },
                new MetaContext.HorizonalMargin(0.1f),
                new MetaContext.Button(() => { AllowedUsingMic = false; screen.CloseScreen(); }, new(TextAttribute.BoldAttr) { Size = new(0.42f, 0.2f) }) { Alignment = IMetaContext.AlignmentOption.Center, TranslationKey = "ui.dialog.no" }));

            screen.SetContext(context);

            while (screen) yield return null;

            if (!AllowedUsingMic) yield break;
        }

        myListener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 11010);
        myListener.Start();

        StartSubprocess();
        //マイク使用中

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

        usingMic = true;

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

            int read = 0;
            byte[] res = new byte[resSize];
            while (read < resSize)
            {
                var readBodyTask = voiceStream.ReadAsync(res, read, resSize - read);
                if (!readBodyTask.IsCompleted) yield return null;
                read += readBodyTask.Result;
            }

            if (IsMuting) continue;
            RpcSendAudio.Invoke((PlayerControl.LocalPlayer.PlayerId, currentRadio != null, currentRadio?.RadioMask ?? 0, resSize, res));
        }
    }

    public void Dispose()
    {
        myClient?.Close();
        myClient?.Dispose();
        myListener?.Stop();
        myPlayer?.Stop();
        myPlayer?.Dispose();
        childProcess?.Kill();
        childProcess = null;

        if (myCoroutine != null) NebulaManager.Instance?.StopCoroutine(myCoroutine);
    }

    static private RemoteProcess<(byte clientId,bool isRadio,int radioMask,int dataLength,byte[] dataAry)> RpcSendAudio = new(
        "SendAudio",
        (writer,message) => {
            writer.Write(message.clientId);
            writer.Write(message.isRadio);
            writer.Write(message.radioMask);
            writer.Write(message.dataLength);
            writer.Write(message.dataAry,0,message.dataLength);
        },
        (reader) => {
            byte id = reader.ReadByte();
            bool isRadio = reader.ReadBoolean();
            int radioMask = reader.ReadInt32();
            int length = reader.ReadInt32();
            return (id, isRadio, radioMask, length, reader.ReadBytes(length));
            },
        (message,calledByMe) => {
            if (NebulaGameManager.Instance?.VoiceChatManager?.allClients.TryGetValue(message.clientId, out var client) ?? false)
                client?.OnReceivedData(message.isRadio, message.radioMask, message.dataAry);
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