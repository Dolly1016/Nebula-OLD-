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

            public static ObjectTypes.VisibleTrap AccelTrap = new ObjectTypes.VisibleTrap(0, "AccelTrap", "Nebula.Resources.AccelTrap.png");
            public static ObjectTypes.VisibleTrap DecelTrap = new ObjectTypes.VisibleTrap(1, "DecelTrap", "Nebula.Resources.DecelTrap.png");
            public static ObjectTypes.InvisibleTrap KillTrap = new ObjectTypes.InvisibleTrap(2, "KillTrap", "Nebula.Resources.KillTrap.png");
            public static ObjectTypes.InvisibleTrap CommTrap = new ObjectTypes.InvisibleTrap(3, "CommTrap", "Nebula.Resources.CommTrap.png");
            public static ObjectTypes.SniperRifle Rifle = new ObjectTypes.SniperRifle();

            public byte Id { get; }
            public string ObjectName { get; }

            public bool IsBack { get; set; }
            public bool IsFront { get; set; }
            public virtual void Update(CustomObject obj) { }
            public virtual void Initialize(CustomObject obj) { }

            public Type(byte id,string objectName,bool isBack = true)
            {
                Id = id;
                this.ObjectName = objectName;

                IsBack = isBack;

                AllTypes.Add(Id,this);
            }
        }

        public static Dictionary<ulong,CustomObject> Objects=new Dictionary<ulong, CustomObject>();
        public static HashSet<System.Action<PlayerControl>> ObjectUpdateFunctions = new HashSet<Action<PlayerControl>>();
        public static Dictionary<Type, Func<CustomObject>> Constructors = new Dictionary<Type, Func<CustomObject>>();
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
            if (type.IsBack) pos += new Vector3(0,0, position.y/1000f + 1f);
            else pos += new Vector3(0, 0, position.y / 1000f - 1f);
            GameObject.transform.position = pos;
            Renderer = GameObject.AddComponent<SpriteRenderer>();

            PassedMeetings = 0;

            ObjectType.Initialize(this);

            if (Objects.ContainsKey(id)) Objects[id].Destroy();
            Objects[id] = this;
        }

        public static void Update()
        {
            //オブジェクトに対するアップデート関数
            foreach(CustomObject obj in Objects.Values)
            {
                obj.ObjectType.Update(obj);
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

        static public void Load()
        {

        }
    }
}
