using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public interface ITextComponent
{
    string Text { get; }

    public static ITextComponent From(string translationKey, Color color)
    {
        return new ColorTextComponent(color, new TranslateTextComponent(translationKey));
    }
}

public class CombinedComponent : ITextComponent
{
    ITextComponent[] components;

    public CombinedComponent(params ITextComponent[] components)
    {
        this.components = components;
    }

    public string Text { get {
            StringBuilder builder = new();
            foreach (var str in components) builder.Append(str.Text);
            return builder.ToString();
        }
    }
}

public class RawTextComponent : ITextComponent
{
    public string RawText { get; set; }
    public string Text => RawText;

    public RawTextComponent(string text)
    {
        RawText = text;
    }
}

public class LazyTextComponent : ITextComponent
{
    private Func<string> supplier;
    public LazyTextComponent(Func<string> supplier)
    {
        this.supplier = supplier;
    }

    public string Text => supplier.Invoke();
}

public class ColorTextComponent : ITextComponent
{
    public Color Color { get; set; }
    ITextComponent Inner { get; set; }
    public string Text => Inner.Text.Color(Color);
    public ColorTextComponent(Color color, ITextComponent inner)
    {
        Color = color;
        Inner = inner;
    }
}

public class TranslateTextComponent : ITextComponent
{
    public string TranslationKey { get; set; }
    public string Text => Language.Translate(TranslationKey);
    public TranslateTextComponent(string translationKey)
    {
        TranslationKey = translationKey;
    }
}
