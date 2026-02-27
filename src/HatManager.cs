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
using LanguageExt.ClassInstances;
using System.Security.Cryptography;
using System.Diagnostics.Eventing.Reader;

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

internal struct ScriptHatTemplate {
    internal ScriptHatData data;
}

internal struct WalkingPetTemplate {
    internal TeamsBitmap teams;
}

internal struct HatTemplate {
    internal WearableHatTemplate? wearable;
    internal List<ScriptHatTemplate> scriptHats = new();
    internal string hatName;
    internal string hatPath;

    public HatTemplate() {

    }

    internal void Instantiate() {
        LuaLogger.Info($"Instantiating hat {hatName}");

        if (wearable.Value is var wearableTemplate) {
            Option<LuaScript> scriptOpt = None;
            if (wearableTemplate.data.baseData.localScriptPath is var localPath && localPath != null) {
                var script = LuaScript.New(Path.Combine(hatPath, Constants.SCRIPT_DIR, localPath));
                var images_path = Path.Combine(hatPath, Constants.IMAGES_DIR);
                script.SetImagesPath(images_path);
                script.Select();
                scriptOpt = script;
            }
            var wearable = WearableHat.New(wearableTemplate.teams, wearableTemplate.data, wearableTemplate.animations, scriptOpt);
            HatsOnLevel.Add(wearable);
        }
        foreach (var scriptHatTemplate in scriptHats) {
            //TODO: init script hat properly
            var scriptHat = ScriptHat.New(scriptHatTemplate.data, None);
            HatsOnLevel.Add(scriptHat);
        }
    }
}

internal static class HatManager {
    internal static Dictionary<string, string> pathsByNames = [];
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
        template.hatPath = hatPath;

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

        var hatDataOpt = LoadHatData(Path.Combine(hatPath, Constants.DATA_FILE));
        if (hatDataOpt.IsSome && hatDataOpt.ValueUnsafe() is var hatData) { } else {
            return Err<HatTemplate>("could not load hat data");
        }
        template.hatName = hatData.name;
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
        var hatPaths = new List<string>(Team.hatSearchPaths.Filter((dir) => Directory.Exists(dir)));
        hatPaths.Add(HatsPlusPlus2.GetPathFixed("Hats"));
        foreach (var hatsDir in hatPaths) {
            var dirInfo = new DirectoryInfo(hatsDir);

            foreach (var hatDir in dirInfo.GetDirectories()) {
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

                var dataOpt = LoadHatData(Path.Combine(hatDir.FullName,Constants.DATA_FILE));
                if (dataOpt.IsSome && dataOpt.ValueUnsafe() is var data)  {
                } else {
                    continue;
                }


                var previewOpt = data.Preview();
                if (previewOpt.IsSome && previewOpt.ValueUnsafe() is var preview) {
                } else {
                    LuaLogger.Warn($"preview element not found");
                    continue;
                }

                if (preview.baseData.localImagePath is var previewImagePath && previewImagePath != null) { } else {
                    LuaLogger.Warn($"preview.local_image_path not found");
                    continue;
                }

                var fullPreviewPath = Path.Combine(imagesDir, previewImagePath);
                var previewTeamBitmapR = BitmapUtils.Load(fullPreviewPath);
                if (previewTeamBitmapR.OkErrUnsafe() is ((var previewTeamBitmap, var _), var bitmapErr) && previewTeamBitmapR.IsOk) { } else {
                    LuaLogger.Warn($"could not load preview bitmap: {bitmapErr}");
                    continue;
                }
                var previewTeamName = $"[HATS++] {data.name}";
                var previewTeamR = TeamsStorage.BitmapToTeam(previewTeamBitmap, previewTeamName);
                if (previewTeamR.OkErrUnsafe() is (var previewTeam, var teamErr) && previewTeamR.IsOk) { } else {
                    LuaLogger.Warn($"could not load preview team: {teamErr}");
                    continue;
                }

                Teams.AddExtraTeam(previewTeam);
                pathsByNames.Add(previewTeamName, hatDir.FullName);
            }
        }
    } 

    internal static Option<HatData> LoadHatData(string path) {
            if (!File.Exists(path)) {
                LuaLogger.Warn($"data.json file not found");
                return None;
            }
            string dataJsonText;
            try {
                dataJsonText = File.ReadAllText(path);
            } catch (Exception e) {
                LuaLogger.Warn($"Could not read from {path}: {e.ToString()}");
                return None;
            }
            return JsonConvert.DeserializeObject<HatData>(dataJsonText);

    }

    internal static void OnHatSelectorClose() {
        runner.Run(OnHatSelectorCloseCoroutine());

    }
}
