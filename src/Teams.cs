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

internal enum TeamType {
    Single,
    Chopped
}

struct AsepriteData {

}

internal static class TeamsStorage {
    internal static TeamSlots slots;
    internal static Dictionary<TeamHandle, TeamData> loadedTeams;
    internal static int hatSlotOffset;

    internal static void Init() {
        hatSlotOffset = Teams.core.extraTeams.Count;
        slots = TeamSlots.New();
        loadedTeams = [];
    }

    internal static Option<TeamData> GetTeamData(TeamHandle handle) {
        if (loadedTeams.TryGetValue(handle, out var data)) {
            return Some(data);
        }
        return None;
    }

    internal static void UnloadBitmap(TeamsBitmap bitmap) {
        foreach (var frame in bitmap.frames) {
            foreach (var teamHandle in frame.teamHandles) {
                UnloadTeam(teamHandle);
            }
        }
    }

    internal static HResult<(Bitmap, Option<AsepriteData>)> LoadBitmap(string path) {
        throw new NotImplementedException();
    }
    internal static Result.HResult<TeamsBitmap> LoadTeamsBitmap(string path, IVector2 frameSize, TeamType teamType = TeamType.Chopped, ChopMode chopMode = ChopMode.WithGaps) {
        var bitmap = Bitmap.FromPath(path);
        return LoadTeamsBitmap(bitmap, frameSize, teamType);
    }
    internal static Result.HResult<TeamsBitmap> LoadTeamsBitmap(Bitmap bitmap, IVector2 frameSize, TeamType teamType = TeamType.Chopped, ChopMode chopMode = ChopMode.WithGaps) {
        switch (teamType) {
            case TeamType.Chopped:
                var bitmaps = ChopBitmap(bitmap, frameSize, chopMode);
                var teams = new List<List<(Team, Bitmap)>>();
                foreach (var frame in bitmaps) {
                    var frameTeams = new List<(Team, Bitmap)>();
                    foreach (var framePart in frame) {
                        var teamResult = BitmapToTeam(framePart, Constants.TEAM_TAG);
                        if (teamResult.OkErrUnsafe() is (var team, var error) && teamResult.isErr) {
                            return error;
                        }

                        frameTeams.Add((team, framePart));
                    }

                    teams.Add(frameTeams);

                }
                var teamFrames = new List<TeamFrame>();
                foreach (var teamFrame in teams) {
                    var handles = new List<TeamHandle>();
                    foreach (var (team, map) in teamFrame) {
                        var handleOption = AddTeam(team, map);
                        if (handleOption.ValueUnsafe() is var handle && handleOption.IsNone) {
                            return "could not add team: team limit exceeded";
                        }
                        handles.Add(handle);
                    }
                    teamFrames.Add(TeamFrame.New(handles));
                }
                return new TeamsBitmap {
                    isBig = teamFrames.Count > 1,
                    frames = teamFrames,
                    frameSize = frameSize,
                    chopMode = chopMode,
                };
            case TeamType.Single:
                return BitmapToTeam(bitmap, Constants.TEAM_TAG).Match(
                    (value) => {
                        var handleOption = AddTeam(value, bitmap);
                        if (handleOption.ValueUnsafe() is var handle && handleOption.IsNone) {
                            return "could not add team: team limit exceeded";
                        }
                        return Ok(new TeamsBitmap {
                            isBig = false,
                            frames =  [TeamFrame.New([handle])],
                            frameSize = frameSize,
                            chopMode = chopMode,
                        });
                    },
                    (err) => err
                );
            default:
                throw new Exception("unreachable");
        }
    }

    internal static List<List<Bitmap>> ChopBitmap(Bitmap bitmap, IVector2 frameSize, ChopMode chopMode) {
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
                currentFrame = chopMode == ChopMode.WithGaps ? ChopBitmapFrame(frame) : ChopBitmapFrameSimple(frame);
                id++;
            } else {
                currentFrame = [frame];
            }
            chopedFrames.Add(currentFrame);
        }

        return chopedFrames;
    }

    internal static List<Bitmap> ChopBitmapFrameSimple(Bitmap frame) {
        var frameSize = new IVector2(Constants.MIN_FRAME_SZIE);
        var partsAmountX = (int)Math.Ceiling((float)frame.Width / (float)frameSize.X);
        var partsAmountY = (int)Math.Ceiling((float)frame.Height / (float)frameSize.Y);
        var sizeX = frame.Width;
        var sizeY = frame.Height;

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

    internal static List<Bitmap> ChopBitmapFrame(Bitmap frame) {
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

    internal static HResult<Team> BitmapToTeam(Bitmap bitmap, string teamName) {
        if (bitmap.Width < Constants.MIN_DG_HAT_SIZE || bitmap.Height < Constants.MIN_DG_HAT_SIZE) {
            return Err<Team>("expected bitmap size to be at least 32x32");
        }
        if (bitmap.Width > Constants.MAX_DG_HAT_SIZE.X || bitmap.Height > Constants.MAX_DG_HAT_SIZE.Y) {
            return Err<Team>($"expected bitmap size to be {Constants.MAX_DG_HAT_SIZE} max");
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

    internal static TeamsBitmap ReloadTeamsBitmap(TeamsBitmap bitmap, Bitmap newBitmap) {
        throw new NotImplementedException();
    }

    internal static void ReloadTeam(TeamHandle handle, Team team) {
        Teams.core.extraTeams[(int)handle.id.value + hatSlotOffset] = team;
        loadedTeams[handle].team = team;
        //Stop the hat from being sent again if it's in the queue
        TeamsSender.RemoveTeamFromQueue(handle);
        TeamsSender.AddTeam(handle);
    }

    internal static Option<TeamHandle> AddTeam(Team team, Bitmap image) {
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


        int index = (int)handle.id.value + hatSlotOffset;
        Assert(index <= Teams.core.extraTeams.Count, "expected team index not exceed extraTeams length");
        if (index == Teams.core.extraTeams.Count) {
            Teams.core.extraTeams.Add(team);
        } else {
            Teams.core.extraTeams[index] = team;
        }
        TeamsSender.AddTeam(handle);

        return handle;
    }

    internal static Option<TeamData> TeamByImage(Bitmap image) {
        foreach (var teamData in loadedTeams.Values) {
            var other = teamData.image;
            if (other.IsEqualTo(image)) {
                return teamData;
            }
        }
        return None;
    }

    internal static void UnloadAll() {
        foreach (var (handle, data) in loadedTeams.Map((pair) => (pair.Key, pair.Value))) {
            slots.RemoveTeam(handle);
            TeamsSender.RemoveTeam(handle);
        }

        loadedTeams.Clear();
    }

    internal static void UnloadTeam(TeamHandle handle) {
        if (!loadedTeams.TryGetValue(handle, out var teamData)) {
            return;
        }

        TeamsSender.RemoveTeam(handle);
        slots.RemoveTeam(handle);
        loadedTeams.Remove(handle);
    }
}
