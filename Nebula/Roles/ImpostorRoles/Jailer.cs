using Nebula.Patches;
using Nebula.Roles.RoleSystem;

namespace Nebula.Roles.Impostor;

public class Jailer : Role
{
    public class InheritedJailer : ExtraRole
    {
        public override void Assignment(Patches.AssignMap assignMap){}

        public override void ButtonInitialize(HudManager __instance) => ImpAdminSystem.ButtonInitialize(__instance, Roles.Jailer.canMoveWithLookingMapOption.getBool(), Roles.Jailer.ignoreCommSabotageOption.getBool(), Roles.Jailer.canIdentifyImpostorsOption.getBool());

        public override void CleanUp() => ImpAdminSystem.CleanUp();

        public override void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay, Action<Vector2, bool> iconGenerator) => ImpAdminSystem.OnShowMapTaskOverlay(mapTaskOverlay, iconGenerator, Roles.Jailer.canMoveWithLookingMapOption.getBool(), Roles.Jailer.ignoreCommSabotageOption.getBool(), Roles.Jailer.canIdentifyImpostorsOption.getBool());

        public override void OnMapClose(MapBehaviour mapBehaviour) => ImpAdminSystem.OnMapClose(mapBehaviour);


        public InheritedJailer() : base("InheritedJailer", "inheritedJailer", Palette.ImpostorRed, 0)
        {
            IsHideRole = true;
        }
    }

    /* オプション */
    private Module.CustomOption? ignoreCommSabotageOption;
    private Module.CustomOption? canMoveWithLookingMapOption;
    private Module.CustomOption? canIdentifyImpostorsOption;
    private Module.CustomOption? inheritAbilityOption;

    public override void LoadOptionData()
    {
        canMoveWithLookingMapOption = CreateOption(Color.white, "canMoveWithLookingMap", true);
        canIdentifyImpostorsOption = CreateOption(Color.white, "canIdentifyImpostors", true);
        ignoreCommSabotageOption = CreateOption(Color.white, "ignoreCommSabotage", true);
        inheritAbilityOption = CreateOption(Color.white, "canInheritAbility", false);
    }


    /* ボタン */

    public override void ButtonInitialize(HudManager __instance) => ImpAdminSystem.ButtonInitialize(__instance,canMoveWithLookingMapOption.getBool(),ignoreCommSabotageOption.getBool(),canIdentifyImpostorsOption.getBool());

    public override void CleanUp() => ImpAdminSystem.CleanUp();

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Disturber);
        RelatedRoles.Add(Roles.Doctor);
        RelatedRoles.Add(Roles.NiceTrapper);
        RelatedRoles.Add(Roles.EvilTrapper);
        RelatedRoles.Add(Roles.Arsonist);
        RelatedRoles.Add(Roles.Opportunist);
    }

    public override void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay, Action<Vector2, bool> iconGenerator) => ImpAdminSystem.OnShowMapTaskOverlay(mapTaskOverlay,iconGenerator,canMoveWithLookingMapOption.getBool(),ignoreCommSabotageOption.getBool(),canIdentifyImpostorsOption.getBool());
  
    public override void OnMapClose(MapBehaviour mapBehaviour)=>ImpAdminSystem.OnMapClose(mapBehaviour);

    public override void OnDied()
    {
        if (!inheritAbilityOption.getBool()) return;

        var cand = (Game.GameData.data.AllPlayers.Values.Where((d) => d.IsAlive && d.role.side == Side.Impostor && d.role != Roles.Jailer)).ToArray();
        if (cand.Length == 0) return;

        RPCEventInvoker.AddExtraRole(Helpers.playerById(cand[NebulaPlugin.rnd.Next(cand.Length)].id), Roles.InheritedJailer, 0);
    }

    public Jailer()
        : base("Jailer", "jailer", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
             Impostor.impostorSideSet, Impostor.impostorSideSet,
             Impostor.impostorEndSet,
             true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
    }
}
