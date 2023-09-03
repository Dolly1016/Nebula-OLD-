using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

public abstract class LobbySlide
{
    public string Tag { get; private set; }
    public string Title { get; private set; }
    public bool AmOwner { get; private set; }

    public bool Shared = false;
    public abstract bool Loaded { get; }

    public LobbySlide(string tag, string title, bool amOwner) {
        Tag = tag;
        Title = title;
        AmOwner = amOwner;
    }

    public virtual void Load() { }

    public void Share()
    {
        if (Shared) return;
        Reshare();
        Shared = true;
    }

    public abstract void Reshare();
    public virtual void Abandon() { }
    public abstract IMetaContext Show(out float height);


    protected static TextAttribute TitleAttribute = new(TextAttribute.TitleAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new Vector2(5f, 0.5f) };
    protected static TextAttribute CaptionAttribute = new(TextAttribute.NormalAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new Vector2(6f, 0.5f) };
}

public abstract class LobbyImageSlide : LobbySlide
{
    protected Sprite? mySlide { get; set; } = null;
    public override bool Loaded => mySlide;

    public string Caption { get; private set; }

    public LobbyImageSlide(string tag, string title, string caption, bool amOwner) : base(tag,title,amOwner)
    {
        Caption = caption;
    }
    public override void Abandon()
    {
        if (mySlide && mySlide!.texture) GameObject.Destroy(mySlide.texture);
    }

    public override IMetaContext Show(out float height)
    {
        height = 1.4f;

        MetaContext context = new();

        context.Append(new MetaContext.Text(TitleAttribute) { RawText = Title, Alignment = IMetaContext.AlignmentOption.Center });

        //縦に大きすぎる画像はそれに合わせて調整する
        float width = Mathf.Min(5.4f, mySlide.bounds.size.x / mySlide.bounds.size.y * 2.9f);
        height += width / mySlide.bounds.size.x * mySlide.bounds.size.y;

        context.Append(new MetaContext.Image(mySlide!) { Alignment = IMetaContext.AlignmentOption.Center, Width = width });

        context.Append(new MetaContext.VerticalMargin(0.2f));

        context.Append(new MetaContext.Text(CaptionAttribute) { RawText = Caption, Alignment = IMetaContext.AlignmentOption.Center });


        return context;
    }
}

[NebulaRPCHolder]
public class LobbyOnlineImageSlide : LobbyImageSlide
{
    private string url;

    public LobbyOnlineImageSlide(string tag,string title,string caption,bool amOwner,string url) : base(tag,title,caption,amOwner) 
    {
        this.url = url;
    }

    public override void Load() => LobbySlideManager.StartCoroutine(CoLoad());
    public override void Reshare()
    {
        RpcShare.Invoke((Tag, Title, Caption, url));
    }

    private async Task<byte[]> DownloadAsync()
    {
        var response = await NebulaPlugin.HttpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) return Array.Empty<byte>();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private IEnumerator CoLoad()
    {
        var task = DownloadAsync();
        while (!task.IsCompleted) yield return new WaitForSeconds(0.5f);

        if (task.Result.Length > 0)
        {
            mySlide = GraphicsHelper.LoadTextureFromByteArray(task.Result).ToSprite(100f);
            NebulaGameManager.Instance?.LobbySlideManager.OnLoaded(this);
        }
    }

    static private RemoteProcess<(string tag, string title, string caption, string url)> RpcShare = new(
        "ShareOnlineLobbySlide",
        (message, amOwner) => NebulaGameManager.Instance?.LobbySlideManager.RegisterSlide(new LobbyOnlineImageSlide(message.tag, message.title, message.caption, amOwner, message.url))
        );
}

public class LobbySlideTemplate
{
    [JsonSerializableField]
    public string Tag;
    [JsonSerializableField]
    public string Title = "None";
    [JsonSerializableField]
    public string SlideType = "None";
    [JsonSerializableField]
    public string Argument = "None";
    [JsonSerializableField]
    public string Caption = "None";

    public LobbySlide? Generate()
    {
        switch (SlideType.ToLower())
        {
            case "online":
            case "onlineimage":
                return new LobbyOnlineImageSlide(Tag,Title,Caption,true,Argument);
        }

        return null;
    }
}

[NebulaRPCHolder]
[NebulaPreLoad(typeof(NebulaAddon))]
public class LobbySlideManager
{
    public Dictionary<string,LobbySlide> allSlides = new();
    static public List<LobbySlideTemplate> AllTemplates = new();
    private MetaScreen? myScreen = null;
    private (string tag, bool detatched)? lastShowRequest;
    public bool IsValid { get; private set; } = true;

    static public IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Lobby Slides";
        yield return null;

        foreach (var addon in NebulaAddon.AllAddons)
        {
            using var stream = addon.OpenStream("Slides/LobbySlides.json");
            if (stream == null) continue;

            var templates = JsonStructure.Deserialize<List<LobbySlideTemplate>>(stream);
            if (templates == null) continue;

            foreach (var entry in templates) entry.Tag = addon.AddonName + "." + entry.Tag;
            AllTemplates.AddRange(templates);

            yield return null;
        }
    }

    public void RegisterSlide(LobbySlide slide)
    {
        if (!IsValid) return;

        if (!allSlides.ContainsKey(slide.Tag))
        {
            allSlides[slide.Tag] = slide;
            slide.Load();
            if (slide.AmOwner) slide.Share();
        }
    }

    public void RpcReshareSlide(string tag)
    {
        if (!IsValid) return;

        if (allSlides.TryGetValue(tag,out var slide))
        {
            slide.Reshare();
        }
    }

    public void Abandon()
    {
        if(!IsValid) return;

        foreach (var slide in allSlides.Values) slide.Abandon();
        if (myScreen) myScreen!.CloseScreen();
        IsValid = false;
    }

    static public RemoteProcess<(string tag, bool detatched)> RpcShow = new(
        "ShowSlide", (message, _) => NebulaGameManager.Instance?.LobbySlideManager.ShowSlide(message.tag, message.detatched)
        );

    public void RpcShowScreen(string tag,bool detatched)
    {
        if (!IsValid) return;

        if (allSlides.TryGetValue(tag, out var slide))
        {
            slide.Reshare();
            RpcShow.Invoke((tag, detatched));
        }
    }

    private void ShowSlide(string tag, bool detatched)
    {
        if (!allSlides.TryGetValue(tag, out var slide) || !slide.Loaded)
            lastShowRequest = (tag, detatched);
        else
        {
            if (myScreen)
            {
                myScreen!.CloseScreen();
                myScreen = null;
            }

            var context = slide.Show(out float height);
            var screen = MetaScreen.GenerateWindow(new(6.2f, Mathf.Min(height, 4.3f)), HudManager.Instance.transform, new Vector3(0, 0, -100f), true, false);
            screen.SetContext(context);

            if (!detatched) myScreen = screen;

            lastShowRequest = null;
        }
    }

    public void OnLoaded(LobbySlide slide)
    {
        if (lastShowRequest == null) return;

        if (slide.Tag == lastShowRequest?.tag)
        {
            ShowSlide(lastShowRequest.Value.tag, lastShowRequest.Value.detatched);
            lastShowRequest = null;
        }
    }

    static public void StartCoroutine(IEnumerator coroutine)
    {
        if (LobbyBehaviour.Instance) LobbyBehaviour.Instance.StartCoroutine(coroutine.WrapToIl2Cpp());
    }

    public void TryRegisterAndShow(LobbySlide slide)
    {
        RegisterSlide(slide);
        RpcShowScreen(slide.Tag, false);
    }
}
