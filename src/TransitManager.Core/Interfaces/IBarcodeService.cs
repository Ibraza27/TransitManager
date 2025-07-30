using System.Threading.Tasks;
using TransitManager.Core.Entities; // <-- using ajouté

namespace TransitManager.Core.Interfaces
{
    public interface IBarcodeService
    {
        string GenerateBarcode();
        Task<byte[]> GenerateBarcodeImageAsync(string barcodeText, int width = 300, int height = 100);
        Task<byte[]> GenerateQRCodeAsync(string data, int size = 200);
        Task<string?> ScanBarcodeAsync(byte[] imageData);
        Task<string?> ScanBarcodeFromFileAsync(string filePath);
        Task GenerateLabelAsync(Colis colis);
    }
}