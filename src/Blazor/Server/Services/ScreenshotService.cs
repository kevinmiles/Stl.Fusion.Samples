using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.OS;
using Samples.Blazor.Common.Services;

namespace Samples.Blazor.Server.Services
{
    [ComputeService(typeof(IScreenshotService))]
    public class ScreenshotService : IScreenshotService
    {
        private readonly ImageCodecInfo _jpegEncoder;
        private readonly EncoderParameters _jpegEncoderParameters;
        private readonly Rectangle _displayDimensions;

        public ScreenshotService()
        {
            _jpegEncoder = ImageCodecInfo
                .GetImageDecoders()
                .Single(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            _jpegEncoderParameters = new EncoderParameters(1) {
                Param = {[0] = new EncoderParameter(Encoder.Quality, 50L)}
            };
            _displayDimensions = DisplayInfo.PrimaryDisplayDimensions
                ?? new Rectangle(0, 0, 1920, 1080);
        }

        public virtual async Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default)
        {
            var bScreen = await GetScreenshotAsync(cancellationToken).ConfigureAwait(false);
            // The code below scales a full-resolution screenshot to a desirable resolution
            var (w, h) = (_displayDimensions.Width, _displayDimensions.Height);
            var ow = width;
            var oh = h * ow / w;
            using var bOut = new Bitmap(ow, oh);
            using var gOut = Graphics.FromImage(bOut);
            gOut.CompositingQuality = CompositingQuality.HighSpeed;
            gOut.InterpolationMode = InterpolationMode.Default;
            gOut.CompositingMode = CompositingMode.SourceCopy;
            gOut.DrawImage(bScreen, 0, 0, ow, oh);
            await using var stream = new MemoryStream();
            bOut.Save(stream, _jpegEncoder, _jpegEncoderParameters);
            var bytes = stream.ToArray();
            var base64Content = Convert.ToBase64String(bytes);
            return new Screenshot(ow, oh, base64Content);
        }

        [ComputeMethod(AutoInvalidateTime = 0.02)]
        protected virtual Task<Bitmap> GetScreenshotAsync(CancellationToken cancellationToken = default)
        {
            // This method takes a full-resolution screenshot
            var (w, h) = (_displayDimensions.Width, _displayDimensions.Height);
            var bScreen = new Bitmap(w, h);
            using var gScreen = Graphics.FromImage(bScreen);
            gScreen.CopyFromScreen(0, 0, 0, 0, bScreen.Size);
            Computed.GetCurrent()!.Invalidated += c => Task.Delay(2000).ContinueWith(_ => {
                // Let's dispose these values in 2 seconds
                var computed = (IComputed<Bitmap>) c;
                if (computed.HasValue)
                    computed.Value.Dispose();
            });
            return Task.FromResult(bScreen);
        }
    }
}
