using System.Collections;
using System.Linq;


namespace Nebula.Modules;

public enum SynchronizeTag
{
    PreSpawnMinigame,
    PostExile
}

[NebulaRPCHolder]
public class Synchronizer
{
    public Dictionary<SynchronizeTag, uint> sync = new();

    public void ResetSync(SynchronizeTag tag)
    {
        sync[tag] = 0;
    }

    private void Sync(SynchronizeTag tag,byte playerId)
    {
        if(!sync.ContainsKey(tag)) sync[tag] = 0;
        sync[tag] |= (uint)1 << (int)playerId;
    }

    static public RemoteProcess<(SynchronizeTag, byte)> RpcSync = new(
        "Syncronize",
        (message, calledByMe) => NebulaGameManager.Instance.Syncronizer.Sync(message.Item1,message.Item2)
        );

    public void SendSync(SynchronizeTag tag)
    {
        RpcSync.Invoke((tag,PlayerControl.LocalPlayer.PlayerId));
    }
    
    public IEnumerator CoSync(SynchronizeTag tag, bool withSurviver = true,bool withGhost = false,bool withBot = false)
    {
        if (!sync.ContainsKey(tag)) sync[tag] = 0;

        while (true)
        {
            if (TestSync(tag, withSurviver, withGhost, withBot)) yield break;

            yield return null;
        }        
    }

    public bool TestSync(SynchronizeTag tag, bool withSurviver = true, bool withGhost = false, bool withBot = false)
    {
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (!withSurviver && !p.Data.IsDead) continue;
            if (!withGhost && p.Data.IsDead) continue;
            if (!withBot && p.isDummy) continue;

            if ((sync[tag] & (1 << p.PlayerId)) == 0) return false;
        }
        return true;
    }
}
