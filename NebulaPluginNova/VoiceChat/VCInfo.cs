using Il2CppInterop.Runtime.Injection;
using Nebula.Behaviour;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.VoiceChat;

public class VoiceChatInfo : MonoBehaviour
{
    static VoiceChatInfo() => ClassInjector.RegisterTypeInIl2Cpp<VoiceChatInfo>();

    static SpriteLoader backgroundSprite = SpriteLoader.FromResource("Nebula.Resources.UpperBackground.png", 100f);
    static SpriteLoader iconRadioSprite = SpriteLoader.FromResource("Nebula.Resources.UpperIconRadio.png", 100f);

    private MetaScreen myScreen = null!;

    public void Awake() {
        gameObject.AddComponent<SpriteRenderer>().sprite = backgroundSprite.GetSprite();
        myScreen = MetaScreen.GenerateScreen(new Vector2(2f, 0.45f), transform, new Vector3(0, 0, -1f), false, false, false);
    }

    private IMetaContext? radioContext = null;
    private float timer = 0f;
    private bool isMute = false;
    private bool MustShow => timer > 0f || radioContext != null || isMute;

    public void SetRadioContext(string displayText,Color color)
    {
        var context = new MetaContext();

        context.Append(
            new ParallelContext(
            new(new MetaContext.Image(iconRadioSprite.GetSprite()) { Width = 0.22f, Alignment = IMetaContext.AlignmentOption.Center }, 0.35f),
            new(new MetaContext()
            .Append(new MetaContext.VerticalMargin(0.015f))
            .Append(new MetaContext.Text(SmallTextAttribute) { Alignment = IMetaContext.AlignmentOption.Center, TranslationKey = "voiceChat.info.radio" })
            .Append(new MetaContext.VerticalMargin(-0.07f))
            .Append(new MetaContext.Text(TextAttribute) { Alignment = IMetaContext.AlignmentOption.Center, RawText = displayText.Color(color) })
            , 1.6f))
            { Alignment = IMetaContext.AlignmentOption.Center });
        radioContext = context;

        ShowContext();
    }

    public void UnsetRadioContext()
    {
        radioContext = null;
        ShowContext();
    }

    private void ShowContext()
    {
        timer = 2.7f;

        if (isMute)
            myScreen.SetContext(new MetaContext.Text(TextAttribute) { RawText = Language.Translate("voiceChat.info.mute"), Alignment = IMetaContext.AlignmentOption.Center });
        else if (radioContext != null)
            myScreen.SetContext(radioContext);
        else
            myScreen.SetContext(new MetaContext.Text(TextAttribute) { RawText = Language.Translate("voiceChat.info.unmute"), Alignment = IMetaContext.AlignmentOption.Center });
    }

    public static TextAttribute TextAttribute { get; private set; } = new(TextAttribute.BoldAttr) { Size = new(1.2f, 0.4f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaxSize = 1.8f, FontMinSize = 1f, FontSize = 1.8f };
    public static TextAttribute SmallTextAttribute { get; private set; } = new(TextAttribute.BoldAttr) { Size = new(1.2f, 0.15f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaxSize = 1.2f, FontMinSize = 0.7f, FontSize = 1.2f };
    public void SetMute(bool mute)
    {
        if (isMute == mute) return;
        isMute = mute;

        ShowContext();
    }

    public void Update()
    {
        float y = transform.localPosition.y;
        if (MustShow)
        {
            timer -= Time.deltaTime;
            y -= (y - 2.6f) * Mathf.Clamp01(Time.deltaTime * 4.1f);
        }
        else
        {
            y -= (y - 4f) * Mathf.Clamp01(Time.deltaTime * 2.8f);
        }

        transform.localPosition = new Vector3(0f, y, transform.localPosition.z);
    }
}
