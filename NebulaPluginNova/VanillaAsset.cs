using AmongUs.GameOptions;
using Il2CppSystem.Runtime.Serialization;
using Il2CppSystem.Web.Util;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Twitch;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Nebula;

public class VanillaAsset
{
    static public Sprite PopUpBackSprite { get; private set; }
    static public Sprite FullScreenSprite { get; private set; }
    static public Sprite TextButtonSprite { get; private set; }
    static public Sprite CloseButtonSprite { get; private set; }
    static public TMPro.TextMeshPro StandardTextPrefab { get; private set; }
    static public AudioClip HoverClip { get; private set; }
    static public AudioClip SelectClip { get; private set; }
    static public Material StandardMaskedFontMaterial { get {
            if (standardMaskedFontMaterial == null) standardMaskedFontMaterial = UnityHelper.FindAsset<Material>("LiberationSans SDF - BlackOutlineMasked")!;
            return standardMaskedFontMaterial;
        }
    }
    static public Material OblongMaskedFontMaterial { get { 
            if(oblongMaskedFontMaterial == null) oblongMaskedFontMaterial = UnityHelper.FindAsset<Material>("Brook Atlas Material Masked");
            return oblongMaskedFontMaterial;
        } }
    static private Material? standardMaskedFontMaterial = null;
    static private Material? oblongMaskedFontMaterial = null;

    static private TMP_FontAsset? preSpawnFont = null;
    static public TMP_FontAsset PreSpawnFont { get
        {
            if(preSpawnFont==null) preSpawnFont = UnityHelper.FindAsset<TMP_FontAsset>("DIN_Pro_Bold_700 SDF")!;
            return preSpawnFont;
        }
    }
    static public GameSettingMenu PlayerOptionsMenuPrefab { get; private set; }

    static public void LoadAssetAtInitialize()
    {
        HoverClip = UnityHelper.FindAsset<AudioClip>("UI_Hover")!;
        SelectClip = UnityHelper.FindAsset<AudioClip>("UI_Select")!;

        PlayerOptionsMenuPrefab = UnityHelper.FindAsset<GameSettingMenu>("PlayerOptionsMenu")!;
    }

    static public IEnumerator CoLoadAssetOnTitle()
    {
        var twitchPopUp = TwitchManager.Instance.transform.GetChild(0);
        PopUpBackSprite = twitchPopUp.GetChild(3).GetComponent<SpriteRenderer>().sprite;
        TextButtonSprite = twitchPopUp.GetChild(2).GetComponent<SpriteRenderer>().sprite;
        FullScreenSprite = twitchPopUp.GetChild(0).GetComponent<SpriteRenderer>().sprite;
        CloseButtonSprite = UnityHelper.FindAsset<Sprite>("closeButton")!;
        

        StandardTextPrefab = GameObject.Instantiate(twitchPopUp.GetChild(1).GetComponent<TMPro.TextMeshPro>(),null);
        StandardTextPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.Destroy(StandardTextPrefab.spriteAnimator);
        GameObject.DontDestroyOnLoad(StandardTextPrefab.gameObject);

        while (AmongUsClient.Instance == null) yield return null;

        /*
        //AsyncOperationHandle<GameObject> handle;
        //AmongUsClient.Instance.ShipPrefabs[2].RuntimeKey;
        UnityEngine.AddressableAssets.Addressables.InstantiateAsync(AmongUsClient.Instance.ShipPrefabs[2].RuntimeKey, null, false, false);
        //Polus
        //handle = AmongUsClient.Instance.ShipPrefabs[2].InstantiateAsync(null, false);
        //yield return handle;
        //var polus = handle.Result.GetComponent<PolusShipStatus>();
        */

        /*
        //Airship
        handle = AmongUsClient.Instance.ShipPrefabs[4].InstantiateAsync(null, false);
        yield return handle;
        */

        yield break;
    }

    static public Scroller GenerateScroller(Vector2 size, Transform transform, Vector3 scrollBarLocalPos, Transform target, FloatRange bounds, float scrollerHeight)
    {
        var barBack = GameObject.Instantiate(PlayerOptionsMenuPrefab.RegularGameSettings.transform.FindChild("UI_ScrollbarTrack").gameObject, transform);
        var bar = GameObject.Instantiate(PlayerOptionsMenuPrefab.RegularGameSettings.transform.FindChild("UI_Scrollbar").gameObject, transform);
        barBack.transform.localPosition = scrollBarLocalPos + new Vector3(0.12f, 0f, 0f);
        bar.transform.localPosition = scrollBarLocalPos;

        var scrollBar = bar.GetComponent<Scrollbar>();

        var scroller = UnityHelper.CreateObject<Scroller>("Scroller", transform, new Vector3(0, 0, 5));
        scroller.gameObject.AddComponent<BoxCollider2D>().size = size;

        scrollBar.parent = scroller;
        scrollBar.graphic = bar.GetComponent<SpriteRenderer>();
        scrollBar.trackGraphic = barBack.GetComponent<SpriteRenderer>();
        scrollBar.trackGraphic.size = new Vector2(scrollBar.trackGraphic.size.x, scrollerHeight);

        var ratio = scrollerHeight / 3.88f;

        scroller.Inner = target;
        scroller.SetBounds(bounds, null);
        scroller.allowY = true;
        scroller.allowX = false;
        scroller.ScrollbarYBounds = new FloatRange(-1.8f * ratio + scrollBarLocalPos.y + 0.4f, 1.8f * ratio + scrollBarLocalPos.y - 0.4f);
        scroller.ScrollbarY = scrollBar;
        scroller.active = true;
        //scroller.Colliders = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider2D>(new Collider2D[] { hitBox });

        scroller.ScrollToTop();

        return scroller;
    }
}
