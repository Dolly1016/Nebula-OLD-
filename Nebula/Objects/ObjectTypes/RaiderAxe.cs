using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes
{
    public class RaidAxe : TypeWithImage
    {
        private const int ANGLE_DIVIDE = 1000000;

        public enum AxeState
        {
            Static,
            Thrown,
            Crashed
        }

        public RaidAxe() : base(5, "RaidAxe", "Nebula.Resources.RaiderAxe.png", false)
        {
            CanSeeInShadow = true;
        }

        public override bool IsBack(CustomObject? obj) { return obj==null ? false : obj.Data[0] != (int)AxeState.Static; }
        public override bool IsFront(CustomObject? obj) { return obj == null ? true : obj.Data[0] == (int)AxeState.Static; }

        private Sprite CrashedSprite;
        private Sprite ThrownSprite;

        private Sprite GetCrashedSprite()
        {
            if (CrashedSprite) return CrashedSprite;
            CrashedSprite = Helpers.loadSpriteFromResources("Nebula.Resources.RaiderAxeCrashed.png", 150f);
            return CrashedSprite;
        }

        private Sprite GetThrownSprite()
        {
            if (ThrownSprite) return ThrownSprite;
            ThrownSprite = Helpers.loadSpriteFromResources("Nebula.Resources.RaiderAxeThrown.png", 150f);
            return ThrownSprite;
        }

        public override void Initialize(CustomObject obj)
        {
            base.Initialize(obj);

            obj.Data = new int[2];
        }

        public void SetAngle(CustomObject obj,float angle)
        {
            obj.Data[1] = (int)(angle * (float)ANGLE_DIVIDE);
        }
        public void UpdateState(CustomObject obj,AxeState state)
        {
            obj.Data[0] = (int)state;
            switch (state)
            {
                case AxeState.Thrown:
                    obj.Renderer.sprite = GetThrownSprite();
                    break;
                case AxeState.Crashed:
                    obj.Renderer.sprite = GetCrashedSprite();
                    obj.GameObject.transform.eulerAngles = new Vector3(0f,0f, (float)obj.Data[1] / (float)ANGLE_DIVIDE);
                    break;
            }
        }

        public override void Update(CustomObject obj)
        {
            switch (obj.Data[0])
            {
                case (int)AxeState.Static:
                    var player = Game.GameData.data.players[obj.OwnerId];
                    var targetPosition = Helpers.playerById(obj.OwnerId).transform.position + new Vector3(0.4f * (float)Math.Cos(player.MouseAngle), 0.4f * (float)Math.Sin(player.MouseAngle));
                    obj.GameObject.transform.position += (targetPosition - obj.GameObject.transform.position) * 0.4f;
                    obj.Renderer.transform.eulerAngles = new Vector3(0f, 0f, (float)(player.MouseAngle * 360f / Math.PI / 2f));
                    if (Math.Cos(player.MouseAngle) < 0.0)
                    {
                        if (obj.Renderer.transform.localScale.y > 0)
                            obj.Renderer.transform.localScale = new Vector3(1f, -1f);
                    }
                    else
                    {
                        if (obj.Renderer.transform.localScale.y < 0)
                            obj.Renderer.transform.localScale = new Vector3(1f, 1f);
                    }

                    if (Helpers.playerById(obj.OwnerId).inVent)
                        obj.GameObject.active = false;
                    else
                        obj.GameObject.active = true;
                    break;
                case (int)AxeState.Thrown:
                    float angle = (float)obj.Data[1] / (float)ANGLE_DIVIDE;
                    Vector2 vec = new Vector2(Mathf.Cos(angle / 180f * Mathf.PI), Mathf.Sin(angle / 180f * Mathf.PI));
                    float d;
                    float c = Roles.Roles.Raider.axeSpeedOption.getFloat() * 4f * Time.deltaTime;
                    if (Helpers.AnyNonTriggersBetween(obj.GameObject.transform.position, vec, c, Constants.ShipAndAllObjectsMask,out d)) 
                    {
                        obj.GameObject.transform.position += (Vector3)(vec * d);
                        UpdateState(obj, AxeState.Crashed);
                    }
                    else
                    {
                        obj.GameObject.transform.position += (Vector3)(vec * c);
                        obj.GameObject.transform.eulerAngles += new Vector3(0, 0,
                            obj.Renderer.transform.localScale.y < 0 ? Time.deltaTime * 2000f : Time.deltaTime * -2000f);
                        
                    }
                    break;
            }
        }
    }
}
