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

internal struct AsepriteData {
    internal IVector2 frameSize;
    internal List<Animation> animations;
}

internal enum SyncVariant {
    Sync,
    NotSync
}

internal static class TeamsStorage {
    internal static TeamSlots slots;
    internal static Dictionary<TeamHandle, TeamData> loadedTeams;
    internal static Dictionary<TeamId,Option<TeamHandle>> handlesByIds;

    internal static void Init() {
        slots = TeamSlots.New();
        loadedTeams = [];
        handlesByIds = [];
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

    internal static Result.HResult<TeamsBitmap> LoadTeams(
        Bitmap bitmap,
        Option<IVector2> frameSizeOpt,
        Option<IVector2> partSizeOpt,
        ChopMode chopMode = ChopMode.WithGaps) {
        var frameSize = frameSizeOpt.ValueOr(bitmap.Size);
        var partSize = partSizeOpt.ValueOr(Constants.MIN_HAT_SIZE_VEC);

        IVector2 frameSizeWithGaps;
        var bitmaps = ChopBitmap(bitmap, frameSize, partSize, chopMode, out frameSizeWithGaps);
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
        return Ok<TeamsBitmap>((new TeamsBitmap {
            frameSizeWithGaps = frameSizeWithGaps,
            isBig = teamFrames.Count > 1,
            frames = teamFrames,
            frameSize = frameSize,
            chopMode = chopMode,
            asepriteData = None
        }));
    //case TeamType.Single:
    //    return BitmapToTeam(bitmap, Constants.TEAM_TAG).MatchRet(
    //        (value) => {
    //            var handleOption = AddTeam(value, bitmap);
    //            if (handleOption.ValueUnsafe() is var handle && handleOption.IsNone) {
    //                return "could not add team: team limit exceeded";
    //            }
    //            return Ok<TeamsBitmap>((new TeamsBitmap {
    //                isBig = false,
    //                frames =  [TeamFrame.New([handle])],
    //                frameSize = frameSize,
    //                chopMode = chopMode,
    //                asepriteData = None
    //            }));
    //        },
    //        (err) => err
    //    );
    //default:
    //    throw new Exception("unreachable");
    }
    internal static Result.HResult<TeamsBitmap> LoadTeams(
        string path,
        Option<IVector2> frameSizeOpt,
        Option<IVector2> partSizeOpt,
        ChopMode chopMode = ChopMode.WithGaps) {
        var bitmapResult = BitmapUtils.Load(path);
        if (bitmapResult.OkUnsafe() is var (bitmap, asepriteDataOption) && bitmapResult.IsOk) { } else {
            return Err<TeamsBitmap>(bitmapResult.ErrUnsafe());
        }
        IVector2 frameSize;
        if (asepriteDataOption.ValueUnsafe() is var asepriteData && asepriteDataOption.IsSome) {
            frameSize = asepriteData.frameSize;
        } else {
            frameSize = frameSizeOpt.ValueOr(bitmap.Size);
        }
        var partSize = partSizeOpt.ValueOr(Constants.MIN_HAT_SIZE_VEC);
        var bitmaps = ChopBitmap(bitmap, frameSize, partSize, chopMode, out IVector2 frameSizeWithGaps);
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
        return Ok<TeamsBitmap>((new TeamsBitmap {
            frameSizeWithGaps = frameSizeWithGaps,
            isBig = frameSize.X > 32 || frameSize.Y > 32,
            frames = teamFrames,
            frameSize = frameSize,
            chopMode = chopMode,
            asepriteData = asepriteDataOption
        }));
        //    case TeamType.Single:
        //        return BitmapToTeam(bitmap, Constants.TEAM_TAG).MatchRet(
        //            (value) => {
        //                var handleOption = AddTeam(value, bitmap);
        //                if (handleOption.ValueUnsafe() is var handle && handleOption.IsNone) {
        //                    return "could not add team: team limit exceeded";
        //                }
        //                return Ok<TeamsBitmap>((new TeamsBitmap {
        //                    isBig = false,
        //                    frames = [TeamFrame.New([handle])],
        //                    frameSize = frameSize,
        //                    chopMode = chopMode,
        //                    asepriteData = asepriteDataOption,
        //                }));
        //            },
        //            (err) => err
        //        );
        //    default:
        //        throw new Exception("unreachable");
        //}
    }

    internal static List<List<Bitmap>> ChopBitmap(Bitmap bitmap, IVector2 frameSize, IVector2 partSize, ChopMode chopMode, out IVector2 frameSizeWithGaps) {
        var framesAmountX = (int)Math.Floor((float)bitmap.Width / (float)frameSize.X);
        var framesAmountY = (int)Math.Floor((float)bitmap.Height / (float)frameSize.Y);

        frameSizeWithGaps = frameSize;

        var frames = new List<Bitmap>();
        for (int y = 0; y < framesAmountY; y++) {
            for (int x = 0; x < framesAmountX; x++) {
                var pos = new IVector2(x,y);
                var frame = bitmap.ClonePart(pos * frameSize, frameSize);
                frames.Add(frame);
            }
        }

        var chopedFrames = new List<List<Bitmap>>();
        foreach (var frame in frames) {
            List<Bitmap> currentFrame = null;
            if (frame.Width > Constants.MIN_HAT_SIZE || frame.Height > Constants.MIN_HAT_SIZE) {
                currentFrame = chopMode == ChopMode.WithGaps ? ChopBitmapFrame(frame, partSize, out frameSizeWithGaps) : ChopBitmapFrameSimple(frame, partSize);
            } else {
                currentFrame = [frame];
            }
            chopedFrames.Add(currentFrame);
        }

        return chopedFrames;
    }

    internal static List<Bitmap> ChopBitmapFrameSimple(Bitmap frame, IVector2 partSize) {
        var partsAmountX = (int)Math.Ceiling((float)frame.Width / (float)partSize.X);
        var partsAmountY = (int)Math.Ceiling((float)frame.Height / (float)partSize.Y);
        var sizeX = frame.Width;
        var sizeY = frame.Height;

        var frames = new List<Bitmap>();
        for (int y = 0; y < partsAmountY; y++) {
            for (int x = 0; x < partsAmountX; x++) {
                var pos = new IVector2(x, y);
                var framePart = frame.ClonePart(pos * partSize, partSize);
                var framePartExtended = Bitmap.Empty(partSize.X, partSize.Y);
                framePartExtended.Draw(framePart, IVector2.Zero);
                frames.Add(framePartExtended);
            }
        }
        return frames;
    }

    internal static List<Bitmap> ChopBitmapFrame(Bitmap frame, IVector2 partSize, out IVector2 frameSize) {
        var partsAmountX = (int)Math.Ceiling((float)frame.Width / (float)partSize.X);
        var partsAmountY = (int)Math.Ceiling((float)frame.Height / (float)partSize.Y);
        var gapsAmountX = partsAmountX - 1;
        var gapsAmountY = partsAmountY - 1;
        var sizeX = frame.Width + gapsAmountX;
        var sizeY = frame.Height + gapsAmountY;
        frameSize = new IVector2(sizeX, sizeY);

        var partsAmountWithGapsX = (int)Math.Ceiling((float)sizeX / (float)partSize.X);
        var partsAmountWithGapsY = (int)Math.Ceiling((float)sizeY / (float)partSize.Y);

        var frameWithGaps = Bitmap.Empty(sizeX, sizeY);

        for (int y = 0; y < frameWithGaps.Width; y++) {
            for (int x = 0; x < frameWithGaps.Height; x++) {
                frameWithGaps.SetPixel(new IVector2(x, y), DuckGame.Color.White);
            }
        }
        for (int y = 0; y < partsAmountWithGapsY; y++) {
            for (int x = 0; x < partsAmountWithGapsX; x++) {
                var pos = new IVector2(x, y);
                var framePart = frame.ClonePart(pos * partSize, partSize);
                var framePartExtended = Bitmap.Empty(partSize.X, partSize.Y);
                framePartExtended.Draw(framePart, IVector2.Zero);

                frameWithGaps.Draw(framePartExtended, pos * partSize + pos);
            }
        }

        for (int gapId = 0; gapId < gapsAmountX; gapId++) {
            var x = (gapId + 1) * partSize.X + gapId;

            for (int y = 0; y < frameWithGaps.Height; y++) {
                var rightPixel = frameWithGaps.GetPixel(new IVector2(x - 1, y)).Unwrap();
                if (rightPixel.a == 255) {
                    frameWithGaps.SetPixel(new IVector2(x, y), rightPixel);
                }
            }
        }

        for (int gapId = 0; gapId < gapsAmountY; gapId++) {
            var y = (gapId + 1) * partSize.Y + gapId;
            for (int x = 0; x < frameWithGaps.Width; x++) {
                var topPixel = frameWithGaps.GetPixel(new IVector2(x, y - 1)).Unwrap();
                if (topPixel.a == 255) {
                    frameWithGaps.SetPixel(new IVector2(x, y), topPixel);
                }
            }
        }
        //TODO: figure out what's wrong with ClonePart
        var frames = new List<Bitmap>();
        for (int y = 0; y < partsAmountWithGapsY; y++) {
            for (int x = 0; x < partsAmountWithGapsX; x++) {
                var bitmap = frameWithGaps.ClonePart(new IVector2(x, y) * partSize, partSize);
                frames.Add(bitmap);
            }
        }

        return frames;
    }

    internal static HResult<Team> BitmapToTeam(Bitmap bitmap, string teamName) {
        if (bitmap.Width < Constants.MIN_HAT_SIZE || bitmap.Height < Constants.MIN_HAT_SIZE) {
            return Err<Team>("expected bitmap size to be at least 32x32");
        }
        if (bitmap.Width > Constants.MAX_TEAM_SIZE.X || bitmap.Height > Constants.MAX_TEAM_SIZE.Y) {
            return Err<Team>($"expected bitmap size to be {{{Constants.MAX_TEAM_SIZE.X}, {Constants.MAX_TEAM_SIZE.Y}}} max");
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
        Teams.core.extraTeams[(int)handle.id.value + TeamIdOffset()] = team;
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

        handlesByIds[handle.id] = handle;
        loadedTeams.Add(handle, new TeamData {
            handle = handle,
            //clone to make sure that the bitmap stays intact
            image = image.Clone(),
            team = team,
        });


        int index = (int)handle.id.value + TeamIdOffset();
        Assert(index <= Teams.core.extraTeams.Count, "expected team index to be less than extraTeams length");
        if (index == (Teams.core.extraTeams.Count)) {
            Teams.core.extraTeams.Add(team);
        } else {
            Teams.core.extraTeams[index] = team;
        }

        TeamsSender.AddTeam(handle);

        return handle;
    }

    internal static int TeamIdOffset() {
        var offset = 0;

        foreach (var team in Teams.core.extraTeams) {
            if (team.name.Contains(Constants.TEAM_TAG)) {
                break;
            }
            offset++;
        }
        return offset;
    }

    internal static Option<TeamData> TeamByImage(Bitmap image) {
        foreach (var teamData in loadedTeams.Values) {
            var other = teamData.image;
            if (other.SameAs(image)) {
                return teamData;
            }
        }
        return None;
    }

    internal static void UnloadAll() {
        foreach (var (handle, data) in loadedTeams.Map((pair) => (pair.Key, pair.Value))) {
            var team = GetTeamData(handle).Unwrap().team;
            //int index = TeamIdOffset() + (int)handle.id.value;
            //Teams.core.extraTeams[index] = null;
            slots.RemoveTeam(handle);
            TeamsSender.RemoveTeam(handle);
        }
        slots.Clear();

        handlesByIds.Clear();
        loadedTeams.Clear();
    }

    internal static void UnloadTeam(TeamHandle handle) {
        if (!loadedTeams.TryGetValue(handle, out var teamData)) {
            return;
        }

        //int index = TeamIdOffset() + (int)handle.id.value;
        //Teams.core.extraTeams[index] = null;

        TeamsSender.RemoveTeam(handle);
        slots.RemoveTeam(handle);
        loadedTeams.Remove(handle);
        handlesByIds[handle.id] = None;
    }
}
