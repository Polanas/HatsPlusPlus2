using System;
using DuckGame;
using ImGuiNET;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Numerics;

namespace HatsPlusPlus; 
public class BufferingTest {

    List<Team> teams = new();
    Team empty;

    TeamHat hat1;
    TeamHat hat2;
    TeamHat hat3;
    Vector2 position;
    bool spawned_hats;
    int anim_frame;
    int anim_counter;
    int state;
    float time;
    bool exec_frame = false;
    int counter = 0;


    ScoreRock rock;

    public void Init() {
        var my_bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("image.png"));
        var bitmap = new System.Drawing.Bitmap(my_bitmap.Width, my_bitmap.Height);
        for (int x = 0; x < bitmap.Width; x++) {
            for (int y = 0; y < bitmap.Height; y++) {
                var pixel = my_bitmap.GetPixel(IVector2.New(x, y)).Unwrap();
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixel.a, pixel.r, pixel.g, pixel.b));
            }
        }
        var bitmap1 = new System.Drawing.Bitmap(Mod.GetPath<HatsPlusPlus2>("image1.png"));
        var bitmap2 = new System.Drawing.Bitmap(Mod.GetPath<HatsPlusPlus2>("image2.png"));
        var bitmap3 = new System.Drawing.Bitmap(Mod.GetPath<HatsPlusPlus2>("image3.png"));
        var bitmap4 = new System.Drawing.Bitmap(Mod.GetPath<HatsPlusPlus2>("image4.png"));
        var empty_b = new System.Drawing.Bitmap(Mod.GetPath<HatsPlusPlus2>("empty.png"));

        var data = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
        var data1 = (byte[])new ImageConverter().ConvertTo(bitmap1, typeof(byte[]));
        var data2 = (byte[])new ImageConverter().ConvertTo(bitmap2, typeof(byte[]));
        var data3 = (byte[])new ImageConverter().ConvertTo(bitmap3, typeof(byte[]));
        var data4 = (byte[])new ImageConverter().ConvertTo(bitmap4, typeof(byte[]));
        var data_e = (byte[])new ImageConverter().ConvertTo(empty_b, typeof(byte[]));
        var team = Team.DeserializeFromPNG(data, "image.png", "");
        var team1 = Team.DeserializeFromPNG(data1, "image1.png", "");
        var team2 = Team.DeserializeFromPNG(data2, "image2.png", "");
        var team3 = Team.DeserializeFromPNG(data3, "image3.png", "");
        var team4 = Team.DeserializeFromPNG(data4, "image4.png", "");
        empty = Team.DeserializeFromPNG(data_e, "empty", "");

        teams.Add(team);
        teams.Add(team1);
        teams.Add(team2);
        teams.Add(team3);
        teams.Add(team4);
    }

    public void Update() {
        time += 1f / 60f;
        anim_counter++;

        ImGui.Begin("Test window");
        if (ImGui.Button("spawn hats")) {
            foreach (var team in teams) {
                Teams.AddExtraTeam(team);
                Send.Message(new NMSpecialHat(team, DuckNetwork.localProfile, false));
            }

            hat1 = new TeamHat(20, 20, null);
            Level.Add(hat1);
            hat2 = new TeamHat(20, 20, null);
            Level.Add(hat2);
            hat3 = new TeamHat(20, 20, null);
            Level.Add(hat3);

            rock = new ScoreRock(0, -100, DuckNetwork.localProfile);
            rock.netDepth = DuckNetwork.localProfile.duck.depth.value + 0.1f;
            Level.Add(rock);
        }

        if (ImGui.Button("Exec frame")) {
            exec_frame = true;
        }

        if (ImGui.Button("setup 1")) {
            hat1.team = teams[2];
            hat1.y = -10000;

            hat2.y = -10000;
            hat2.team = teams[1];
            hat2.owner = rock;
            //state 1 to 3 is IMPOSSIBLE (while also animating)
            hat3.team = teams[0];
            hat3.owner = rock;

        }
        if (ImGui.Button("setup 2")) {
            hat3.active = false;
            hat3.position = new Vec2(position.X, position.Y);
            hat3.owner = null;
        }
        if (ImGui.Button("begin")) {
            spawned_hats = true;
        }
        bool should_animate = anim_counter % 10 == 0;
        if (spawned_hats) {
            var duck_pos = DuckNetwork.localProfile.duck.position;
            position = new Vector2(duck_pos.x, duck_pos.y);
            //hat2.angle = time;
            hat3.angle = 0;
            //hat1.angle = time;
        }
        if (spawned_hats && should_animate) {
            var h1 = hat1;
            var h2 = hat2;
            var h3 = hat3;

            hat2.position = new Vec2(position.X, position.Y);
            hat2.active = false;
            hat2.owner = null;

            hat3.active = true;
            if (should_animate) {
                counter++;
                if (counter == teams.Length()) {
                    counter = 0;
                }
                hat3.team = teams[counter];
            }
            hat3.owner = null;
            hat3.y = -10000;

            hat1.owner = rock;


            hat1 = h3;
            hat2 = h1;
            hat3 = h2;
        } else {
            if (hat3 != null) {
                hat3.position = new Vec2(position.X, position.Y);
            }
        }

        if (exec_frame) {
            exec_frame = false;
        }

        if (spawned_hats) {
            state++;
            if (state == 3) {
                state = 0;
            }
        ImGui.End();
        }
}
}
