using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Events
{
    public delegate GlobalEvent GlobalEventGenerator(float duration);
    public class GlobalEvent
    {
        static public HashSet<GlobalEvent> Events=new HashSet<GlobalEvent>();
        static private Dictionary<Type, GlobalEventGenerator> Generators = new Dictionary<Type, GlobalEventGenerator>();

        public class Type
        {
            static private byte availableId=0;
            static public Type Camouflage = new Type();

            public byte Id { get; }

            private Type()
            {
                Id = availableId;
                availableId++;
            }

            public static HashSet<Type> AllTypes = new HashSet<Type>()
            {
                Camouflage
            };

            public static Type GetType(byte id)
            {
                foreach(Type type in AllTypes)
                {
                    if (type.Id == id)
                    {
                        return type;
                    }
                }
                return null;
            }
        }

        public Type type {get;}
        public float duration { get; private set; }

        public bool CheckTerminal()
        {
            if(duration < 0)
            {
                OnTerminal();
                return true;
            }
            return false;
        }

        public virtual void OnTerminal()
        {

        }

        public virtual void OnActivate()
        {

        }

        protected GlobalEvent(Type type,float duration)
        {
            this.type = type;
            this.duration = duration;
        }

        static public bool IsActive(Type type)
        {
            foreach (GlobalEvent globalEvent in Events)
            {
                if (globalEvent.type != type) continue;
                if (globalEvent.duration > 0) return true;
            }
            return false;
        }

        static public void Update()
        {
            foreach(GlobalEvent globalEvent in Events)
            {
                globalEvent.duration -= Time.deltaTime;
            }

            Events.RemoveWhere(e => e.CheckTerminal());
        }

        static public bool Activate(GlobalEvent.Type type,float duration)
        {
            if (Generators.ContainsKey(type))
            {
                GlobalEvent e = Generators[type](duration);
                e.OnActivate();
                Events.Add(e);
                return true;
            }
            return false;
        }

        static public void Register(GlobalEvent.Type type,GlobalEventGenerator generator)
        {
            Generators.Add(type,generator);
        }

        static public void Initialize()
        {
            Events.Clear();
        }
    }
}
