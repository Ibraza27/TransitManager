using System;
using System.IO;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace TransitManager.Infrastructure.Services
{
    public class BarcodeService : IBarcodeService
    {
        private readonly BarcodeWriter _barcodeWriter;
        private readonly BarcodeReader _barcodeReader;

        public BarcodeService()
        {
            // Configuration pour la génération de codes-barres
            _barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 100,
                    Width = 300,
                    Margin = 10,
                    PureBarcode = false // Afficher le texte sous le code-barres
                }
            };

            // Configuration pour la lecture de codes-barres
            _barcodeReader = new BarcodeReader
            {
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.EAN_8,
                        BarcodeFormat.QR_CODE,
                        BarcodeFormat.DATA_MATRIX
                    }
                }
            };
        }

        public string GenerateBarcode()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(1000, 9999);
            return $"{timestamp}{random}";
        }

#if WINDOWS
        public Task<byte[]> GenerateBarcodeImageAsync(string barcodeText, int width = 300, int height = 100)
        {
            return Task.Run(() =>
            {
                _barcodeWriter.Options.Width = width;
                _barcodeWriter.Options.Height = height;
                using var bitmap = _barcodeWriter.Write(barcodeText);
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            });
        }

        public Task<byte[]> GenerateQRCodeAsync(string data, int size = 200)
        {
            return Task.Run(() =>
            {
                var qrWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = size,
                        Width = size,
                        Margin = 10,
                        ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
                    }
                };
                using var bitmap = qrWriter.Write(data);
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            });
        }

        public Task<string?> ScanBarcodeAsync(byte[] imageData)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(imageData);
                    using var bitmap = new Bitmap(stream);
                    var result = _barcodeReader.Decode(bitmap);
                    return result?.Text;
                }
                catch
                {
                    return null;
                }
            });
        }

        public async Task<string?> ScanBarcodeFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            var imageData = await File.ReadAllBytesAsync(filePath);
            return await ScanBarcodeAsync(imageData);
        }

        public Task GenerateLabelAsync(Colis colis)
        {
            return Task.Run(() =>
            {
                using var bitmap = new Bitmap(400, 600);
                using var graphics = Graphics.FromImage(bitmap);

                graphics.Clear(Color.White);
                var titleFont = new Font("Arial", 16, FontStyle.Bold);
                var normalFont = new Font("Arial", 10);

                graphics.DrawString("TRANSIT MANAGER", titleFont, Brushes.Black, new PointF(10, 10));
                graphics.DrawLine(new Pen(Color.Black, 2), 10, 40, 390, 40);
                int y = 50;
                graphics.DrawString($"Référence: {colis.NumeroReference}", normalFont, Brushes.Black, new PointF(10, y));
                y += 25;
                graphics.DrawString($"Client: {colis.Client?.NomComplet ?? "N/A"}", normalFont, Brushes.Black, new PointF(10, y));

                var barcodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions { Height = 80, Width = 380, Margin = 0, PureBarcode = true }
                };
                using var barcodeBitmap = barcodeWriter.Write(colis.NumeroReference);
                graphics.DrawImage(barcodeBitmap, new Point(10, 200));
                var labelPath = Path.Combine("Labels", $"{colis.NumeroReference}_label.png");
                Directory.CreateDirectory("Labels");
                bitmap.Save(labelPath, ImageFormat.Png);
            });
        }
#else
        public Task<byte[]> GenerateBarcodeImageAsync(string barcodeText, int width = 300, int height = 100)
        {
            return Task.FromException<byte[]>(new NotImplementedException("GenerateBarcodeImageAsync requires a platform-specific implementation."));
        }

        public Task<byte[]> GenerateQRCodeAsync(string data, int size = 200)
        {
            return Task.FromException<byte[]>(new NotImplementedException("GenerateQRCodeAsync requires a platform-specific implementation."));
        }

        public Task<string?> ScanBarcodeAsync(byte[] imageData)
        {
            return Task.FromException<string?>(new NotImplementedException("ScanBarcodeAsync requires a platform-specific implementation."));
        }

        public Task<string?> ScanBarcodeFromFileAsync(string filePath)
        {
            return Task.FromException<string?>(new NotImplementedException("ScanBarcodeFromFileAsync requires a platform-specific implementation."));
        }

        public Task GenerateLabelAsync(Colis colis)
        {
            return Task.FromException(new NotImplementedException("GenerateLabelAsync requires a platform-specific implementation."));
        }
#endif
    }
}
