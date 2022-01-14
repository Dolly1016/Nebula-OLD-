using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Events
{
    public class LocalEvent
    {
        static public List<LocalEvent> Events = new List<LocalEvent>();

        public float duration { get; private set; }
        public bool SpreadOverMeeting { get; protected set; }

        public bool CheckTerminal()
        {
            if (duration < 0)
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

        protected LocalEvent(float duration)
        {
            this.duration = duration;
        }

        static public void Update()
        {
            foreach (LocalEvent localEvent in Events)
            {
                localEvent.duration -= Time.deltaTime;
            }

            Events.RemoveAll(e => e.CheckTerminal());
        }

        static public void Activate(LocalEvent localEvent)
        {
            localEvent.OnActivate();
            Events.Add(localEvent);
        }

        static public void Initialize()
        {
            Events.Clear();
        }

        static public void OnMeeting()
        {
            foreach (LocalEvent localEvent in Events)
            {
                if (!localEvent.SpreadOverMeeting)
                {
                    localEvent.duration = -1;
                }
            }

            Events.RemoveAll(e => e.CheckTerminal());
        }
    }
}
