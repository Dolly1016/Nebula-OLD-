using LibCpp2IL;
using Nebula.Expansion;
using Nebula.Module;
using TMPro;

namespace Nebula.Roles.NeutralRoles;

public class Paparazzo : Role, Template.HasWinTrigger
{
    public struct PaparazzoImageMessage
    {
        public byte sender;
        public int id;
        public float angle;
        public int division;
        public int index;
        public byte[] data;
    }

    public static RemoteProcess<PaparazzoImageMessage> SharePaparazzoImage = new RemoteProcess<PaparazzoImageMessage>(
        (writer,message) => { 
            writer.Write(message.sender);
            writer.Write(message.id);
            writer.Write(message.angle);
            writer.Write(message.division);
            writer.Write(message.index);
            writer.Write(message.data.Length);
            writer.Write(message.data);
        },
        (reader) =>
        {
            PaparazzoImageMessage message = new PaparazzoImageMessage();
            message.sender = reader.ReadByte();
            message.id = reader.ReadInt32();
            message.angle = reader.ReadSingle();
            message.division = reader.ReadInt32();
            message.index = reader.ReadInt32();
            int length = reader.ReadInt32();
            message.data = reader.ReadBytes(length);
            return message;
        },
        (message, isCalledByMe) =>
        {
            IncompleteImageMessage.ReceiveMessage(message.sender,message.id,message.angle,message.division,message.index,message.data);
        }
        );

    public class IncompleteImageMessage
    {
        static Dictionary<int, IncompleteImageMessage> allMessages = new Dictionary<int, IncompleteImageMessage>();
        static public void Initialize()
        {
            allMessages.Clear();
        }
        static public void ReceiveMessage(byte sender,int id,float angle,int division,int index, byte[] data)
        {
            int key = (sender << 16) + id;
            IncompleteImageMessage message;
            if (allMessages.TryGetValue(key,out message))
            {
                message.data[index] = data;
            }
            else
            {
                message = new IncompleteImageMessage();
                message.data = new byte[]?[division];
                message.data[index] = data;
                allMessages[key] = message;
            }

            foreach(var d in message.data) if (d == null) return;

            //データをすべて受け取ったとき
            allMessages.Remove(key);
            int length = 0;
            foreach (var d in message.data) length += d.Length;
            byte[] received=new byte[length];
            int i = 0;
            foreach (var d in message.data) {
                Array.Copy(d,0, received, i, d.Length);
                i += d.Length;
            }

            Texture2D texture = new Texture2D(1, 1);

            ImageConversion.LoadImage(texture,received);
            new MeetingHudExpansion.DisclosedPicture(texture, angle);
        }

        byte[]?[] data;
    }
    

    Transform PicturesHolder;

    public class PictureData
    {
        public class PlayerInfo
        {
            public byte PlayerId { get; private set; }
            public Vector2 Pos { get; private set; }
            public PoolablePlayer Display { get {
                    if (poolablePlayer == null) poolablePlayer = Helpers.playerById(PlayerId).CopyToPoolablePlayer();
                    
                    return poolablePlayer;
                } 
            }
            private PoolablePlayer poolablePlayer = null;

            public PlayerInfo(PlayerControl player, Vector2 pos)
            {
                Pos = pos;
                PlayerId = player.PlayerId;
            }   
        }

        public Texture2D Picture { get; private set; }
        public int Id { get; private set; }
        static public int AvailableId { get; set; }
        public float Angle { get; private set; }
        public PlayerInfo[] Players { get; private set; }
        public bool IsShown { get; private set; }
        public bool CannotShow { get; private set; }
        public GameObject Holder { get; set; }


        public void Share()
        {
            IsShown = true;

            foreach(var p in Players)
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                Roles.Paparazzo.activePlayers.Add(p.PlayerId);
            }

            byte[] bytes = UnityEngine.ImageConversion.EncodeToJPG(Picture, 60);
            if (bytes.Length == 0) return;

            int sent = 0;
            int division = (int)((bytes.Length - 1) / 4098) + 1;
            int i = 0;
            while (sent < bytes.Length)
            {
                int length = bytes.Length - sent;
                if (length > 4098) length = 4098;
                var subBytes = bytes.SubArray(sent,length);

                SharePaparazzoImage.Invoke(new PaparazzoImageMessage
                {
                    sender = PlayerControl.LocalPlayer.PlayerId,
                    id = Id,
                    angle = Angle,
                    division = division,
                    index = i,
                    data = subBytes
                }
                );

                sent += length;
                i++;
            }
                    

            
            
        }
        public PictureData(GameObject finder, Texture2D picture, Vector2 size)
        {
            Picture = picture;
            Angle = finder.transform.localEulerAngles.z;
            IsShown = false;
            CannotShow = false;
            Holder = null;
            Id = AvailableId++;

            List<PlayerInfo> playersList = new();
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if (!p.Visible) continue;
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                bool flag = true;
                for (int i = 0; i < 4; i++)
                {
                    var locPos = (Vector2)finder.transform.InverseTransformPoint(
                        p.transform.position + new Vector3(i < 2 ? 0.1f : -0.1f, i % 2 == 0 ? 0.22f : -0.22f));
                    var locPosPositive = locPos;
                    if (locPosPositive.x < 0) locPosPositive.x = -locPosPositive.x;
                    if (locPosPositive.y < 0) locPosPositive.y = -locPosPositive.y;

                    if (!(locPosPositive.x < size.x/2f && locPosPositive.y < size.y/2f))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    playersList.Add(new PlayerInfo(p, finder.transform.InverseTransformPoint(p.transform.position)));
                    Roles.Paparazzo.filmedPlayers.Add(p.PlayerId);
                }
            }
            Players = playersList.ToArray();
        }

        public void UpdateProgress()
        {
            foreach (var p in Players)
            {
                if (Roles.Paparazzo.activePlayers.Contains(p.PlayerId)) p.Display.setSemiTransparent(false);
            }
        }

        public void UpdateState()
        {
            if (CannotShow) return;

            foreach(var p in Players)
            {
                if (PlayerControl.LocalPlayer.PlayerId == p.PlayerId) continue;
                if (!Helpers.playerById(p.PlayerId).Data.IsDead) return;
            }
            CannotShow = true;
        }
    }

    private PictureData TakePicture(GameObject finder,Vector2 size,int roughness)
    {
        GameObject.Destroy(finder.GetComponent<PassiveButton>());
        var scale = finder.transform.localScale;
        size = size * 100f;
        GameObject camObj = new GameObject();
        camObj.transform.SetParent(finder.transform);
        camObj.transform.localScale = new Vector3(1,1);
        camObj.transform.localPosition = new Vector3(0,0);
        camObj.transform.localEulerAngles = new Vector3(0, 0, 0);
        var pos = camObj.transform.position;
        pos.z = -0.5f;
        camObj.transform.position = pos;

        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size.y / 200f * scale.y;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = 0b1101100000001;
        cam.enabled = true;

        RenderTexture rt = new RenderTexture((int)(size.x / roughness * scale.x), (int)(size.y / roughness * scale.y), 16);
        rt.Create();

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = cam.targetTexture;
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);
        GameObject.Destroy(cam);

        var pictureData = new PictureData(finder, texture2D, size / 100f);

        CheckWin();

        Objects.SoundPlayer.PlaySound(Module.AudioAsset.Paparazzo);

        return pictureData;
    }

    static public Color RoleColor = new Color(202f / 255f, 118f / 255f, 140f / 255f);

    static private CustomButton cameraButton;
    private GameObject finderObject;
    private List<PictureData> pictures;
    private HashSet<byte> activePlayers;
    private HashSet<byte> filmedPlayers;
    private float shareCount;

    private bool CanSharePicture { get
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            if (!MeetingHud.Instance) return false;
            if (!(shareCount > 0)) return false;
            return (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.state == MeetingHud.VoteStates.Voted);
        } 
    }

    private void UpdatePlayersInfo()
    {
        foreach(var picture in pictures)
        {
            picture.UpdateProgress();
            foreach (var player in picture.Players)
            {
                if (Helpers.playerById(player.PlayerId).Data.IsDead)
                    player.Display.SetBodyAsGhost();
            }

            if (picture.IsShown) continue;
            picture.UpdateState();
        }

        pictures.RemoveAll((p) =>
        {
            if (p.IsShown) return false;

            foreach (var player in p.Players)
            {
                if (!Helpers.playerById(player.PlayerId).Data.IsDead && !activePlayers.Contains(player.PlayerId)) return false;
            }

            if(p.Holder)GameObject.Destroy(p.Holder);
            if (p.Picture) GameObject.Destroy(p.Picture);
            return true;
            
        });
    }

    public GameObject? hourglassObj = null;
    public TextMeshPro? hourglassText = null;

    public override void OnMeetingStart()
    {
        shareCount = 20f;
        UpdatePlayersInfo();

        hourglassObj = new GameObject();
        hourglassObj.layer = LayerExpansion.GetUILayer();
        hourglassObj.transform.SetParent(PicturesHolder);
        hourglassObj.AddComponent<SpriteRenderer>().sprite = hourglassSprite.GetSprite();
        hourglassText = GameObject.Instantiate(HudManager.Instance.KillButton.cooldownTimerText,hourglassObj.transform);
        hourglassText.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        hourglassText.text = ((int)shareCount).ToString();
    }
    public override void OnMeetingEnd()
    {
        shareCount = 0;
        UpdatePlayersInfo();
        CheckWin();

        if (hourglassObj)
        {
            GameObject.Destroy(hourglassObj);
            hourglassObj= null;
            hourglassText = null;
        }
    }

    public override void MeetingUpdate(MeetingHud __instance, TextMeshPro meetingInfo)
    {
        if (hourglassObj && hourglassObj.active)
        {
            if (shareCount > 0)
                hourglassText.text = ((int)shareCount).ToString();
            else
                hourglassObj.SetActive(false);
        }
    }

    private Module.CustomOption shootCoolDownOption;
    private Module.CustomOption winConditionSubjectOption;
    private Module.CustomOption winConditionDisclosedOption;
    private Module.CustomOption canUseVentsOption;
    private Module.CustomOption isGuessableOption;

    public override bool IsGuessableRole { get => isGuessableOption.getBool(); }


    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;


    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(cameraSprite,"role.paparazzo.help.camera",0.3f)
        };

    public override void LoadOptionData()
    {
        shootCoolDownOption = CreateOption(Color.white, "shootCoolDown", 10f, 0f, 60f, 2.5f);
        shootCoolDownOption.suffix = "second";

        winConditionSubjectOption = CreateOption(Color.white, "winConditionSubject", CustomOptionHolder.GetStringMixedSelections("role.paparazzo.winConditionSubject.allAlives", 1, 15, 1, 15, 1).ToArray(), "role.paparazzo.winConditionSubject.allAlives");
        winConditionDisclosedOption = CreateOption(Color.white, "winConditionDisclosed", 6f, 1f, 15f, 1f);

        isGuessableOption = CreateOption(Color.white, "isGuessable", false);

        canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
    }

    private void CheckWin()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if ((int)winConditionDisclosedOption.getFloat() > activePlayers.Count) return;
        if (winConditionSubjectOption.getSelection() == 0)
        {
            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (!p.Data.IsDead && filmedPlayers.Contains(p.PlayerId)) return;
            }
        }
        else if ((int)winConditionSubjectOption.getFloat() > filmedPlayers.Count) return;
        
        RPCEventInvoker.WinTrigger(this);
    }

    public override bool ShowTaskText => false;

    public override string? GetCustomTaskText()
    {
        var text = Language.Language.GetString("role.paparazzo.taskText");
        var detail = "";


        detail = Language.Language.GetString("role.paparazzo.taskTextDisclosed")
            .Replace("%GD%", winConditionDisclosedOption.getFloat().ToString())
            .Replace("%CD%", activePlayers.Count.ToString());
        if (winConditionSubjectOption.getSelection() != 0 && winConditionDisclosedOption.getFloat()<winConditionSubjectOption.getFloat()) {
            detail = Language.Language.GetString("role.paparazzo.taskTextSubject")
                .Replace("%GS%", winConditionSubjectOption.getFloat().ToString())
                .Replace("%CS%", filmedPlayers.Count.ToString())
                + ", " + detail;
        }
        return text.Replace("%DETAIL%",detail);
    }


    SpriteLoader cameraSprite = new SpriteLoader("Nebula.Resources.CameraButton.png", 115f, "ui.button.paparazzo.film");
    SpriteLoader hourglassSprite = new SpriteLoader("Nebula.Resources.Hourglass.png", 100f);

    public override void GlobalIntroInitialize(PlayerControl __instance)
    {
        canMoveInVents = canUseVentsOption.getBool();
        VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        shareCount = 0;

        PictureData.AvailableId = 0;
        WinTrigger = false;
        pictures.Clear();
        activePlayers.Clear();
        filmedPlayers.Clear();

        PicturesHolder = new GameObject("PlayerIcons").transform;
        PicturesHolder.SetParent(HudManager.Instance.UseButton.transform.parent);
        PicturesHolder.localScale = new Vector3(1f, 1f, 1f);
        PicturesHolder.localPosition = new Vector3(0f, 0f, -20f);
        Expansion.GridArrangeExpansion.AddGridArrangeContent(PicturesHolder.gameObject,
            Expansion.GridArrangeExpansion.GridArrangeParameter.LeftSideContent | Expansion.GridArrangeExpansion.GridArrangeParameter.OccupyingLineContent | Expansion.GridArrangeExpansion.GridArrangeParameter.AlwaysVisible);
    }

    public override void CleanUp()
    {
        base.CleanUp();

        if (hourglassObj) GameObject.Destroy(hourglassObj);
        hourglassObj = null;
        hourglassText = null;
        
        if(finderObject) GameObject.Destroy(finderObject);
        foreach(var p in pictures) if(p.Picture)GameObject.Destroy(p.Picture);
        if(PicturesHolder) GameObject.Destroy(PicturesHolder.gameObject);

        WinTrigger = false;

        if (cameraButton != null)
        {
            cameraButton.Destroy();
            cameraButton = null;
        }
    }

    private IEnumerator GetShootEnumerator(GameObject finder,Vector2 size,PictureData picture)
    {
        GameObject? holder = null;
        if (picture.Players.Length > 0 && picture.Players.Any((p) => !activePlayers.Contains(p.PlayerId)))
        {
            holder = new GameObject("PictureHolder");
            holder.transform.SetParent(PicturesHolder);
        }

        finder.transform.GetChild(1).gameObject.SetActive(false);
        finder.transform.GetChild(2).gameObject.SetActive(false);
        finder.transform.GetChild(3).gameObject.SetActive(true);
        var flash = finder.transform.GetChild(3).gameObject.GetComponent<SpriteRenderer>();
        var obj = new GameObject("Picture");
        obj.transform.SetParent(finder.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = new Vector3(1f / finder.transform.localScale.x, 1f / finder.transform.localScale.y, 1f);
        obj.transform.localEulerAngles= Vector3.zero;
        obj.layer = LayerExpansion.GetUILayer();
        var renderer = obj.AddComponent<SpriteRenderer>();
        Sprite sprite = Helpers.loadSpriteFromResources(picture.Picture, 100f, new Rect(0, 0, picture.Picture.width, picture.Picture.height)); ;
        renderer.sprite = sprite;
        

        finder.transform.SetParent(HudManager.Instance.transform);
       

        float p = 0f;
        while (p < 1f)
        {
            flash.color *= 0.92f;

            p += Time.deltaTime * 1.8f;
            yield return null;
        }

        flash.color = Color.clear;

        p = 0f;

        if (holder != null)
        {
            renderer.material = ConsoleExpansion.GetHighlightMaterial();
            
            var button = finder.AddComponent<PassiveButton>();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnClick.RemoveAllListeners();
            button.OnMouseOut.AddListener(
                (UnityEngine.Events.UnityAction)(() => {
                    renderer.material.SetFloat("_Outline", 0f);
                    if (picture.IsShown) return;
                    renderer.material.SetColor("_AddColor", Color.clear);
                })
            );
            button.OnMouseOver.AddListener(
                (UnityEngine.Events.UnityAction)(() => {
                    if (!CanSharePicture || picture.IsShown) return;

                    renderer.material.SetFloat("_Outline", 1f);
                    renderer.material.SetColor("_OutlineColor", Color.yellow);
                    renderer.material.SetColor("_AddColor", Color.yellow);
                })
            );
            button.OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() => {
                    if (!CanSharePicture || picture.IsShown) return;

                    var dialog = MetaDialog.OpenDialog(new Vector2(5f, 3.5f), "");

                    dialog.AddTopic(new MSString(4.2f, Language.Language.GetString("role.paparazzo.sharing.confirm"),2f,1f,TextAlignmentOptions.Center,FontStyles.Normal));
                    dialog.CustomUse(2.1f);
                    GameObject pictureObj = new GameObject("Picture");
                    pictureObj.transform.SetParent(dialog.screen.screen.transform);
                    pictureObj.transform.localPosition = new Vector3(0, 0.1f, -1f);
                    pictureObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                    pictureObj.transform.localEulerAngles = new Vector3(0f, 0f, picture.Angle);
                    pictureObj.layer = LayerExpansion.GetUILayer();
                    pictureObj.AddComponent<SpriteRenderer>().sprite = sprite;

                    dialog.AddTopic(
                        new MSButton(1f, 0.4f, Language.Language.GetString("config.option.yes"), FontStyles.Bold,
                        () => {
                            MetaDialog.EraseDialogAll();
                            if (CanSharePicture && !picture.IsShown)
                            {
                                picture.Share();
                                UpdatePlayersInfo();
                                renderer.material.SetColor("_AddColor", Color.green);
                                shareCount = 0f;
                            }
                            else
                            {
                                var failedDialog = MetaDialog.OpenDialog(new Vector2(4f, 1.5f), "");
                                failedDialog.AddTopic(new MSString(3.6f, Language.Language.GetString("role.paparazzo.sharing.failed"), 2f, 1f, TextAlignmentOptions.Center, FontStyles.Normal));
                                failedDialog.CustomUse(0.15f);
                                failedDialog.AddTopic(
                                    new MSButton(1f, 0.4f, Language.Language.GetString("config.option.confirm"), FontStyles.Bold,
                                    () => MetaDialog.EraseDialogAll()));
                            }
                        }),
                        new MSButton(1f, 0.4f, Language.Language.GetString("config.option.no"), FontStyles.Bold,
                        () => {
                            MetaDialog.EraseDialogAll();
                        }));
                })
            );


            pictures.Add(picture);
            picture.Holder = holder;
            int n = 0;
            int l = picture.Players.Length;
            foreach (var player in picture.Players)
            {
                player.Display.setSemiTransparent(!activePlayers.Contains(player.PlayerId));
                player.Display.transform.SetParent(holder.transform);
                player.Display.transform.localPosition = new Vector3((float)(1 - l + (n * 2)) * 0.3f, 2.2f, 0f);
                player.Display.transform.localScale = new Vector3(0.5f,0.5f,1f);
                n++;
            }

            float angleLeft = 720f;

            finder.transform.SetParent(holder.transform);

            while (p < 1f)
            {
                finder.transform.localEulerAngles += new Vector3(0, 0, angleLeft * 0.05f);
                finder.transform.localPosition *= 0.82f;
                holder.transform.localScale = holder.transform.localScale * 0.8f + new Vector3(0.2f, 0.2f,1f) * 0.2f;
                angleLeft *= 0.9f;

                p += Time.deltaTime * 1f;
                yield return null;
            }
            finder.transform.localEulerAngles += new Vector3(0, 0, angleLeft);
            finder.transform.localPosition = Vector3.zero;
            holder.transform.localScale = new Vector3(0.2f, 0.2f,1f);
        }
        else
        {
            while (p < 1f)
            {
                finder.transform.localEulerAngles += new Vector3(0, 0, 720f * Time.deltaTime);
                finder.transform.localScale *= 0.95f;

                p += Time.deltaTime * 1f;
                yield return null;
            }
            GameObject.Destroy(finder);
        }

    }

    private void OnFireFilmButton()
    {
        if (cameraButton.Timer > 0f) return;
        if (!PlayerControl.LocalPlayer.CanMove || PlayerControl.LocalPlayer.Data.IsDead) return;

        if (finderObject)
        {
            var picture = TakePicture(finderObject, new Vector2(3.1f, 1.9f), 1);

            HudManager.Instance.StartCoroutine(GetShootEnumerator(finderObject, new Vector2(3.1f, 1.9f), picture).WrapToIl2Cpp());
            finderObject = null;
        }
        cameraButton.Timer = cameraButton.MaxTimer;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (cameraButton != null)
        {
            cameraButton.Destroy();
        }
        cameraButton = new CustomButton(
            OnFireFilmButton,
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                cameraButton.Timer = cameraButton.MaxTimer;
            },
            cameraSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.film"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        cameraButton.MaxTimer = shootCoolDownOption.getFloat();
    }

    public override void MyUpdate()
    {
        base.MyUpdate();

        if (!(cameraButton.Timer > 0f) && !PlayerControl.LocalPlayer.Data.IsDead && !MeetingHud.Instance)
        {
            if (!finderObject)
            {
                finderObject = GameObject.Instantiate(AssetLoader.CameraFinderPrefab);
                var collider = finderObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(3.1f, 1.9f);
                var button = finderObject.SetUpButton(OnFireFilmButton);


                finderObject.transform.position = PlayerControl.LocalPlayer.transform.position;
                finderObject.transform.localScale = Vector3.zero;
            }
            var myData = Game.GameData.data.myData.getGlobalData();
            var lastPos = finderObject.transform.position;
            var distance = myData.MouseDistance;
            if (distance > 2.8f) distance = 2.8f;
            var goalPos = PlayerControl.LocalPlayer.transform.position + distance * new Vector3(Mathf.Cos(myData.MouseAngle), Mathf.Sin(myData.MouseAngle));
            var pos = lastPos * 0.82f + goalPos * 0.18f;
            var diff = pos - PlayerControl.LocalPlayer.transform.position;
            var angle = Mathf.Atan2(diff.y, diff.x);
            pos.z = -2f;
            finderObject.transform.position = pos;
            //ある程度近距離なら倍率は一定
            if (distance < 1.5f) distance = 1.5f;
            finderObject.transform.localScale = finderObject.transform.localScale * 0.75f + Vector3.one * ((2.8f - distance) / 1.3f * 0.5f + 0.5f) * 0.25f;
            if(!Input.GetMouseButton(0))finderObject.transform.eulerAngles = new Vector3(0, 0, -90f + angle * 180f / Mathf.PI);
        }
        else
        {
            if (finderObject)
            {
                GameObject.Destroy(finderObject);
                finderObject = null;
            }
        }

        //写真を整列する
       for(int i = 0; i < PicturesHolder.childCount; i++)
        {
            var newPos = PicturesHolder.GetChild(i).transform.localPosition * 0.9f + new Vector3(0.65f * i - 0.4f, -0.36f) * 0.1f;
            newPos.z = i * -0.25f;
            PicturesHolder.GetChild(i).transform.localPosition = newPos;
        }

        //会議中
        if (CanSharePicture) shareCount -= Time.deltaTime;
        
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();
        
        /*
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if(!filmedPlayers.Contains(player.PlayerId)) Patches.PlayerControlPatch.SetPlayerOutline(player, Color.yellow);
        }
        */
        
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Empiric);
        RelatedRoles.Add(Roles.Arsonist);
        RelatedRoles.Add(Roles.Oracle);
        RelatedRoles.Add(Roles.Morphing);
        RelatedRoles.Add(Roles.Painter);
        RelatedRoles.Add(Roles.Banshee);
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);
        IncompleteImageMessage.Initialize();
    }



    public Paparazzo()
        : base("Paparazzo", "paparazzo", RoleColor, RoleCategory.Neutral, Side.Paparazzo, Side.Paparazzo,
             new HashSet<Side>() { Side.Paparazzo }, new HashSet<Side>() { Side.Paparazzo },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.PaparazzoWin },
             true, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
        cameraButton = null;

        Patches.EndCondition.PaparazzoWin.TriggerRole = this;

        pictures = new List<PictureData>();
        activePlayers = new HashSet<byte>();
        filmedPlayers = new HashSet<byte>();
    }
}
