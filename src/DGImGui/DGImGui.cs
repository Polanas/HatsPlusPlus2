using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HatsPlusPlus;
using System.Runtime.CompilerServices;

namespace DuckGame.CustomStuffHack.DGImGui
{
    internal static class DGImGui
    {
        public static ImGuiRenderer GuiRenderer;

        private static GraphicsDeviceManager _graphics;
        private static SpriteBatch _spriteBatch;
        private static Game _game;

        public static BufferingTest bufferingTest;
        public static ImFontPtr font;
        public static Updater updater;

        public static void Initialize()
        {
            _game = MonoMain.instance;
            GuiRenderer = new ImGuiRenderer(_game);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
            ImGui.GetIO().FontGlobalScale = 1.01f;

            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);

            font = ImGui.GetIO().Fonts.AddFontFromFileTTF(Mod.GetPath<HatsPlusPlus2>("CaskaydiaCoveNerdFont-Regular.ttf"), 16f);

            DGImGui.GuiRenderer.RebuildFontAtlas();
            DGImGui.bufferingTest = new BufferingTest();
            //DGImGui.bufferingTest.Init();
        }

        public static void Draw(GameTime gameTime)
        {
            Graphics.mouseVisible = true;

            GuiRenderer.BeginLayout(gameTime);
            updater.Draw(gameTime);

            //ImGui.End();

            GuiRenderer.EndLayout();
        }
    }
}
