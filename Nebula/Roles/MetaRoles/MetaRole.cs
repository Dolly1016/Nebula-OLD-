using Nebula.Module;
using Nebula.Module.Information;
using Nebula.Tasks;
using UnityEngine;

namespace Nebula.Roles.MetaRoles;

public class MetaObjectPreviewBehaviour : MonoBehaviour
{
    Controller MetaController = new Controller();
    Collider2D Collider;

    static MetaObjectPreviewBehaviour()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MetaObjectPreviewBehaviour>();
    }

    public MetaObjectPreviewBehaviour()
    {
    }

    public void OnEnable()
    {
        Collider = gameObject.GetComponent<Collider2D>();
    }

    public void Update()
    {
        MetaController.Update();

        if (MetaController.CheckHover(Collider))
        {
            gameObject.transform.position += new Vector3(0, 0, Input.mouseScrollDelta.y / 100f);
        }

        if (MetaController.CheckDrag(Collider)==DragState.Dragging)
        {
            Vector3 pos = MetaController.DragPosition;
            pos.z = gameObject.transform.position.z;
            gameObject.transform.position = pos;
        }

        var text = Roles.MetaRole.MetaObjectManager.MetaInfoText;
        if (text!=null)
        {
            var pos = gameObject.transform.position;

            if (text[0]) text[0].text = "X: " + String.Format("{0:f3}", pos.x);
            if (text[1]) text[1].text = "Y: " + String.Format("{0:f3}", pos.y);
            if (text[2]) text[2].text = "Z: " + String.Format("{0:f3}", pos.z * 10f);
        }

        if (PlayerControl.LocalPlayer.transform.position.Distance(transform.position) > 6f)
            Roles.MetaRole.MetaObjectManager.CloseObjectDetail();
    }
}

public class MetaObjectManager
{
    GameObject? MetaObject;
    SpriteRenderer? MetaRenderer;
    CustomTextureAsset? MetaTextureAsset;
    MetaScreen? MetaScreen = null;
    public TMPro.TextMeshPro[]? MetaInfoText = null;
    public TextInputField? TextInputField = null;

    public bool MetaScreenIsShown => MetaScreen != null && MetaScreen.screen;
    public void Destroy()
    {
        EraseMetaObject();
        if (MetaScreenIsShown) GameObject.Destroy(MetaScreen.screen);
        MetaScreen = null;

        if (MetaTextureAsset != null)
        {
            MetaTextureAsset.Discard();
            MetaTextureAsset = null;
        }
        MetaInfoText = null;
    }

    public void EraseMetaObject()
    {
        if(MetaObject)GameObject.Destroy(MetaObject);
        MetaObject = null;
        MetaRenderer = null;
    }

    public GameObject? SpawnMetaObject(string textureId)
    {
        TexturePack.LoadAsset(textureId,ref MetaTextureAsset);
        

        Sprite? sprite = null;
        if (MetaTextureAsset != null)
        {
            if (MetaTextureAsset.staticSprite != null) sprite = MetaTextureAsset.staticSprite;
            else if (MetaTextureAsset.animation != null) sprite = MetaTextureAsset.animation[0];
        }
        else
        {
            sprite = Helpers.loadSpriteFromResources("Nebula.Resources.PuzzlePiece.png",100f);
        }
        if (sprite == null) return MetaObject;

        if (!MetaObject)
        {
            MetaObject = GameObject.Instantiate(AssetLoader.MetaObjectPrefab);
            MetaObject.transform.localPosition = PlayerControl.LocalPlayer.transform.localPosition;
            MetaObject.transform.localScale = Vector3.one * Helpers.GetDefaultNormalizeRate();
            MetaObject.AddComponent<BoxCollider2D>().isTrigger = true;
            MetaObject.AddComponent<MetaObjectPreviewBehaviour>();
        }

        var highlight = MetaObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        MetaRenderer = MetaObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();
        MetaRenderer.gameObject.layer = LayerExpansion.GetObjectsLayer();
        var collider = MetaObject.GetComponent<BoxCollider2D>();

        var size = new Vector2(sprite.rect.width / 100f, sprite.rect.height / 100f);
        highlight.size = size;
        if (size.x < 0) size.x = -size.x;
        if (size.y < 0) size.y = -size.y;
        collider.size = size;
        MetaRenderer.sprite = sprite;

        
        return MetaObject;
    }

    public void ShowObjectDetail()
    {
        if (MetaScreenIsShown) return;
        var size = new Vector2(3f,1f);
        var designer = MetaScreen.OpenScreen(HudManager.Instance.gameObject, size, new Vector3(0f, -2.2f));
        MetaScreen = designer.screen;

        MetaScreen.screen.layer = LayerExpansion.GetUILayer();
        MetaScreen.screen.transform.localPosition -= new Vector3(0, 0, 200f);
        var renderer = MetaScreen.screen.AddComponent<SpriteRenderer>(); 
        renderer.sprite = Module.MetaScreen.GetButtonBackSprite();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size + new Vector2(0.4f, 0.4f);
        renderer.color = new Color(1f, 1f, 1f, 0.95f);

        designer.CustomUse(-0.3f);

        var multiString = new MSMultiString[3];
        for (int i = 0; i < 3; i++) multiString[i] = new MSMultiString(1f, 1.8f, "", TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal);
            

        var textInput = new MSTextInput(2.5f, 0.3f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal);
        var canSeeInShadow = new MSRadioButton(true, 1f, Language.Language.GetString("metaObject.canSeeInShadow"), 1.4f, 1.4f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        var canSeeOnlyInShadow = new MSRadioButton(false, 1f, Language.Language.GetString("metaObject.canSeeOnlyInShadow"), 1.4f, 1.4f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        designer.AddTopic(multiString);
        designer.CustomUse(-0.1f);
        designer.AddTopic(canSeeInShadow, canSeeOnlyInShadow);
        designer.CustomUse(-0.1f);
        designer.AddTopic(new MetaScreenContent[] { textInput });

        canSeeInShadow.FlagUpdateAction = (flag) => {
            if (flag && canSeeOnlyInShadow.Flag) canSeeOnlyInShadow.Flag = false;
            if (flag)
                MetaRenderer.gameObject.layer = LayerExpansion.GetObjectsLayer();
            else
                MetaRenderer.gameObject.layer = LayerExpansion.GetDefaultLayer();
        };
        canSeeOnlyInShadow.FlagUpdateAction = (flag) => {
            if (flag && canSeeInShadow.Flag) canSeeInShadow.Flag = false;
            if(flag)
                MetaRenderer.gameObject.layer = LayerExpansion.GetShadowLayer();
            else
                MetaRenderer.gameObject.layer = LayerExpansion.GetDefaultLayer();
        };

        MetaInfoText = new TMPro.TextMeshPro[multiString.Length];
        for (int i = 0; i < multiString.Length; i++) MetaInfoText[i] = multiString[i].text;
        TextInputField = textInput.TextInputField;
        TextInputField.HintText = "freePlay.metaObject";
        textInput.TextInputField.LoseFocusAction = (id) => SpawnMetaObject(id);

        SpawnMetaObject("freePlay.metaObject");   
    }

    public void CloseObjectDetail()
    {
        IEnumerator GetEnumerator(Transform screen)
        {
            
            while(screen.localScale.x > 0.01f)
            {
                yield return null;
                screen.localScale -= Vector3.one * (7f * Time.deltaTime);
            }
            GameObject.Destroy(screen.gameObject);
        }

        if (!MetaScreenIsShown) return;

        var transform = MetaScreen!.screen.transform;
        MetaScreen = null;
        HudManager.Instance.StartCoroutine(GetEnumerator(transform).WrapToIl2Cpp());
        EraseMetaObject();
    }
    public void ToggleObjectDetail()
    {
        if (MetaScreenIsShown)
            CloseObjectDetail();
        else
            ShowObjectDetail();
    }
}

public class MetaButtons : Module.Information.UpperInformation
{
    PassiveButton[] buttons;
    SpriteRenderer LifeAndDeathButtonRenderer;
    static DividedSpriteLoader buttonSprites = new DividedSpriteLoader("Nebula.Resources.MetaButton.png", 100f, 4, 1);
    public MetaButtons() : base("MetaButtons")
    {
        height = 0.4f;

        buttons = new PassiveButton[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = new GameObject("Button");
            obj.transform.SetParent(gameObject.transform);
            obj.transform.localPosition = new Vector3(0.4f * (i - 1), 0, 0);
            obj.transform.localScale = Vector3.one;
            obj.layer = LayerExpansion.GetUILayer();
            PassiveButton button = obj.AddComponent<PassiveButton>();
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = buttonSprites.GetSprite(i);
            if (i == 2) LifeAndDeathButtonRenderer = renderer;
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, 0.3f);
            collider.isTrigger = true;

            switch (i)
            {
                case 0:
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        if (MeetingHud.Instance) return;
                        Roles.MetaRole.MetaObjectManager.ToggleObjectDetail();
                    }));
                    break;
                case 1:
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        if (Module.MetaDialog.AnyDialogShown) return;

                        Module.MetaDialog.MSDesigner? dialog = null;
                        dialog = Module.MetaDialog.OpenRolesDialog((r) =>
                            r.category != RoleCategory.Complex &&
                            r != Roles.DamnedCrew &&
                            r != Roles.CrewmateWithoutTasks &&
                            r != Roles.HnSCrewmate &&
                            r != Roles.HnSReaper
                        , 0, 60, (r) =>
                        {
                            RPCEventInvoker.ImmediatelyChangeRole(PlayerControl.LocalPlayer, r);
                            dialog?.screen.Close();
                        });
                    }));
                    break;
                case 2:
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead)
                            RPCEventInvoker.RevivePlayer(PlayerControl.LocalPlayer);
                        else
                            Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, Game.PlayerData.PlayerStatus.Suicide, false, false);
                    }));
                    break;
            }
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        }
    }

    public override bool Update()
    {
        LifeAndDeathButtonRenderer.sprite = buttonSprites.GetSprite(PlayerControl.LocalPlayer.Data.IsDead ? 2 : 3);
        return true;
    }
}

public class MetaRole : ExtraRole
{
    static public Color Color = new Color(255 / 255f, 255 / 255f, 255 / 255f);
    public MetaObjectManager MetaObjectManager;

    public override void Assignment(Patches.AssignMap assignMap)
    {
        if (Game.GameData.data.GameMode != Module.CustomGameMode.FreePlay) return;

        foreach (var player in Game.GameData.data.AllPlayers.Keys)
        {
            assignMap.AssignExtraRole(player, this.id, 0);
        }
    }

    public override void Initialize(PlayerControl __instance)
    {
        new MetaButtons();
    }

    public override void CleanUp()
    {
        MetaObjectManager.Destroy();
        Module.Information.UpperInformationManager.Remove((i) =>
       i is MetaButtons
       );
    }

    public override void OnMeetingStart()
    {
        if (MetaObjectManager.MetaScreenIsShown) MetaObjectManager.CloseObjectDetail();
    }

    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(
                Color, "⌘");
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        EditDisplayNameForcely(playerId, ref displayName);
    }

    public MetaRole() : base("MetaRole", "metaRole", Color, 1)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.FreePlay;
        MetaObjectManager = new MetaObjectManager();
    }
}
