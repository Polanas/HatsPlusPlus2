using ImGuiNET;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using System.Linq;
using System;
using System.Collections.Generic;
using System.CodeDom;

namespace HatsPlusPlus;

public struct AnimId {
    public static int lastAnimId = 0;
    public int value;

    public static AnimId New(int value) {
        return new AnimId {
            value = value
        };
    }

    public static AnimId GenNew() {
        var id = lastAnimId;
        lastAnimId++;
        return new AnimId {
            value = id
        };
    }

    public static bool operator ==(AnimId a, AnimId b) => a.value == b.value;
    public static bool operator !=(AnimId a, AnimId b) => a.value != b.value;
}

public enum ClearState { 
    Yes, No
}

public class HatSprite
{

    public bool Finished => finished;
    public bool FrameChanged { get; private set; }
    public bool AnimChanged { get; private set; }
    public AnimFrame CurrentFrame {
        get {
            return forceCurrentFrame.ValueOrElse(() => {
                return currentAnimId
                .Map((id) => animations[id].frames[currentFrameId])
                .IfNone(() => AnimFrame.New(currentFrameId));
            });
        }
    }

    public Option<AnimFrame> forceCurrentFrame;
    public Dictionary<AnimId, Animation> animations;
    public Option<AnimId> currentAnimId;
    public bool frozen;
    public int currentFrameId;
    public float timeAccumulator;
    bool finished;

    public static HatSprite New() {
        return new HatSprite {
            animations = new(),
        };
    }

    public Option<Animation> CurrentAnim() {
        if (currentAnimId.Value() is AnimId anim_id && currentAnimId.IsSome) {
            if (animations.TryGetValue(anim_id, out var anim)) {
                return anim;
            }
            return None;
        } else {
            return None;
        }
    }

    public AnimId AddAnim(Animation animation) {
        var id = animation.Id;
        this.animations.Add(animation.Id, animation);
        return id;
    }

    public Option<Animation> AnimById(AnimId id) {
        if (animations.TryGetValue(id, out Animation anim)) {
            return Some(anim);
        }
        return None;
    }

    public void SetAnim(AnimId id, ClearState clearState = ClearState.Yes) {
        if (currentAnimId.Value() is AnimId current_id && currentAnimId.IsSome && current_id != id) {
            finished = false;
            if (clearState == ClearState.Yes) {
                ClearFrameState();
            }
            AnimChanged = true;
        }
        this.currentAnimId = id;
    }

    public bool HasAnim(AnimId id) {
        return animations.Values.Any((anim) => anim.Id == id);
    }

    public void SetAnim(string name, ClearState clearState = ClearState.Yes) {
        foreach (var pair in animations) {
            (var currentAnim, var currentId) = (pair.Value, pair.Key);

            if (currentAnim.name == name) {
                if (currentAnimId.Map((id) => id != currentId).ValueOr(true)) {
                    finished = false;
                    if (clearState == ClearState.Yes) {
                        ClearFrameState();
                    }
                    AnimChanged = true;
                }
                currentAnimId = currentId;
            }
        }
    }

    public Option<AnimFrame> NextFrame() {
        var self = this;
        return forceCurrentFrame.Match(
            (value) => value,
            () => {
                return self.CurrentAnim().Map((a) => a.NextFrame(self.currentFrameId));
            });
    }

    public void ClearFrameState() {
        currentFrameId = 0;
        timeAccumulator = 0;
    }

    public void Update(GameTime gameTime) {
        FrameChanged = false;
        AnimChanged = false;
        if (frozen || finished) {
            return;
        }

        AnimFrame currentFrame;

        if (this.currentAnimId.Value() is AnimId currentAnimId && this.currentAnimId.IsSome) {
            var currentAnim = animations[currentAnimId];
            currentFrame = currentAnim.frames[currentFrameId];
            var delay = currentFrame.delay.IfNone(() => currentAnim.delay);

            timeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeAccumulator >= delay) {
                timeAccumulator = 0;
                currentFrameId += 1;
                FrameChanged = true;
            }
            if (currentFrameId == currentAnim.frames.Length()) {
                if (currentAnim.looping) {
                    currentFrameId = 0;
                } else {
                    finished = true;
                    currentFrameId -= 1;
                }
            }
        } else {
            currentFrame = forceCurrentFrame.Value();
            return;
        }
    }
}
