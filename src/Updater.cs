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

internal enum HatSelectorState {
    OnOpen,
    Opened,
    OnClose,
    Closed,
}

internal class Updater {
    Level lastLevel;
    bool wasNetworkActive;

    StateMachine<HatSelectorState> hatSelectorMachine;
    DepthHat depthHat;

    static GameTime gameTime;

    internal static GameTime GameTime() {
        return gameTime;
    }

    internal static Updater New() {
        var updater = new Updater();
        updater.hatSelectorMachine = new StateMachine<HatSelectorState>();
        updater.hatSelectorMachine.SetCallBacks(HatSelectorState.OnOpen, updater.OnOpen);
        updater.hatSelectorMachine.SetCallBacks(HatSelectorState.OnClose, updater.OnClose);
        updater.hatSelectorMachine.SetCallBacks(HatSelectorState.Opened, updater.Opened);
        updater.hatSelectorMachine.SetCallBacks(HatSelectorState.Closed, updater.Closed);

        return updater;
    }

    internal HatSelectorState OnOpen() {
        return HatSelectorState.Opened;
    }

    internal HatSelectorState Opened() {
        if (Level.current is TeamSelect2 teamSelect) {
            var hatSelector = teamSelect._profiles.Get(DuckNetwork.localDuckIndex).AndThen((p) => p._hatSelector != null ? Some(p._hatSelector) : None); 
            if (hatSelector.Map((s) => !s.open).ValueOr(false)) {
                return HatSelectorState.OnClose;
            }
        }
        return HatSelectorState.Opened;
    }

    internal HatSelectorState OnClose() {
        HatManager.OnHatSelectorClose();
        return HatSelectorState.Closed;
    }

    internal HatSelectorState Closed() {
        if (Level.current is TeamSelect2 teamSelect) {
            var hatSelector = teamSelect._profiles.Get(DuckNetwork.localDuckIndex).AndThen((p) => p._hatSelector != null ? Some(p._hatSelector) : None); 
            if (hatSelector.Map((s) => s.open).ValueOr(false)) {
                return HatSelectorState.OnOpen;
            }
        }
        return HatSelectorState.Closed;
    }

    internal void OnEnteringOnline() {
        TeamsStorage.UnloadAll();
    }

    internal void OnLobbyEnter() {
        HatManager.OnLobbyEnter();
    }

    //A level where hats can be spawned
    internal void OnGameLevelEnter() {
        HatManager.OnGameLevelEnter();
    }

    internal void OnLevelEnter() {
        HatsOnLevel.OnLevelStart();

        if (Level.current is TeamSelect2) {
            OnLobbyEnter();
        } else {
            hatSelectorMachine.ForceState(HatSelectorState.Closed);

            //TODO: account for scoreboard level
            OnGameLevelEnter();
        }
    }
    internal void Update(GameTime gameTime) {
        Updater.gameTime = gameTime;
        foreach (var duck in Level.current.things[typeof(Duck)]) {
            var d = (Duck)duck;

            if (d.profile == DuckNetwork.localProfile) {
                Ducks.mainDuck = d;
                break;
            }
        }
        if (Ducks.mainDuck == null) {
            return;
        }
        HatManager.Update(gameTime);
        hatSelectorMachine.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        if (lastLevel != Level.current) {
            OnLevelEnter();
        }
        if (wasNetworkActive != Network.isActive && Network.isActive) {
            OnEnteringOnline();
        }

        if (this.depthHat != null) {
            this.depthHat.position = Ducks.mainDuck.position;
        }

        HatsOnLevel.Update(gameTime);
        TeamsSender.Update(gameTime);

        lastLevel = Level.current;
        wasNetworkActive = Network.isActive;
    }

    internal void TeamSlotsDebugWindow() {
        ImGui.Begin("Team slots");
        ImGui.Text($"Slots used total: {TeamsStorage.slots.Length}");
        ImGui.Text($"Teams loaded: {TeamsStorage.loadedTeams.Count}");
        ImGui.Text("Team slots: ");
        ImGui.Text("Red = inactive, Green = active");
        for (int i = 0; i < TeamsStorage.slots.Length; i++) {
            var handle = TeamsStorage.handlesByIds.Get(TeamId.New((uint)i)).Flatten();
            if (i % 20 != 0) {
                ImGui.SameLine(0, 5);
            }
            if (handle.IsSome) {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(39/255f, 111/255f, 36/255f, 1)); 
            } else {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(111/255f, 36/255f, 40/255f, 1)); 
            }
            ImGui.Button(" ", new System.Numerics.Vector2(20,20));
            ImGui.PopStyleColor();
        }
        ImGui.End();
    }

    internal void Draw(GameTime gameTime) {
        HppDebugWindow.Draw();
        LuaLogger.Show();
        TeamsSender.DebugWindow();
        HatsOnLevel.Draw(gameTime);
        TeamSlotsDebugWindow();

        ImGui.Begin("test");
        if (ImGui.Button("spawn depth hat")) {
            var teams = TeamsStorage.LoadTeams(HatsPlusPlus2.GetPathFixed("nikoEye.png"), None, None).Unwrap();
            this.depthHat = DepthHat.New(teams, None, true);
            this.depthHat.depth = 1;
            this.depthHat.SetState(DepthHatState.DepthInactive);
            HatsOnLevel.Add(this.depthHat);
        }
        ImGui.End();
    }
}
