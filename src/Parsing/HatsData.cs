using OneOf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#nullable enable
namespace HatsPlusPlus.Parsing;

public struct HatMetadata {
    public string name;
    public Option<IVector2> size;

    public static HatMetadata New(string name, Option<IVector2> size) {
        return new HatMetadata {
            name = name,
            size = size,
        };
    }
}

public enum HatType
{
    Wereable,
    Wings,
    Extra,
    FlyingPet,
    WalkingPet,
    Room,
}
public enum LinkFrameState
{
    Default,
    Saved,
    Inverted,
}

public struct BaseHatData
{
    public HatType hatType;
    public IVector2 frameSize;
    public string path;
}

public struct FlyingPetData
{
    public BaseHatData hatData;
    public BasePetData petData;
    public bool changesAngle;
    public int speed;
}

public struct WalkingPet
{
    public BaseHatData hatData;
    public BasePetData petData;
}

public struct BasePetData
{
    public bool flipped;
    public LinkFrameState linkFrameState;
    public List<Animation> animations;
}

public struct PreviewData
{
    public BaseHatData hatData;
}

public struct Wings
{
    public BaseHatData hatBase;
    public IVector2 generalOffset;
    public IVector2 crouchOffset;
    public IVector2 ragdollOffset;
    public IVector2 slideOffset;
    public IVector2 netOffset;
    public bool generateAnimations;
    public int autoGlideFrame;
    public int AutoIdleFrame;
    public int AutoAnimSpeed;
    public bool changesAnimations;
    public bool size_state;
    public List<Animation> animations;
}
public struct Wereable
{
    public BaseHatData hatBase;
    public bool strappedOn;
    public LinkFrameState linkFrameState;
    public Option<AnimType> onSpawnAnimation;
    public List<Animation> animations;
}

public struct Extra
{
    public BaseHatData hatBase;
    public List<Animation> animations;
}