using ImGuiNET;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using System.Linq;
using System;
using System.Collections.Generic;
using System.CodeDom;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace HatsPlusPlus;


public enum ClearState { 
    Yes = 1, No
}

[MoonSharpUserData]
public class HatSprite
{
    public bool Finished => finished;
    public bool FrameChanged { get; private set; }
    public bool ForceFrameChanged { get; private set; }
    public bool AnimChanged { get; private set; }
    public AnimFrame CurrentFrame {
        get {
            return forceCurrentFrame.Map((frame) => AnimFrame.New(frame)).ValueOrElse(() => {
                return currentAnimName
                .Map((name) => anims[name].frames[currentFrameId])
                .IfNone(() => AnimFrame.New(currentFrameId));
            });
        }
    }
    public int PreviousFrameId { get; private set; }

    public Option<int> forceCurrentFrame;
    public Dictionary<string, Animation> anims;
    public Option<string> currentAnimName;
    public bool frozen;
    public int currentFrameId;
    public float timeAccumulator;
    bool finished;
    bool frameChanged;
    Option<int> lastForceCurrentFrame;

    [MoonSharpVisible(false)]
    public static HatSprite New() {
        return new HatSprite {
            anims = new(),
            lastForceCurrentFrame = Some(-1),
            PreviousFrameId = -1,
        };
    }

    public Option<Animation> currentAnim() {
        return currentAnimName.AndThen((a) => anims.Get(a));
    }

    public void addAnim(Animation anim) {
        anims[anim.name] = anim;
    }

    public void addAnim(string name, float delay, bool looping, List<AnimFrame> frames) {
        var anim = Animation.New(name, delay, looping, frames);
        anims[name] = anim;
    }

    public bool hasAnim(string animName) {
        return anims.Values.Any((anim) => anim.name == animName);
    }

    public void setAnim(string name, ClearState clearState = ClearState.Yes) {
        foreach (var pair in anims) {
            (var _, var animName) = (pair.Value, pair.Key);
            if (animName == name) {
                if (currentAnimName.Map((name) => name != animName).ValueOr(true)) {
                    finished = false;
                    if (clearState == ClearState.Yes) {
                        clearFrameState();
                    }
                    AnimChanged = true;
                    currentAnimName = animName;
                }
            }
        }
    }

    public Option<AnimFrame> nextFrame() {
        var self = this;
        return forceCurrentFrame.Match(
            (value) => AnimFrame.New(value),
            () =>  self.currentAnim()
                   .Map((a) => a.NextFrame(self.currentFrameId))
            );
    }

    public void clearFrameState() {
        currentFrameId = 0;
        timeAccumulator = 0;
    }

    public void update(GameTime gameTime) {
        FrameChanged = false;
        AnimChanged = false;
        PreviousFrameId = currentFrameId;
        ForceFrameChanged = false;
        if (lastForceCurrentFrame != forceCurrentFrame && forceCurrentFrame.IsSome) {
            ForceFrameChanged = true;
        }
        if (frozen || finished) {
            return;
        }

        AnimFrame currentFrame;
        lastForceCurrentFrame = forceCurrentFrame;
        if (this.currentAnimName.ValueUnsafe() is var  currentAnimName && this.currentAnimName.IsSome) {
            var currentAnim = anims[currentAnimName];
            currentFrame = currentAnim.frames[currentFrameId];
            var delay = currentFrame.delay.IfNone(() => currentAnim.delay);

            timeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeAccumulator >= delay) {
                timeAccumulator = 0;
                currentFrameId += 1;
                if (currentFrameId == currentAnim.frames.Count) {
                    if (currentAnim.looping) {
                        currentFrameId = 0;
                    } else {
                        finished = true;
                        currentFrameId -= 1;
                    }
                } else {
                    FrameChanged = true;
                }
            }
        } else {
            currentFrame = AnimFrame.New(forceCurrentFrame.Value());
            return;
        }
    }
}
