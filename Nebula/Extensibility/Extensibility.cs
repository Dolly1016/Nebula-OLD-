using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Extensibility
{
    static public class NebulaManager
    {
        static public void AddRole(Nebula.Roles.Role role)
        {
            Nebula.Roles.Roles.AllRoles.Add(role);
        }
    }

    public interface NebulaExtension
    {
        /// <summary>
        /// ロールのインスタンス化はここで行ってください。
        /// </summary>
        public void NebulaLoad();
    }
}
