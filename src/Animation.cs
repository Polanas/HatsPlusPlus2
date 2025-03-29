using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus;

public enum AnimType {
    OnDefault,
    OnPressQuack,
    OnReleaseQuack,
    OnPetStop,
    OnPetApproach,
    OnDuckDeath,
    OnDuckJump,
    OnDuckLand,
    OnDuckGlide,
    OnDuckWalk,
    OnDuckSneak,
    OnDuckNetted,
    OnDuckSpawned,
    OnHatPickedUp,
}

[MoonSharpUserData]
public record struct AnimFrame {
    [MoonSharpVisible(true)]
    public int value;
    [MoonSharpVisible(true)]
    public Option<float> delay;

    public static AnimFrame New() {
        return new AnimFrame {
            value = 0,
            delay = None
        };
    }
    public static AnimFrame New(int value) {
        return new AnimFrame {
            value = value,
            delay = None
        };
    }

    public static AnimFrame New(int value, float delay) {
        return new AnimFrame {
            value = value,
            delay = delay
        };
    }

    public AnimFrame WithFrame(int newFrame) {
        value = newFrame;
        return this;
    }

    public AnimFrame WithDelay(float newDelay) {
        delay = newDelay;
        return this;
    }
};

[MoonSharpUserData]
public struct Animation {
    public string name;
    public float delay;
    public bool looping;
    public List<AnimFrame> frames;
    [JsonProperty(PropertyName = "anim_type")]
    public AnimType? animType;

    public static Animation New(string name, float delay, bool looping, List<AnimFrame> frames) {
        return new Animation {
            delay = delay,
            looping = looping,
            frames = frames,
            name = name,
        };
    }

    public AnimFrame NextFrame(int frameId) {
        var self = this;
        return frames.Get(frameId+1).ValueOrElse(() => {
            if (frameId < 0) {
                return self.frames.First();
            }
            if (frameId >= self.frames.Count - 1) {
                return self.looping ? self.frames.First() : self.frames.Last();
            }
            return self.frames[frameId + 1];
        });
    }
}
