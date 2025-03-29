using Coroutines;
using DuckGame;
using HatsPlusPlus.Parsing;
using ImGuiNET;
using LanguageExt.SomeHelp;
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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Script = MoonSharp.Interpreter.Script;

namespace HatsPlusPlus;

/*
What do I need? I need a hat class that will be able to do everything that a hat needs, which is:
    1) store and display animations,
    2) have size up to 64x64 pixel,
    3) be able to flip, change position and angle
    4) be able to change depth if needed, while also animating in moving freely (is hard)
 */

//public struct TeamFrame {
//    public List<TeamData> teams;
//}


/// <summary>
/// This class represents a custom hat object, which behaves like a normal hat expect it can have animations,
/// and changeable depth
/// </summary>
//public class CustomHat {
//    //public Vector2 position;
//    //public float  angle;
//    //public bool flipped;
//    //public List<TeamFrame> frames;
//}

public struct TeamData {
    public Team team;
    public TeamHandle handle;
    public Bitmap image;
}


[MoonSharpUserData]
public struct TeamFrame {
    [MoonSharpVisible(true)]
    public List<TeamHandle> teamHandles;

    public static TeamFrame New(List<TeamHandle> handles) {
        return new TeamFrame {
            teamHandles = handles
        };
    }
}

[MoonSharpUserData]
public struct TeamsBitmap {
    [MoonSharpVisible(true)]
    public IVector2 frameSize;
    [MoonSharpVisible(true)]
    public List<TeamFrame> frames;
    /// <summary>
    /// Indicates that frames are bigger than 32x32 (=each frame consists of multiple teams)
    /// </summary>
    [MoonSharpVisible(true)]
    public bool isBig;
}


public abstract class AbstractHat {
    public Vec2 position;
    public float depth;
    public float angle;
    public bool flippedHorizontally;
    public ScoreRock rock;
    public TeamsBitmap teamsBitmap;
    public HatSprite sprite;


    public abstract HatId GetId();
    public abstract void Remove();
    public abstract void Init(Option<DynValue> luaState);
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime);

    public bool IsAlive() {
        return Hats.IsAlive(GetId());
    }
}

public enum DepthHatState {
    Regular = 1,
    Depth, 
    DepthInactive
}

public enum InnerDepthHatState {
    None,
    ToRegular,
    ToDepthInactive,
}

static class Functions {
    public static IVector2 CoordsFromIndex(int index, int width) {
        return (index % width, index / width);
    }

    public static (List<int>, List<int>) GetIndices(IVector2 hatsAmount) {
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

    public static Option<AnimFrame> NextAnimFrame(int currentFrameId, Animation anim) {
        return anim.frames.Get(currentFrameId).Map((frame) => {
            if (frame.value == anim.frames.Length() - 1) {
                return frame.WithFrame(anim.looping ? 0 : frame.value);
            }
            return frame.WithFrame(frame.value + 1);
        });
    }
}


public enum DepthAnimState {
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
public enum ChangeFrameState {
    None,
    Frame1,
    Frame2,
}

public enum NewAnim {
    Yes,
    No
}

public class ScriptableHat : AbstractHat {
    HatId id;
    public Option<DynValue> luaState;

    public static ScriptableHat New() {
        return new ScriptableHat {
            id = Hats.NewHatId()
        };
    }

    public override HatId GetId() {
        return id;
    }

    public override void Init(Option<DynValue> luaState) {
        this.luaState = luaState;
        var value = luaState.Unwrap();
        //TryCall("init");
    }

    public override void Remove() {
        TryCall("remove");
    }

    void TryCall(string functionName, params object[] args) {
        try {

            if (luaState.ValueUnsafe() is var state && luaState.IsSome) {
                var functionTable = state.Table.Get(functionName);
                if (functionTable.Function is var fn && fn is not null) {
                    fn.Call(args);
                }
            }
        }
        catch (ScriptRuntimeException e) {
            LuaLogger.Log($"Error: {e.DecoratedMessage ?? e.Message}");
        }
    }

    public override void Update(GameTime gameTime) {
        TryCall("update", gameTime);
    }

    public override void Draw(GameTime gameTime) {
        TryCall("draw", gameTime);
    }
}

public class DepthAnimHat: AbstractHat {
    public DepthHat[] hats;
    public IVector2 hatsAmount;
    List<int> normalIndices;
    List<int> horizIndices;
    DepthAnimState state;
    Option<DepthHat> shownHat;
    CoroutineRunner coroutines;
    ChangeFrameState changeFrameState;
    bool cancelChangingFrame;
    bool forceChangeFrame;
    Option<CoroutineHandle> changeFrameHandler;
    Option<CoroutineHandle> changeAnimHandler;
    bool firstAnimSet;
    HatId id;
    int previousFrameId = -1;

    static Vec2 OFF_SCREEN_POS = new Vec2(0, -10_000);

    public override void Draw(GameTime gameTime) {
    }

    public override HatId GetId() {
        return id;
    }

    public override void Remove() {
        foreach (var hat in hats) {
            hat.Remove();
        }
    }

    public override void Init(Option<DynValue> luaState) {
        var hats = new DepthHat[] {
            Hats.Add(DepthHat.New(teamsBitmap, rock)) as DepthHat,
            Hats.Add(DepthHat.New(teamsBitmap, rock)) as DepthHat,
        };

        foreach (var hat in hats) {
            hat.position = OFF_SCREEN_POS;
        }

        this.hats = hats;
    }

    public static DepthAnimHat New(TeamsBitmap teamsBitmap, Option<ScoreRock> rockOption) {
        var hatsAmountX = (int)Math.Ceiling((float)teamsBitmap.frameSize.X / (float)Constants.MIN_DG_HAT_SIZE);
        var hatsAmountY = (int)Math.Ceiling((float)teamsBitmap.frameSize.Y / (float)Constants.MIN_DG_HAT_SIZE);
        var hatsAmount = new IVector2(hatsAmountX, hatsAmountY);
        var (normalIndices, horizontalIndices) = Functions.GetIndices(hatsAmount);

        var hat = new DepthAnimHat {
            teamsBitmap = teamsBitmap,
            sprite = HatSprite.New(),
            hatsAmount = hatsAmount,
            normalIndices = normalIndices,
            horizIndices = horizontalIndices,
            rock = rockOption.ValueOrElse(() => {
                var rock = new ScoreRock(0, 0, DuckNetwork.localProfile ?? Profiles.DefaultPlayer1);
                Level.Add(rock);
                return rock;
            }),
            hats = null,
            coroutines = new CoroutineRunner(),
            id = Hats.NewHatId()
        };

        return hat;
    }

    IEnumerator ChangeFrame(NewAnim newAnim = NewAnim.No) {
        //so we don't waste time changing frames if it's the same one
        if (sprite.currentFrameId == previousFrameId) {
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
            yield break;
        }
        if (!((hats[0].State == DepthHatState.DepthInactive && hats[1].State == DepthHatState.Depth) || (
            hats[0].State == DepthHatState.Depth && hats[1].State == DepthHatState.DepthInactive))) {
            throw new Exception("expected hat state to be: 1 Depth, 1 DepthInactive");
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

            yield return ChangeFrame(NewAnim.Yes);
            yield break;
        }

        var depthHat = FindHatWith(DepthHatState.Depth).Unwrap(); 
        var depthInactiveHat = FindHatWith(DepthHatState.DepthInactive).Unwrap();
        depthHat.sprite.forceCurrentFrame = sprite.CurrentFrame.value;
        yield return null;

        yield return ChangeFrame(NewAnim.Yes);
    }

    DepthHat SwapShownHat(DepthHat newShownHat) {
        var currentShownHat = shownHat.Unwrap();
        var oldShownHat = currentShownHat;
        currentShownHat.position = OFF_SCREEN_POS;
        shownHat = newShownHat;
        return oldShownHat;
    }

    public override void Update(GameTime gameTime) {
        if (sprite.AnimChanged || sprite.ForceFrameChanged) {
            if (changeFrameState == ChangeFrameState.Frame1) {
                if (changeFrameHandler.Value() is var change_handler && this.changeFrameHandler.IsSome) {
                    coroutines.Stop(change_handler);
                }
                changeFrameHandler = None;
            }
            changeAnimHandler = coroutines.Run(ChangeAnim());
        } else if (sprite.FrameChanged && changeAnimHandler.Map((h) => !coroutines.IsRunning(h)).ValueOr(true)) {
            changeFrameHandler = coroutines.Run(ChangeFrame());
        }
        if (Keyboard.Pressed(Keys.O)) {
            changeFrameHandler = coroutines.Run(ChangeFrame());
        }

        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (this.shownHat.ValueUnsafe() is var shownHat && shownHat is not null) {
            shownHat.position = position;
            shownHat.angle = angle;
            shownHat.depth = depth;
        }

        foreach (var hat in hats) {
            hat.depth = depth;
            if (this.shownHat.Map((h) => h != hat).ValueOr(true)) {
                hat.position = OFF_SCREEN_POS;
            }
        }
        sprite.update(gameTime);
    }

    public Option<DepthHat> FindHatWith(DepthHatState state) {
        foreach (var hat in hats) {
            if (hat.State == state) {
                return hat;
            }
        }
        return None;
    }
}

public class VanillaHat: AbstractHat {
    public TeamHat hat;
    HatId id;

    public static VanillaHat New(TeamsBitmap bitmap) {
        var hat = new TeamHat(0, 0, null);
        Level.Add(hat);

        return new VanillaHat {
            id = Hats.NewHatId(),
            hat = hat,
            teamsBitmap = bitmap,
            sprite = HatSprite.New(),
        };
    }

    public override void Draw(GameTime gameTime) {

    }

    public override HatId GetId() {
        return id;
    }

    public override void Init(Option<DynValue> luaState) {
    }

    public override void Remove() {
        Level.Remove(hat);
    }

    public override void Update(GameTime gameTime) {
        sprite.update(gameTime);
        var currentFrame = sprite.CurrentFrame;
        var teamFrame = teamsBitmap.frames[currentFrame.value].teamHandles;
        var teamData = TeamsStorage.GetTeamData(teamFrame[0]);
        teamData.IfSome((data) => hat.team = data.team);
    }
}

public class WearableHat : AbstractHat {
    public HatId id;
    public WearableHatData hatData;
    public Option<DynValue> luaState;
    public Script script;
    public AbstractHat hat;

    void TryCall(string functionName, params object[] args) {
        try {

            if (luaState.ValueUnsafe() is var state && luaState.IsSome) {
                var functionTable = state.Table.Get(functionName);
                if (functionTable.Function is var fn && fn is not null) {
                    fn.Call(args);
                }
            }
        }
        catch (ScriptRuntimeException e) {
            LuaLogger.Log($"Error: {e.DecoratedMessage ?? e.Message}");
        }
    }

    public static WearableHat New(Script script,TeamsBitmap bitmap, WearableHatData data) {
        AbstractHat hat;
        if (data.baseData.frameSize[0] <= 32 && data.baseData.frameSize[1] <= 32) {
            hat = VanillaHat.New(bitmap);
            Ducks.MainDuck.Equip(((VanillaHat)hat).hat, false);
        } else {
            hat = DepthAnimHat.New(bitmap, None);
        }
        Hats.Add(hat);
        return new WearableHat {
            id = Hats.NewHatId(),
            hatData = data,
            hat = hat,
        };
    }

    public override void Draw(GameTime gameTime) {
        TryCall("draw", gameTime);
    }

    public override HatId GetId() {
        return id;
    }

    public override void Init(Option<DynValue> luaState) {
        this.luaState = luaState;
    }

    public override void Remove() {

    }

    public override void Update(GameTime gameTime) {
        var wearableTable = DynValue.NewTable(script);
        wearableTable.Table["sprite"] = hat.sprite;
        wearableTable.Table["depth"] = ((VanillaHat)hat).hat.depth.value;

        TryCall("update", gameTime, wearableTable);
    }
}

public class DepthHat : AbstractHat {
    public List<TeamHat> hats;
    public IVector2 hatsAmount;
    List<int> normalIndices;
    List<int> horizIndices;
    CoroutineRunner coroutines;
    HatId id;
    DepthHatState state = DepthHatState.Regular;

    public DepthHatState State { get => state; private set {
            state = value; 
        }
    }
    public HatId Id { get => id; }

    [MoonSharpVisible(false)]
    public override HatId GetId() {
        return id;
    }

    public override void Draw(GameTime gameTime) { }

    public override void Remove() {
        foreach (var hat in hats) {
            Level.Remove(hat);
        }
        Level.Remove(rock);
    }

    public override void Init(Option<DynValue> luaState) { }

    public static DepthHat New(TeamsBitmap teamsBitmap, Option<ScoreRock> rockOption) {
        var hatsAmountX = (int)Math.Ceiling((float)teamsBitmap.frameSize.X / (float)Constants.MIN_DG_HAT_SIZE);
        var hatsAmountY = (int)Math.Ceiling((float)teamsBitmap.frameSize.Y / (float)Constants.MIN_DG_HAT_SIZE);
        var hatsAmount = new IVector2(hatsAmountX, hatsAmountY);
        var (normalIndices, horizontalIndices) = Functions.GetIndices(hatsAmount);

        var depthHat = new DepthHat {
            teamsBitmap = teamsBitmap,
            sprite = HatSprite.New(),
            hats = new(),
            hatsAmount = hatsAmount,
            normalIndices = normalIndices,
            horizIndices = horizontalIndices,
            rock = rockOption.ValueOrElse(() => {
                var rock = new ScoreRock(0, 0, DuckNetwork.localProfile ?? Profiles.DefaultPlayer1);
                Level.Add(rock);
                return rock;
            }),
            coroutines = new CoroutineRunner(),
            id = Hats.NewHatId()
        };

        depthHat.State = DepthHatState.Regular;

        for (int i = 0; i < hatsAmountX * hatsAmountY; i++) {
            var hat = new TeamHat(0, -10000, TeamsStorage.GetTeamData(teamsBitmap.frames[0].teamHandles[0]).Unwrap().team);
            Level.Add(hat);
            depthHat.hats.Add(hat);
        }

        return depthHat;
    }

    IEnumerator SetupState(DepthHatState previousState) {
        switch (previousState) {
            case DepthHatState.DepthInactive:
                switch (State) {
                    case DepthHatState.Depth:
                        foreach (var hat in hats) {
                            hat.owner = rock;
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
                            Hats.inactiveHatDepths[hat.GetHashCode()] = hat.owner.depth.value;

                            hat.owner = null;
                            hat.active = false;
                        }
                        yield break;
                    case DepthHatState.Regular:
                        foreach (var hat in hats) {
                            hat.owner = null;
                        }
                        yield break;
                }
                break;
            case DepthHatState.Regular:
                switch (State) {
                    case DepthHatState.Depth:
                        foreach (var hat in hats) {
                            hat.owner = rock;
                        }
                        yield break;
                    case DepthHatState.DepthInactive:
                        foreach (var hat in hats) {
                            hat.owner = rock;
                            Hats.inactiveHatDepths.Add(hat.GetHashCode(), hat.owner.depth.value);
                        }
                        yield return null;

                        foreach (var hat in hats) {
                            hat.owner = null;
                            hat.active = false;
                        }

                        break;
                }
                break;
        }
    }

    public bool SetState(DepthHatState state) {
        if (this.State == state) {
            return false;
        }

        var oldState = this.State;
        this.State = state;
        coroutines.StopAll();
        coroutines.Run(SetupState(oldState));

        return true;
    }

    public override void Update(GameTime gameTime) {
        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        const float HAT_OFFSET = 31f;

        rock.position.y = -10000;
        rock.depth = new Depth(depth);

        sprite.update(gameTime);
        var currentFrame = sprite.CurrentFrame;
        //TODO: get the frame from the animation if there is one
        //also: update sprite

        var currentTeams = teamsBitmap.frames[currentFrame.value].teamHandles;
        for (int x = 0; x < hatsAmount.X; x++) {
            for (int y = 0; y < hatsAmount.Y; y++) {
                var hatIndex = y * hatsAmount.X + x;
                var hat = hats[hatIndex];
                var teamHandle = currentTeams[hatIndex];
                var teamDataOption = TeamsStorage.GetTeamData(teamHandle);
                if (teamDataOption.ValueUnsafe() is var teamData && teamDataOption.IsSome) {
                    hat.team = teamData.team;
                    var virtualIndex = flippedHorizontally ? horizIndices[hatIndex] : normalIndices[hatIndex];
                    var virtualPosition = Functions.CoordsFromIndex(virtualIndex, hatsAmount.X);
                    var virtualPositionVec = new Vec2(virtualPosition.X, virtualPosition.Y);
                    hat.position = virtualPositionVec * (HAT_OFFSET) + position + new Vec2(Constants.MIN_DG_HAT_SIZE / 2);
                    hat.flipHorizontal = flippedHorizontally;
                }
            }
        }

        if (flippedHorizontally && hatsAmount.X > 1) {
            foreach (var hat in hats) {
                hat.position.x -= hatsAmount.X + 1;
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
                hat.angle = Maths.DegToRad(angle);
                var vecToHat = hat.position - position;
                var rotationVec = vecToHat.Rotate(hat.angle, Vec2.Zero);
                hat.position = position + rotationVec;
            }
        }
    }
}

[MoonSharpUserData]
public record struct HatId {
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
public struct HatStorageData {
    public AbstractHat hat;
}

/// <summary>
/// Tracks all hats which are loaded during the current level. It gets cleared after a new level is loaded.
/// </summary>
public static class Hats {
    public static Queue<HatId> recycledIds;
    public static Dictionary<HatId, HatStorageData> hatsData;
    public static Dictionary<int, float> inactiveHatDepths = [];

    public static void Init() {
        hatsData = [];
        recycledIds = [];
    }

    public static void OnLevelStart() {
        inactiveHatDepths = [];
        Hats.RemoveAll();
    }

    public static Option<AbstractHat> Get(HatId id) {
        if (!Hats.IsAlive(id)) {
            return None;
        }

        return hatsData.Get(id).Map((data) => data.hat);
    }

    public static bool IsAlive(HatId id) {
        if (Hats.hatsData.Get(id).Value() is var hatData && hatData.hat != null) { } else {
            return false;
        }

        return hatData.hat.GetId().gen == id.gen;
    }

    public static HatId NewHatId() {
        if (recycledIds.Count > 0) {
            var recycledId = recycledIds.Dequeue();
            recycledId.gen += 1;
            return recycledId;
        }

        return HatId.New((uint)hatsData.Count, 0);
    }

    public static AbstractHat Add(AbstractHat hat, Option<DynValue> luaState = default) {
        var data = new HatStorageData {
            hat = hat,
        };
        hatsData.Add(hat.GetId(), data);
        hat.Init(luaState);
        return hat;
    }

    public static void Remove(HatId id) {
        if (hatsData.RemoveGet(id).ValueUnsafe() is var data && data.hat is not null) {
            recycledIds.Enqueue(id);
            data.hat.Remove();
        }
    }
    
    public static void RemoveAll() {
        foreach (var data in hatsData.Values) {
            recycledIds.Enqueue(data.hat.GetId());
            data.hat.Remove();
        }
        hatsData.Clear();
    }

    public static void Update(GameTime gameTime) {
        //TODO: FIX REMOVING WHILE ITERATING
        foreach (var data in hatsData.Values) {
            data.hat.Update(gameTime);
        }
    }

    public static void Draw(GameTime gameTime) {
        foreach (var data in hatsData.Values) {
            data.hat.Draw(gameTime);
        }
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

//public interface DGHats {
//    void SetTeamFrame(ref TeamFrame frame);
//    void MoveTo(Vector2 position);
//    void Rotate(float angle);
//    void SetFlip(bool flip);
//    void SetDepth(Depth depth);
//}
