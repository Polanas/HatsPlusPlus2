using DuckGame;
using DuckGame.CustomStuffHack.DGImGui;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.LuaStateInterop;
using MoonSharp.Interpreter.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

[assembly: AssemblyTitle("HatsPlusPlus 2.0")]
[assembly: AssemblyDescription("Idk")]
[assembly: AssemblyCompany("Polanas")]


[assembly: AssemblyCopyright("Copyright ©  2024")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyConfiguration("")]


[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace HatsPlusPlus;
public class HatsPlusPlus2 : DisabledMod {
    public static ImFontPtr font;

    static HatsPlusPlus2() {
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }
    public override DuckGame.Priority priority {
        get { return base.priority; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs eventArgs) {
        var short_name = new AssemblyName(eventArgs.Name).Name;
        var dll_path = Directory.GetFiles(Mod.GetPath<HatsPlusPlus2>("DLLs\\"), "*.dll", SearchOption.AllDirectories)
            .First(path => path.Contains(short_name));

        return Assembly.LoadFile(dll_path) ?? null;
    }

    protected override void OnPreInitialize() {
        base.OnPreInitialize();
    }
    protected override void OnPostInitialize() {
        var harmony = new HarmonyLib.Harmony("hats_plus_plus_2.0");
        harmony.PatchAll();

        TeamsStorage.Init();
        Hats.Init();
        DGImGui.Initialize();
        var updater = Updater.New();
        DGImGui.updater = updater;

        (typeof(Game).GetField("updateableComponents", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).GetValue(MonoMain.instance) as List<IUpdateable>).Add(new ModUpdate() { updater = updater });
        (typeof(Game).GetField("drawableComponents", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).GetValue(MonoMain.instance) as List<IDrawable>).Add(new ModDraw());


        //make sure LanguageExt is loaded early to prevent lag spike
        {
            var sprite = HatSprite.New();
            sprite.AddAnim(Animation.New(AnimType.OnDefault, 1f, true, "normal", []));
            sprite.AddAnim(Animation.New(AnimType.OnDefault, 1f, true, "rev", []));
            sprite.SetAnim("normal");
        }

        var script = new MoonSharp.Interpreter.Script();
        script.Options.ScriptLoader = new FileSystemScriptLoader();
        (script.Options.ScriptLoader as ScriptLoaderBase).ModulePaths = [Path.Combine(GetPath<HatsPlusPlus2>("LuaScripts"), "?.lua")];
        static DynValue MyFunction(ScriptExecutionContext ctx, CallbackArguments args) {
            var arguments = args.GetArray();
            var script = ctx.GetScript();
            StringBuilder log = new StringBuilder();
            foreach (var arg in arguments) {
                var argString = script.Call(script.Globals["tostring"], arg).String;
                log.Append(argString);
                log.Append("\t");
            }

            LuaLogger.Log(log.ToString());

            return DynValue.Nil;
        }
        MoonSharp.Interpreter.Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vec2>((script, vec) => DynValue.NewNumber(420));
        script.Globals["print"] = DynValue.NewCallback(MyFunction);
        script.Globals["test"] = new Vec2(20);
        script.DoString("print(test)");
    }
}

internal class ModUpdate : IUpdateable {
    public bool Enabled {
        get {
            return true;
        }
    }

    public int UpdateOrder {
        get {
            return 1;
        }
    }

    public event EventHandler<EventArgs> EnabledChanged;
    public event EventHandler<EventArgs> UpdateOrderChanged;

    public Updater updater;

    public void Update(GameTime gameTime) {
        updater.Update(gameTime);
    }
}

internal class ModDraw : IDrawable {
    public bool Visible {
        get {
            return true;
        }
    }

    public int DrawOrder {
        get {
            return 1;
        }
    }

    public event EventHandler<EventArgs> VisibleChanged;
    public event EventHandler<EventArgs> DrawOrderChanged;

    public void Draw(GameTime gameTime) {
        Graphics.screen.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, DuckGame.Matrix.Identity);
        //ImGui.PushFont(ImGui.GetFont());
        //ImGui.PopFont();
        DGImGui.Draw(gameTime);
        Graphics.screen.End();
    }
}
