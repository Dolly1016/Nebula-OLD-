using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.ScriptComponents;

public class Arrow : INebulaScriptComponent
{
    private SpriteRenderer? arrowRenderer;
    public Vector2 TargetPos;

    public bool IsAffectedByComms { get; set; } = false;
    public bool IsSmallenNearPlayer { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool FixedAngle { get; set; } = false;
    public bool IsDisappearing { get; set; } = false;

    private static SpriteLoader arrowSprite = SpriteLoader.FromResource("Nebula.Resources.Arrow.png", 185f);

    public Arrow(Sprite? sprite = null, bool usePlayerMaterial = true)
    {
        arrowRenderer = UnityHelper.CreateObject<SpriteRenderer>("Arrow", HudManager.Instance.transform, new Vector3(0, 0, -10f), LayerExpansion.GetUILayer());
        arrowRenderer.sprite = sprite ?? arrowSprite.GetSprite();
        arrowRenderer.sharedMaterial = usePlayerMaterial ? HatManager.Instance.PlayerMaterial : HatManager.Instance.DefaultShader;
        if (usePlayerMaterial) SetColor(Color.white, Color.gray);
    }

    public Arrow SetColorByOutfit(GameData.PlayerOutfit outfit)
    {
        return SetColor(Palette.PlayerColors[outfit.ColorId], Palette.ShadowColors[outfit.ColorId]);
    }

    public Arrow SetColor(Color mainColor, Color shadowColor)
    {
        arrowRenderer?.material.SetColor(PlayerMaterial.BackColor, shadowColor);
        arrowRenderer?.material.SetColor(PlayerMaterial.BodyColor, mainColor);
        return this;
    }

    public Arrow SetColor(Color mainColor) => SetColor(mainColor, mainColor * 0.65f);

    public override void OnReleased()
    {
        if (arrowRenderer) GameObject.Destroy(arrowRenderer!.gameObject);
        arrowRenderer = null;
    }

    private static float perc = 0.925f;
    public override void Update()
    {
        if (!arrowRenderer) return;

        //視点中心からのベクトル
        Camera main = Camera.main;
        Vector2 vector = TargetPos - (Vector2)main.transform.position;
        float num = vector.magnitude / (main.orthographicSize * perc);

        //近くの矢印を隠す
        bool flag = IsActive && (!IsSmallenNearPlayer || (double)num > 0.3);
        arrowRenderer!.gameObject.SetActive(flag);
        if (!flag) return;

        bool Between(float value, float min, float max) => value > min && value < max;
        Vector2 viewportPoint = main.WorldToViewportPoint(TargetPos);
        if (Between(viewportPoint.x, 0f, 1f) && Between(viewportPoint.y, 0f, 1f))
        {
            //画面内を指す矢印

            arrowRenderer.transform.localPosition = vector - vector.normalized * 0.6f;
            arrowRenderer.transform.localScale = IsSmallenNearPlayer ? Vector3.one * Mathf.Clamp(num, 0f, 1f) : Vector3.one;
        }
        else
        {
            //画面外を指す矢印
            Vector2 vector3 = new Vector2(Mathf.Clamp(viewportPoint.x * 2f - 1f, -1f, 1f), Mathf.Clamp(viewportPoint.y * 2f - 1f, -1f, 1f));
            float orthographicSize = main.orthographicSize;
            float num3 = main.orthographicSize * main.aspect;
            Vector3 vector4 = new Vector3(Mathf.LerpUnclamped(0f, num3 * 0.88f, vector3.x), Mathf.LerpUnclamped(0f, orthographicSize * 0.79f, vector3.y), 0f);
            arrowRenderer.transform.localPosition = vector4;
            arrowRenderer.transform.localScale = Vector3.one;
        }

        vector.Normalize();

        if(FixedAngle)
            arrowRenderer.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        else
            arrowRenderer.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Atan2(vector.y, vector.x) * 180f / Mathf.PI);

        if (IsDisappearing)
        {
            float a = arrowRenderer.color.a;
            a -= Time.deltaTime * 0.85f;
            if (a < 0f)
            {
                Release();
                a = 0f;
            }
            arrowRenderer.color = new Color(arrowRenderer.color.r, arrowRenderer.color.g, arrowRenderer.color.b, a);
        }
    }

    public void MarkAsDisappering()
    {
        IsDisappearing = true;
    }

    public IEnumerator CoWaitAndDisappear(float waiting)
    {
        yield return new WaitForSeconds(waiting);
        IsDisappearing = true;
    }
}
