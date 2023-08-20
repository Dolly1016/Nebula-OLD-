using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

[NebulaPreLoad(true)]
public class TranslatableTag
{
    static public List<TranslatableTag> AllTag = new();

    public string TranslateKey { get; private set; }
    public string Text => Language.Translate(TranslateKey);
    public int Id { get;private set; }

    
    public static void Load()
    {
        AllTag.Sort((tag1,tag2 )=> tag1.TranslateKey.CompareTo(tag2.TranslateKey));
        for (int i = 0; i < AllTag.Count; i++) AllTag[i].Id = i;
    }

    public TranslatableTag(string translateKey)
    {
        TranslateKey = translateKey;

        if (NebulaPreLoad.FinishedLoading)
            Debug.LogError("[Nebula] Pre-loading has been finished. Translatable tag \"" + TranslateKey + "\" is invalid on current process.");
        else
            AllTag.Add(this);
        
    }

    static public TranslatableTag? ValueOf(int id)
    {
        if(id < AllTag.Count && id>=0)
            return AllTag[id];
        return null;
    }
}
