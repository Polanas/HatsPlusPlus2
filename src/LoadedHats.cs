using DuckGame;
using HatsPlusPlus.Parsing;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Script = MoonSharp.Interpreter.Script;

namespace HatsPlusPlus; 

record struct ScanHatData(Team previewTeam, string name);

internal record struct SelectedHat(HatData Data, string hatPath);
internal static class LoadedHats {
    internal static Dictionary<string, string> hatPathsByName = [];
    internal static Option<SelectedHat> selectedHatOpt;

    internal static void Scan() {
        foreach (var hatSearchPath in Team.hatSearchPaths.Filter((p) => Directory.Exists(p))) {
            var dirInfo = new DirectoryInfo(hatSearchPath);
            //TODO: scan for .hatspp files
            foreach (var dir in dirInfo.GetDirectories()) {
                ScanHatDir(dir.FullName).Match(
                    (hatData) => {
                        hatPathsByName.Add(hatData.name, dir.FullName);
                        Teams.AddExtraTeam(hatData.previewTeam);
                    },
                    (err) => {
                        LuaLogger.Log($"error: could not load hat at {dir.FullName}: {err}");
                    }
                );
            }
        }
    }

    internal static void OnHatSelectorClose() {
        //TODO: may case OutOfIndexException. Delay deletion of teams by one frame?
        TeamsStorage.UnloadAll();
        HatsOnLevel.RemoveAll();
        var hat = Ducks.mainDuck.hat;
        if (hat != null && hat is TeamHat teamHat && teamHat.team?.name != null) { } else {
            return;
        }
        if (hatPathsByName.Get(teamHat.team.name).ValueUnsafe() is string hatPath) { } else {
            return;
        }

        LuaLogger.Log($"loading hat at {hatPath}");
        if (Directory.Exists(hatPath)) {
            var dataJsonPath = Path.Combine(hatPath, "data.json");
            var imagesPath = Path.Combine(hatPath, "images");
            if (!File.Exists(dataJsonPath)) {
                LuaLogger.Log($"could not find data.json at {dataJsonPath}");
                return;
            }
            if (!Directory.Exists(imagesPath)) {
                LuaLogger.Log($"could not find direcotry images at {imagesPath}");
                return;
            }
            string dataJsonText;
            try {
                dataJsonText = File.ReadAllText(dataJsonPath);
            } catch (Exception e) {
                LuaLogger.Log($"could not read from {dataJsonPath}: {e.ToString()}");
                return;
            }

            var hatData = JsonConvert.DeserializeObject<HatData>(dataJsonText);
            selectedHatOpt = new SelectedHat(hatData, hatPath);
            LoadSelectedHatDir();
        } else {
            //TODO
        }
    }

    internal static void LoadSelectedHatDir() {
        if (selectedHatOpt.IsSome && selectedHatOpt.ValueUnsafe() is var selectedHat) { } else {
            return;
        }
        var hatData = selectedHat.Data;
        var wearableHat = hatData.elements.Find((e) => e.wearable != null).wearable;
        if (wearableHat.HasValue && wearableHat.Value is var wearableHatValue) {
            var wearableImagePath = Path.Combine(selectedHat.hatPath, "images", wearableHatValue.baseData.localImagePath);
            var bitmapRes = BitmapUtils.Load(wearableImagePath);
            if (bitmapRes.IsOk && bitmapRes.OkUnsafe() is var bitmap) { } else {
                LuaLogger.Log($"could not load wearable hat: {bitmapRes.ToString()}");
                return;
            }
            //TODO: add extrahat support
            var teamsBitmapRes = TeamsStorage.LoadTeams(bitmap.Item1, bitmap.Item2.Map((d) => d.frameSize), Constants.MIN_HAT_SIZE_VEC);
            if (teamsBitmapRes.IsOk && teamsBitmapRes.OkUnsafe() is var teamsBitmap) { } else {
                LuaLogger.Log($"could not load wearable hat: {teamsBitmapRes.ToString()}");
                return;
            }
            Option<(Script, DynValue)> scriptOpt = None;
            if (wearableHatValue.baseData.localScriptPath != null) {
                var scriptPath = Path.Combine(selectedHat.hatPath, "scripts", wearableHatValue.baseData.localScriptPath);
                if (!File.Exists(scriptPath)) {
                    LuaLogger.Log($"could not load script at {scriptPath}: file not found");
                } else {
                    string scriptText;
                    try {
                        scriptText = File.ReadAllText(scriptPath);
                        var script = new MoonSharp.Interpreter.Script();
                        LuaUtils.LoadApi(script);
                        var state = script.DoString(scriptText, null, scriptPath);
                        if (state.Table == null) {
                            LuaLogger.Log($"expected script at {scriptPath} to return a table, but got {state.Type.ToString()} instead");
                        }
                        var selected = Level.current is TeamSelect2;
                        scriptOpt = (script, state);
                    } catch (Exception e) {
                        LuaLogger.Log($"could not load script at {scriptPath}: {e.ToString()}");
                    }
                }
            }
            var wearable = WearableHat.New(teamsBitmap, wearableHatValue, bitmap.Item2.Map((d) => d.animations), scriptOpt);
            HatsOnLevel.Add(wearable);
        }
    }

    internal static void OnLevelStart() {

    }

    internal static void Update(GameTime gameTime) {

    }

    internal static HResult<ScanHatData> ScanHatDir(string path) {
        var dataJsonPath = Path.Combine(path, "data.json");
        var imagesPath = Path.Combine(path, "images");

        if (!File.Exists(dataJsonPath)) {
            return Err<ScanHatData>($"could not find data.json at {dataJsonPath}");
        }
        if (!Directory.Exists(imagesPath)) {
            return Err<ScanHatData>($"could not find direcotry images at {imagesPath}");
        }

        string dataJsonText;
        try {
            dataJsonText = File.ReadAllText(dataJsonPath);
        } catch (Exception e) {
            return Err<ScanHatData>($"could not read from {dataJsonPath}: {e.ToString()}");
        }

        var data_json = JsonConvert.DeserializeObject<HatData>(dataJsonText);
        var previewData = data_json.elements.Find((e) => e.preview != null);
        if (previewData.preview == null) {
            return Err<ScanHatData>("hats should have a preivew element");
        }

        var previewImagePath = previewData.preview.Value.baseData.localImagePath;
        if (previewImagePath == null) {
            return Err<ScanHatData>("preview hat should have a localImagePath");
        }

        var fullPreviewPath = Path.Combine(imagesPath, previewImagePath);
        var previewTeamBitmapResult = BitmapUtils.Load(fullPreviewPath);
        if (previewTeamBitmapResult.OkErrUnsafe() is ((var previewTeamBitmap, var _), var bitmapErr) && previewTeamBitmapResult.IsOk) { } else {
            return HResult<ScanHatData>.ErrUnsafe(bitmapErr).WithContext("could not load preview bitmap");
        }
        var previewTeamResult = TeamsStorage.BitmapToTeam(previewTeamBitmap, data_json.name);
        if (previewTeamResult.OkErrUnsafe() is (var team, var teamErr) && previewTeamResult.IsOk) { } else {
            return HResult<ScanHatData>.ErrUnsafe(teamErr).WithContext("could not load preview team");
        }

        return new ScanHatData(team, data_json.name);
    }

    internal static HResult<ScanHatData> ScanHatFile(string path) {
        throw new Exception();
    }
}
