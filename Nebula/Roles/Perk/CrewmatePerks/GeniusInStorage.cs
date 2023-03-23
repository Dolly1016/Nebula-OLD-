using Nebula.Game;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class GeniusInStorage : Perk
{
    public override bool IsAvailable => true;

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return;

        var rooms = ShipStatus.Instance.FastRooms;
        PlainShipRoom? shipRoom = null;

        if (shipRoom == null) rooms.TryGetValue(SystemTypes.Storage, out shipRoom);
        if (shipRoom == null) rooms.TryGetValue(SystemTypes.CargoBay, out shipRoom);

        if ((!(perkData.DataAry[0] > 0f)) && shipRoom.roomArea.OverlapPoint(PlayerControl.LocalPlayer.GetTruePosition()))
        {
            HnSModificator.ProceedTimer.Invoke(new HnSModificator.HnSTaskBonusMessage() { 
                TimeDeduction= IP(0, PerkPropertyType.Second),
                IsFinishTaskBonus=false,
                CanProceedFinalTimer=true,
                ContributorId = PlayerControl.LocalPlayer.PlayerId
            });
            perkData.DataAry[0] = IP(1, PerkPropertyType.Second);
        }

        if (perkData.DataAry[0] > 0f) perkData.DataAry[0] -= Time.deltaTime;

        perkData.Display?.SetCool(perkData.DataAry[0] / IP(1, PerkPropertyType.Second));
    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[] { IP(1, PerkPropertyType.Second) };
    }

    public GeniusInStorage(int id) : base(id, "geniusInStorage", true, 25, 2, new Color(0.2f, 0.5f, 0.7f))
    {
        ImportantProperties = new float[] { 1f, 3f };
    }
}
