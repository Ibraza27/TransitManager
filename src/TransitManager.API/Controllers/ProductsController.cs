using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Entities.Commerce;
using TransitManager.Core.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public ProductsController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] string? search = null)
        {
            IEnumerable<Product> products;
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = await _productService.SearchAsync(search);
            }
            else
            {
                products = await _productService.GetAllAsync();
            }
            return Ok(_mapper.Map<IEnumerable<ProductDto>>(products));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(_mapper.Map<ProductDto>(product));
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(ProductDto dto)
        {
            var product = _mapper.Map<Product>(dto);
            // Ensure ID logic if not provided
            if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
            
            await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, _mapper.Map<ProductDto>(product));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> Update(Guid id, ProductDto dto)
        {
            if (id != dto.Id) return BadRequest();
            
            var existing = await _productService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            _mapper.Map(dto, existing);
            await _productService.UpdateAsync(existing);
            return Ok(_mapper.Map<ProductDto>(existing));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("delete-many")]
        public async Task<IActionResult> DeleteMany([FromBody] IEnumerable<Guid> ids)
        {
            await _productService.DeleteManyAsync(ids);
            return NoContent();
        }
        
        [HttpPost("import")]
        public async Task<IActionResult> ImportCsv()
        {
             // Accept multipart/form-data with file
             var file = Request.Form.Files.GetFile("file");
             if (file == null || file.Length == 0) return BadRequest("No file uploaded");
             
             using var reader = new StreamReader(file.OpenReadStream());
             var content = await reader.ReadToEndAsync();
             var count = await _productService.ImportCsvAsync(content);
             return Ok(new { Count = count });
        }
        
        [HttpGet("export")]
        public async Task<IActionResult> ExportCsv()
        {
            var bytes = await _productService.ExportCsvAsync();
            return File(bytes, "text/csv", "produits.csv");
        }
    }
}
