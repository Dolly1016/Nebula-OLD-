using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Objects
{
    public class CustomObject
    {
        public class Type
        {
            static public Dictionary<byte, Type> AllTypes = new Dictionary<byte, Type>();

            static public Type AccelTrap = new Type("AccelTrap", "Nebula.Resources.AccelTrap.png",(obj)=> {},(obj)=> {
                if (obj.PassedMeetings == 0)
                {
                    if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        if (obj.Renderer.color.a != 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
                    }
                }
                else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            });
            static public Type DecelTrap = new Type("DecelTrap", "Nebula.Resources.DecelTrap.png", (obj) => { }, (obj) => {
                if (obj.PassedMeetings == 0)
                {
                    if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        if (obj.Renderer.color.a != 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
                    }
                }
                else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            });
            static public Type KillTrap = new Type("KillTrap", "Nebula.Resources.KillTrap.png", (obj) => { }, (obj) => {
                if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId) obj.GameObject.active = false;
                if (obj.PassedMeetings == 0)
                {
                    if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
                }
                else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            });
            static public Type CommTrap = new Type("CommTrap", "Nebula.Resources.CommTrap.png", (obj) => { }, (obj) => {
                if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId) obj.GameObject.active = false;
                if (obj.PassedMeetings == 0)
                {
                    if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
                }
                else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            });

            static private byte AvailableId = 0;

            public byte Id { get; }
            public string ObjectName { get; }
            private Sprite Sprite { get; set; }
            public string SpriteAddress { get; set; }

            public bool IsBack { get; set; }
            public bool IsFront { get; set; }

            public Sprite GetSprite()
            {
                if (Sprite) return Sprite;
                Sprite = Helpers.loadSpriteFromResources(SpriteAddress, 150f);
                return Sprite;
            }

            public Action<CustomObject> UpdateFunction;
            public Action<CustomObject> SetUpFunction;

            public Type(string objectName, string spriteAddress, Action<CustomObject> setUp, Action<CustomObject> updater,bool isBack = true)
            {
                Id = AvailableId;
                AvailableId++;
                this.ObjectName = objectName;
                Sprite = null;
                SpriteAddress = spriteAddress;
                UpdateFunction = updater;
                SetUpFunction = setUp;

                IsBack = isBack;

                AllTypes.Add(Id,this);
            }
        }

        public static Dictionary<ulong,CustomObject> Objects=new Dictionary<ulong, CustomObject>();
        public static HashSet<System.Action<PlayerControl>> ObjectUpdateFunctions = new HashSet<Action<PlayerControl>>();
        public GameObject GameObject { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public byte OwnerId { get; set; }
        public Type ObjectType { get; }
        public ulong Id { get; }
        public int PassedMeetings { get; set; }

        static public void RegisterUpdater(Action<PlayerControl> action)
        {
            ObjectUpdateFunctions.Add(action);

        }

        static public void OnMeetingEnd()
        {
            foreach(CustomObject obj in Objects.Values)
            {
                obj.PassedMeetings++;
            }
        }

        public CustomObject(byte ownerId,Type type,ulong id,Vector3 position)
        {
            ObjectType = type;
            GameObject = new GameObject(type.ObjectName);
            Id = id;
            OwnerId = ownerId;

            Vector3 pos = new Vector3(position.x, position.y, 0f);
            if (type.IsBack) pos += new Vector3(0,0,7f);
            GameObject.transform.position = pos;
            Renderer = GameObject.AddComponent<SpriteRenderer>();
            Renderer.sprite = type.GetSprite();

            PassedMeetings = 0;

            type.SetUpFunction.Invoke(this);

            if (Objects.ContainsKey(id)) Objects[id].Destroy();
            Objects[id] = this;
        }

        public static void Update()
        {
            //オブジェクトに対するアップデート関数
            foreach(CustomObject obj in Objects.Values)
            {
                obj.ObjectType.UpdateFunction.Invoke(obj);
            }

            //オブジェクト群にたいするプレイヤーのアップデート関数
            foreach(Action<PlayerControl> action in ObjectUpdateFunctions)
            {
                action.Invoke(PlayerControl.LocalPlayer);
            }
        }

        public static CustomObject GetTarget(float distance, PlayerControl player,Predicate<CustomObject> condition, params Type[] targetType)
        {
            CustomObject result = null;
            float num;
            foreach(CustomObject obj in Objects.Values)
            {
                if (!targetType.Contains<Type>(obj.ObjectType)) continue;
                if (!condition.Invoke(obj)) continue;
                num= player.transform.position.Distance(obj.GameObject.transform.position);
                if (num < distance)
                {
                    distance = num;
                    result = obj;
                }
            }
            return result;
        }

        public static CustomObject GetTarget(float distance, PlayerControl player, params Type[] targetType)
        {
            CustomObject result = null;
            float num;
            foreach (CustomObject obj in Objects.Values)
            {
                if (!targetType.Contains<Type>(obj.ObjectType)) continue;
                num = player.transform.position.Distance(obj.GameObject.transform.position);
                if (num < distance)
                {
                    distance = num;
                    result = obj;
                }
            }
            return result;
        }


        public void Destroy()
        {
            UnityEngine.Object.Destroy(GameObject);
            Objects.Remove(this.Id);
        }

        static public void Initialize()
        {
            foreach(CustomObject co in Objects.Values)
            {
                UnityEngine.Object.Destroy(co.GameObject);
            }
            Objects.Clear();
        }
    }
}
