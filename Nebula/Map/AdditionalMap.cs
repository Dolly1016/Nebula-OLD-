using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnityEngine;
using Hazel;


namespace Nebula.Map
{
    public class AdditionalMap
    {
        private static byte AvailableId = 5;

        public string MapName { get; }
        public byte BaseMapId { get; }
        public byte MapId { get; }


        public AdditionalMap(string mapName, byte baseMapId)
        {
            MapId = AvailableId;
            AvailableId++;

            MapName = mapName;
            BaseMapId = baseMapId;
        }
    }

    public class AdditionalMapManager
    {
        static public AdditionalMap Remnants;

        static public List<AdditionalMap> AdditionalMaps = new List<AdditionalMap>();

        static public void Load()
        {
            Remnants = new AdditionalMap("Remnants", 2);

            //AdditionalMaps.Add(Remnants);

            var list = Constants.MapNames.ToList();
            foreach (var additionalMap in AdditionalMaps)
            {
                list.Add(additionalMap.MapName);
            }
            Constants.MapNames = new UnhollowerBaseLib.Il2CppStringArray(list.ToArray());
        }

        static public void AddPrefabs(AmongUsClient client)
        {
            foreach (var additionalMap in AdditionalMaps)
                client.ShipPrefabs.Add(client.ShipPrefabs[additionalMap.BaseMapId]);
        }
    }
}
