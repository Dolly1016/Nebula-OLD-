using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles
{
    public class GhostRole : Assignable
    {
        public byte id { get; private set; }

        //使用済みロールID
        static private byte maxId = 0;

        protected GhostRole(string name, string localizeName, Color color) :
           base(name, localizeName, color)
        {
            this.id = maxId;
            maxId++;
        }

        public static GhostRole GetRoleById(byte id)
        {
            foreach (GhostRole role in Roles.AllGhostRoles) if (role.id == id) return role;
            return null;
        }
    }
}
