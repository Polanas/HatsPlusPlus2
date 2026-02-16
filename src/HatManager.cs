using System;
using DuckGame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HatsPlusPlus.Parsing;
using Newtonsoft.Json;
using LanguageExt.UnsafeValueAccess;
using System.Security.Authentication.ExtendedProtection.Configuration;
using MoonSharp.Interpreter;
using Script = MoonSharp.Interpreter.Script;
using Coroutines;
using Microsoft.Xna.Framework;
using System.Collections;

namespace HatsPlusPlus;

#nullable disable

internal struct WearableHatTemplate {
    internal TeamsBitmap teams;
    internal Option<List<Animation>> animations;
    internal WearableHatData data;

    public WearableHatTemplate(TeamsBitmap teams, WearableHatData data, Option<List<Animation>> animations) {
        this.teams = teams;
        this.animations = animations;
        this.data = data;
    }
}

internal struct WalkingPetTemplate {
    internal TeamsBitmap teams;
}

internal struct HatTemplate {
    internal WearableHatTemplate? wearable;
    internal string hatHame;

    public HatTemplate() {

    }

    internal void Instantiate() {
        LuaLogger.Info($"Instantiating hat {hatHame}");

        if (wearable.Value is var wearableTemplate) {
            Option<(Script, DynValue)> scriptData = None;
            if (wearableTemplate.data.baseData.localScriptPath is var scriptPath && scriptPath != null) {
                var script = new Script(CoreModules.Preset_HardSandbox);
                LuaUtils.LoadApi(script);
                var output = script.DoString(scriptPath);
                scriptData = (script, output);
            }
            var wearable = WearableHat.New(wearableTemplate.teams, wearableTemplate.data, wearableTemplate.animations, scriptData);
            HatsOnLevel.Add(wearable);
        }
    }
}

internal static class HatManager {
    internal static Dictionary<string, string> pathsByNames = [];
    internal static Dictionary<string, HatData> hatsDataByPaths = [];
    internal static Option<HatTemplate> selectedHatTemplate;
    internal static CoroutineRunner runner;

    internal static void Init() {
        runner = new();
    }

    internal static void Update(GameTime gameTime) {
        runner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    internal static void OnLobbyEnter() {
        runner.Run(SpawnHatCoroutine());
    }

    internal static void OnGameLevelEnter() {
        runner.Run(SpawnHatCoroutine());
    }

    internal static IEnumerator SpawnHatCoroutine() {
        if (Level.current is TeamSelect2) {
            yield return 0.2f;
        } else {
            yield return 1.0f;
        }
        if (!Network.isActive) {
            yield break;
        }
        if (selectedHatTemplate.ValueUnsafe() is var template && selectedHatTemplate.IsSome) {
            var hat = Ducks.mainDuck.hat as TeamHat;
            if (hat != null) {
                hat.UnEquip();
                Ducks.mainDuck.Unequip(hat, true);
            }
            yield return 0.1f;
            Level.Remove(hat);
            template.Instantiate();
        }
    }
    internal static IEnumerator OnHatSelectorCloseCoroutine() {
        HatsOnLevel.RemoveAll();
        yield return 0.1f;

        TeamsStorage.UnloadAll();

        var hat = Ducks.mainDuck.hat;
        if (hat != null && hat is TeamHat teamHat && teamHat.team?.name != null) { } else {
            yield break;
        }
        if (pathsByNames.Get(teamHat.team.name).ValueUnsafe() is string hatPath) { } else {
            yield break;
        }

        LuaLogger.Info($"Loading hat at {hatPath}");

        if (!Directory.Exists(hatPath)) {
            LuaLogger.Error("Could not load hat: directory not found");
            yield break;
        } 

        var hatTemplateR = LoadHatTemplate(hatPath);
        if (hatTemplateR.OkErrUnsafe() is (var hatTemplate, var templateErr) && hatTemplateR.IsOk) { } else {
            LuaLogger.Error($"{templateErr}");
            yield break;
        }

        var equippedHat = Ducks.mainDuck.hat as TeamHat;
        if (equippedHat != null) {
            equippedHat.UnEquip();
            Ducks.mainDuck.Unequip(equippedHat, true);
        }
        Level.Remove(equippedHat);
        hatTemplate.Instantiate();
        selectedHatTemplate = hatTemplate;
    }

    internal static HResult<HatTemplate> LoadHatTemplate(string hatPath) {
        HatTemplate template = new();

        var imagesPath = Path.Combine(hatPath, "images");
        if (!Directory.Exists(imagesPath)) {
            return Err<HatTemplate>($"could not load hat: images directory not found");
        }

        HResult<(TeamsBitmap, Option<AsepriteData>)> LoadBitmap(string path, 
        Option<IVector2> frameSize,
        Option<IVector2> partSize,
        ChopMode chopMode = ChopMode.WithGaps) {
            var bitmapR = BitmapUtils.Load(path);
            if (bitmapR.OkErrUnsafe() is ((var bitmap, var asepriteDataOpt), var bitmapErr) && bitmapR.IsOk) { } else {
                return Err<(TeamsBitmap, Option<AsepriteData>)>($"{bitmapErr}"); 
            }
            if (asepriteDataOpt.IsSome && asepriteDataOpt.ValueUnsafe() is var asepriteData) {
                frameSize = asepriteData.frameSize;
            }

            var teamsBitmapR = TeamsStorage.LoadTeams(bitmap, frameSize, partSize, chopMode);
            if (teamsBitmapR.OkErrUnsafe() is (var teamsBitmap, var teamsBitmapErr) && teamsBitmapR.IsOk) { } else {
                return Err<(TeamsBitmap, Option<AsepriteData>)>($"{teamsBitmapErr}"); 
            }

            return (teamsBitmap, asepriteDataOpt);
        }

        var hatData = hatsDataByPaths[hatPath];
        template.hatHame = hatData.name;
        foreach (var element in hatData.elements) {
            if (element.wearable != null && element.wearable.Value is var wearable) {
                var imagePath = Path.Combine(imagesPath, wearable.baseData.localImagePath);
                var frameSize = new IVector2(wearable.baseData.frameSize[0], wearable.baseData.frameSize[1]);

                var bitmapR = LoadBitmap(imagePath, frameSize, None);
                if (bitmapR.OkErrUnsafe() is ((var bitmap, var asepriteData), var bitmapErr) && bitmapR.IsOk) { } else {
                    return Err<HatTemplate>($"could not load hat: {bitmapErr}"); 
                }

                var animations = asepriteData.Map((data) => data.animations).ValueOr([]);
                wearable.baseData.frameSize =
                    asepriteData.Map<List<int>>(( d) => [d.frameSize.X, d.frameSize.Y]).ValueOr(wearable.baseData.frameSize);
                animations.AddRange(wearable.animations ?? []);
                template.wearable = new WearableHatTemplate(bitmap, wearable, animations);
            }
        }

        return template;
    }

    internal static void ScanHats() {
        foreach (var hatsDir in Team.hatSearchPaths.Filter((dir) => Directory.Exists(dir))) {
            var dirInfo = new DirectoryInfo(hatsDir);

            foreach (var hatDir in dirInfo.GetDirectories()) {
                var hatMarkerPath = Path.Combine(hatDir.FullName, "hpp_hat_marker");
                if (!File.Exists(hatMarkerPath)) {
                    continue;
                }

                var imagesDir = Path.Combine(hatDir.FullName, "images");
                if (!Directory.Exists(imagesDir)) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: images directory not found");
                    continue;
                }
                var scriptsDir = Path.Combine(hatDir.FullName, "scripts");
                if (!Directory.Exists(scriptsDir)) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: scripts directory not found");
                    continue;
                }
                var dataPath = Path.Combine(hatDir.FullName, "data.json");
                if (!File.Exists(dataPath)) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: data.json file not found");
                    continue;
                }
                string dataJsonText;
                try {
                    dataJsonText = File.ReadAllText(dataPath);
                } catch (Exception e) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: could not read from {dataPath}: {e.ToString()}");
                    continue;
                }

                var hatData = JsonConvert.DeserializeObject<HatData>(dataJsonText);
                var previewData = hatData.elements.Find((e) => e.preview != null);
                if (previewData.preview == null) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: preview element not found");
                    continue;
                }

                var previewImagePath = previewData.preview.Value.baseData.localImagePath;
                if (previewImagePath == null) {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: preview.localImagePath not found");
                    continue;
                }

                var fullPreviewPath = Path.Combine(imagesDir, previewImagePath);
                var previewTeamBitmapResult = BitmapUtils.Load(fullPreviewPath);
                if (previewTeamBitmapResult.OkErrUnsafe() is ((var previewTeamBitmap, var _), var bitmapErr) && previewTeamBitmapResult.IsOk) { } else {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: could not load preview bitmap: {bitmapErr}");
                    continue;
                }
                var previewTeamName = $"[HATS++] {hatData.name}";
                var previewTeamResult = TeamsStorage.BitmapToTeam(previewTeamBitmap, previewTeamName);
                if (previewTeamResult.OkErrUnsafe() is (var previewTeam, var teamErr) && previewTeamResult.IsOk) { } else {
                    LuaLogger.Warn($"While loading a hat at {hatDir}: could not load preview team: {teamErr}");
                    continue;
                }

                Teams.AddExtraTeam(previewTeam);
                pathsByNames.Add(previewTeamName, hatDir.FullName);
                hatsDataByPaths.Add(hatDir.FullName, hatData);
            }
        }
    } 

    internal static void OnHatSelectorClose() {
        runner.Run(OnHatSelectorCloseCoroutine());

    }
}
