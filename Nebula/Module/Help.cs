using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;

namespace Nebula.Module;

public class HelpContent
{
    static public HelpContent rootContent;

    List<HelpContent>? subContents;
    public HelpContent? Parent { get; private set; }
    public float occupancy { get; private set; }

    public Action<MetaScreen.MSDesigner>? ContentGenerator { get; set; }

    public HelpContent(HelpContent? parent)
    {
        Parent = parent ?? null;
        subContents = null;
        ContentGenerator = null;
        occupancy = 1f;

        if (Parent != null)
        {
            if (Parent.subContents == null) Parent.subContents = new List<HelpContent>();
            Parent.subContents.Add(this);
        }
    }

    private void Load(JToken jtkn)
    {
        void LoadSubContent(JArray jary)
        {
            if (jary == null || jary.Count == 0) return;

            for (JToken current = jary[0]; current != null; current = current.Next)
            {
                HelpContent child = new HelpContent(this);
                child.Load(current);
            }
        }

        if (jtkn.Type == JTokenType.Array)
        {
            JArray jary = jtkn.Cast<JArray>();

            if (jary.Count == 2)
            {
                var val = jary[0];
                if (val.Type is JTokenType.Float or JTokenType.Integer)
                {
                    occupancy = float.Parse(val.ToString());
                    //占有率指定付きコンテンツ
                    Load(jary[1]);
                    return;
                }
            }

            //Topic形式
            LoadSubContent(jary);

            if (subContents == null) return;
            float[] rates = new float[subContents.Count];
            for (int i = 0; i < rates.Length; i++) rates[i] = subContents[i].occupancy;

            ContentGenerator = (designer) =>
            {
                var designers = designer.SplitVertically(rates);
                for (int i = 0; i < rates.Length; i++)
                {
                    var subContent = subContents[i];
                    if (subContent.ContentGenerator != null) subContent.ContentGenerator(designers[i]);
                }
                float maxUses = 0f;
                foreach (var d in designers) if (maxUses < d.Used) maxUses = d.Used;
                designer.CustomUse(maxUses);
            };
        }
        else if (jtkn.Type == JTokenType.Object)
        {
            JObject jobj = jtkn.Cast<JObject>();
            string langKey = "";
            if (jobj.ContainsKey("caption")) langKey = jobj["caption"].ToString();

            if (jobj.ContainsKey("subcontents"))
            {
                //ボタンコンテンツ

                LoadSubContent(jobj["subcontents"].Cast<JArray>());

                Vector2 size = new Vector2(7.5f, 5f);

                if (jobj.ContainsKey("size") && jobj["size"].Type == JTokenType.Array)
                {
                    JArray jSize = jobj["size"].Cast<JArray>();
                    size = new Vector2(float.Parse(jSize[0].ToString()), float.Parse(jSize[1].ToString()));
                }


                if (Parent == null)
                {
                    ContentGenerator = (designer) =>
                    {
                        if (subContents == null) return;
                        foreach (var content in subContents)
                        {
                            if (content.ContentGenerator != null) content.ContentGenerator(designer);
                        }
                    };
                }
                else
                {
                    ContentGenerator = (designer) =>
                    {
                        string text = Language.Language.GetString(langKey);
                        designer.AddTopic(new MSButton(designer.size.x - 0.4f, 0.4f, text, TMPro.FontStyles.Normal, () =>
                        {
                            var dialog = MetaDialog.OpenDialog(size, text);
                            foreach (var content in subContents)
                            {
                                if (content.ContentGenerator != null) content.ContentGenerator(dialog);
                            }
                        }));
                    };
                }
            }
            else if (jobj.ContainsKey("picture"))
            {
                //画像コンテンツ

                string pic = "Nebula.Resources." + jobj["picture"].ToString() + ".png";
                float size = 1f;
                if (jobj.ContainsKey("size")) size = float.Parse(jtkn["size"].ToString());

                ContentGenerator = (designer) =>
                {
                    designer.CustomUse(0.15f);
                    Utilities.SpriteLoader sprite = new Utilities.SpriteLoader(pic, 100f);
                    designer.AddTopic(new MSSprite(sprite, 0.1f, size));
                };
            }
            else if (jobj.ContainsKey("destination"))
            {
                //リンクコンテンツ

                string dest = jtkn["destination"].ToString();
            }
            else if (jobj.ContainsKey("margin"))
            {
                //余白コンテンツ
                float margin = float.Parse(jtkn["margin"].ToString());

                ContentGenerator = (designer) =>
                {
                    designer.AddTopic(new MSMargin(margin));
                    designer.CustomUse(margin);
                };
            }
        }
        else
        {
            //複数行文字列形式
            string langKey = jtkn.ToString();

            ContentGenerator = (designer) =>
            {
                designer.AddTopic(new MSMultiString(designer.size.x - 0.2f, 1.8f, Language.Language.GetString(langKey), TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal));
            };
        }
    }

    public static void Load()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Help.dat");
        using (StreamReader sr = new StreamReader(
                stream, Encoding.GetEncoding("utf-8")))
        {
            string text = sr.ReadToEnd();
            JToken jobj = JObject.Parse(text);

            rootContent = new HelpContent(null);
            rootContent.Load(jobj);
        }
    }
}