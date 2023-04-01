using Hazel;
using Il2CppSystem.Security.Cryptography;
using Nebula.Module;
using Nebula.Roles.NeutralRoles;
using Nebula.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Nebula;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaRPCHolder : Attribute
{

}

public class RemoteProcessBase
{

    static public Dictionary<int,RemoteProcessBase> AllNebulaProcess = new();

    public int Hash { get; private set; } = -1;
    public string Name { get; private set; }
    
    private const int MulPrime = 127;
    private const int SurPrime = 104729;

    public RemoteProcessBase(string name)
    {
        int val = 0;
        int mul = 1;
        foreach(char c in name)
        {
            mul *= MulPrime;
            mul %= SurPrime;
            val += (int)c * mul;
            val %= SurPrime;
        }
        Hash= val;
        Name = name;

        if (AllNebulaProcess.ContainsKey(Hash)) NebulaPlugin.Instance.Logger.Print("NebulaRPC", $"Identifier conflict has been occured at \"{Name}\"");
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


public class RemoteProcess<Parameter> : RemoteProcessBase 
{
    public delegate void Process(Parameter parameter,bool isCalledByMe);
    
    private Action<MessageWriter, Parameter> Sender { get; set; }
    private Func<MessageReader, Parameter> Receiver { get; set; }
    private Process Body { get; set; }

    public RemoteProcess(string name,Action<MessageWriter, Parameter> sender, Func<MessageReader, Parameter> receiver, RemoteProcess<Parameter>.Process process)
    :base(name){
        Sender = sender;
        Receiver = receiver;
        Body = process;
    }

    public void Invoke(Parameter parameter) {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Hash);
        Sender(writer,parameter);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(parameter,true);
    }

    public void LocalInvoke(Parameter parameter) {
        Body.Invoke(parameter, true);
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(Receiver.Invoke(reader), false);
    }
}

public class RemoteProcess : RemoteProcessBase
{
    public delegate void Process(bool isCalledByMe);
    private Process Body { get; set; }
    public RemoteProcess(string name,Process process)
    :base(name){
        Body = process;
    }

    public void Invoke()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Hash);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(true);
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(false);
    }
}

public class DivisibleRemoteProcess<Parameter,DividedParameter> : RemoteProcessBase
{
    public delegate void Process(DividedParameter parameter, bool isCalledByMe);

    private Action<Parameter, Action<DividedParameter>> Sender;
    private Action<MessageWriter, DividedParameter> DividedSender { get; set; }
    private Func<MessageReader, DividedParameter> Receiver { get; set; }
    private Process Body { get; set; }

    public DivisibleRemoteProcess(string name, Action<Parameter, Action<DividedParameter>> sender, Action<MessageWriter, DividedParameter> dividedSender, Func<MessageReader, DividedParameter> receiver, DivisibleRemoteProcess<Parameter,DividedParameter>.Process process)
    : base(name)
    {
        Sender = sender;
        DividedSender = dividedSender;
        Receiver = receiver;
        Body = process;
    }

    public void Invoke(Parameter parameter)
    {
        void dividedSend(DividedParameter param)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
            writer.Write(Hash);
            DividedSender(writer, param);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            Body.Invoke(param, true);
        }

        Sender(parameter, dividedSend);
    }

    public void LocalInvoke(Parameter parameter)
    {
        Sender(parameter, (param) => Body.Invoke(param, true));
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(Receiver.Invoke(reader), false);
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

