using Nebula.Module;
using Nebula.Patches;

namespace Nebula;

public static class MeetingHudExpansion
{
    public class DisclosedPicture
    {
        public Sprite picture { get; private set; }
        public float angle { get; private set; }
        public DisclosedPicture(Texture2D picture,float angle)
        {
            

            this.picture = Helpers.loadSpriteFromResources(picture, 100f, new Rect(0, 0, picture.width, picture.height));
            this.picture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            this.angle = angle;

            AllPictures.Add(this);
            GeneratePicture(AllPictures.Count - 1);
        }

        public void GeneratePicture(int index)
        {
            if (!MeetingHud.Instance) return;

            GameObject obj = new GameObject("Picture");
            obj.transform.SetParent(MeetingHud.Instance.transform);
            obj.layer = LayerExpansion.GetUILayer();
            obj.transform.localPosition = new Vector3(-4.8f, 1.8f - 1.2f*(float)index, -40f);
            obj.transform.localScale = new Vector3(0f, 0f);
            obj.transform.localEulerAngles = new Vector3(0, 0, angle);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite=picture;
            var frameObj = GameObject.Instantiate(AssetLoader.CameraFinderPrefab.transform.GetChild(0).gameObject);
            frameObj.transform.SetParent(obj.transform);
            frameObj.transform.localScale = new Vector3(1, 1);
            frameObj.transform.localEulerAngles = new Vector3(0, 0, 0);
            frameObj.transform.localPosition = new Vector3(0, 0, -1);
            frameObj.GetComponent<SpriteRenderer>().size = renderer.size;

            IEnumerator GetEnumerator() {
                float p = 0f;
                while (p < 1f) {
                    float r = 1f - p;
                    r = 1 - Mathf.Pow(r,5);
                    r *= 0.34f;
                    obj.transform.localScale = new Vector3(r,r);
                    p += Time.deltaTime;
                    yield return null;
                }
                obj.transform.localScale=new Vector3(0.34f,0.34f);
            }
            MeetingHud.Instance.StartCoroutine(GetEnumerator().WrapToIl2Cpp());
        }
    }

    public static List<DisclosedPicture> AllPictures = new();

    public static void Initialize()
    {
        foreach (var p in AllPictures) if(p.picture)GameObject.Destroy(p.picture);
        AllPictures.Clear();
    }

    [HarmonyPatch(typeof(MeetingHud),nameof(MeetingHud.OnDestroy))]
    public class MeetingHudDestroyPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            Initialize();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            
        }
    }

    public static void RecheckPlayerState(this MeetingHud meetingHud)
    {
        bool existsDeadPlayer = false;

        foreach (PlayerVoteArea pva in meetingHud.playerStates)
        {
            bool isDead = !Game.GameData.data.GetPlayerData(pva.TargetPlayerId).IsAlive;
            bool mismatch = pva.AmDead != isDead;

            if (!mismatch) continue;

            pva.SetDead(pva.DidReport, isDead);
            pva.Overlay.gameObject.SetActive(isDead);

            if (isDead)
            {
                foreach (PlayerVoteArea voter in meetingHud.playerStates)
                {
                    if (voter.VotedFor != pva.TargetPlayerId) continue;

                    PlayerControl p = Helpers.playerById(voter.TargetPlayerId);
                    if (p.AmOwner)
                    {
                        meetingHud.ClearVote();
                        Helpers.RoleAction(p, (r) => r.OnVoteCanceled(Patches.MeetingHudPatch.GetVoteWeight(voter.TargetPlayerId)));
                    }

                    voter.ThumbsDown.enabled = false;
                    voter.UnsetVote();
                }
            }
        }
    }
}