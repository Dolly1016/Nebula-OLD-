using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Roles.Perk.ImpostorPerks;

[NebulaRPCHolder]
public class BruteSabotage : Perk
{
    public static RemoteProcess<Tuple<Vector2,float>> DoorSabotageEvent = new RemoteProcess<Tuple<Vector2, float>>(
        "DoorSabotage",   
        (writer, message) =>
           {
               writer.Write(message.Item1.x);
               writer.Write(message.Item1.y);
               writer.Write(message.Item2);
           },
           (reader) =>
           {
               return new Tuple<Vector2, float>(new Vector2(reader.ReadSingle(), reader.ReadSingle()), reader.ReadSingle());
           },
           (message, isCalledByMe) =>
           {
               PlainDoor? door = ShipStatus.Instance.AllDoors.FirstOrDefault((door) => door.transform.position.Distance(message.Item1) < 0.2f);
               if (door == null) return;

               IEnumerator CoDoorOpen()
               {
                   float t = message.Item2;
                   while (t > 0f)
                   {
                       if (door.Open) yield break;
                       t -= Time.deltaTime;
                       yield return null;
                   }

                   door.SetDoorway(true);
               }

               door.SetDoorway(false);
               door.StartCoroutine(CoDoorOpen().WrapToIl2Cpp());
           }
           );

    public override bool IsAvailable => true;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.DoorButton.png", 115f, "ui.button.perk.door");
    private PlainDoor? currentDoor = null;
    public override void MyControlUpdate(PerkHolder.PerkInstance perkData)
    {
        currentDoor = null;

        PlainDoor? door = null;
        float num = float.MaxValue;
        var myPos = PlayerControl.LocalPlayer.transform.position;
        foreach (var d in ShipStatus.Instance.AllDoors)
        {
            if (d.Room is SystemTypes.Decontamination or SystemTypes.Decontamination2 or SystemTypes.Decontamination3) continue;
            float dis = d.transform.position.Distance(myPos);
            if (dis > num) continue;

            num= dis;
            door = d;
        }

        if (door == null) return;
        if (num > 0.9f) return;

        currentDoor = door;

        var renderer = door.animator.GetComponent<SpriteRenderer>(); 
        renderer.material.SetColor("_AddColor", Color.yellow);
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        if (perkData.DataAry[0] > 0f) perkData.DataAry[0] -= Time.deltaTime;
        perkData.Display.SetCool(perkData.DataAry[0] / IP(0, PerkPropertyType.Second));
    }

    public override void ButtonInitialize(PerkHolder.PerkInstance perkData, Action<CustomButton> buttonRegister)
    {
        currentDoor = null;

        CustomButton button = null;
        button = new CustomButton(
            () =>
            {
                PlayerControl.LocalPlayer.NetTransform.Halt();
                PlayerControl.LocalPlayer.moveable = false;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && currentDoor != null; },
            () =>
            {
                button.Timer = button.MaxTimer;
                PlayerControl.LocalPlayer.moveable = true;
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            HudManager.Instance,
            null,
            true,
           IP(1, PerkPropertyType.Second),
           () =>
           {
               DoorSabotageEvent.Invoke(new Tuple<Vector2, float>(currentDoor?.transform.position ?? new Vector2(-100f, -100f), IP(0, PerkPropertyType.Second)));
               perkData.DataAry[0] -= IP(0, PerkPropertyType.Second);
               PlayerControl.LocalPlayer.moveable = true;
               button.Timer = button.MaxTimer;
           },
            "button.label.close"
        ).SetTimer(10f);
        button.Timer = button.MaxTimer = IP(2, PerkPropertyType.Second);

        buttonRegister.Invoke(button);
    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[1] { 0 };
    }

    public BruteSabotage(int id) : base(id, "bruteSabotage", false, 40, 0, new Color(0.2f, 0.5f, 0.6f))
    {
        ImportantProperties = new float[] { 10f,2f,20f };
    }
}
