using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus;

public static class AnimTypes {
    public static string OnDefault = nameof(OnDefault);
    public static string OnPressQuack = nameof(OnPressQuack);
    public static string OnReleaseQuack = nameof(OnReleaseQuack);
    public static string OnPetStop = nameof(OnPetStop);
    public static string OnPetAppoach = nameof(OnPetAppoach);
    public static string OnDuckDeath = nameof(OnDuckDeath);
    public static string OnDuckJump = nameof(OnDuckJump);
    public static string OnDuckLand = nameof(OnDuckLand);
    public static string OnDuckGlide = nameof(OnDuckGlide);
    public static string OnDuckWalk = nameof(OnDuckWalk);
    public static string OnDuckSneak = nameof(OnDuckSneak);
    public static string OnDuckNetted = nameof(OnDuckNetted);
    public static string OnDuckSpawned = nameof(OnDuckSpawned);
    public static string OnHatPickedUp = nameof(OnHatPickedUp);
}

[MoonSharpUserData]
internal record struct AnimFrame {
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
internal struct Animation {
    public string name;
    public float delay;
    public bool looping;
    public List<AnimFrame> frames;
    [JsonProperty(PropertyName = "anim_type")]
    public string animType;

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
