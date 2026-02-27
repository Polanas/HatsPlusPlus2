using DuckGame;
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

internal enum HatType {
    Wearable,
    Wings,
    Extra,
    FlyingPet,
    WalkingPet,
    Room,
    Preview,
}
internal enum LinkFrameState {
    Default,
    Saved,
    Inverted,
}

internal struct ScriptHatData {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
}

internal struct WearableHatData {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
    [JsonProperty(PropertyName = "strapped_on")]
    public bool strappedOn;
    public List<Animation> animations;
    [JsonProperty(PropertyName = "custom_depth")]
    public float? customDepth;
    [JsonProperty(PropertyName = "extra_hat")]
    public ExtraHat? extraHat;
}

internal struct HatBaseData {
    [JsonProperty(PropertyName = "hat_type")]
    public HatType hatType;
    [JsonProperty(PropertyName = "frame_size")]
    public List<int> frameSize;
    [JsonProperty(PropertyName = "local_image_path")]
    public string? localImagePath;
    [JsonProperty(PropertyName = "local_script_path")]
    public string? localScriptPath;
}

internal struct RoomHatData {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
}

internal struct ExtraHat {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
}

internal struct PetBaseData {
    public int distance;
    public bool flipped;
}

internal struct PreviewHatData {
    [JsonProperty(PropertyName = "base")]
    public HatBaseData baseData;
}

internal struct HatElementData {
    [JsonProperty(PropertyName = "Wearable")]
    public WearableHatData? wearable;
    [JsonProperty(PropertyName = "Room")]
    public RoomHatData? room;
    [JsonProperty(PropertyName = "Preview")]
    public PreviewHatData? preview;
    [JsonProperty(PropertyName = "Script")]
    public ScriptHatData? script;
}

internal struct HatData {
    public List<HatElementData> elements;
    public string name;

    internal Option<PreviewHatData> Preview() {
        foreach (var elem in elements) {
            if (elem.preview != null) {
                return elem.preview.Value;
            }
        }
        return None;
    }

    internal Option<WearableHatData> Wearable() {
        foreach (var elem in elements) {
            if (elem.wearable != null) {
                return elem.wearable.Value;
            }
        }
        return None;
    }

    //internal Option<WearableHatData> Wings() {
    //    foreach (var elem in elements) {
    //        if (elem.wearable != null) {
    //            return elem.wearable.Value;
    //        }
    //    }
    //    return None;
    //}

    internal Option<RoomHatData> Room() {
        foreach (var elem in elements) {
            if (elem.room != null) {
                return elem.room.Value;
            }
        }
        return None;
    }
}