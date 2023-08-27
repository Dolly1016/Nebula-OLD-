using Nebula.Configuration;
using UnityEngine.AI;

namespace Nebula.Roles.Crewmate;

public class Necromancer : ConfigurableStandardRole
{
    static public Necromancer MyRole = new Necromancer();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "necromancer";
    public override Color RoleColor => new Color(108f / 255f, 50f / 255f, 160f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration ReviveCoolDownOption;
    private NebulaConfiguration ReviveDurationOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        ReviveCoolDownOption = new NebulaConfiguration(RoleConfig, "reviveCoolDown", null, 5f, 60f, 5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
        ReviveDurationOption = new NebulaConfiguration(RoleConfig, "reviveDuration", null, 0.5f, 10f, 0.5f, 3f, 3f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        private Scripts.Draggable? draggable = null;
        private ModAbilityButton? reviveButton = null;
        private Arrow? myArrow;
        private TMPro.TextMeshPro message;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.ReviveButton.png", 115f);

        private Dictionary<byte, SystemTypes> resurrectionRoom = new();

        public Instance(PlayerControl player) : base(player)
        {
            if (AmOwner)
            {
                draggable = Bind(new Scripts.Draggable());

                message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab,HudManager.Instance.transform);
                new TextAttribute(TextAttribute.NormalAttr) { Size = new Vector2(5f, 0.9f) }.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(message);
                message.transform.localPosition = new Vector3(0, -1.2f, -4f);
                Bind(new GameObjectBinding(message.gameObject));

                SystemTypes? currentTargetRoom = null;

                bool canReviveHere()
                {
                    return !(!currentTargetRoom.HasValue || !player.GetModInfo().HoldingDeadBody.HasValue || !ShipStatus.Instance.FastRooms[currentTargetRoom.Value].roomArea.OverlapPoint(player.GetTruePosition()));
                }

                myArrow = Bind(new Arrow());
                myArrow.IsActive = false;
                myArrow.SetColor(MyRole.RoleColor);

                draggable.OnHoldingDeadBody = (deadBody) =>
                {
                    if (!resurrectionRoom.ContainsKey(deadBody.ParentId))
                    {
                        //復活部屋を計算
                        List<Tuple<float, PlainShipRoom>> cand = new();
                        foreach(var entry in ShipStatus.Instance.FastRooms)
                        {
                            if (entry.Key == SystemTypes.Ventilation) continue;

                            float d = entry.Value.roomArea.Distance(player.Collider).distance;
                            if (d < 3f) continue;

                            cand.Add(new(d,entry.Value));
                        }

                        //近い順にソートし、遠すぎる部屋は候補から外す 少なくとも1部屋は候補に入るようにする
                        cand.Sort((c1, c2) => Math.Sign(c1.Item1 - c2.Item1));
                        int lastIndex = cand.FindIndex((tuple) => tuple.Item1 > 15f);
                        if (lastIndex == -1) lastIndex = cand.Count;
                        if (lastIndex == 0) lastIndex = 1;

                        resurrectionRoom[deadBody.ParentId] = cand[System.Random.Shared.Next(lastIndex)].Item2.RoomId;
                    }

                    currentTargetRoom = resurrectionRoom[deadBody.ParentId];
                    myArrow.TargetPos = ShipStatus.Instance.FastRooms[currentTargetRoom.Value].roomArea.transform.position;
                    message.text = Language.Translate("role.necromancer.phantomMessage").Replace("%ROOM%",AmongUsUtil.ToDisplayString(currentTargetRoom.Value));
                };

                reviveButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.G);
                reviveButton.SetSprite(buttonSprite.GetSprite());
                reviveButton.Availability = (button) => player.CanMove && player.GetModInfo().HoldingDeadBody.HasValue && canReviveHere();
                reviveButton.Visibility = (button) => !player.Data.IsDead;
                reviveButton.OnClick = (button) => {
                    button.ActivateEffect();
                };
                reviveButton.OnEffectEnd = (button) =>
                {
                    if (!button.EffectTimer.IsInProcess)
                    {
                        Helpers.GetPlayer(player.GetModInfo().HoldingDeadBody.Value)?.ModRevive(player.transform.position, true);
                        reviveButton.CoolDownTimer.Start();
                    }
                };
                reviveButton.OnMeeting = (button) =>
                {
                    reviveButton.InactivateEffect();
                };
                reviveButton.OnUpdate = (button) => {
                    if (!button.EffectActive) return;
                    if (!canReviveHere()) button.InactivateEffect();
                };
                reviveButton.CoolDownTimer = Bind(new Timer(0f, MyRole.ReviveCoolDownOption.GetFloat()!.Value));
                reviveButton.EffectTimer = Bind(new Timer(0f, MyRole.ReviveDurationOption.GetFloat()!.Value));
                reviveButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                reviveButton.SetLabel("revive");
            }
        }

        public override void LocalUpdate()
        {
            bool flag = MyPlayer.HoldingDeadBody.HasValue;
            myArrow.IsActive = flag;
            message.gameObject.SetActive(flag);
            if (flag) message.color = MyRole.RoleColor.AlphaMultiplied(MathF.Sin(Time.time * 2.4f) * 0.2f + 0.8f);
        }

        public override void OnActivated()
        {
            draggable?.OnActivated(this);

        }

        public override void OnDead()
        {
            draggable?.OnDead(this);
        }

        protected override void OnInactivated()
        {
            draggable?.OnInactivated(this);
        }

        public override void OnPlayerDeadLocal(PlayerControl dead)
        {
            resurrectionRoom?.Remove(dead.PlayerId);
        }

        public override void OnGameStart()
        {
            reviveButton?.StartCoolDown();
        }

        public override void OnGameReenabled()
        {
            reviveButton?.StartCoolDown();
        }
    }
}

