using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DuckGame;
using ImGuiNET;
using System.Linq;
using LanguageExt.UnsafeValueAccess;
using System;
using Coroutines;

namespace HatsPlusPlus;

public class Updater {
    Level lastLevel;
    bool wasNetworkActive;

    Option<DepthAnimHat> hat;
    CoroutineRunner coroutines;
    TeamsBitmap teamsBitmap;
    HatSprite hatSpriteTest;

    public static Updater New() {
        var updater = new Updater();
        var bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("animation.png"));
        updater.teamsBitmap = TeamsStorage.LoadBitmap(bitmap, new IVector2(32)).UnwrapOk();
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
        Hats.RemoveAll();

        if (Level.current is TeamSelect2) {
            OnLobbyEnter();
        }
        if (DuckNetwork.status == DuckNetStatus.Connected) {
            OnEnteringOnline();
        }
    }

    public void Update(GameTime gameTime) {
        hat.IfSome((hat) => { hat.position = DuckNetwork.localProfile.duck.position + new Vec2(-1,-8f); });
        Hats.Update(gameTime);

        if (lastLevel != Level.current) {
            OnLevelEnter();
        }
        if (wasNetworkActive != Network.isActive && Network.isActive) {
            OnEnteringOnline();
        }

        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        lastLevel = Level.current;
        wasNetworkActive = Network.isActive;
    }

    public void Draw(GameTime gameTime) {
        LuaLogger.Show();
        ImGui.Begin("test");
        if (ImGui.Button("test")) {
        }
        if (ImGui.Button("load hat")) {
            var rock = new ScoreRock(0, 0, Profiles.DefaultPlayer1);
            Level.Add(rock);
            var hat = DepthAnimHat.New(teamsBitmap, rock);
            var frames = Enumerable.Range(0, 8).Map((frame) => AnimFrame.New(frame)).ToList();
            var framesRev = Enumerable.Range(0, 8).Map((frame) => AnimFrame.New(frame)).Rev().ToList();
            hat.sprite.AddAnim(Animation.New(AnimType.OnDefault, 1f/60f * 4f, false, "normal", frames));
            hat.sprite.AddAnim(Animation.New(AnimType.OnDefault, 1f/60f * 4f, false, "rev", framesRev));
            foreach (var frame in teamsBitmap.frames) {
                var teamData = TeamsStorage.GetTeamData(frame.TeamHandles[0]).Unwrap();
                Teams.AddExtraTeam(teamData.team);
                Send.Message(new NMSpecialHat(teamData.team, DuckNetwork.localProfile, false));
            }
            Hats.Add(hat);
            this.hat = hat;
        }
        if (this.hat.ValueUnsafe() is var _hat && this.hat.IsSome) {
            ImGui.Text(_hat.sprite.timeAccumulator.ToString());
        }
        if (ImGui.Button("set anim 1") || Keyboard.Pressed(Keys.E)) {
            if (this.hat.ValueUnsafe() is var hat && this.hat.IsSome) {
                var oldFrameId = hat.sprite.currentFrameId;
                hat.sprite.SetAnim("normal", ClearState.Yes);
                hat.sprite.currentFrameId = 7 - oldFrameId;
                hat.depth = DuckNetwork.localProfile.duck.depth.value + 0.1f;
            }
        }
        if (ImGui.Button("set anim 2") || Keyboard.Released(Keys.E)) {
            if (this.hat.ValueUnsafe() is var hat && this.hat.IsSome) {
                var oldFrameId = hat.sprite.currentFrameId;
                hat.sprite.SetAnim("rev", ClearState.No);
                hat.sprite.currentFrameId = 7 - oldFrameId;
                hat.depth = DuckNetwork.localProfile.duck.depth.value + 0.1f;
            }
        }
        ImGui.End();
    }
}
