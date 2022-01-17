using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Events
{
    class Schedule
    {
        static List<System.Action> PreMeetingActions = new List<Action>();
        static List<System.Action> PostMeetingActions = new List<Action>();

        public static void Initialize()
        {
            PreMeetingActions.Clear();
            PostMeetingActions.Clear();
        }

        public static void RegisterPreMeetingAction(Action action)
        {
            PreMeetingActions.Add(action);
        }

        public static void RegisterPostMeetingAction(Action action)
        {
            PostMeetingActions.Add(action);
        }

        public static void OnPreMeeting()
        {
            foreach(Action action in PreMeetingActions)
            {
                action.Invoke();
            }
            PreMeetingActions.Clear();
        }

        public static void OnPostMeeting()
        {
            foreach (Action action in PostMeetingActions)
            {
                action.Invoke();
            }
            PostMeetingActions.Clear();
        }
    }
}
