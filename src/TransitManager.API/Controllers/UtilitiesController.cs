using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilitiesController : ControllerBase
    {
        private readonly IBarcodeService _barcodeService;

        public UtilitiesController(IBarcodeService barcodeService)
        {
            _barcodeService = barcodeService;
        }

        // GET: api/utilities/generate-barcode
        [HttpGet("generate-barcode")]
        public ActionResult<string> GenerateBarcode()
        {
            return Ok(_barcodeService.GenerateBarcode());
        }
    }
}