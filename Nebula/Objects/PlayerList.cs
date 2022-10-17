using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;
using BepInEx.IL2CPP.Utils.Collections;

namespace Nebula.Objects
{
    public class PlayerList
    {
        public static PlayerList Instance;

        bool isOpen;
        GameObject listParent;
        Dictionary<byte, Tuple<GameObject,PoolablePlayer>> allPlayers;
        Coroutine? lastCoroutine=null;

        //拡大・縮小時も操作できるようボタンを適切な位置に動かす
        PassiveButton[] changeTargetButtons;
        SpriteRenderer[] changeTargetRenderer;

        public PlayerList(PoolablePlayer playerPrefab)
        {
            listParent = new GameObject("PlayerList");
            listParent.transform.SetParent(HudManager.Instance.gameObject.transform);
            listParent.SetActive(true);
            isOpen = false;

            allPlayers = new Dictionary<byte, Tuple<GameObject, PoolablePlayer>>();

            Sprite sprite = Helpers.loadSpriteFromResources("Nebula.Resources.PlayerMask.png",100f);

            foreach (var p in PlayerControl.AllPlayerControls) {
                GameObject obj = new GameObject(p.name);
                obj.transform.SetParent(listParent.transform);
                obj.layer = LayerExpansion.GetUILayer();
                var mask = obj.AddComponent<SpriteMask>();
                mask.sprite= sprite;
                
                

                var poolable = GameObject.Instantiate(playerPrefab,obj.transform);
                poolable.SetPlayerDefaultOutfit(p);
                poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.SimpleUI);

                poolable.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
                poolable.transform.localPosition = new Vector3(0,-0.2f,0);

                allPlayers.Add(p.PlayerId, new Tuple<GameObject, PoolablePlayer>(obj,poolable));
            }

            SetParentPosition(-3.5f);

            listParent.SetActive(false);

            Instance = this;
        }

        private void SetParentPosition(float y)
        {
            listParent.transform.localPosition = new Vector3(0, y, -10f);
        }
        private float UpdateParentPosition(float goalY)
        {
            float y = listParent.transform.localPosition.y;
            y += (goalY - y) * Time.deltaTime * 4.5f;
            SetParentPosition(y);
            return Mathf.Abs(goalY - y);
        }

        private IEnumerator CoShow()
        {
            if (isOpen) yield break;

            listParent.SetActive(true);

            while (true)
            {
                if (UpdateParentPosition(-2.7f) < 0.005f) break;
                yield return null;
            }
            SetParentPosition(-2.7f);
        }

        public void Show()
        {
            if (lastCoroutine != null) HudManager.Instance.StopCoroutine(lastCoroutine);
            lastCoroutine = HudManager.Instance.StartCoroutine(CoShow().WrapToIl2Cpp());
        }

        private IEnumerator CoClose()
        {
            if (isOpen) yield break;

            while (true)
            {
                if (UpdateParentPosition(-3.5f) < 0.005f) break;
                yield return null;
            }

            listParent.SetActive(false);
        }

        public void Close()
        {
            if (lastCoroutine != null) HudManager.Instance.StopCoroutine(lastCoroutine);
            lastCoroutine=HudManager.Instance.StartCoroutine(CoClose().WrapToIl2Cpp());
        }

        public void ListUpPlayers(Predicate<byte> predicate)
        {
            float x = 0f;
            foreach(var entry in allPlayers)
            {
                if (predicate(entry.Key))
                {
                    entry.Value.Item1.SetActive(true);
                    entry.Value.Item1.transform.localPosition = new Vector3(x, 0, entry.Value.Item1.transform.localPosition.z);
                    x += 0.25f;
                }
                else
                {
                    entry.Value.Item1.SetActive(false);
                    entry.Value.Item1.transform.localPosition = new Vector3(0, 0, 0);
                }
            }

            x -= 0.25f;

            foreach(var tuple in allPlayers.Values)
            {
                if (tuple.Item1.activeSelf)
                {
                    tuple.Item1.transform.localPosition -= new Vector3(x * 0.5f, 0, 0);
                }
            }
        }

        public void SelectPlayer(byte id)
        {
            foreach(var entry in allPlayers)
            {
                entry.Value.Item2.setSemiTransparent(entry.Key != id);
                entry.Value.Item1.transform.localPosition = new Vector3(entry.Value.Item1.transform.localPosition.x, 0, (entry.Key == id) ? -10f: 0f);
            }
        }
    }
}
