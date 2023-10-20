using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Neutral;


public class PaparazzoShot : MonoBehaviour
{
    SpriteRenderer flashRenderer = null!;
    SpriteRenderer frameRenderer = null!;
    SpriteRenderer backRenderer = null!;
    SpriteRenderer centerRenderer = null!;
    public bool IsVert = false;
    public void ToggleDirection() => IsVert = !IsVert;
    static PaparazzoShot() => ClassInjector.RegisterTypeInIl2Cpp<PaparazzoShot>();

    public void Awake()
    {
        frameRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        flashRenderer = transform.GetChild(1).GetComponent<SpriteRenderer>();
        backRenderer = transform.GetChild(2).GetComponent<SpriteRenderer>();
        centerRenderer = transform.GetChild(3).GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        var mouseInfo = PlayerModInfo.LocalMouseInfo;
        var dis = Mathf.Min(mouseInfo.distance, 2f);
        var targetPos = PlayerControl.LocalPlayer.transform.localPosition + new Vector3(Mathf.Cos(mouseInfo.angle), Mathf.Sin(mouseInfo.angle)) * dis;
        targetPos.z = -10f;
        transform.localPosition -= (transform.localPosition - targetPos) * Time.deltaTime * 4.2f;
        var scale = transform.localScale.x;
        scale -= (scale - 1f) * Time.deltaTime * 2.4f;
        transform.localScale = Vector3.one * scale;

        transform.eulerAngles = new Vector3(0, 0, mouseInfo.angle * 180f / Mathf.PI + (IsVert ? 90f : 0f));
    }
}


/*
public class Paparazzo : ConfigurableStandardRole
{
    static public Paparazzo MyRole = new Paparazzo();
    static public Team MyTeam = new("teams.paparazzo", MyRole.RoleColor, TeamRevealType.OnlyMe);

    public override RoleCategory RoleCategory => RoleCategory.NeutralRole;

    public override string LocalizedName => "paparazzo";
    public override Color RoleColor => new Color(202f / 255f, 118f / 255f, 140f / 255f);
    public override Team Team => MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration ShotCoolDownOption = null!;
    private new VentConfiguration VentConfiguration = null!;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        VentConfiguration = new(RoleConfig, null, (5f, 60f, 15f), (2.5f, 30f, 10f));
        ShotCoolDownOption = new NebulaConfiguration(RoleConfig, "shotCoolDown", null, 2.5f, 60f, 2.5f, 20f, 20f);
    }

    public class Instance : RoleInstance
    {
        public override AbstractRole Role => MyRole;

        private Timer ventCoolDown = new Timer(MyRole.VentConfiguration.CoolDown).SetAsAbilityCoolDown().Start();
        private Timer ventDuration = new(MyRole.VentConfiguration.Duration);
        public override Timer? VentCoolDown => ventCoolDown;
        public override Timer? VentDuration => ventDuration;

        public Instance(PlayerModInfo player) : base(player)
        {
        }

        private ModAbilityButton? shotButton = null;
        static private ISpriteLoader cameraButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.CameraButton.png", 115f);

        private ComponentBinding<PaparazzoShot>? MyFinder = null;
        public override bool CheckWins(CustomEndCondition endCondition, ref ulong _) => false;


        public override void OnActivated()
        {
            if (AmOwner)
            {
                shotButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability).SubKeyBind(KeyAssignmentType.AidAction);
                shotButton.SetSprite(cameraButtonSprite.GetSprite());
                shotButton.Availability = (button) => MyPlayer.MyControl.CanMove && MyFinder != null;
                shotButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                shotButton.OnClick = (button) => {
                    //MyFinder!.MyObject
                    MyFinder?.Detach();
                };
                shotButton.OnSubAction= (button) => MyFinder?.MyObject!.ToggleDirection();
                shotButton.CoolDownTimer = Bind(new Timer(0f, MyRole.ShotCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                shotButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                shotButton.SetLabel("shot");
            }

        }

        public override void LocalUpdate()
        {
            if(!(shotButton?.CoolDownTimer?.IsInProcess ?? true) && MyFinder == null)
            {
                MyFinder = Bind(new ComponentBinding<PaparazzoShot>(GameObject.Instantiate(NebulaAsset.PaparazzoShot, null)));
                var shot = MyFinder.MyObject!;
                shot.gameObject.layer = LayerExpansion.GetUILayer();
                shot.transform.localScale = Vector3.zero;
                var pos = MyPlayer.MyControl.transform.localPosition;
                pos.z = -10f;
                shot.transform.localPosition = pos;
            }
        }

        public override void OnMeetingStart()
        {
            MyFinder?.Release();
            MyFinder = null;
        }
    }
}
*/