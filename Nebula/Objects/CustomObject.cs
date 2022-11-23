using UnhollowerRuntimeLib;
using UnhollowerBaseLib.Attributes;

namespace Nebula.Objects
{
    public class CustomObjectBehaviour : MonoBehaviour
    {
        static CustomObjectBehaviour()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomObjectBehaviour>();
        }
        
        public void OnDestroy()
        {
            if (HudManager.Instance.PlayerCam.Target == this)
            {
                HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
            }
        }
    }

    [Il2CppImplementsAttribute(typeof(IUsable))]
    public class UsableCustomObjectBehaviour : MonoBehaviour
    {
        static UsableCustomObjectBehaviour()
        {
            ClassInjector.RegisterTypeInIl2Cpp<UsableCustomObjectBehaviour>();
        }

        public float UsableDistance { get => 0.8f; }
        public float PercentCool { get => 0f; }

        public ImageNames UseIcon { get => ImageNames.UseButton; }

        public void SetOutline(bool on, bool mainTarget)
        {
            if (this.CustomObject.Renderer)
            {
                CustomObject.Renderer.material.SetFloat("_Outline", (float)(on ? 1 : 0));
                CustomObject.Renderer.material.SetColor("_OutlineColor", Color.white);
                CustomObject.Renderer.material.SetColor("_AddColor", mainTarget ? Color.white : Color.clear);
            }
        }


        public float CanUse(GameData.PlayerInfo pc, out bool canUse, out bool couldUse)
        {
            float num = float.MaxValue;
            PlayerControl @object = pc.Object;
            Vector2 truePosition = @object.GetTruePosition();
            Vector3 position = base.transform.position;
            couldUse = (CustomObject.ObjectType.CanUse(CustomObject, PlayerControl.LocalPlayer) && @object.CanMove);
            canUse = couldUse;
            if (canUse)
            {
                num = Vector2.Distance(truePosition, base.transform.position);
                canUse &= (num <= this.UsableDistance);
                canUse &= !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShadowMask, false);
            }
            return num;
        }


        public void Use()
        {
            bool flag;
            bool flag2;
            this.CanUse(PlayerControl.LocalPlayer.Data, out flag, out flag2);
            if (!flag)
            {
                return;
            }
            CustomObject.ObjectType.Use(CustomObject);
        }

        public CustomObject CustomObject;
    }

    public class CustomObject
    {
        public const int MAX_PLAYER_OBJECTS = 0xFFFFFF;
        public enum ObjectOrder
        {
            IsBack,
            IsFront,
            IsOnSameRow
        }

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
            public static ObjectTypes.Decoy Decoy = new ObjectTypes.Decoy();
            public static ObjectTypes.DelayedObject Antenna = new ObjectTypes.DelayedObject(9, "Antenna", "Nebula.Resources.Antenna.png");
            public static ObjectTypes.Diamond Diamond = new ObjectTypes.Diamond();
            public static ObjectTypes.Footprint Footprint = new ObjectTypes.Footprint();

            public byte Id { get; }
            public string ObjectName { get; }

            public virtual ObjectOrder GetObjectOrder(CustomObject? obj) { return ObjectOrder.IsBack; }

            public bool canSeeInShadow { get; set; }
            public virtual bool CanSeeInShadow(CustomObject? obj) { return canSeeInShadow; }
            public virtual bool RequireMonoBehaviour { get { return false; } }
            protected void FixZPosition(CustomObject obj)
            {
                Vector3 position = obj.GameObject.transform.position;
                Vector3 pos = new Vector3(position.x, position.y, position.y / 1000f);
                switch (GetObjectOrder(obj))
                {
                    case ObjectOrder.IsBack:
                        pos += new Vector3(0, 0, 0.001f);
                        break;
                    case ObjectOrder.IsFront:
                        pos += new Vector3(0, 0, -1f);
                        break;
                }
                obj.GameObject.transform.position = pos;
            }

            public virtual void Update(CustomObject obj) { }
            public virtual void Update(CustomObject obj,int command) { }
            public virtual void Initialize(CustomObject obj) { }

            public virtual bool IsUsable { get => false; }
            public virtual Color UsableColor { get => Color.white; }
            public virtual bool CanUse(CustomObject obj, PlayerControl player) { return true; }
            public virtual void Use(CustomObject obj) { }

            public Type(byte id,string objectName)
            {
                Id = id;
                this.ObjectName = objectName;

                canSeeInShadow = false;

                AllTypes.Add(Id,this);
            }
        }

        public static Dictionary<ulong,CustomObject> Objects=new Dictionary<ulong, CustomObject>();
        public static HashSet<System.Action<PlayerControl>> ObjectUpdateFunctions = new HashSet<Action<PlayerControl>>();
        public static Dictionary<Type, Func<CustomObject>> Constructors = new Dictionary<Type, Func<CustomObject>>();
        public GameObject? GameObject { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public byte OwnerId { get; set; }
        public Type ObjectType { get; }
        public ulong Id { get; }
        public int PassedMeetings { get; set; }
        public int[] Data { get; set; }

        static public implicit operator bool(CustomObject obj) { return obj.GameObject == null || obj.GameObject; }

        public CustomObjectBehaviour? Behaviour { get; private set; }
        public UsableCustomObjectBehaviour? UsableBehaviour { get; private set; }

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

            Vector3 pos = new Vector3(position.x, position.y, position.y / 1000f);
            switch (type.GetObjectOrder(null))
            {
                case ObjectOrder.IsBack:
                    pos += new Vector3(0, 0, 0.001f);
                    break;
                case ObjectOrder.IsFront:
                    pos += new Vector3(0, 0, -1f);
                    break;
            }
            GameObject.transform.position = pos;
            Renderer = GameObject.AddComponent<SpriteRenderer>();
            Renderer.material = new Material(ShipStatus.Instance.AllConsoles[0].Image.material);

            if (type.RequireMonoBehaviour) Behaviour = GameObject.AddComponent<CustomObjectBehaviour>();
            else Behaviour = null;
            if (type.IsUsable)
            {
                var usableObj = new GameObject("UsableObject");
                usableObj.transform.SetParent(GameObject.transform);
                usableObj.transform.localPosition = new Vector3(0f,0f,0f);
                usableObj.transform.localScale = new Vector3(1f,1f,1f);
                usableObj.layer = LayerExpansion.GetShortObjectsLayer();
                UsableBehaviour = usableObj.AddComponent<UsableCustomObjectBehaviour>();
                var circle = usableObj.AddComponent<CircleCollider2D>();
                circle.radius = UsableBehaviour.UsableDistance;
                circle.isTrigger = true;
                UsableBehaviour.CustomObject = this;
            }
            else UsableBehaviour = null;

            Data = new int[0];

            PassedMeetings = 0;

            ObjectType.Initialize(this);

            if (ObjectType.CanSeeInShadow(this)) GameObject.layer = LayerExpansion.GetObjectsLayer();

            if (Objects.ContainsKey(id)) Objects[id].Destroy();
            Objects[id] = this;
        }

        public static CustomObject CreatePrivateObject(Type type, Vector3 position)
        {
            ulong id;
            while (true)
            {
                id = (ulong)NebulaPlugin.rnd.Next((int)MAX_PLAYER_OBJECTS);
                if (!Objects.ContainsKey((id + (ulong)PlayerControl.LocalPlayer.PlayerId * MAX_PLAYER_OBJECTS))) break;
            }
            return new CustomObject(PlayerControl.LocalPlayer.PlayerId, type, id, position);
        }
        public static void Update()
        {
            //オブジェクトに対するアップデート関数
            foreach(CustomObject obj in Objects.Values)
            {
                if (obj.ObjectType.CanSeeInShadow(obj)) obj.GameObject.layer = LayerExpansion.GetObjectsLayer();
                else obj.GameObject.layer = LayerExpansion.GetDefaultLayer();

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
            if (HudManager.Instance.PlayerCam.Target.gameObject == GameObject)
            {
                HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
            }
            UnityEngine.Object.Destroy(GameObject);
            Objects.Remove(this.Id);
            GameObject = null;
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

        static public CustomObject? GetObject(ulong id)
        {
            if (Objects.ContainsKey(id))
            {
                return Objects[id];
            }
            return null;
        }
    }
}
