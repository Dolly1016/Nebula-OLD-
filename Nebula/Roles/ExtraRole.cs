using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles
{
    public class ExtraRole : Assignable
    {
        public byte id { get; private set; }
  
        //使用済みロールID
        static private byte maxId = 0;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/


        protected ExtraRole(string name, string localizeName, Color color,
            HashSet<Patches.EndCondition> winReasons) :
            base(name, localizeName, color, winReasons)
        {
            this.id = maxId;
            maxId++;
        }

        public static ExtraRole GetRoleById(byte id)
        {
            foreach (ExtraRole role in Roles.AllExtraRoles)
            {
                if (role.id == id)
                {
                    return role;
                }
            }
            return null;
        }

        static public void LoadAllOptionData()
        {
            foreach (ExtraRole role in Roles.AllExtraRoles)
            {
                if (!role.IsHideRole)
                {
                    role.SetupRoleOptionData();
                    role.LoadOptionData();
                }
            }
        }
    }
}
