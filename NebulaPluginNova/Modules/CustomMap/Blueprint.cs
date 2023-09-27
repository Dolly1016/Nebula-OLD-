using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.CustomMap;

public class ImageAsset
{
    [JsonSerializableField]
    string? Path = null;

    [JsonSerializableField]
    int Length = 1;
}

public class BlueprintChain
{
    [JsonSerializableField]
    Vector2 LocalPos = new(0f, 0f);

    [JsonSerializableField]
    public string? Name = null;

    Blueprint? cache = null;
    
}

public class BlueprintShipRoom
{
    [JsonSerializableField]
    Vector2[]? RoomArea = null;

    [JsonSerializableField]
    public string? TranslationKey = null;
}

public class Blueprint
{
    [JsonSerializableField]
    Vector2[][]? Colliders = null;

    [JsonSerializableField]
    Vector2[][]? Shadows = null;

    [JsonSerializableField]
    BlueprintShipRoom? ShipRoom = null;

    [JsonSerializableField]
    int OrderZ = 0;

    [JsonSerializableField]
    BlueprintChain[]? Children = null;
}

