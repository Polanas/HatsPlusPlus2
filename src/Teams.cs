using DuckGame;
using LanguageExt.UnsafeValueAccess;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 

public static class TeamsStorage {
    public static TeamSlots slots;
    public static Dictionary<TeamHandle, TeamData> loadedTeams;

    public static void Init() {
        slots = TeamSlots.New();
        loadedTeams = [];
    }

    public static Option<TeamData> GetTeamData(TeamHandle handle) {
        if (loadedTeams.TryGetValue(handle, out var data)) {
            return Some(data);
        }
        return None;
    }

    public static void UnloadBitmap(TeamsBitmap bitmap) {
        foreach (var frame in bitmap.frames) {
            foreach (var teamHandle in frame.teamHandles) {
                UnloadTeam(teamHandle);
            }
        }
    }

    public static Either<TeamsBitmap, string> LoadTeamsBitmap(Bitmap bitmap, IVector2 frameSize) {
        var bitmaps = ChopBitmap(bitmap, frameSize);
        var teams = new List<List<(Team, Bitmap)>>();
        bool isBig = false;
        foreach (var frame in bitmaps) {
            var frame_teams = new List<(Team, Bitmap)>();
            foreach (var framePart in frame) {
                if (framePart.Width > Constants.MIN_DG_HAT_SIZE || framePart.Width > Constants.MIN_DG_HAT_SIZE) {
                    isBig = true;
                }

                var team = BitmapToTeam(framePart, "TODO");
                if (team.IsRight) {
                    return team.UnwrapErr();
                }

                frame_teams.Add((team.UnwrapOk(), framePart));
            }

            teams.Add(frame_teams);
        }

        var teamFrames = new List<TeamFrame>();
        foreach (var teamFrame in teams) {
            var handles = new List<TeamHandle>();
            foreach (var (team, map) in teamFrame) {
                var handle = AddTeam(team, map);
                if (handle.IsNone) {
                    return "could not add team: team limit exceeded";
                }
                handles.Add(handle.Unwrap());
            }
            teamFrames.Add(TeamFrame.New(handles));
        }
        return new TeamsBitmap {
            isBig = isBig,
            frames = teamFrames,
            frameSize = frameSize
        };
    }

    public static List<List<Bitmap>> ChopBitmap(Bitmap bitmap, IVector2 frameSize) {
        var framesAmountX = (int)Math.Floor((float)bitmap.Width / (float)frameSize.X);
        var framesAmountY = (int)Math.Floor((float)bitmap.Height / (float)frameSize.Y);

        var frames = new List<Bitmap>();
        for (int y = 0; y < framesAmountY; y++) {
            for (int x = 0; x < framesAmountX; x++) {
                var pos = new IVector2(x,y);
                var frame = bitmap.ClonePart(pos * frameSize, frameSize);
                frames.Add(frame);
            }
        }

        var chopedFrames = new List<List<Bitmap>>();
        int id = 0;
        foreach (var frame in frames) {
            List<Bitmap> currentFrame = null;
            if (frame.Width > Constants.MIN_DG_HAT_SIZE || frame.Height > Constants.MIN_DG_HAT_SIZE) {
                currentFrame = ChopBitmapFrame(frame);
                id++;
            } else {
                currentFrame = [frame];
            }
            chopedFrames.Add(currentFrame);
        }

        return chopedFrames;
    }
    public static List<Bitmap> ChopBitmapFrameSimple(Bitmap frame) {
        var frameSize = new IVector2(Constants.MIN_FRAME_SZIE);
        var partsAmountX = (int)Math.Ceiling((float)frame.Width / (float)frameSize.X);
        var partsAmountY = (int)Math.Ceiling((float)frame.Height / (float)frameSize.Y);
        var gapsAmountX = partsAmountX - 1;
        var gapsAmountY = partsAmountY - 1;
        var sizeX = frame.Width + gapsAmountX;
        var sizeY = frame.Height + gapsAmountY;

        var frames = new List<Bitmap>();
        for (int y = 0; y < partsAmountY; y++) {
            for (int x = 0; x < partsAmountX; x++) {
                var pos = new IVector2(x, y);
                var framePart = frame.ClonePart(pos * frameSize, frameSize);
                var framePartExtended = Bitmap.Empty(frameSize.X, frameSize.Y);
                framePartExtended.Draw(framePart, IVector2.Zero);
                frames.Add(framePartExtended);
            }
        }
        return frames;
    }

    public static List<Bitmap> ChopBitmapFrame(Bitmap frame) {
        var frameSize = new IVector2(Constants.MIN_FRAME_SZIE);
        var partsAmountX = (int)Math.Ceiling((float)frame.Width / (float)frameSize.X);
        var partsAmountY = (int)Math.Ceiling((float)frame.Height / (float)frameSize.Y);
        var gapsAmountX = partsAmountX - 1;
        var gapsAmountY = partsAmountY - 1;
        var sizeX = frame.Width + gapsAmountX;
        var sizeY = frame.Height + gapsAmountY;

        var frameWithGaps = Bitmap.Empty(sizeX, sizeY);

        for (int y = 0; y < partsAmountY; y++) {
            for (int x = 0; x < partsAmountX; x++) {
                var pos = new IVector2(x, y);
                var framePart = frame.ClonePart(pos * frameSize, frameSize);
                var framePartExtended = Bitmap.Empty(frameSize.X, frameSize.Y);
                framePartExtended.Draw(framePart, IVector2.Zero);
                frameWithGaps.Draw(framePartExtended, pos * frameSize + pos);
            }
        }

        for (int gapId = 0; gapId < gapsAmountX; gapId++) {
            var x = (gapId + 1) * frameSize.X + gapId;

            for (int y = 0; y < frameWithGaps.Height; y++) {
                var rightPixel = frameWithGaps.GetPixel(new IVector2(x - 1, y)).Unwrap();
                if (rightPixel.a == 255) {
                    frameWithGaps.SetPixel(new IVector2(x, y), rightPixel);
                }
            }
        }

        for (int gapId = 0; gapId < gapsAmountY; gapId++) {
            var y = (gapId + 1) * frameSize.Y + gapId;
            for (int x = 0; x < frameWithGaps.Width; x++) {
                var topPixel = frameWithGaps.GetPixel(new IVector2(x, y - 1)).Unwrap();
                if (topPixel.a == 255) {
                    frameWithGaps.SetPixel(new IVector2(x, y), topPixel);
                }
            }
        }

        var frames = new List<Bitmap>();
        for (int y = 0; y < partsAmountY; y++) {
            for (int x = 0; x < partsAmountX; x++) {
                var bitmap = frameWithGaps.ClonePart(new IVector2(x, y) * frameSize, frameSize);
                frames.Add(bitmap);
            }
        }

        return frames;
    }

    public static Either<Team, string> BitmapToTeam(Bitmap bitmap, string teamName) {
        if (bitmap.Width < Constants.MIN_DG_HAT_SIZE || bitmap.Height < Constants.MIN_DG_HAT_SIZE) {
            return Right("expected bitmap size to be at least 32x32");
        }
        if (bitmap.Width > Constants.MAX_DG_HAT_SIZE.X || bitmap.Height > Constants.MAX_DG_HAT_SIZE.Y) {
            return Right($"expected bitmap size to be {Constants.MAX_DG_HAT_SIZE} max");
        }

        var systemBitmap = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height);
        for (int x = 0; x < bitmap.Width; x++) {
            for (int y = 0; y < bitmap.Height; y++) {
                var pixel = bitmap.GetPixel(IVector2.New(x, y)).Unwrap();
                systemBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixel.a, pixel.r, pixel.g, pixel.b));
            }
        }

        var data = (byte[])new ImageConverter().ConvertTo(systemBitmap, typeof(byte[]));
        return Team.DeserializeFromPNG(data, teamName, bitmap.Path.IfNone(static () => ""));
    }

    public static Option<TeamHandle> AddTeam(Team team, Bitmap image) {
        var teamByImageOption = TeamByImage(image);
        if (teamByImageOption.ValueUnsafe() is var teamByImage && teamByImageOption.IsSome) {
            return teamByImage.handle;
        }

        var handleOption = slots.AddTeam();
        if (handleOption.Value() is var handle && handleOption.IsSome) { } else {
            return None;
        }

        loadedTeams.Add(handle, new TeamData {
            handle = handle,
            //clone to make sure that the bitmap stays intact
            image = image.Clone(),
            team = team,
        });

        Teams.AddExtraTeam(team);
        TeamsSender.AddTeam(handle);

        return handle;
    }

    public static Option<TeamData> TeamByImage(Bitmap image) {
        foreach (var teamData in loadedTeams.Values) {
            var other = teamData.image;
            if (other.IsEqualTo(image)) {
                return teamData;
            }
        }
        return None;
    }

    public static void UnloadAll() {
        foreach (var (handle, data) in loadedTeams.Map((pair) => (pair.Key, pair.Value))) {
            Teams.core.extraTeams.Remove(data.team);
            slots.RemoveTeam(handle);
            TeamsSender.RemoveTeam(handle);
        }

        loadedTeams.Clear();
    }

    public static void UnloadTeam(TeamHandle handle) {
        if (!loadedTeams.TryGetValue(handle, out var teamData)) {
            return;
        }

        Teams.core.extraTeams.Remove(teamData.team);
        TeamsSender.RemoveTeam(handle);
        slots.RemoveTeam(handle);
        loadedTeams.Remove(handle);
    }
}
