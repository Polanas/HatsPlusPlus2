using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Common;
using AsepriteDotNet.IO;
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
using AsepriteDotNet.Aseprite.Types;

namespace HatsPlusPlus;

internal class DrawThingTemp : Thing {
    public static Vec2 current;
    public static Vec2 first;
    public static int counter;
    public override void Draw() {
        base.Draw();
        Graphics.DrawCircle(new Vec2(0, 0), 50, DuckGame.Color.Red, 0, new Depth(2));
    }

    public DrawThingTemp(): base(0,-1000) {
        this.layer = Layer.Blocks;
    }
}

internal class Updater {
    Level lastLevel;
    bool wasNetworkActive;
    Script script;

    Option<DepthAnimHat> hat;
    Option<ScriptableHat> scriptableHat;
    CoroutineRunner coroutines;
    Option<WearableHat> wearableHat;
    TeamsBitmap teamsBitmap;
    HatSprite hatSpriteTest;

    internal static Updater New() {
        var updater = new Updater();
        updater.coroutines = new CoroutineRunner();

        return updater;
    }

    internal void OnEnteringOnline() {
        //TeamsStorage.RemoveAll();
        DevConsole.Log(DCSection.Connection, "Entering online!");
    }

    internal void OnLobbyEnter() {

    }

    internal void OnLevelEnter() {
        Hats.OnLevelStart();

        if (Level.current is TeamSelect2) {
            OnLobbyEnter();
        }
        if (DuckNetwork.status == DuckNetStatus.Connected) {
            OnEnteringOnline();
        }
    }
    ScoreRock rock;
    internal void Update(GameTime gameTime) {
        if (Keyboard.Pressed(Keys.O)) {
            Level.Add(new DrawThingTemp());
        }
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

    internal void Draw(GameTime gameTime) {
        LuaLogger.Show();
        Hats.Draw(gameTime);
        ImGui.Begin("test");

        ImGui.Text($"current: {DrawThingTemp.current.x}");
        ImGui.Text($"first: {DrawThingTemp.first.x}");
        ImGui.Text($"difference: {Math.Abs(DrawThingTemp.first.x - DrawThingTemp.current.x)}");
        ImGui.Text($"counter: {DrawThingTemp.counter}");

        if (ImGui.Button("remove all")) {
            Hats.RemoveAll();
            script = null;
        }
        if (ImGui.Button("reload script")) {
            var state = script.DoString(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\skebob.lua")), null, "wearable.lua");
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
            var state = script.DoString(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\skebob.lua")), null, "wearable.lua");
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
            teamsBitmap = TeamsStorage.LoadTeamsBitmap(HatsPlusPlus2.GetPathFixed("niko.png"), new IVector2(32)).Unwrap();
        }
        if (ImGui.Button("load room")) {
            var hatData = JsonConvert.DeserializeObject<HatData>(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("RoomHatTest\\data.json")));
            var roomData = hatData.elements[0].room;
            var bitmap = Bitmap.FromPath(HatsPlusPlus2.GetPathFixed("RoomHatTest\\images\\room.png"));
            var roomHat = RoomHat.New(roomData, None, bitmap);
            Hats.Add(roomHat);
        }
        if (ImGui.Button("load wearable")) {
            //AsepriteFile file = AsepriteFileLoader.FromFile(HatsPlusPlus2.GetPathFixed("niko.aseprite"));
            //Rgba32[] framePixels = file.Frames[0].FlattenFrame(onlyVisibleLayers: true, includeBackgroundLayer: false, includeTilemapCels: false);

            //var teamsBitmap = TeamsStorage.LoadTeamsBitmap(HatsPlusPlus2.GetPathFixed("niko.png"), new IVector2(32)).Unwrap();
            var hatData = JsonConvert.DeserializeObject<HatData>(File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("data.json")));
            var script = new Script(MoonSharp.Interpreter.CoreModules.Preset_Complete);
            LuaUtils.LoadApi(script);
            var wearableHat = WearableHat.New(script, teamsBitmap, hatData.elements[0].wearable);
            var text = File.ReadAllText(Mod.GetPath<HatsPlusPlus2>("LuaScripts\\skebob.lua"));
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
            this.script = script;
        }
        if (ImGui.Button("rock n roll bitch")) {
            var bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("rock.png"));
            //var team = TeamsStorage.BitmapToTeam(bitmap, "team").UnwrapOk();
            var profile = Ducks.MainDuck.profile;
            profile.team.hat.texture = Teams.all.Find(x => x.hat.texture.textureName == "hats/noHat").hat.texture; 
            var rock = new ScoreRock(20, 20, profile);
            Level.Add(rock);
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
