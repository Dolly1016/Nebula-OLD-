using BepInEx.Unity.IL2CPP.Utils;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class Mayday : Perk
{
    public override bool IsAvailable => true;

    public override bool CanPing(PerkHolder.PerkInstance perkData, byte playerId)
    {
        bool result = perkData.IntegerAry[0] == 1;
        perkData.IntegerAry[0] = result ? 0 : 1;
        return !result;
    }

    IEnumerator CoNoticeSeeker()
    {
        int left = (int)IP(0);
        float t = 0f;
        while (true)
        {
            if (!(t > 0f))
            {
                if (PlayerControl.LocalPlayer.Data.IsDead) break;
                Game.HnSModificator.NoticeSeekerEvent.Invoke();
                t = IP(1, PerkPropertyType.Second);
                left--;
                if (left <= 0) break;
            }
            t -= Time.deltaTime;
            yield return null;
        }
    }

    public override void OnTaskComplete(PerkHolder.PerkInstance perkData, PlayerTask? task)
    {
        if (task.TaskType == TaskTypes.UploadData) PlayerControl.LocalPlayer.StartCoroutine(CoNoticeSeeker().WrapToIl2Cpp());
    }

    public Mayday(int id) : base(id, "mayday", true, 10, 2, new Color(0.6f, 0.5f, 0.32f))
    {
        ImportantProperties = new float[] { 5f, 5f };
    }
}
