﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula
{
    /// <summary>
    /// 自身が実行するメソッド
    /// </summary>
    public class RoleLocalMethod : Attribute
    {
    }

    /// <summary>
    /// 全プレイヤーが実行するメソッド
    /// </summary>
    public class RoleGlobalMethod : Attribute
    {
    }

    /// <summary>
    /// 実行者不定のメソッド
    /// </summary>
    public class RoleIndefiniteMethod : Attribute
    {
    }
}
