using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus;

public enum AnimType {
    OnDefault,
    OnPressQuack,
    OnReleaseQuack,
    //to be added
}

public struct AnimFrame {
    public int value;
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

public struct Animation {
    public AnimType animType;
    public Option<string> name;
    public float delay;
    public bool looping;
    public List<AnimFrame> frames;
    public AnimId Id { get; private set; }

    public static Animation New(AnimType animType, float delay, bool looping, Option<string> name, List<AnimFrame> frames) {
        return new Animation {
            animType = animType,
            delay = delay,
            looping = looping,
            frames = frames,
            name = name,
            Id = AnimId.GenNew(),
        };
    }

    public AnimFrame NextFrame(int frameId) {
        var self = this;
        return frames.Get(frameId).ValueOrElse(() => {
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
