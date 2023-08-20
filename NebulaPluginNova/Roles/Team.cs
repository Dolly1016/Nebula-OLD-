using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles;

public enum TeamRevealType
{
    OnlyMe,
    Everyone,
    Teams,
}

[NebulaPreLoad]
public class Team
{
    public string TranslationKey { get; private init; }
    public Color Color { get; private init; }
    public int Id { get; set; }
    public TeamRevealType RevealType { get; set; }
    public Team(string translationKey, Color color, TeamRevealType revealType)
    {
        TranslationKey = translationKey;
        Color = color;
        Roles.Register(this);
        RevealType = revealType;
    }
}
