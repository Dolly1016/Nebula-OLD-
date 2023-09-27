using HarmonyLib;
using Hazel;
using Il2CppSystem.CodeDom;
using Il2CppSystem.Reflection.Internal;
using Nebula.Utilities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Player.PlayerModInfo;

namespace Nebula.Modules;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaRPCHolder : Attribute
{

}

public class NebulaRPCInvoker
{

    Action<MessageWriter> sender;
    Action localBodyProcess;
    int hash;

    public NebulaRPCInvoker(int hash, Action<MessageWriter> sender, Action localBodyProcess)
    {
        this.hash = hash;
        this.sender = sender;
        this.localBodyProcess = localBodyProcess;
    }

    public void Invoke(MessageWriter writer)
    {
        writer.Write(hash);
        sender.Invoke(writer);
        localBodyProcess.Invoke();
    }

    public void InvokeSingle()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(hash);
        sender.Invoke(writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        localBodyProcess.Invoke();
    }

    public void InvokeLocal()
    {
        localBodyProcess.Invoke();
    }
}

public static class RPCRouter
{
    public class RPCSection : IDisposable
    {
        public string Name;
        public void Dispose()
        {
            if (currentSection != this) return;

            currentSection = null;
            Debug.Log($"End Evacuating Rpcs ({Name})");

            CombinedRemoteProcess.CombinedRPC.Invoke(evacuateds.ToArray());
            evacuateds.Clear();
        }

        public RPCSection(string? name = null)
        {
            Name = name ?? "Untitled";
            if (currentSection == null)
            {
                currentSection = this;
                Debug.Log($"Start Evacuating Rpcs ({Name})");
            }
        }
    }

    static public RPCSection CreateSection(string? label = null) => new RPCSection(label);

    static RPCSection? currentSection = null;
    static List<NebulaRPCInvoker> evacuateds = new();
    public static void SendRpc(string name, int hash, Action<MessageWriter> sender, Action localBodyProcess) { 
        if(currentSection == null)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
            writer.Write(hash);
            sender.Invoke(writer);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            try
            {
                localBodyProcess.Invoke();
            }
            catch(Exception ex)
            {
                Debug.LogError($"Error in RPC(Invoke: {name})" + ex.Message + ex.StackTrace);
            }

            //Debug.Log($"Called RPC : {name}");
        }
        else
        {
            evacuateds.Add(new(hash, sender, localBodyProcess));

            //Debug.Log($"Evacuated RPC : {name} (by {currentSection!.Name})");
        }
    }
}

[NebulaPreLoad(true)]
public class RemoteProcessBase
{
    static public Dictionary<int, RemoteProcessBase> AllNebulaProcess = new();


    public int Hash { get; private set; } = -1;
    public string Name { get; private set; }


    public RemoteProcessBase(string name)
    {
        Hash = name.ComputeConstantHash();
        Name = name;

        if (AllNebulaProcess.ContainsKey(Hash)) NebulaPlugin.Log.Print(null, name + " is duplicated.");

        AllNebulaProcess[Hash] = this;
    }

    static public void Load()
    {
        var types = Assembly.GetAssembly(typeof(RemoteProcessBase))?.GetTypes().Where((type) => type.IsDefined(typeof(NebulaRPCHolder)));
        if (types == null) return;

        foreach (var type in types)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    }

    public virtual void Receive(MessageReader reader) { }
}



public static class RemoteProcessAsset
{
    private static Dictionary<Type, (Action<MessageWriter, object>, Func<MessageReader, object>)> defaultProcessDic = new();

    static RemoteProcessAsset()
    {
        defaultProcessDic[typeof(byte)] = ((writer, obj) => writer.Write((byte)obj), (reader) => reader.ReadByte());
        defaultProcessDic[typeof(short)] = ((writer, obj) => writer.Write((short)obj), (reader) => reader.ReadInt16());
        defaultProcessDic[typeof(int)] = ((writer, obj) => writer.Write((int)obj), (reader) => reader.ReadInt32());
        defaultProcessDic[typeof(ulong)] = ((writer, obj) => writer.Write((ulong)obj), (reader) => reader.ReadUInt64());
        defaultProcessDic[typeof(float)] = ((writer, obj) => writer.Write((float)obj), (reader) => reader.ReadSingle());
        defaultProcessDic[typeof(bool)] = ((writer, obj) => writer.Write((bool)obj), (reader) => reader.ReadBoolean());
        defaultProcessDic[typeof(string)] = ((writer, obj) => writer.Write((string)obj), (reader) => reader.ReadString());
        defaultProcessDic[typeof(byte[])] = ((writer, obj) => writer.WriteBytesAndSize((byte[])obj), (reader) => reader.ReadBytesAndSize().ToArray());
        defaultProcessDic[typeof(int[])] = ((writer, obj) => { var ary = (int[])obj; writer.Write(ary.Length); for (int i = 0; i < ary.Length; i++) writer.Write(ary[i]); }, (reader) => { var ary = new int[reader.ReadInt32()]; for (int i = 0; i < ary.Length; i++) ary[i] = reader.ReadInt32(); return ary; });
        defaultProcessDic[typeof(float[])] = ((writer, obj) => { var ary = (float[])obj; writer.Write(ary.Length); for (int i = 0; i < ary.Length; i++) writer.Write(ary[i]); }, (reader) => { var ary = new float[reader.ReadInt32()]; for (int i = 0; i < ary.Length; i++) ary[i] = reader.ReadSingle(); return ary; });
        defaultProcessDic[typeof(string[])] = ((writer, obj) => { var ary = (string[])obj; writer.Write(ary.Length); for (int i = 0; i < ary.Length; i++) writer.Write(ary[i]); }, (reader) => { var ary = new string[reader.ReadInt32()]; for (int i = 0; i < ary.Length; i++) ary[i] = reader.ReadString(); return ary; });
        defaultProcessDic[typeof(Vector2)] = ((writer, obj) => { var vec = (Vector2)obj; writer.Write(vec.x); writer.Write(vec.y); }, (reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle()));
        defaultProcessDic[typeof(Vector3)] = ((writer, obj) => { var vec = (Vector3)obj; writer.Write(vec.x); writer.Write(vec.y); writer.Write(vec.z); }, (reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        defaultProcessDic[typeof(OutfitCandidate)] = (
            (writer, obj) => {
                var cand = (OutfitCandidate)obj;
                writer.Write(cand.outfit.PlayerName);
                writer.Write(cand.outfit.HatId);
                writer.Write(cand.outfit.SkinId);
                writer.Write(cand.outfit.VisorId);
                writer.Write(cand.outfit.PetId);
                writer.Write(cand.outfit.ColorId);
                writer.Write(cand.Tag);
                writer.Write(cand.Priority);
                writer.Write(cand.SelfAware);
            },
            (reader) => {
                GameData.PlayerOutfit outfit = new() { PlayerName = reader.ReadString(), HatId = reader.ReadString(), SkinId = reader.ReadString(), VisorId = reader.ReadString(), PetId = reader.ReadString(), ColorId = reader.ReadInt32() };
                return new OutfitCandidate(reader.ReadString(), reader.ReadInt32(), reader.ReadBoolean(), outfit);
            }
        );
        defaultProcessDic[typeof(SpeedModulator)] = (
            (writer, obj) =>
            {
                var mod = (SpeedModulator)obj;
                writer.Write(mod.Num);
                writer.Write(mod.IsMultiplier);
                writer.Write(mod.Timer);
                writer.Write(mod.CanPassMeeting);
                writer.Write(mod.Priority);
                writer.Write(mod.DuplicateTag);
            },
            (reader) => new SpeedModulator(reader.ReadSingle(), reader.ReadBoolean(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadInt32())
        ); ;
        defaultProcessDic[typeof(TranslatableTag)] = ((writer, obj) => writer.Write(((TranslatableTag)obj).Id), (reader) => TranslatableTag.ValueOf(reader.ReadInt32()));
    }

    static public (Action<MessageWriter, object>, Func<MessageReader, object>) GetProcess(Type type)
    {
        if(type.IsAssignableTo(typeof(Enum)))
            return defaultProcessDic[typeof(int)];
        return defaultProcessDic[type];
    }

    public static void GetMessageTreater<Parameter>(out Action<MessageWriter, Parameter> sender, out Func<MessageReader, Parameter> receiver)
    {
        Type paramType = typeof(Parameter);

        if (!typeof(Parameter).IsAssignableTo(typeof(ITuple))) throw new Exception("Can not generate sender and receiver for Non-tuple object.");

        int count = 0;


        List<(Action<MessageWriter, object>, Func<MessageReader, object>)> processList = new();
        while (true)
        {
            var field = paramType.GetField("Item" + (count + 1).ToString());
            if (field == null) break;

            processList.Add(RemoteProcessAsset.GetProcess(field.FieldType));
            count++;
        }

        var processAry = processList.ToArray();
        var constructor = paramType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == processAry.Length);

        if (constructor == null) throw new Exception("Can not Tuple Constructor");

        sender = (writer, param) => {
            var tuple = (param as ITuple)!;
            for (int i = 0; i < processAry.Length; i++) processAry[i].Item1.Invoke(writer, tuple[i]);
        };
        receiver = (reader) => {
            return (Parameter)constructor.Invoke(processAry.Select(p => p.Item2.Invoke(reader)).ToArray());
        };
    }

}
public class RemoteProcess<Parameter> : RemoteProcessBase
{
    public delegate void Process(Parameter parameter, bool isCalledByMe);

    private Action<MessageWriter, Parameter> Sender { get; set; }
    private Func<MessageReader, Parameter> Receiver { get; set; }
    private Process Body { get; set; }

    public RemoteProcess(string name, Action<MessageWriter, Parameter> sender, Func<MessageReader, Parameter> receiver, RemoteProcess<Parameter>.Process process)
    : base(name)
    {
        Sender = sender;
        Receiver = receiver;
        Body = process;
    }

    public RemoteProcess(string name, RemoteProcess<Parameter>.Process process) : base(name)  
    {
        Body = process;
        RemoteProcessAsset.GetMessageTreater<Parameter>(out var sender,out var receiver);
        Sender = sender;
        Receiver = receiver;
    }


    public void Invoke(Parameter parameter)
    {
        RPCRouter.SendRpc(Name,Hash,(writer)=>Sender(writer,parameter),()=>Body.Invoke(parameter,true));
    }

    public NebulaRPCInvoker GetInvoker(Parameter parameter)
    {
        return new NebulaRPCInvoker(Hash, (writer) => Sender(writer, parameter), () => Body.Invoke(parameter, true));
    }

    public void LocalInvoke(Parameter parameter)
    {
        Body.Invoke(parameter, true);
    }

    public override void Receive(MessageReader reader)
    {
        try
        {
            Body.Invoke(Receiver.Invoke(reader), false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in RPC(Received: {Name})" + ex.Message);
        }
    }
}

public static class RemotePrimitiveProcess
{
    public static RemoteProcess<int> OfInteger(string name, RemoteProcess<int>.Process process) => new(name, (writer, message) => writer.Write(message), (reader) => reader.ReadInt32(), process);
    public static RemoteProcess<float> OfFloat(string name, RemoteProcess<float>.Process process) => new(name, (writer, message) => writer.Write(message), (reader) => reader.ReadSingle(), process);
    public static RemoteProcess<string> OfString(string name, RemoteProcess<string>.Process process) => new(name, (writer, message) => writer.Write(message), (reader) => reader.ReadString(), process);
    public static RemoteProcess<byte> OfByte(string name, RemoteProcess<byte>.Process process) => new(name, (writer, message) => writer.Write(message), (reader) => reader.ReadByte(), process);
    public static RemoteProcess<Vector2> OfVector2(string name, RemoteProcess<Vector2>.Process process) => new(name, (writer, message) => { writer.Write(message.x); writer.Write(message.y); }, (reader) => new(reader.ReadSingle(), reader.ReadSingle()), process);
    public static RemoteProcess<Vector3> OfVector3(string name, RemoteProcess<Vector3>.Process process) => new(name, (writer, message) => { writer.Write(message.x); writer.Write(message.y); writer.Write(message.z); }, (reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()), process);
}

[NebulaRPCHolder]
public class CombinedRemoteProcess : RemoteProcessBase
{
    public static CombinedRemoteProcess CombinedRPC = new();
    CombinedRemoteProcess() : base("CombinedRPC") { }

    public override void Receive(MessageReader reader)
    {
        int num = reader.ReadInt32();
        for (int i = 0; i < num; i++) RemoteProcessBase.AllNebulaProcess[reader.ReadInt32()].Receive(reader);
    }

    public void Invoke(params NebulaRPCInvoker[] invokers)
    {
        RPCRouter.SendRpc(Name, Hash, (writer) =>
        {
            writer.Write(invokers.Length);
            foreach (var invoker in invokers) invoker.Invoke(writer);
        },
        () => { });
    }
}

public class RemoteProcess : RemoteProcessBase
{
    public delegate void Process(bool isCalledByMe);
    private Process Body { get; set; }
    public RemoteProcess(string name, Process process)
    : base(name)
    {
        Body = process;
    }

    public void Invoke()
    {
        RPCRouter.SendRpc(Name, Hash, (writer) => { }, () => Body.Invoke(true));
    }

    public NebulaRPCInvoker GetInvoker()
    {
        return new NebulaRPCInvoker(Hash, (writer) => { }, () => Body.Invoke(true));
    }

    public override void Receive(MessageReader reader)
    {
        try
        {
            Body.Invoke(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in RPC(Received: {Name})" + ex.Message);
        }
    }
}

public class DivisibleRemoteProcess<Parameter, DividedParameter> : RemoteProcessBase
{
    public delegate void Process(DividedParameter parameter, bool isCalledByMe);

    private Func<Parameter, IEnumerator<DividedParameter>> Divider;
    private Action<MessageWriter, DividedParameter> DividedSender { get; set; }
    private Func<MessageReader, DividedParameter> Receiver { get; set; }
    private Process Body { get; set; }

    public DivisibleRemoteProcess(string name, Func<Parameter,IEnumerator<DividedParameter>> divider, Action<MessageWriter, DividedParameter> dividedSender, Func<MessageReader, DividedParameter> receiver, DivisibleRemoteProcess<Parameter, DividedParameter>.Process process)
    : base(name)
    {
        Divider = divider;
        DividedSender = dividedSender;
        Receiver = receiver;
        Body = process;
    }

    public DivisibleRemoteProcess(string name, Func<Parameter, IEnumerator<DividedParameter>> divider, DivisibleRemoteProcess<Parameter, DividedParameter>.Process process)
    : base(name)
    {
        Divider = divider;
        RemoteProcessAsset.GetMessageTreater<DividedParameter>(out var sender,out var receiver);
        DividedSender = sender;
        Receiver = receiver;
        Body = process;
    }

    public void Invoke(Parameter parameter)
    {
        void dividedSend(DividedParameter param)
        {
            RPCRouter.SendRpc(Name, Hash, (writer) => DividedSender(writer, param), () => Body.Invoke(param, true));
        }
        var enumerator = Divider.Invoke(parameter);
        while (enumerator.MoveNext()) dividedSend(enumerator.Current);
    }

    public void LocalInvoke(Parameter parameter)
    {
        var enumerator = Divider.Invoke(parameter);
        while (enumerator.MoveNext()) Body.Invoke(enumerator.Current, true);
    }

    public override void Receive(MessageReader reader)
    {
        try
        {
            Body.Invoke(Receiver.Invoke(reader), false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in RPC(Received: {Name})" + ex.Message);
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
class NebulaRPCHandlerPatch
{
    static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (callId != 64) return;

        RemoteProcessBase.AllNebulaProcess[reader.ReadInt32()].Receive(reader);
    }
}

