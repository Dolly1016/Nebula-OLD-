﻿using Nebula.Roles.Assignment;

namespace Nebula.Roles.Modifier;


public class Lover : ConfigurableModifier
{
    static public Lover MyRole = new Lover();
    public override string LocalizedName => "lover";
    public override string CodeName => "LVR";
    public override Color RoleColor => new Color(255f / 255f, 0f / 255f, 184f / 255f);
    private NebulaConfiguration NumOfPairsOption = null!;
    private NebulaConfiguration ChanceOfAssigningImpostorsOption = null!;
    private NebulaConfiguration AllowExtraWinOption = null!;
    public override ModifierInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player, arguments[0]);

    public override void Assign(IRoleAllocator.RoleTable roleTable) {
        var impostors = roleTable.GetPlayers(RoleCategory.ImpostorRole).OrderBy(_=>Guid.NewGuid()).ToArray();
        var others = roleTable.GetPlayers(RoleCategory.CrewmateRole | RoleCategory.NeutralRole).OrderBy(_ => Guid.NewGuid()).ToArray();
        int impostorsIndex = 0;
        int othersIndex = 0;

        int maxPairs = NumOfPairsOption;
        float chanceImpostor = ChanceOfAssigningImpostorsOption.GetFloat() / 100f;
        (byte playerId, AbstractRole role)? first,second;

        for (int i = 0; i < maxPairs; i++)
        {
            try
            {
                first = others[othersIndex++];
                second = (impostorsIndex < impostors.Length && (float)System.Random.Shared.NextDouble() < chanceImpostor) ? impostors[impostorsIndex++] : second = others[othersIndex++];

                roleTable.SetModifier(first.Value.playerId, this, new int[] { i });
                roleTable.SetModifier(second.Value.playerId, this, new int[] { i });
            }
            catch
            {
                //範囲外アクセス(これ以上割り当てできない)
                break;
            }
        }
    }

    protected override void LoadOptions()
    {
        NumOfPairsOption = new(RoleConfig, "numOfPairs", null, 0, 7, 0, 0);
        ChanceOfAssigningImpostorsOption = new(RoleConfig, "chanceOfAssigningImpostors", null, 0f, 100f, 10f, 0f, 0f) { Decorator = NebulaConfiguration.PercentageDecorator };
        AllowExtraWinOption = new(RoleConfig, "allowExtraWin", null, true, true);
    }

    public class Instance : ModifierInstance
    {
        public override AbstractModifier Role => MyRole;

        static private Color[] colors = new Color[] { MyRole.RoleColor,
        (Color)new Color32(254, 132, 3, 255) ,
        (Color)new Color32(3, 254, 188, 255) ,
        (Color)new Color32(255, 255, 0, 255) ,
        (Color)new Color32(3, 183, 254, 255) ,
        (Color)new Color32(8, 255, 10, 255) ,
        (Color)new Color32(132, 3, 254, 255) };
        private int loversId; 
        public Instance(PlayerModInfo player,int loversId) : base(player)
        {
            this.loversId = loversId;
        }
        public override bool CheckWins(CustomEndCondition endCondition, ref ulong _) => endCondition == NebulaGameEnd.LoversWin && !MyPlayer.IsDead;

        public override void DecoratePlayerName(ref string text, ref Color color)
        {
            if (AmOwner || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false) || (MyLover?.AmOwner ?? false)) text += " ♥".Color(colors[loversId]);
        }

        public override void OnMurdered(PlayerControl murder)
        {
            if(AmOwner && murder.PlayerId != MyPlayer.PlayerId)
            {
                var myLover = MyLover;
                if (myLover == null) return;
                if (myLover.IsDead) return;

                myLover.MyControl.ModKill(myLover.MyControl,false,PlayerState.Suicide,EventDetail.Kill);
            }
        }

        public override void OnExiled()
        {
            if (AmOwner)
            {
                MyLover?.MyControl.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide);
            }
        }

        public override void OnActivated()
        {
            NebulaGameManager.Instance?.CriteriaManager.AddCriteria(NebulaEndCriteria.LoversCriteria);
        }

        public override bool CheckExtraWins(CustomEndCondition endCondition, int winnersMask, ref ulong extraWinMask)
        {
            if (!MyRole.AllowExtraWinOption) return false;

            var myLover = MyLover;
            if (myLover == null) return false;
            if (myLover.IsDead) return false;
            if ((myLover.PlayerId & winnersMask) == 0) return false;

            extraWinMask |= NebulaGameEnd.ExtraLoversWin.Id;
            return true;
        }

        public PlayerModInfo? MyLover => NebulaGameManager.Instance?.AllPlayerInfo().FirstOrDefault(p => p.PlayerId != MyPlayer.PlayerId && p.AllModifiers.Any(m => m is Lover.Instance lover && lover.loversId == loversId));
        public override string? IntroText => Language.Translate("role.lover.blurb").Replace("%NAME%", (MyLover?.DefaultName ?? "ERROR").Color(MyRole.RoleColor));
    }
}
