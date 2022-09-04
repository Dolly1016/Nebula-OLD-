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
            protected static void VisibleObjectUpdate(CustomObject obj)
            {
                if (obj.PassedMeetings == 0)
                {
                    if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo)
                    {
                        if (obj.Renderer.color.a != 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
                    }
                }
                else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            }

            static public Dictionary<byte, Type> AllTypes = new Dictionary<byte, Type>();

            public static ObjectTypes.VisibleTrap AccelTrap = new ObjectTypes.VisibleTrap(0, "AccelTrap", "Nebula.Resources.AccelTrap.png");
            public static ObjectTypes.VisibleTrap DecelTrap = new ObjectTypes.VisibleTrap(1, "DecelTrap", "Nebula.Resources.DecelTrap.png");
            public static ObjectTypes.KillTrap KillTrap = new ObjectTypes.KillTrap(2, "KillTrap", "Nebula.Resources.KillTrap.png");
            public static ObjectTypes.InvisibleTrap CommTrap = new ObjectTypes.InvisibleTrap(3, "CommTrap", "Nebula.Resources.CommTrap.png");
            public static ObjectTypes.SniperRifle Rifle = new ObjectTypes.SniperRifle();
            public static ObjectTypes.RaidAxe Axe = new ObjectTypes.RaidAxe();
            public static ObjectTypes.ElecPole ElecPole = new ObjectTypes.ElecPole();
            public static ObjectTypes.ElecPoleGuide ElecPoleGuide = new ObjectTypes.ElecPoleGuide();

            protected bool isBack { get; set; }
            protected bool isFront { get; set; }

            public byte Id { get; }
            public string ObjectName { get; }

            public virtual bool IsBack(CustomObject? obj) { return isBack; }
            public virtual bool IsFront(CustomObject? obj) { return isFront; }

            public bool canSeeInShadow { get; set; }
            public virtual bool CanSeeInShadow(CustomObject? obj) { return canSeeInShadow; }

            protected void FixZPosition(CustomObject obj)
            {
                Vector3 position = obj.GameObject.transform.position;
                Vector3 pos = new Vector3(position.x, position.y, 0f);
                if (IsBack(obj)) pos += new Vector3(0, 0, position.y / 1000f + 0.001f);
                else if (IsFront(obj)) pos += new Vector3(0, 0, position.y / 1000f - 1f);
                obj.GameObject.transform.position = pos;
            }

            public virtual void Update(CustomObject obj) { }
            public virtual void Update(CustomObject obj,int command) { }
            public virtual void Initialize(CustomObject obj) { }

            public Type(byte id,string objectName,bool isBack = true)
            {
                Id = id;
                this.ObjectName = objectName;

                this.isBack = isBack;
                isFront = false;
                canSeeInShadow = false;

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
        public int[] Data { get; set; }

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
            if (type.IsBack(null)) pos += new Vector3(0,0, position.y/1000f + 0.001f);
            else if (type.IsFront(null)) pos += new Vector3(0, 0, position.y / 1000f - 1f);
            GameObject.transform.position = pos;
            Renderer = GameObject.AddComponent<SpriteRenderer>();

            Data = new int[0];

            PassedMeetings = 0;

            ObjectType.Initialize(this);

            if (ObjectType.CanSeeInShadow(this)) GameObject.layer = LayerMask.NameToLayer("Objects");

            if (Objects.ContainsKey(id)) Objects[id].Destroy();
            Objects[id] = this;
        }

        public static CustomObject CreatePrivateObject(Type type, Vector3 position)
        {
            ulong id;
            while (true)
            {
                id = (ulong)NebulaPlugin.rnd.Next(64);
                if (!Objects.ContainsKey((id + (ulong)PlayerControl.LocalPlayer.PlayerId * 64))) break;
            }
            return new CustomObject(PlayerControl.LocalPlayer.PlayerId, type, id, position);
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

        //コマンドを受け付けた際のアップデート
        public void Update(int command)
        {
            ObjectType.Update(this, command);
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
