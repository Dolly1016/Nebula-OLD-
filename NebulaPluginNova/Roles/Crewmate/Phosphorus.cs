using BepInEx.Unity.IL2CPP.Utils;
using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

[NebulaRPCHolder]
public class Phosphorus : ConfigurableStandardRole
{
    static public Phosphorus MyRole = new Phosphorus();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "phosphorus";
    public override Color RoleColor => new Color(249f / 255f, 188f / 255f, 81f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration NumOfLampsOption;
    private NebulaConfiguration PlaceCoolDownOption;
    private NebulaConfiguration LampCoolDownOption;
    private NebulaConfiguration LampDurationOption;
    
    protected override void LoadOptions()
    {
        base.LoadOptions();

        NumOfLampsOption = new NebulaConfiguration(RoleConfig, "numOfLamps", null, 1, 10, 2, 2);
        PlaceCoolDownOption = new NebulaConfiguration(RoleConfig, "placeCoolDown", null, 5f, 60f, 5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
        LampCoolDownOption = new NebulaConfiguration(RoleConfig, "lampCoolDown", null, 5f, 60f, 5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
        LampDurationOption = new NebulaConfiguration(RoleConfig, "lampDuration", null, 7.5f, 30f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    private static IDividedSpriteLoader lanternSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.Lantern.png", 100f, 4);

    [NebulaPreLoad]
    public class Lantern : NebulaSyncStandardObject
    {
        public static string MyGlobalTag = "LanternGlobal";
        public static string MyLocalTag = "LanternLocal";
        public Lantern(Vector2 pos,bool isLocal) : base(pos,ZOption.Just,true,lanternSprite.GetSprite(0),isLocal){}

        public static void Load()
        {
            NebulaSyncObject.RegisterInstantiater(MyGlobalTag, (args) => new Lantern(new(args[0], args[1]), false));
            NebulaSyncObject.RegisterInstantiater(MyLocalTag, (args) => new Lantern(new(args[0], args[1]), true));
        }
    }

    static private SpriteLoader lightMaskSprite = SpriteLoader.FromResource("Nebula.Resources.LightMask.png", 100f);

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? placeButton = null;
        private ModAbilityButton? lanternButton = null;

        static private ISpriteLoader placeButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LanternPlaceButton.png", 115f);
        static private ISpriteLoader lanternButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LanternButton.png", 115f);

        public override AbstractRole Role => MyRole;
        
        public Instance(PlayerModInfo player) : base(player)
        {
        }


        private int[]? globalLanterns = null;
        List<NebulaSyncStandardObject> localLanterns = null;

        public override void OnActivated()
        {
            if (AmOwner)
            {
                localLanterns = new();

                lanternButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                lanternButton.SetSprite(lanternButtonSprite.GetSprite());
                lanternButton.Availability = (button) => MyPlayer.MyControl.CanMove ;
                lanternButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && globalLanterns != null;
                lanternButton.OnClick = (button) => {
                    button.ActivateEffect();
                };
                lanternButton.OnEffectStart = (button) =>
                {
                    CombinedRemoteProcess.CombinedRPC.Invoke(globalLanterns!.Select((id)=>RpcLantern.GetInvoker(id)).ToArray());
                };
                lanternButton.OnEffectEnd = (button) => lanternButton.StartCoolDown();
                lanternButton.CoolDownTimer = Bind(new Timer(0f, MyRole.LampCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                lanternButton.EffectTimer = Bind(new Timer(0f, MyRole.LampDurationOption.GetFloat()));
                lanternButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                lanternButton.SetLabel("lantern");

                int left = MyRole.NumOfLampsOption;

                placeButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                var usesText = placeButton.ShowUsesIcon(3);
                placeButton.SetSprite(placeButtonSprite.GetSprite());
                placeButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                placeButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && globalLanterns == null && left > 0;
                placeButton.OnClick = (button) => {
                    var pos = PlayerControl.LocalPlayer.GetTruePosition();
                    localLanterns.Add(Bind<NebulaSyncStandardObject>(NebulaSyncObject.LocalInstantiate(Lantern.MyLocalTag, new float[] { pos.x, pos.y }) as NebulaSyncStandardObject));

                    left--;
                    usesText.text = left.ToString();

                    placeButton.StartCoolDown();
                };
                placeButton.CoolDownTimer = Bind(new Timer(0f, MyRole.PlaceCoolDownOption.GetFloat()).SetAsAbilityCoolDown());
                placeButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                placeButton.SetLabel("place");
                usesText.text = left.ToString();

                lanternButton.StartCoolDown();
                placeButton.StartCoolDown();
            }
        }


        public override void OnMeetingStart()
        {
            //ランタンを全て設置していたら全員に公開する
            if(localLanterns != null && localLanterns.Count == MyRole.NumOfLampsOption)
            {
                globalLanterns = new int[localLanterns.Count];
                for (int i = 0;i<localLanterns.Count;i++) {
                    globalLanterns[i] = NebulaSyncObject.RpcInstantiate(Lantern.MyGlobalTag, new float[] { localLanterns[i].Position.x, localLanterns[i].Position.y }).ObjectId;
                    NebulaSyncObject.LocalDestroy(localLanterns[i].ObjectId);
                }
                localLanterns = null;
            }
        }

      

    }

    public static RemoteProcess<int> RpcLantern = RemotePrimitiveProcess.OfInteger(
      "Lantern",
      (message, _) =>
      {
          var lantern = NebulaSyncObject.GetObject<Lantern>(message);
          if (lantern != null)
          {
              SpriteRenderer lightRenderer = AmongUsUtil.GenerateCustomLight(lantern.Position, lightMaskSprite.GetSprite());

              IEnumerator CoLight()
              {
                  float t = MyRole.LampDurationOption.GetFloat();
                  float indexT = 0f;
                  int index = 0;
                  while (t > 0f)
                  {
                      t -= Time.deltaTime;
                      indexT -= Time.deltaTime;

                      if (indexT < 0f)
                      {
                          indexT = 0.13f;
                          lantern.Sprite = lanternSprite.GetSprite(index + 1);
                          index = (index + 1) % 3;
                      }
                      yield return null;
                  }

                  lantern.Sprite = lanternSprite.GetSprite(0);
                  t = 1f;

                  while (t > 0f)
                  {
                      t -= Time.deltaTime * 2.9f;
                      lightRenderer.material.color = new Color(1, 1, 1, t);
                      yield return null;
                  }

                  GameObject.Destroy(lightRenderer.gameObject);
              }

              IEnumerator CoLightBegin()
              {
                  float t;

                  t = 0.6f;
                  while (t > 0f)
                  {
                      t -= Time.deltaTime * 1.8f;
                      lightRenderer.material.color = new Color(1, 1, 1, t);
                      yield return null;
                  }

                  t = 0.4f;
                  while (t > 0f)
                  {
                      t -= Time.deltaTime * 0.8f;
                      lightRenderer.material.color = new Color(1, 1, 1, t);
                      yield return null;
                  }

                  while (t < 1f)
                  {
                      t += Time.deltaTime * 0.6f;
                      lightRenderer.material.color = new Color(1, 1, 1, t);
                      yield return null;
                  }

                  lightRenderer.material.color = Color.white;
              }

              NebulaManager.Instance.StartCoroutine(CoLight());
              NebulaManager.Instance.StartCoroutine(CoLightBegin());
          }
      }
      );
}
