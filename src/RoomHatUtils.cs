using DuckGame;
using LanguageExt.UnsafeValueAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 

enum VanillaRoomType {
    Bar,
    Basic,
    Bios,
    Fancy,
    Freezer,
    Greenhouse,
    Music,
    Office,
    Oldstone,
    Oldwood,
    Ship,
    Tiles,
    Tree
}

internal struct RoomInfo {
    internal HResult<TeamsBitmap> fg;
    internal HResult<TeamsBitmap> bg;
    internal Vec2 position;
}

internal static class RoomHatUtils {
    internal static readonly IVector2 roomSize = new(141, 87);
    internal static Dictionary<VanillaRoomType, Bitmap> masks = [];

    internal static void Init() {
        var roomTypes = Enumerable.Range((int)VanillaRoomType.Bar, (int)VanillaRoomType.Tree + 1);
        List<string> fileNames = [
            "bar",
            "basic",
            "bios",
            "fancy",
            "freezer",
            "greenhouse",
            "music",
            "office",
            "oldstone",
            "oldwood",
            "ship",
            "tiles",
            "tree",
        ];

        foreach (var (roomType, fileName) in roomTypes.Zip(fileNames)) {
            var bitmap = Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>($"RoomMasks\\{fileName}.png"));
            masks[(VanillaRoomType)roomType] = bitmap;
        }
    }

    internal static Option<VanillaRoomType> CurrentRoomType() {
        //TODO: implement this
        return VanillaRoomType.Oldstone;
    }

    internal static Option<RoomInfo> GetRoomInfo(Bitmap roomSprite) {
        var teamSelect = Level.current as TeamSelect2;
        if (teamSelect is null) {
            return None;
        }

        var profileBoxRect = teamSelect._profiles.First((p) => p.profile == Ducks.MainDuck.profile).rectangle;
        var roomFlipped = new Vec2(profileBoxRect.x, profileBoxRect.y) switch {
            Vec2 { x: 1, y: 1 } => false,
            Vec2 { x: 1, y: 90 } => false,
            Vec2 { x: 2, y: 179 } => false,
            _ => true
        };
        var currentRoomTypeOption = CurrentRoomType();
        if (currentRoomTypeOption.ValueUnsafe() is var currentRoomType && currentRoomTypeOption.IsSome) { } else {
            return None;
        };
        var mask = masks[currentRoomType];

        var fg = Bitmap.Empty((int)roomSize.X, (int)roomSize.Y);
        var bg = Bitmap.Empty((int)roomSize.X, (int)roomSize.Y);
        var halfWidth = (int)(roomSize.X / 2);

        for (int x = 0; x < roomSize.X; x++) {
            for (int y = 0; y < roomSize.Y; y++) {
                var spriteCoords = new IVector2(x, y);
                var correctedX = x;
                if (roomFlipped) {
                    var distanceToCenter = halfWidth - x;
                    correctedX += distanceToCenter * 2;

                    spriteCoords.X += roomSize.X;
                }
                var maskPixel = mask.GetPixel(new IVector2(correctedX,y)).Unwrap();
                var spritePixel = roomSprite.GetPixel(spriteCoords).Unwrap();

                var layer = maskPixel.r == 0 ? fg : bg;
                layer.SetPixel(new IVector2(x, y), spritePixel);
            }
        }

        var profileId = Ducks.ProfileId;
        return new RoomInfo {
            fg = TeamsStorage.LoadTeamsBitmap(fg, roomSize, TeamType.Chopped, ChopMode.Simple),
            bg = TeamsStorage.LoadTeamsBitmap(bg, roomSize, TeamType.Chopped, ChopMode.Simple),
            position = new Vec2(profileBoxRect.x, profileBoxRect.y)
        };
    }
}
