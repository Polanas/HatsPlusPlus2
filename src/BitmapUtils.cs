using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.IO;
using DuckGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

namespace HatsPlusPlus; 
internal static class BitmapUtils {
    internal static HResult<(Bitmap, Option<AsepriteData>)> Load(string path) {
        switch (Path.GetExtension(path)) {
            case ".png":
                return Bitmap.FromPath(path).Map((b) => (b, Option<AsepriteData>.None));
            case ".aseprite":
                AsepriteFile aseprite;
                try {
                    aseprite = AsepriteFileLoader.FromFile(path);
                } catch (Exception e) {
                    return Err<(Bitmap, Option<AsepriteData>)>($"could not load aseprite file: {e.ToString()}");
                }
                var bitmap = Bitmap.Empty(aseprite.CanvasWidth * aseprite.FrameCount, aseprite.CanvasHeight);
                for (int i = 0; i < aseprite.Frames.Length; i++) {
                    var frame = aseprite.Frames[i];
                    var frameOffsetX = i * aseprite.CanvasWidth;
                    var frameRgba = frame.FlattenFrame(includeBackgroundLayer: true);
                    for (int x = 0; x < aseprite.CanvasWidth; x++) {
                        for (int y = 0; y < aseprite.CanvasHeight; y++) {
                            var index = y * aseprite.CanvasWidth + x;
                            var pixel = frameRgba[index];
                            if (bitmap.SetPixel(new IVector2(x + frameOffsetX, y), new Color(pixel.R, pixel.G, pixel.B, pixel.A)).IsNone) {
                                return Err<(Bitmap, Option<AsepriteData>)>($"bitmap size is ({bitmap.Width}, {bitmap.Height}), attempted to draw pixel at ({x+frameOffsetX}, {y})");
                            }
                        }
                    }
                }
                var animations = new List<Animation>();
                foreach (var tag in aseprite.Tags) {
                    var frames = Enumerable.Range(tag.From, (tag.To - tag.From)+1).ToList();
                    animations.Add(Animation.New(
                        name: tag.Name,
                        delay: 0,
                        looping: true, //TODO: implement looping,
                        frames: frames.Map((f) => AnimFrame.New(f, (float)aseprite.Frames[f].Duration.TotalSeconds)).ToList()
                    ));
                }

                return (bitmap, new AsepriteData {
                    frameSize = new IVector2(aseprite.CanvasWidth,aseprite.CanvasHeight),
                    animations = animations,
                });
        default:
                return Err<(Bitmap, Option<AsepriteData>)>($"expected extension to be either .png or .aseprite, got {Path.GetExtension(path)}");
        };
    }
}
