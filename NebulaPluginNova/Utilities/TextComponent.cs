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
