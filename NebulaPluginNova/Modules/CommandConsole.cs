using Nebula.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Modules;

public class CommandConsole
{
    TextField myInput;
    GameObject consoleObject;
    public bool IsShown { get => consoleObject.active; set => consoleObject.SetActive(value); }

    public CommandConsole()
    {
        consoleObject = UnityHelper.CreateObject("CommandConsole", UnityHelper.FindCamera(LayerExpansion.GetUILayer())!.transform, new Vector3(-2.15f, -2.8f, -800f), LayerExpansion.GetUILayer());

        Vector2 size = new Vector2(6f, 0.225f);

        myInput = UnityHelper.CreateObject<TextField>("InputField", consoleObject.transform, new Vector3(0, 0, -1f));
        myInput.SetSize(size,1.6f);

        var backGround = UnityHelper.CreateObject<SpriteRenderer>("Background", myInput.transform, new Vector3(0, 0, 1f));
        backGround.sprite = NebulaAsset.SharpWindowBackgroundSprite.GetSprite();
        backGround.drawMode = SpriteDrawMode.Sliced;
        backGround.tileMode = SpriteTileMode.Continuous;
        backGround.size = size + new Vector2(0.15f, 0.008f);
        backGround.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    }
}
