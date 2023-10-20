using Hazel.Dtls;
using Il2CppInterop.Runtime.Injection;
using JetBrains.Annotations;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Behaviour
{
    public enum UncertifiedReason
    {
        Waiting,
        UnmatchedEpoch,
        UnmatchedBuild,
        Uncertified,
    }

    [NebulaRPCHolder]
    public class Certification
    {
        private static RemoteProcess<(byte playerId, int epoch, int build)> RpcHandshake = new(
            "Handshake", (message, calledByMe) => {
                var player = Helpers.GetPlayer(message.playerId);
                if (player?.gameObject.TryGetComponent<UncertifiedPlayer>(out var certification) ?? false)
                {
                    if (message.epoch != NebulaPlugin.PluginEpoch)
                        certification.Reject(UncertifiedReason.UnmatchedEpoch);
                    else if (message.build != NebulaPlugin.PluginBuildNum)
                        certification.Reject(UncertifiedReason.UnmatchedBuild);
                    else
                        certification.Certify();
                }
            }
            );

        private static RemoteProcess RpcRequireHandshake = new(
            "RequireHandshake", (_) => Handshake()
            );

        private static void Handshake()
        {
            RpcHandshake.Invoke((PlayerControl.LocalPlayer.PlayerId, NebulaPlugin.PluginEpoch, NebulaPlugin.PluginBuildNum));
        }

        public static void RequireHandshake()
        {
            IEnumerator CoWaitAndRequireHandshake()
            {
                yield return new WaitForSeconds(0.5f);
                RpcRequireHandshake.Invoke();
            }

            AmongUsClient.Instance.StartCoroutine(CoWaitAndRequireHandshake().WrapToIl2Cpp());
        }

    }
    public class UncertifiedPlayer : MonoBehaviour
    {
        static UncertifiedPlayer() => ClassInjector.RegisterTypeInIl2Cpp<UncertifiedPlayer>();

        private static string ReasonToTranslationKey(UncertifiedReason reason) => "certification." + reason.ToString().HeadLower();

        public UncertifiedReason State { get; private set; }
        private TMPro.TextMeshPro myText = null!;
        private GameObject myShower = null!;
        public PlayerControl? MyControl = null;
        public void Start()
        {
            State = UncertifiedReason.Waiting;

            myShower = UnityHelper.CreateObject("UncertifiedHolder",gameObject.transform,new Vector3(0,0,-20f), LayerExpansion.GetPlayersLayer());
            (new MetaContext.Text(TextAttribute.BoldAttr) {
                TranslationKey = ReasonToTranslationKey(UncertifiedReason.Uncertified),
                PostBuilder = (text) => myText = text })
                .Generate(myShower, Vector2.zero,out _);
            myText.color = Color.red.RGBMultiplied(0.92f);
            myText.gameObject.layer = LayerExpansion.GetPlayersLayer();

            var button = myShower.SetUpButton(false);
            var collider = myShower.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.6f, 0.2f);
            button.OnMouseOver.AddListener(() =>
            {
                NebulaManager.Instance.SetHelpContext(button, new MetaContext.VariableText(TextAttribute.ContentAttr) { Alignment = IMetaContext.AlignmentOption.Left, TranslationKey = ReasonToTranslationKey(State) + ".detail" });
            });
            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpContextIf(button));

            IEnumerator CoWaitAndUpdate()
            {
                yield return new WaitForSeconds(1.5f);
                if (State == UncertifiedReason.Waiting) Reject(UncertifiedReason.Uncertified);
            }
            StartCoroutine(CoWaitAndUpdate().WrapToIl2Cpp());
        }
        public void Certify()
        {
            GameObject.Destroy(this);
        }
        public void Reject(UncertifiedReason reason)
        {
            State = reason;
            OnStateChanged();

            if(MyControl?.OwnerId == AmongUsClient.Instance.HostId)
            {
                var screen = MetaScreen.GenerateWindow(new(3.2f,1.58f),HudManager.Instance.transform,Vector3.zero, true,false,true);
                var context = new MetaContext();
                context.Append(new MetaContext.Text(TextAttribute.BoldAttr) { Alignment = IMetaContext.AlignmentOption.Center, TranslationKey = ReasonToTranslationKey(State) });
                context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.NormalAttr) { Alignment = TMPro.TextAlignmentOptions.Top, Size = new(3f, 0.7f) }.EditFontSize(1.5f, 0.7f, 1.5f)) { TranslationKey = ReasonToTranslationKey(State) + ".client", Alignment = IMetaContext.AlignmentOption.Center });
                context.Append(new MetaContext.Button(() => AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame), TextAttribute.BoldAttr) { TranslationKey = "ui.dialog.exit", Alignment = IMetaContext.AlignmentOption.Center });
                screen.SetContext(context);
            }
            
        }

        private void OnStateChanged()
        {
            myText.text = Language.Translate(ReasonToTranslationKey(State));
        }

        public void Update()
        {
            myShower.SetActive(AmongUsClient.Instance.AmHost && State != UncertifiedReason.Waiting);
        }

        public void OnDestroy()
        {
            GameObject.Destroy(myShower);
        }
    }
}
