using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DuckGame;
using ImGuiNET;
using System.Linq;
using LanguageExt.UnsafeValueAccess;
using System;
using Coroutines;
using Script = MoonSharp.Interpreter.Script;
using System.IO;
using MoonSharp.Interpreter;
using System.Security.Cryptography;
using Newtonsoft.Json;
using HatsPlusPlus.Parsing;

namespace HatsPlusPlus;

public class Updater {
    Level lastLevel;
    bool wasNetworkActive;
    Script script;

    Option<DepthAnimHat> hat;
    Option<ScriptableHat> scriptableHat;
    CoroutineRunner coroutines;
    Option<WearableHat> wearableHat;
    TeamsBitmap teamsBitmap;
    HatSprite hatSpriteTest;

    public static Updater New() {
        var updater = new Updater();
        updater.coroutines = new CoroutineRunner();

        return updater;
    }

    public void OnEnteringOnline() {
        //TeamsStorage.RemoveAll();
        DevConsole.Log(DCSection.Connection, "Entering online!");
    }

    public void OnLobbyEnter() {

    }

    public void OnLevelEnter() {
        Hats.OnLevelStart();

        if (Level.current is TeamSelect2) {
            OnLobbyEnter();
        }
        if (DuckNetwork.status == DuckNetStatus.Connected) {
            OnEnteringOnline();
        }
    }
    ScoreRock rock;
    public void Update(GameTime gameTime) {
        //if (rock is null) {
        //    rock = new ScoreRock(20, 20, DuckNetwork.localProfile);
        //    rock.depth = -10;
        //    Level.Add(rock);
        //}
        //if (Ducks.MainDuck.ragdoll != null) {
        //    Ducks.MainDuck.ragdoll.part1.owner = rock;
        //    Ducks.MainDuck.ragdoll.part2.owner = rock;
        //    Ducks.MainDuck.ragdoll.part3.owner = rock;
        //}
        //Ducks.MainDuck.ragdoll.part1.owner = Ducks.MainDuck;
        //Ducks.MainDuck.ragdoll.part1._joint.visible = false;
        //Ducks.MainDuck.ragdoll.part1.connect.visible = false;
        //Ducks.MainDuck.ragdoll.part1.visible = false;
        //Ducks.MainDuck.ragdoll.visible = false;
        if (script is not null) {
            LuaUtils.UpdateMouse(script);
            LuaUtils.UpdateDucks(script);
            LuaUtils.UpdateLevel(script);
            script.Globals["positionScreen"] = Mouse.positionScreen;
        }

        var duck = DuckNetwork.localProfile?.duck ?? Profiles.DefaultPlayer1.duck;
        if (duck != null) {
            hat.IfSome((hat) => { hat.position = duck.position + new Vec2(-1,-8f); });
        }
        Hats.Update(gameTime);

        if (lastLevel != Level.current) {
            OnLevelEnter();
        }
        if (wasNetworkActive != Network.isActive && Network.isActive) {
            OnEnteringOnline();
        }

        TeamsSender.Update(gameTime);
        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        lastLevel = Level.current;
        wasNetworkActive = Network.isActive;
    }

    public void Draw(GameTime gameTime) {
        LuaLogger.Show();
        Hats.Draw(gameTime);
        ImGui.Begin("test");
        if (ImGui.Button("remove all")) {
            Hats.RemoveAll();
            script = null;
        }
        if (ImGui.Button("reload script")) {
            var state = script.DoString(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\wearable.lua")), null, "wearable.lua");
            //state.Table.Get("load").Function.Call();
            this.wearableHat.IfSome((hat) => {
                hat.luaState = state;
            });
        }
        if (ImGui.Button("clear teams")) {
            TeamsStorage.UnloadAll();
        }
        if (ImGui.Button("load scriptable hat")) {
            var hat = ScriptableHat.New();
            script = new Script(MoonSharp.Interpreter.CoreModules.Preset_Complete);
            LuaUtils.LoadApi(script);
            var state = script.DoString(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\wearable.lua")), null, "wearable.lua");
            Hats.Add(hat, state);
            try {
                state.Table.Get("init").Function.Call();
                state.Table.Get("load").Function.Call();
            } catch (ScriptRuntimeException e) {
                LuaLogger.Log($"Error: {e.DecoratedMessage ?? e.Message}");
            }
            this.scriptableHat = hat;
        }
        if (ImGui.Button("load teams")) {
            //NOTE: teams should be loaded BEFORE any hats are spawn to ensure hats are displayed correctly
            var bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("niko.png"));
            teamsBitmap = TeamsStorage.LoadTeamsBitmap(bitmap, new IVector2(32)).UnwrapOk();
        }
        if (ImGui.Button("load wearable")) {
            var hatData = JsonConvert.DeserializeObject<HatData>(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("data.json")));
            script = new Script(MoonSharp.Interpreter.CoreModules.Preset_Complete);
            LuaUtils.LoadApi(script);
            var wearableHat = WearableHat.New(script, teamsBitmap, hatData.elements[0].wearable);
            var text = File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\wearable.lua"));
            var state = script.DoString(text, null, "wearable");
            Hats.Add(wearableHat, state);
            LuaUtils.UpdateDucks(script);
            LuaUtils.UpdateLevel(script);
            LuaUtils.UpdateMouse(script);
            try {
                state.Table.Get("init").Function.Call();
            } catch (ScriptRuntimeException e) {
                LuaLogger.Log($"Error: {e.DecoratedMessage ?? e.Message}");
            }
            this.wearableHat = wearableHat;

        }
        if (ImGui.Button("load hat")) {
            var bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("animation.png"));
            teamsBitmap = TeamsStorage.LoadTeamsBitmap(bitmap, new IVector2(32)).UnwrapOk();
            var rock = new ScoreRock(0, 0, Profiles.DefaultPlayer1);
            Level.Add(rock);
            var hat = DepthAnimHat.New(teamsBitmap, rock);
            var frames = Enumerable.Range(0, 8).Map((frame) => AnimFrame.New(frame)).ToList();
            var framesRev = Enumerable.Range(0, 8).Map((frame) => AnimFrame.New(frame)).Rev().ToList();
            hat.sprite.addAnim(Animation.New("normal", 1f/60f*4f, false, frames));
            hat.sprite.addAnim(Animation.New("rev", 1f/60f*4f, false, framesRev));
            Hats.Add(hat);
            this.hat = hat;
        }
        if (this.hat.ValueUnsafe() is var _hat && this.hat.IsSome) {
            ImGui.Text(_hat.sprite.timeAccumulator.ToString());
        }
        if (ImGui.Button("set anim 1") || Keyboard.Pressed(Keys.E)) {
            if (this.hat.ValueUnsafe() is var hat && this.hat.IsSome) {
                var oldFrameId = hat.sprite.currentFrameId;
                hat.sprite.setAnim("normal", ClearState.Yes);
                hat.sprite.currentFrameId = 7 - oldFrameId;
                var duck = DuckNetwork.localProfile?.duck ?? Profiles.DefaultPlayer1.duck;
                if (duck is not null) {
                    hat.depth = duck.depth.value + 0.1f;
                }
            }
        }
        if (ImGui.Button("set anim 2") || Keyboard.Released(Keys.E)) {
            if (this.hat.ValueUnsafe() is var hat && this.hat.IsSome) {
                var oldFrameId = hat.sprite.currentFrameId;
                hat.sprite.setAnim("rev", ClearState.No);
                hat.sprite.currentFrameId = 7 - oldFrameId;
            }
        }
        ImGui.Text("Profiles");
        foreach (var profile in Profiles.active) {
            ImGui.Text(profile.name.ToString());
        }
        ImGui.End();
    }
}
