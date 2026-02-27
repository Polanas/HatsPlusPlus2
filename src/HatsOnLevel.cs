using Coroutines;
using DuckGame;
using HatsPlusPlus.Parsing;
using ImGuiNET;
using LanguageExt.ClassInstances;
using LanguageExt.ClassInstances.Pred;
using LanguageExt.SomeHelp;
using LanguageExt.UnitsOfMeasure;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter.Interop.LuaStateInterop;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Script = MoonSharp.Interpreter.Script;

namespace HatsPlusPlus;

/*
What do I need? I need a hat class that will be able to do everything that a hat needs, which is:
    1) store and display animations,
    2) have size up to 64x64 pixel,
    3) be able to flip, change position and angle
    4) be able to change depth if needed, while also animating in moving freely (is hard)
 */

//internal struct TeamFrame {
//    internal List<TeamData> teams;
//}


/// <summary>
/// This class represents a custom hat object, which behaves like a normal hat expect it can have animations,
/// and changeable depth
/// </summary>
//internal class CustomHat {
//    //internal Vector2 position;
//    //internal float  angle;
//    //internal bool flipped;
//    //internal List<TeamFrame> frames;
//}

internal class TeamData {
    internal Team team;
    internal TeamHandle handle;
    internal Bitmap image;
}


[MoonSharpUserData]
internal struct TeamFrame {
    [MoonSharpVisible(true)]
    public List<TeamHandle> teamHandles;

    public static TeamFrame New(List<TeamHandle> handles) {
        return new TeamFrame {
            teamHandles = handles
        };
    }
}

internal enum ChopMode {
    Simple,
    WithGaps,
}

[MoonSharpUserData]
internal struct TeamsBitmap {
    [MoonSharpVisible(true)]
    public IVector2 frameSize;
    public IVector2 frameSizeWithGaps;
    [MoonSharpVisible(true)]
    public List<TeamFrame> frames;
    /// <summary>
    /// Indicates that frames are bigger than 32x32 (=each frame consists of multiple teams)
    /// </summary>
    [MoonSharpVisible(true)]
    public bool isBig;
    [MoonSharpVisible(true)]
    public Option<ChopMode> chopMode;
    [MoonSharpVisible(true)]
    public Option<AsepriteData> asepriteData;
}


internal abstract class AbstractHat {
    internal bool strappedOn;
    internal Vec2 position;
    internal float depth;
    internal float angle;
    internal bool flippedHorizontally;
    internal ScoreRock rock;
    internal TeamsBitmap teamsBitmap;
    internal HatSprite sprite;
    internal HatId id;
    internal bool should_update = true;
    internal Option<LuaScript> scriptData;

    internal abstract void Update(GameTime gameTime);
    internal virtual void OnRemove() { }
    internal virtual void Draw(GameTime gameTime) { }

    internal virtual void OnPressQuack() { }
    internal virtual void OnReleaseQuack() { }

    internal virtual void FillFromLua(Table table) {
        position = table.Get(nameof(AbstractHat.position)).ToObject<Vec2>();
        depth = table.Get(nameof(AbstractHat.depth)).ToObject<float>();
        angle = (float)table.Get(nameof(AbstractHat.angle)).ToObject<float>();
        flippedHorizontally = table.Get(nameof(AbstractHat.flippedHorizontally)).ToObject<bool>();
        teamsBitmap = table.Get(nameof(AbstractHat.teamsBitmap)).ToObject<TeamsBitmap>();
        sprite = table.Get(nameof(AbstractHat.sprite)).ToObject<HatSprite>();
    }

    internal bool IsAlive() {
        return HatsOnLevel.IsAlive(id);
    }
}

internal enum DepthHatState {
    Regular = 1,
    Depth, 
    DepthInactive
}

internal enum InnerDepthHatState {
    None,
    ToRegular,
    ToDepthInactive,
}

static class Functions {
    internal static IVector2 CoordsFromIndex(int index, int width) {
        return (index % width, index / width);
    }

    internal static (List<int>, List<int>) GetIndices(IVector2 hatsAmount) {
        var normalIndices = new List<int>();
        var horizontalIndices = new List<int>();

        int indexCount = 0;
        for (int y = 0; y < hatsAmount.Y; y++) {
            int[] indices = new int[hatsAmount.X];
            for (int i = 0; i < indices.Length; i++) {
                indices[i] = indexCount;
                normalIndices.Add(indices[i]);
                indexCount += 1;
            }

            foreach (var index in indices.Reverse()) {
                horizontalIndices.Add(index);
            }
        }

        return (normalIndices, horizontalIndices);
    }

    internal static Option<AnimFrame> NextAnimFrame(int currentFrameId, Animation anim) {
        return anim.frames.Get(currentFrameId).Map((frame) => {
            if (frame.value == anim.frames.Length() - 1) {
                return frame.WithFrame(anim.looping ? 0 : frame.value);
            }
            return frame.WithFrame(frame.value + 1);
        });
    }
}


internal enum DepthAnimState {
    SettingUp,
    Idle,
    ChaningFrame
}
/*
Okay, never mind all'at.
Consider we have TWO hats.
when setting an animation:
if FIRST ANIM:
    [state: 2 normal]
    Frame 1: set both to Depth, set frames to 0 and 1
    IF CALLED SET_ANIM:
        update frames
    [state: 2 depth]
    Frame 2: set frame 0 one to DepthInactive, show it
    [state: 1 depth, 1 inactive (showed)]
ELSE:
    1. AFTER Frame 1 of changing frame:
        [state: two depth inactive (one showed)]
        Frame 1: set not showed depth inactive to depth, set it to frame 0
        IF CALLED SET_ANIM:
            update frames
        frame_change()

/*
frame_change():
    [state: 1 depth, 1 inactive (showed)]
    Frame 1: set Depth one to DepthInactive, swap hats
    [state: 2 depth inactive]
    Frame 2: set old DepthInactive to Depth, set it to next frame
    [state: 1 depth, 1 inactive (showed)]
*/

/*
when setting an animation:
IF FIRST ANIM:
    initial state: 3 normal hats
    Frame 1: take 2 hats, make them Depth. Assign them to frames 0 and 1
    Frame 2: take frame 0 Depth hat, make it DepthInactive, snap to pos
ELSE:
    1. BETWEEN FRAME CHANGE:
        initial state: 3 hats, each in its respective state
        Frame 1: set depth hat to frame 0
        Frame 2: set depth hat to DepthInactive, switch showed hats
        Frame 3: make old DepthInactive hat Depth again, set it to frame 1 (= next frame)
    2. DURING FRAME CHANGE (AFTER FRAME 1):
        initial state: 2 DepthInactive hats, 1 normal hat
        Frame 2: Make old DepthInactive to depth, 

result: 1 hat DepthInactive with frame 0, 1 hat Depth with frame 0, 1 hat normal
when changing frame:
    initial state: 3 hats, each in its respective state
    Frame 1: take Depth hat with next frame, make it inactive and snap to pos, snap current showed (DepthInactive) hat out
    Frame 2: make old DepthInactive hat Depth again, set it to the next frame
 */
internal enum ChangeFrameState {
    None,
    Frame1,
    Frame2,
}

internal enum NewAnim {
    Yes,
    No
}

internal class ScriptHat : AbstractHat {
    internal ScriptHatData hatData;

    internal static ScriptHat New(ScriptHatData hatData, Option<LuaScript> script) {
        return new ScriptHat {
            hatData = hatData,
            scriptData = script,
        };
    }

    internal override void OnRemove() {
        //TryCall("remove");
    }

    internal override void Update(GameTime gameTime) {
        //TryCall("update", gameTime);
    }

    internal override void Draw(GameTime gameTime) {
        //TryCall("draw", gameTime);
    }
}

internal class DepthAnimHat: AbstractHat {
    internal DepthHat[] hats;
    internal IVector2 hatsAmount;
    List<int> normalIndices;
    List<int> horizIndices;
    DepthAnimState state;
    Option<DepthHat> shownHat;
    CoroutineRunner coroutines;
    ChangeFrameState changeFrameState;
    Option<CoroutineHandle> changeFrameHandler;
    Option<CoroutineHandle> changeAnimHandler;
    bool firstAnimSet;
    int previousFrameId = -1;
    bool forceChangeDepth;

    static Vec2 OFF_SCREEN_POS = new Vec2(0,-1000);

    internal override void Draw(GameTime gameTime) {
    }

    internal override void OnRemove() {
    }

    internal static DepthAnimHat New(TeamsBitmap teamsBitmap, Option<ScoreRock> rockOption, bool update) {
        var firstTeamHandle = teamsBitmap.frames.Get(0).AndThen((f) => f.teamHandles.Get(0));
        var teamSize = firstTeamHandle.AndThen((h) => TeamsStorage.GetTeamData(h)).Map((d) => d.image.Size).ValueOr(new IVector2(Constants.MIN_HAT_SIZE));

        var hatsAmountX = (int)Math.Ceiling((float)teamsBitmap.frameSizeWithGaps.X / (float)teamSize.X);
        var hatsAmountY = (int)Math.Ceiling((float)teamsBitmap.frameSizeWithGaps.Y / (float)teamSize.Y);
        var hatsAmount = new IVector2(hatsAmountX, hatsAmountY);
        var (normalIndices, horizontalIndices) = Functions.GetIndices(hatsAmount);

        var hat = new DepthAnimHat {
            teamsBitmap = teamsBitmap,
            sprite = HatSprite.New(),
            hatsAmount = hatsAmount,
            normalIndices = normalIndices,
            horizIndices = horizontalIndices,
            coroutines = new CoroutineRunner(),
            hats = new DepthHat[] {
                HatsOnLevel.Add(DepthHat.New(teamsBitmap, rockOption, false)) as DepthHat,
                HatsOnLevel.Add(DepthHat.New(teamsBitmap, rockOption, false)) as DepthHat,
            },
            should_update = update,
        };

        foreach (var h in hat.hats) {
            h.position = OFF_SCREEN_POS;
        }

        return hat;
    }

    internal void UpdateDepth() {
        forceChangeDepth = true;
    }

    IEnumerator ChangeFrame(bool forceChange = false) {
        //so we don't waste time changing frames if it's the same one
        if ((sprite.currentFrameId == previousFrameId) && !forceChange) {
            changeAnimHandler = None;
            yield break;
        }
        if (hats[0].State == hats[1].State && hats[0].State == DepthHatState.Regular) {
            var hat1 = hats[0];
            var hat2 = hats[1];

            hat1.SetState(DepthHatState.Depth);
            hat1.sprite.forceCurrentFrame = sprite.nextFrame().Map((f) => f.value).ValueOr(sprite.currentFrameId);
            hat2.SetState(DepthHatState.Depth);
            hat2.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
            yield return null;

            hat2.SetState(DepthHatState.DepthInactive);
            shownHat = hat2;
            firstAnimSet = true;
            changeAnimHandler = None;
            yield break;
        }

        if (!(
                (hats[0].State == DepthHatState.DepthInactive && hats[1].State == DepthHatState.Depth)
                || (hats[0].State == DepthHatState.Depth && hats[1].State == DepthHatState.DepthInactive)
            )) {
            if (this.shownHat.ValueUnsafe() is var validShowHat && this.shownHat.IsSome) {
                validShowHat.SetState(DepthHatState.DepthInactive);
                yield break;
            } else {
                hats[0].SetState(DepthHatState.DepthInactive);
                yield break;
            }
        }

        var depthHat = FindHatWith(DepthHatState.Depth).Unwrap();
        depthHat.SetState(DepthHatState.DepthInactive);
        depthHat.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
        var oldShownHat = SwapShownHat(depthHat);
        changeFrameState = ChangeFrameState.Frame1;
        yield return null;

        oldShownHat.SetState(DepthHatState.Depth);
        oldShownHat.sprite.forceCurrentFrame = sprite.nextFrame().Map((f) => f.value).ValueOr(sprite.currentFrameId);

        changeFrameState = ChangeFrameState.Frame2;
        yield return null;

        changeFrameState = ChangeFrameState.None;
        previousFrameId = sprite.currentFrameId;
        changeAnimHandler = None;
    }

    IEnumerator ChangeAnim() {
        if (!firstAnimSet) {
            var hat1 = hats[0];
            var hat2 = hats[1];

            hat1.SetState(DepthHatState.Depth);
            hat1.sprite.forceCurrentFrame = sprite.nextFrame().Map((f) => f.value).ValueOr(sprite.currentFrameId);
            hat2.SetState(DepthHatState.Depth);
            hat2.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
            yield return null;

            hat2.SetState(DepthHatState.DepthInactive);
            shownHat = hat2;
            firstAnimSet = true;
            yield break;
        }
        if (changeFrameState == ChangeFrameState.Frame1) {
            previousFrameId = sprite.currentFrameId;
            changeFrameState = ChangeFrameState.None;
            var notShownHat = hats.Find((h) => h != shownHat).Unwrap();
            notShownHat.SetState(DepthHatState.Depth);
            notShownHat.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
            yield return null;

            yield return ChangeFrame();
            yield break;
        }

        if (!(
                (hats[0].State == DepthHatState.DepthInactive && hats[1].State == DepthHatState.Depth)
                || (hats[0].State == DepthHatState.Depth && hats[1].State == DepthHatState.DepthInactive)
            )) {
            if (this.shownHat.ValueUnsafe() is var validShowHat && this.shownHat.IsSome) {
                validShowHat.SetState(DepthHatState.DepthInactive);
                yield break;
            } else {
                hats[0].SetState(DepthHatState.DepthInactive);
                yield break;
            }
        }

        var depthHat = FindHatWith(DepthHatState.Depth).Unwrap(); 
        var depthInactiveHat = FindHatWith(DepthHatState.DepthInactive).Unwrap();
        depthHat.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
        yield return null;

        yield return ChangeFrame();
    }

    DepthHat SwapShownHat(DepthHat newShownHat) {
        var currentShownHat = shownHat.Unwrap();
        var oldShownHat = currentShownHat;
        currentShownHat.position = OFF_SCREEN_POS;
        shownHat = newShownHat;
        return oldShownHat;
    }

    internal override void Update(GameTime gameTime) {
        if (sprite.AnimChanged || sprite.ForceFrameChanged) {
            if (changeFrameState == ChangeFrameState.Frame1) {
                if (changeFrameHandler.Value() is var change_handler && this.changeFrameHandler.IsSome) {
                    coroutines.Stop(change_handler);
                }
                changeFrameHandler = None;
            }
            changeAnimHandler = coroutines.Run(ChangeAnim());
        } else if ((sprite.FrameChanged || forceChangeDepth) && this.changeFrameState == ChangeFrameState.None) {
            var forceChangeDepth = this.forceChangeDepth;
            changeFrameHandler = coroutines.Run(ChangeFrame(forceChangeDepth));
            this.forceChangeDepth = false;
        }

        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (this.shownHat.ValueUnsafe() is var shownHat && shownHat is not null) {
            shownHat.position = position;
            shownHat.angle = angle;
            shownHat.depth = depth;
        }

        foreach (var hat in hats) {
            hat.flippedHorizontally = flippedHorizontally;
            hat.depth = depth;
            hat.strappedOn = strappedOn;
            if (this.shownHat.Map((h) => h != hat).ValueOr(true)) {
                hat.position = OFF_SCREEN_POS;
            }

            hat.Update(gameTime);
        }
        sprite.update(gameTime);
    }

    internal Option<DepthHat> FindHatWith(DepthHatState state) {
        foreach (var hat in hats) {
            if (hat.State == state) {
                return hat;
            }
        }
        return None;
    }
}

internal class VanillaHat: AbstractHat {
    internal TeamHat inner;

    internal static VanillaHat New(TeamsBitmap bitmap, bool update = true) {
        var hat = new TeamHat(0, 0, null);
        HatsOnLevel.AddTeamHat(hat);

        return new VanillaHat {
            should_update = update,
            inner = hat,
            teamsBitmap = bitmap,
            sprite = HatSprite.New(),
        };
    }

    internal override void FillFromLua(Table table) {
        teamsBitmap = table.Get(nameof(AbstractHat.teamsBitmap)).ToObject<TeamsBitmap>();
        sprite = table.Get(nameof(AbstractHat.sprite)).ToObject<HatSprite>();
    }

    internal override void Draw(GameTime gameTime) {

    }

    internal override void OnRemove() {
        Level.Remove(inner);
    }

    internal override void Update(GameTime gameTime) {
        inner.strappedOn = strappedOn;
        sprite.update(gameTime);
        var currentFrame = sprite.CurrentFrame;
        //TODO: frames mighit not exist
        var teamFrame = teamsBitmap.frames[currentFrame.value].teamHandles;
        var teamData = TeamsStorage.GetTeamData(teamFrame[0]);
        teamData.IfSome((data) => inner.team = data.team);
    }
}

internal class ParallaxLayer {
    internal DepthHat hat;
    internal Vec2 topRightHatPos;
    internal TeamHat topRightHat;
    internal float speed;
    internal Vec2 initialPos;
    internal bool updateHat;
    internal bool updateHatStart;
    internal CoroutineRunner runner;
    internal int counter;
    internal Vec2 pos;

    internal static ParallaxLayer New(DepthHat hat, float speed) {
        var runner = new CoroutineRunner();
        var layer = new ParallaxLayer {
            hat = hat,
            speed = speed,
            initialPos = hat.position,
            runner = runner
        };
        runner.Run(layer.SetHat());
        return layer;
    }

    internal IEnumerator SetHat() {
        yield return null;
        topRightHat = hat.hats[hat.normalIndices[0]];
        topRightHatPos = topRightHat.position;
    }

    internal void ProgressParallax() {
        var newIndices = new int[hat.hatsAmount.X * hat.hatsAmount.Y];
        for (int x = 0; x < hat.hatsAmount.X; x++) {
            for (int y = 0; y < hat.hatsAmount.Y; y++) {
                var index = y * hat.hatsAmount.X + x;
                var indexRight = y * hat.hatsAmount.X + x + 1;
                var hatIndex = hat.normalIndices[index];
                if (x < hat.hatsAmount.X - 1) {
                    newIndices[index] = hat.normalIndices[indexRight];
                } else {
                    var indexZero = y * hat.hatsAmount.X;
                    newIndices[index] = hat.normalIndices[indexZero];
                }
            }
        }
        hat.normalIndices = newIndices.ToList();
    }

    internal void Update(GameTime gameTime) {
        runner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (updateHat) {
            var pos = hat.hats[0].position.x;
            var id = 0;
            for (int i = 0; i < hat.hats.Count; i++) {
                if (hat.hats[i].position.x <= pos) {
                    pos= hat.hats[i].position.x;
                    id = i;
                }
            }
            topRightHat = hat.hats[id];
            topRightHatPos = topRightHat.position;
            updateHat = false;
        }
        if (topRightHat != null) {
            //var diff = 0;
            //if (counter == 0) {
            //    diff = 13;
            //} else if (counter > 0) {
            //    diff = 
            //}
            if (pos.x >= 32) {
                DevConsole.Log("parallax progressed");
                hat.position.x = initialPos.x;
                counter += 1;
                ProgressParallax();
                updateHat = true;
                pos = Vec2.Zero;
                //if (counter == 5) {
                //    counter = -1;
                //}
            }
        }
        hat.position.x += speed;
        pos.x += speed;
    }
}

internal class RoomHat: AbstractHat {
    internal RoomHatData data;
    internal Option<DynValue> luaState;
    internal Option<Script> script;
    internal DepthHat fg;
    internal DepthHat bg;
    internal DepthHat parallax1;
    internal ParallaxLayer parallax1Layer;
    internal DepthHat cape;

    internal DepthAnimHat orbit;

    internal DepthHat parallax2;
    internal ParallaxLayer parallax2Layer;

    internal DepthHat parallax3;
    internal ParallaxLayer parallax3Layer;

    internal DepthHat parallax4;
    internal ParallaxLayer parallax4Layer;

    internal DepthHat hide;
    internal DepthHat hide2;
    internal Bitmap roomSprite;
    internal CoroutineRunner runner;
    internal DepthHat ringTop;
    internal DepthHat ringBottom;

    internal GameTime gameTime;

    internal static RoomHat New(RoomHatData data, Option<Script> script, Bitmap sprite) {
        var roomHat = new RoomHat {
            roomSprite = sprite,
        };
        return roomHat;
    }

    internal override void Draw(GameTime gameTime) {
        ImGui.Begin("debug");
        ImGui.Text($"hat 1 state: {this.orbit.hats[0].State}");
        ImGui.Text($"hat 2 state: {this.orbit.hats[1].State}");
    }


    internal void Add() {
        var roomInfo = RoomHatUtils.GetRoomInfo(roomSprite).Unwrap();

        //fg = DepthHat.New(roomInfo.fg.Unwrap(), None);
        //HatsOnLevel.Add(fg);
        //bg = DepthHat.New(roomInfo.bg.Unwrap(), None);
        //HatsOnLevel.Add(bg);

        //var halfRoomSize = new Vec2(RoomHatUtils.roomSize.X, RoomHatUtils.roomSize.Y) / 2;
        //halfRoomSize.y += 1;
        ////halfRoomSize.x -= 1;
        //fg.position = roomInfo.position + halfRoomSize;
        //bg.position = roomInfo.position + halfRoomSize;

        //var hideTeams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("hide.png"), None, None).Unwrap();
        //hide = DepthHat.New(hideTeams, None);
        //hide.position = new Vec2(158, 90);
        //HatsOnLevel.Add(hide);

        var orbitTeams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("image.aseprite"), None, None).Unwrap();
        orbit = DepthAnimHat.New(orbitTeams, None, true);
        var hatsData = HatsOnLevel.hatsData;
        HatsOnLevel.Add(orbit);
        orbit.sprite.addAnim("default", 0.3f, false, [AnimFrame.New(0), AnimFrame.New(1)]);
        orbit.sprite.setAnim("default");
        //TODO: if no animation is set, hat won't event show up. 

        //var capeTeams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("cape.aseprite"), None, Constants.MAX_TEAM_SIZE).Unwrap();
        //cape = DepthHat.New(capeTeams, None);
        ////HatsOnLevel.Add(cape);

        //var ringTopTeams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("ring_top.aseprite"), None, None).Unwrap();
        //ringTop = DepthHat.New(ringTopTeams, None);
        ////HatsOnLevel.Add(ringTop);

        //var ringBottomTeams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("ring_bottom.aseprite"), None, None).Unwrap();
        ////ringBottom = DepthHat.New(ringBottomTeams, None);
        ////HatsOnLevel.Add(ringBottom);

        //var hide2Teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("hide2.png"), None, None).Unwrap();
        //hide2 = DepthHat.New(hide2Teams, None);
        //hide2.position = new Vec2(1, hide2Teams.frameSize.Y / 2);
        //HatsOnLevel.Add(hide2);
        //var teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("room2.png"), None, None, ChopMode.Simple).Unwrap();
        //parallax1 = DepthHat.New(teams, None);
        //parallax1.position = fg.position;
        //parallax1.position.x = 1f + 192f / 2f - (32 - 13) - 32;
        //HatsOnLevel.Add(parallax1);
        //parallax1Layer = ParallaxLayer.New(parallax1, 0.3f);

        //teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("room3.png"), None, None, ChopMode.Simple).Unwrap();
        //parallax2 = DepthHat.New(teams, None);
        //parallax2.position = fg.position;
        //parallax2.position.x = 1f + 192f / 2f - (32 - 13) - 32;
        //HatsOnLevel.Add(parallax2);
        //parallax2Layer = ParallaxLayer.New(parallax2, 0.2f);

        //teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("room4.png"), None, None, ChopMode.Simple).Unwrap();
        //parallax3 = DepthHat.New(teams, None);
        //parallax3.position = fg.position;
        //parallax3.position.x = 1f + 192f / 2f - (32 - 13) - 32;
        //HatsOnLevel.Add(parallax3);
        //parallax3Layer = ParallaxLayer.New(parallax3, 0.14f);

        //teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("room5.png"), None, None, ChopMode.Simple).Unwrap();
        //parallax4 = DepthHat.New(teams, None);
        //parallax4.position = fg.position;
        //parallax4.position.x = 1f + 192f / 2f - (32 - 13) - 32;
        //HatsOnLevel.Add(parallax4);
        //parallax4Layer = ParallaxLayer.New(parallax4, 0.14f);

        runner = new CoroutineRunner();
        //runner.Run(SetDepth());
        runner.Run(Orbit());
    }
    internal IEnumerator Orbit() {
        while (true) {
            var dt = (float)gameTime.TotalGameTime.TotalSeconds;

            var cos = (float)Math.Cos((double)dt * 1.5f);
            //if (cos >= 0.98f) {
            //    orbit.depth = Ducks.MainDuck.depth.value + 0.1f;
            //} else if (cos <= -0.98) {
            //    orbit.depth = Ducks.MainDuck.depth.value - 0.1f;
            //}
            if (Keyboard.Pressed(Keys.K)) {
                orbit.sprite.setAnim("default");
            }
            orbit.depth = 2.0f;
            orbit.angle = 0.0f;
            var pos = Ducks.mainDuck.position;
            orbit.position.y = pos.y;
            orbit.position.x = pos.x + 20 * cos;

            yield return null;
        }
    }

    internal IEnumerator SetDepth() {
        yield return 0.1f;

        hide.SetState(DepthHatState.DepthInactive);
        hide.depth = -0.8f;
        hide2.SetState(DepthHatState.DepthInactive);
        hide2.depth = -0.8f;
        fg.SetState(DepthHatState.Depth);
        fg.depth = 0.74f;

        //ringTop.SetState(DepthHatState.DepthInactive);
        //ringTop.depth = Ducks.MainDuck.depth.value + 0.1f;
    }

    internal override void OnRemove() {

    }


    internal override void Update(GameTime gameTime) {
        this.gameTime = gameTime;
        //var pos = Lerp.Vec2Smooth(cape.position, Ducks.MainDuck.position, 0.3f);
        //ringTop.position = Lerp.Vec2Smooth(ringBottom.position, Ducks.MainDuck.position, 0.3f);

        //cape.position.x = pos.x + 18 * (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds *2 );
        //cape.position.y = pos.y + 18 * (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds*2) * 0.3f + 2;

        //if (cape.position.x - Ducks.MainDuck.position.x > 17) {
        //    cape.SetState(DepthHatState.Regular);
        //    cape.depth = 0;
        //} else if (cape.position.x - Ducks.MainDuck.position.x < -17) {
        //    cape.SetState(DepthHatState.DepthInactive);
        //    cape.depth = Ducks.MainDuck.depth.value + 0.2f;
        //}


        //parallax1Layer.Update(gameTime);
        //parallax1.SetState(DepthHatState.DepthInactive);
        //parallax1.depth = -0.81f;

        //parallax2Layer.Update(gameTime);
        //parallax2.SetState(DepthHatState.DepthInactive);
        //parallax2.depth = -0.82f;

        //parallax3Layer.Update(gameTime);
        //parallax3.SetState(DepthHatState.DepthInactive);
        //parallax3.depth = -0.83f;

        //parallax4Layer.Update(gameTime);
        //parallax4.SetState(DepthHatState.DepthInactive);
        //parallax4.depth = -0.8355f;

        runner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }
}

internal class ParticleHat : AbstractHat {
    internal VanillaHat particleHat;
    internal Duck phantomDuck;
    internal CoroutineRunner runner;
    internal bool emittingParticle;
    internal ScoreRock rock;

    public static ParticleHat New(TeamsBitmap teams, bool update = true) {
        //TODO: patch camera to ignore phantom ducks
        var particleHat = VanillaHat.New(teams, false);
        var sprite = HatSprite.New();
        HatsOnLevel.Add(particleHat);
        particleHat.sprite = sprite;

        var rock = new ScoreRock(20, 20, Ducks.mainDuck.profile);
        rock.depth = -10;
        Level.Add(rock);

        var hat = new ParticleHat {
            sprite = sprite,
            teamsBitmap = teams,
            phantomDuck = null,
            particleHat = particleHat,
            should_update = update,
            runner = new CoroutineRunner(),
            rock=rock,
        };

        hat.runner.Run(hat.GetHatReady());
        return hat;
    }

    internal IEnumerator EmitCoroutine() {
        emittingParticle = true;
        phantomDuck.quack = 2;
        yield return null;
        yield return null;
        emittingParticle = false;
    }

    internal void Emit() {
        if (emittingParticle || phantomDuck == null) {
            return;
        }
        runner.Run(EmitCoroutine());
    }

    internal IEnumerator GetHatReady() {
        void CookSilent(Duck duck) {
        if (duck._cooked != null) {
            return;
        }

        if (duck.ragdoll != null) {
            position = duck.ragdoll.position;
            if (Network.isActive) {
                duck.ragdoll.Unragdoll();
            } else {
                Level.Remove(duck.ragdoll);
            }

            duck.vSpeed = -2f;
        }

        if (Network.isActive) {
            duck._cooked = duck._cookedInstance;
            if (duck._cookedInstance != null) {
                duck._cookedInstance.active = true;
                duck._cookedInstance.visible = true;
                duck._cookedInstance.solid = true;
                duck._cookedInstance.enablePhysics = true;
                duck._cookedInstance._sleeping = false;
                duck._cookedInstance.x = duck.x;
                duck._cookedInstance.y = duck.y;
                duck._cookedInstance.owner = null;
                Thing.ExtraFondle(duck._cookedInstance, DuckNetwork.localConnection);
                duck.ReturnItemToWorld(duck._cooked);
                duck._cooked.vSpeed = duck.vSpeed;
                duck._cooked.hSpeed = duck.hSpeed;
            }
        } else {
            duck._cooked = new CookedDuck(duck.x, duck.y);
            duck.ReturnItemToWorld(duck._cooked);
            duck._cooked.vSpeed = duck.vSpeed;
            duck._cooked.hSpeed = duck.hSpeed;
            Level.Add(duck._cooked);
        }

        duck.OnTeleport();
        duck.y -= 25000f;
        }

        yield return 0.1f;
        phantomDuck = new Duck(Ducks.mainDuck.x, Ducks.mainDuck.y, Ducks.mainDuck.profile);
        phantomDuck.position = Ducks.mainDuck.position;
        Level.Add(phantomDuck);
        yield return null;
        var hat = phantomDuck.hat;
        phantomDuck.Unequip(hat);
        Level.Remove(hat);
        yield return null;
        particleHat.inner._equippedDuck = phantomDuck;
        //phantomDuck.Equip(particleHat.hat, false);
        yield return null;
        CookSilent(phantomDuck);
        phantomDuck.Netted(new Net(0,0, phantomDuck));
        yield return null;
        phantomDuck._trappedInstance.infinite = true;
        phantomDuck._cookedInstance.solid = false;

        yield return null;
        phantomDuck._trappedInstance.solid = false;
        phantomDuck._trappedInstance._destroyed = true;
        phantomDuck._trappedInstance.visible = false;
        yield return null;
        phantomDuck._cookedInstance._destroyed = true;
    }

    internal override void Draw(GameTime gameTime) {
        ImGui.Begin("little test");
        ImGui.Text($"Things amount on level: {Level.current.things.Count}");
        ImGui.End();
    }

    internal override void OnRemove() {
        Level.Remove(phantomDuck);
    }

    internal override void Update(GameTime gameTime) {
        rock.position.x = -1000;
        if (Ducks.mainDuck.hat is not null) {
            Level.Remove(Ducks.mainDuck.hat);
        }
        if (phantomDuck != null) {
            phantomDuck.position = Ducks.mainDuck.position;
            if (phantomDuck._cookedInstance != null) {
                phantomDuck._cookedInstance.position = Ducks.mainDuck.position;
                //phantomDuck._cookedInstance._destroyed = false;

                if (rock.level == null) {
                    Level.Add(rock);
                }
                phantomDuck._cookedInstance.owner = null;
                phantomDuck._cookedInstance.active=false;
                phantomDuck._cookedInstance.position.x += 30;
                phantomDuck._trappedInstance.position = Mouse.positionScreen;
                //phantomDuck._cookedInstance.active = true;
                //phantomDuck._cookedInstance.enablePhysics = false;
                //phantomDuck._cookedInstance.solid = false;
                //phantomDuck._cookedInstance.canPickUp = false;
                //phantomDuck._cookedInstance.active = true;
                //Level.Remove(phantomDuck._cookedInstance)
                //phantomDuck._cookedInstance.solid = false;
                //phantomDuck._cookedInstance.visible = false;
            }
            if (phantomDuck._trappedInstance != null) {
                //phantomDuck._trappedInstance._destroyed = true;
            }
            particleHat.position = Mouse.positionScreen;
            phantomDuck.Equip(particleHat.inner, false);
            //phantomDuck.position = Mouse.positionScreen;
            phantomDuck.invincible = true;
            phantomDuck.solid = false;
            phantomDuck.visible = false;
            phantomDuck.immobilized = true;
            phantomDuck.enablePhysics = false;
        }
        runner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        sprite.update(gameTime);
        particleHat.Update(gameTime);
    }
}

internal class WearableHat : AbstractHat {
    internal OneOf<VanillaHat, (DepthAnimHat, VanillaHat)> innerHat;
    internal Option<VanillaHat> emptyHat;
    internal WearableHatData hatData;
    internal Option<DynValue> luaHat;

    //shouldn't the hat recieve only data and create bitmap by itself?
    //Upd: no, I *am* supposed to provide the png myself. The path inside data is for the higher level code to load bitmaps, not for the hat itself.
    //Reason? Well, for starters, the hat might be zipped.
    //also: aseprite support
    //script is also optional, btw
    internal static WearableHat New(TeamsBitmap bitmap, WearableHatData data, Option<List<Animation>> anims, Option<LuaScript> scriptData) {
        var animations = anims.ValueOr(data.animations);

        OneOf<VanillaHat, (DepthAnimHat, VanillaHat)> hat;
        var sprite = HatSprite.New(animations);
        sprite.setAnim("OnDefault");

        var needDepthHat = data.customDepth.HasValue ||
            (data.baseData.frameSize[0] > Constants.MIN_FRAME_SIZE || data.baseData.frameSize[1] > Constants.MIN_FRAME_SIZE);

        if (!needDepthHat) {
            var vanillaHat = VanillaHat.New(bitmap);
            vanillaHat.strappedOn = data.strappedOn;
            vanillaHat.sprite = sprite;
            Ducks.mainDuck.Equip(((VanillaHat)vanillaHat).inner, false);
            HatsOnLevel.Add(vanillaHat);
            hat = vanillaHat;
        } else {
            var depthAnimHat = DepthAnimHat.New(bitmap, None, false);
            depthAnimHat.sprite = sprite;
            depthAnimHat.depth = data.customDepth ?? Ducks.mainDuck.depth.value + 0.05f;
            HatsOnLevel.Add(depthAnimHat);

            var emptyBitmap = Bitmap.Empty(32, 32);
            var teams = TeamsStorage.LoadTeams(emptyBitmap, None, None).Unwrap();
            var emptyHat = VanillaHat.New(teams);
            emptyHat.sprite.forceCurrentFrame = 0;
            HatsOnLevel.Add(emptyHat);

            emptyHat.strappedOn = true;
            Ducks.mainDuck.Equip(emptyHat.inner, false);

            hat = (depthAnimHat, emptyHat);
        }

        if (scriptData.IsSome && scriptData.ValueUnsafe() is var script) {
            script.Spawn();
        }

        var wearable = new WearableHat {
            hatData = data,
            scriptData = scriptData,
            sprite = sprite,
            innerHat = hat,
        };

        if (scriptData.IsSome && scriptData.ValueUnsafe() is var script1) {
            wearable.luaHat = wearable.innerHat.Match(
                (hat) => {
                    return DynValue.FromObject(script1, hat);
                },
                (hats) => {
                    return DynValue.FromObject(script1, hats.Item1);
                }
            );
        }
        return wearable;
    }

    internal override void Draw(GameTime gameTime) {
        if (scriptData.ValueUnsafe() is var script && scriptData.IsSome) {
            script.ProtectedCall("draw");
        }
    }

    internal override void OnPressQuack() {
        sprite.setAnim(AnimTypes.OnPressQuack);
    }

    internal override void OnReleaseQuack() {
        sprite.setAnim(AnimTypes.OnReleaseQuack);
    }

    internal override void OnRemove() {
    }

    internal override void Update(GameTime gameTime) {
        innerHat.Switch(
            (hat) => {
                if (Ducks.mainDuck.hat == null) {
                    return;
                }

                if (scriptData.IsSome && scriptData.ValueUnsafe() is var script) {
                    LuaUtils.UpdateScriptData(script);
                    script.Update(gameTime, this.luaHat);

                    innerHat.Match(
                        (hat) => {

                            return Unit.Default;
                        },
                        (hats) => {
                            hats.Item1.FillFromLua(luaHat.Unwrap().Table);
                            return Unit.Default;
                        }
                    );
                }
                hat.Update(gameTime);
            },
            ((DepthAnimHat depthHat, VanillaHat vanillaHat) args) => {
                if (Ducks.mainDuck.hat == null) {
                    return;
                }
                var depthHat = args.depthHat;
                var vanillaHat = args.vanillaHat;

                depthHat.depth = hatData.customDepth ?? vanillaHat.inner.depth.value + 0.05f;
                depthHat.flippedHorizontally = Ducks.mainDuck.offDir == -1;
                depthHat.position = vanillaHat.inner.position;
                depthHat.angle = vanillaHat.inner.angleDegrees;
                depthHat.sprite.forceCurrentFrame = 0;

                depthHat.Update(gameTime);
            }
        );
        //    var wearableTable = DynValue.NewTable(script);
        //    //wearableTable.Table["sprite"] = hat.sprite;
        //    //wearableTable.Table["depth"] = ((VanillaHat)hat).hat.depth.value;
        //    //wearableTable.Table["position"] = ((VanillaHat)hat).hat.position;
        //    script.Update(gameTime, wearableTable);
        //}
    }
}

internal class DepthHat : AbstractHat {
    internal bool Ready { get; private set; }
    internal List<TeamHat> hats;
    internal IVector2 hatsAmount;
    bool firstInactiveWait;
    internal List<int> normalIndices;
    internal List<int> horizIndices;
    CoroutineRunner coroutines;
    CoroutineHandle setupStateHandle;
    CoroutineHandle updateDepthHandle;
    DepthHatState state = DepthHatState.Regular;
    internal bool firstFrameWait;
    internal bool removed;

    internal DepthHatState State { get => state; private set {
            state = value; 
        }
    }
    internal HatId Id { get => id; }

    internal override void Draw(GameTime gameTime) { }

    internal override void OnRemove() {
        foreach (var hat in hats) {
            Level.Remove(hat);
        }
        Level.Remove(rock);
        removed = true;
    }

    internal static DepthHat New(TeamsBitmap teamsBitmap, Option<ScoreRock> rockOption, bool update) {
        var firstTeamHandle = teamsBitmap.frames.Get(0).AndThen((f) => f.teamHandles.Get(0));
        var teamSize = firstTeamHandle.AndThen((h) => TeamsStorage.GetTeamData(h)).Map((d) => d.image.Size).ValueOr(new IVector2(Constants.MIN_HAT_SIZE));

        var hatsAmountX = (int)Math.Ceiling((float)teamsBitmap.frameSizeWithGaps.X / (float)teamSize.X);
        var hatsAmountY = (int)Math.Ceiling((float)teamsBitmap.frameSizeWithGaps.Y / (float)teamSize.Y);

        var hatsAmount = new IVector2(hatsAmountX, hatsAmountY);
        var (normalIndices, horizontalIndices) = Functions.GetIndices(hatsAmount);

        var depthHat = new DepthHat {
            should_update = update,
            teamsBitmap = teamsBitmap,
            sprite = HatSprite.New(),
            hats = new(),
            hatsAmount = hatsAmount,
            normalIndices = normalIndices,
            horizIndices = horizontalIndices,
            rock = rockOption.ValueOrElse(() => {
                var rock = new ScoreRock(0, -1000, DuckNetwork.localProfile ?? Profiles.DefaultPlayer1);
                rock.depth = 10;
                Level.Add(rock);
                return rock;
            }),
            coroutines = new CoroutineRunner(),
        };
        depthHat.coroutines.Run(depthHat.GetReady());

        depthHat.State = DepthHatState.Regular;

        if (teamsBitmap.frames.Count > 0) {
            for (int i = 0; i < hatsAmountX * hatsAmountY; i++) {
                var hat = new TeamHat(0, 0, TeamsStorage.GetTeamData(teamsBitmap.frames[0].teamHandles[0]).Unwrap().team);
                HatsOnLevel.AddTeamHat(hat);
                depthHat.hats.Add(hat);
            }
        }

        return depthHat;
    }

    internal IEnumerator GetReady() {
        //SetState(DepthHatState.Depth);
        yield return 0.1f;
        Ready = true;
    }

    internal IEnumerator SetStateCoroutine(DepthHatState newState) {
        var oldState = this.state;
        this.state = newState;
        if (!firstFrameWait) {
            //Ensure nothing happens with the hat on the first frame to prevent sync issues.
            firstFrameWait = true;
            yield return null;
        }

        switch (oldState) {
            case DepthHatState.DepthInactive:
                switch (State) {
                    case DepthHatState.Depth:
                        foreach (var hat in hats) {
                            hat.owner = rock;
                            rock.depth = depth;
                            hat.active = true;
                        }
                        yield break;
                    case DepthHatState.Regular:
                        foreach (var hat in hats) {
                            hat.active = true;
                        }
                        yield break;
                }
                break;
            case DepthHatState.Depth:
                switch (State) {
                    case DepthHatState.DepthInactive:
                        foreach (var hat in hats) {
                            hat.owner = null;
                            hat.active = false;
                        }
                        yield break;
                    case DepthHatState.Regular:
                        foreach (var hat in hats) {
                            hat.owner = null;
                            hat.active = true;
                        }
                        yield break;
                }
                break;
            case DepthHatState.Regular:
                switch (State) {
                    case DepthHatState.Depth:
                        foreach (var hat in hats) {
                            hat.owner = rock;
                            rock.depth = depth;
                        }
                        yield break;
                    case DepthHatState.DepthInactive:
                        foreach (var hat in hats) {
                            rock.depth = depth;
                            hat.owner = rock;
                        }

                        yield return null;

                        if (!firstInactiveWait) {
                            firstFrameWait = true;
                            yield return null;
                            yield return null;
                        }

                        foreach (var hat in hats) {
                            hat.owner = null;
                            hat.active = false;
                        }

                        yield break;
                }
                break;
        }
    }

    internal bool SetState(DepthHatState newState) {
        if (this.State == newState) {
            return false;
        }

        if (setupStateHandle.IsRunning) {
            coroutines.Stop(setupStateHandle);
        }
        setupStateHandle = coroutines.Run(SetStateCoroutine(newState));

        return true;
    }

    internal IEnumerator UpdateDepthCoroutine() {
        yield return SetStateCoroutine(DepthHatState.Depth);
        yield return SetStateCoroutine(DepthHatState.DepthInactive);
    }

    internal void UpdateDepth() {
        if (this.state != DepthHatState.DepthInactive) {
            return;
        }
        updateDepthHandle = coroutines.Run(UpdateDepthCoroutine());
    }

    internal override void Update(GameTime gameTime) {
        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        rock.position.y = -1000;
        rock.depth = new Depth(depth);

        sprite.update(gameTime);
        var currentFrame = sprite.CurrentFrame;

        var currentTeams = teamsBitmap.frames[currentFrame.value].teamHandles;
        float hatOffset = teamsBitmap.chopMode.Map((m) => m == ChopMode.Simple ? 32 : 31).ValueOr(32);
        for (int x = 0; x < hatsAmount.X; x++) {
            for (int y = 0; y < hatsAmount.Y; y++) {
                var hatIndex = y * hatsAmount.X + x;
                var hat = hats[hatIndex];
                var teamHandle = currentTeams[hatIndex];
                var teamDataOption = TeamsStorage.GetTeamData(teamHandle);
                if (teamDataOption.ValueUnsafe() is var teamData && teamDataOption.IsSome) {
                    hat.strappedOn = strappedOn;
                    hat.team = teamData.team;
                    var virtualIndex = flippedHorizontally ? horizIndices[hatIndex] : normalIndices[hatIndex];
                    var virtualPosition = Functions.CoordsFromIndex(virtualIndex, hatsAmount.X);
                    var virtualPositionVec = new Vec2(virtualPosition.X, virtualPosition.Y);

                    hat.position = virtualPositionVec * hatOffset + position + new Vec2(teamData.image.Size.X / 2, teamData.image.Size.Y / 2);
                    hat.flipHorizontal = flippedHorizontally;
                }
            }
        }

        if (flippedHorizontally && hatsAmount.X > 1) {
            foreach (var hat in hats) {
                var hatSizeX = hatsAmount.X * Constants.MIN_HAT_SIZE;
                hat.position.x -= (hatSizeX - teamsBitmap.frameSize.X) - 1;
            }
        }

        for (int x = 0; x < hatsAmount.X; x++) {
            for (int y = 0; y < hatsAmount.Y; y++) {
                var hatIndex = y * hatsAmount.X + x;
                var hat = hats[hatIndex];
                hat.position -= new Vec2((float)teamsBitmap.frameSize.X / 2f, (float)teamsBitmap.frameSize.Y / 2f);
            }
        }

        for (int x = 0; x < hatsAmount.X; x++) {
            for (int y = 0; y < hatsAmount.Y; y++) {
                var hatIndex = y * hatsAmount.X + x;
                var hat = hats[hatIndex];
                hat.angle = angle;
                var vecToHat = hat.position - position;
                var rotationVec = vecToHat.Rotate(hat.angle, Vec2.Zero);
                hat.position = position + rotationVec;
            }
        }

    }
}

[MoonSharpUserData]
internal record struct HatId {
    [MoonSharpVisible(true)]
    public uint id;
    [MoonSharpVisible(true)]
    public uint gen;

    public static HatId New(uint id, uint gen) {
        return new HatId { 
            id = id,
            gen = gen
        };
    }
}


internal struct HatStorageData {
    internal AbstractHat hat;
}


/// <summary>
/// Tracks all hats which are loaded during the current level. Hats are cleared after a new level is loaded.
/// </summary>
internal static class HatsOnLevel {
    internal static int lastId;
    internal static Queue<HatId> recycledIds;
    internal static Dictionary<HatId, HatStorageData> hatsData;
    internal static Dictionary<int, ScoreRock> inactiveHatDepths = [];
    internal static bool updating;
    internal static List<TeamHat> teamHats = [];
    internal static List<AbstractHat> hatsToAdd = [];
    internal static List<AbstractHat> hatsToRemove = [];
    internal static CoroutineRunner runner;
    internal static GameTime gameTime;
    //needed to make sure that only depth hats change state while ActivateAll() is running
    internal static bool onlyUpdateDepthHats;
    internal static Option<CoroutineHandle> activateAllHandle;

    internal static void ActivateAll() {
        if (activateAllHandle.IsSome && activateAllHandle.Value() is var handle && runner.IsRunning(handle)) {
            runner.Stop(handle);
        }
        activateAllHandle = runner.Run(ActivateAllCoroutine());
    }

    internal static IEnumerator ActivateAllCoroutine() {
        Dictionary<HatId, DepthHatState> states = [];
        List<DepthHat> depthHats = hatsData.Values.Map((v) => v.hat).Where((h) => h is DepthHat hat).Map((h) => h as DepthHat).ToList();

        onlyUpdateDepthHats = true;
        yield return 0.1f;
        foreach (var hat in depthHats) {
            states[hat.id] = hat.State;
        }

        foreach (var hat in depthHats) {
            hat.SetState(DepthHatState.Regular);
        }

        yield return null;
        yield return null;
        yield return null;

        foreach (var hat in depthHats) {
            var state = states[hat.id];
            hat.SetState(state);
        }

        yield return null;
        yield return null;
        yield return null;

        onlyUpdateDepthHats = false;
        activateAllHandle = None;
    }

    internal static void Init() {
        hatsData = [];
        recycledIds = [];

        runner = new CoroutineRunner();
        runner.Run(UpdateCoroutine());
    }

    internal static void OnLevelStart() {
        inactiveHatDepths = [];
        HatsOnLevel.RemoveAll();
        teamHats = [];
    }

    internal static Option<AbstractHat> Get(HatId id) {
        if (!HatsOnLevel.IsAlive(id)) {
            return None;
        }

        return hatsData.Get(id).Map((data) => data.hat);
    }

    internal static bool IsAlive(HatId id) {
        if (HatsOnLevel.hatsData.Get(id).Value() is var hatData && hatData.hat != null) { } else {
            return false;
        }

        return hatData.hat.id.gen == id.gen;
    }

    internal static HatId NewHatId() {
        if (recycledIds.Count > 0) {
            var recycledId = recycledIds.Dequeue();
            recycledId.gen += 1;
            return recycledId;
        }

        lastId += 1;
        return HatId.New((uint)lastId-1, 0);
    }

    internal static AbstractHat Add(AbstractHat hat) {
        var data = new HatStorageData {
            hat = hat,
        };
        if (updating) {
            hatsToAdd.Add(hat);
            return hat;
        }
        hat.id = NewHatId();
        hatsData.Add(hat.id, data);
        return hat;
    }

    internal static void Remove(AbstractHat hat) {
        RemoveById(hat.id);
    }

    internal static void RemoveById(HatId id) {
        if (hatsData.RemoveGet(id).ValueUnsafe() is var data && data.hat is not null) {
            recycledIds.Enqueue(id);

            if (updating) {
                hatsToRemove.Add(data.hat);
                return;
            }
            data.hat.OnRemove();
        }
    }
    
    internal static void RemoveAll() {
        foreach (var data in hatsData.Values) {
            data.hat.OnRemove();
            recycledIds.Enqueue(data.hat.id);
        }
        foreach (var hat in teamHats) {
            Level.Remove(hat);
        }

        hatsData.Clear();
    }

    internal static void AddTeamHat(TeamHat hat) {
        Level.Add(hat);
        teamHats.Add(hat);
    }

    internal static void Update(GameTime gameTime) {
        HatsOnLevel.gameTime = gameTime;
        runner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    internal static IEnumerator UpdateCoroutine() {
        while (true) {
            StartUpdating();
            foreach (var data in hatsData.Values) {
                if (onlyUpdateDepthHats && data.hat is not DepthHat hat) {
                    continue;
                } 

                if (data.hat.should_update) {
                    data.hat.Update(gameTime);
                    if (Ducks.mainDuck != null) {
                        if (Ducks.mainDuck.inputProfile.Pressed("QUACK")) {
                            data.hat.OnPressQuack();
                        }
                        if (Ducks.mainDuck.inputProfile.Released("QUACK")) {
                            data.hat.OnReleaseQuack();
                        }
                    }
                }

            }
            FinishUpdating();

            yield return null;
        }
    }

    internal static void StartUpdating() {
        updating = true;
    }

    internal static void FinishUpdating() {
        updating = false;
        foreach (var hat in hatsToAdd) {
            hat.id = NewHatId();
            hatsData.Add(hat.id, new HatStorageData { hat = hat });
        }
        foreach (var hat in hatsToRemove) {
            hatsData.Remove(hat.id);
        }
        hatsToAdd.Clear();
        hatsToRemove.Clear();
    }

    internal static void Draw(GameTime gameTime) {
        StartUpdating();
        foreach (var data in hatsData.Values) {
            data.hat.Draw(gameTime);
        }
        FinishUpdating();
    }

}
internal class Preload {
    public StbImageSharp.ImageResult image;
}
internal static class Preloading {
    internal static void Preload() {
        //static async IAsyncEnumerable<int> GetValuesAsync() {
        //    for (int i = 0; i < 10; i++) {
        //        await Task.Delay(TimeSpan.FromSeconds(1));
        //        yield return i;
        //    }
        //}
        Preload preloading = new Preload {
            image = null
        };
    }
}
/*
How do depth changing movable animatable hats work?
Basically, a hat can have an owner set so that it depth is changed, but it position becomes fixed.
We can counter this by making the hat inactive, which will free it, but make it unable to animate.

So, what if we have a double buffered hat system?
Imagine we are playing an animation, and we want to display the frame 1 while also having a depth.
We achieve that by firstly setting the depth and frame (1st in-game frame), then making the hat inactive and moving it to a certain position
(2nd in-game frame). Which means, to make this work without one in-game frame delay, we need to have an already configured set of hats ready to be put in a position. But can we achieve that just with two sets of hats?
It's important to note that we can only change hat's state once per each frame.
So, let's begin.
We start with the hat set ONE already preconfigured to display the first frame and locked in place. It's imortant to note that this hat(s) must also be somewhere away, so that they dont' show up before we need them to.
Anyway, during the first frame we transport hat set ONE to a needed location while also making it inactive. At the same time we make the hat set TWO display second frame of the animation. To do that, that hat must not be deactivated. 
Now, the second in-game frame. We move the first hat away while also making it active (WHICH WE CANT DO AT THE SAME TIME) and setting it to dislay the third animation frame. This would require two frames, but we only have one to spare.
Conclusion: two sets of hats is not enough :(

Now let's reconsider using three sets of hats.
Let's outline our states and transitions so it's easier.
STATE 1: no owner + activated. +MOVE, +ANIMATE, -DEPTH. alias: NO_DEPTH
STATE 2: owner + activated. -MOVE, +ANIMATE, +DEPTH. alias: NO_MOVE
STATE 3: owner + diactivated. +MOVE, -ANIMATE, +DEPTH. alias: NO_ANIM

Transitions:
STATE 1 -> STATE 2 (NO_DEPTH -> NO_MOVE): can set DEPTH and ANIM
STATE 2 -> STATE 1 (NO_MOVE -> NO_DEPTH): can ANIM and MOVE 
STATE 2 -> STATE 3 (NO_MOVE -> NO_ANIM): can set DEPTH and MOVE
STATE 3 -> STATE 2 (NO_ANIM -> NO_MOVE): can set DEPTH and ANIM

While transitioning to another state, we can at the same time perform action that the state supports, but we can't perform one which it lacks. Also, one transition takes one in-game frame.

The goal is to achieve an illusion of a hat that do all 3 things by swapping hats. If there's 3 states, we can guess that exactly 3 hat sets will be required for the job.

Each hat set will be in either of the 3 sets, and they will change each frame. But how?
Assume we have hat sets in states 1, 2, and 3. The displayed state will always be state 3, because it's the only state which we can move just in time while it also having the right depth. So, we can call STATE 3 (NO_ANIM) the main state. So we need to have this state in position for each and every frame.
It means that at the start of the frame we need to have a set in STATE 3 which has correct frame.
The logical solution is to cycle between the states, like so:
1. 1 2 3!
2. 2 3! 1
2. 3! 1 2
2. 1 2 3!
(first state number represent set 1, second - set 2, etc.)

Now let's try to visualise this.
State 1: red,
State 2: blue,
State 3: green
 */

//internal interface DGHats {
//    void SetTeamFrame(ref TeamFrame frame);
//    void MoveTo(Vector2 position);
//    void Rotate(float angle);
//    void SetFlip(bool flip);
//    void SetDepth(Depth depth);
//}
