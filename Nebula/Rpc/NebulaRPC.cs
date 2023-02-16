using Hazel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nebula.Module;
using Nebula.Roles.NeutralRoles;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Nebula;


public class RemoteProcessBase
{
    static public RemoteProcessBase[] AllNebulaProcess = new RemoteProcessBase[] { 

    };

    public int Id { get; private set; } = -1;

    static public void Load()
    {
        int i = 0;
        foreach (var p in AllNebulaProcess) p.Id = i++;
    }

    public virtual void Receive(MessageReader reader) { }
}


public class RemoteProcess<Parameter> : RemoteProcessBase where Parameter : struct 
{
    public delegate void Process(in Parameter parameter,bool isCalledByMe);
    
    private Action<MessageWriter, Parameter> Sender { get; set; }
    private Func<MessageReader, Parameter> Receiver { get; set; }
    private Process Body { get; set; }

    public RemoteProcess(Action<MessageWriter, Parameter> sender, Func<MessageReader, Parameter> receiver, RemoteProcess<Parameter>.Process process)
    {
        Sender = sender;
        Receiver = receiver;
        Body = process;
    }

    public void Invoke(in Parameter parameter) {
        if (Id == -1)
        {
            NebulaPlugin.Instance.Logger.Print("[Error] Inactivated process is called.\n Please tell the developper the situation when this error has been occurred.");
            return;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Id);
        Sender(writer,parameter);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(parameter,true);
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
    public RemoteProcess(Process process)
    {
        Body = process;
    }

    public void Invoke()
    {
        if (Id == -1)
        {
            NebulaPlugin.Instance.Logger.Print("[Error] Inactivated process is called.\n Please tell the developper the situation when this error has been occurred.");
            return;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(true);
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(false);
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
