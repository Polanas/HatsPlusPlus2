using LanguageExt.ClassInstances;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using OneOf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

#nullable enable
namespace HatsPlusPlus.Parsing;

public enum HatType {
    Wearable,
    Wings,
    Extra,
    FlyingPet,
    WalkingPet,
    Room,
    Preview,
}
public enum LinkFrameState {
    Default,
    Saved,
    Inverted,
}

public struct WearableHatData {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
    [JsonProperty(PropertyName = "strapped_on")]
    public bool strappedOn;
    public List<Animation> animations;
}

public struct HatBaseData {
    [JsonProperty(PropertyName = "hat_type")]
    public HatType hatType;
    [JsonProperty(PropertyName = "frame_size")]
    public List<int> frameSize;
    [JsonProperty(PropertyName = "local_image_path")]
    public string? localImagePath;
    [JsonProperty(PropertyName = "local_script_path")]
    public string? localScriptPath;
}

public struct PetBaseData {
    public int distance;
    public bool flipped;
}

public struct HatElementData {
    [JsonProperty(PropertyName = "Wearable")]
    public WearableHatData wearable;
}

public struct HatData {
    public List<HatElementData> elements;
    public string name;
}