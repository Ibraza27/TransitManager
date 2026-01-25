using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommerceController : ControllerBase
    {
        private readonly ICommerceService _commerceService;

        public CommerceController(ICommerceService commerceService)
        {
            _commerceService = commerceService;
        }

        // --- Products endpoints moved to ProductsController.cs ---

        // --- Quotes ---

        [HttpGet("quotes")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> GetQuotes([FromQuery] string? search, [FromQuery] Guid? clientId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _commerceService.GetQuotesAsync(search, clientId, status, page, pageSize);
            foreach(var item in result.Items)
            {
                item.PublicUrl = $"https://hippocampetransitmanager.com/portal/quote/{item.PublicToken}";
            }
            return Ok(result);
        }

        [HttpGet("quotes/{id}")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> GetQuote(Guid id)
        {
            var result = await _commerceService.GetQuoteByIdAsync(id);
            if (result == null) return NotFound();
            
            result.PublicUrl = $"https://hippocampetransitmanager.com/portal/quote/{result.PublicToken}";
            return Ok(result);
        }

        [HttpPost("quotes")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> CreateOrUpdateQuote([FromBody] UpsertQuoteDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                System.IO.File.AppendAllText("api_debug_log.txt", $"[{DateTime.Now}] Validation Error: {errors}\n");
                return BadRequest($"Validation Failed: {errors}");
            }

            try
            {
                var result = await _commerceService.CreateOrUpdateQuoteAsync(dto);
                result.PublicUrl = $"https://hippocampetransitmanager.com/portal/quote/{result.PublicToken}";
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("api_debug_log.txt", $"[{DateTime.Now}] Exception: {ex}\n");
                return BadRequest($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }

        [HttpPost("quotes/{id}/status")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> UpdateQuoteStatus(Guid id, [FromQuery] QuoteStatus status, [FromQuery] string? rejectionReason)
        {
            var success = await _commerceService.UpdateQuoteStatusAsync(id, status, rejectionReason);
            if (!success) return NotFound();
            return Ok();
        }

        [HttpPost("quotes/{id}/email")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> SendQuoteEmail(Guid id, [FromBody] SendQuoteEmailDto request)
        {
            await _commerceService.SendQuoteByEmailAsync(id, request.Subject, request.Body, request.TempAttachmentIds);
            return Ok();
        }

        [HttpDelete("quotes/{id}")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> DeleteQuote(Guid id)
        {
            var success = await _commerceService.DeleteQuoteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("quotes/{id}/pdf")]
        [AllowAnonymous] 
        public async Task<IActionResult> DownloadQuotePdf(Guid id, [FromQuery] Guid? token = null)
        {
            // Allow download if Admin OR if valid public token provided matching the quote
            if (token.HasValue)
            {
                var quote = await _commerceService.GetQuoteByTokenAsync(token.Value);
                if (quote == null || quote.Id != id) return Unauthorized();
            }
            else
            {
                // Must be admin if no token
                if (!User.Identity.IsAuthenticated || !User.IsInRole("Administrateur"))
                    return Unauthorized();
            }

            var quoteDto = await _commerceService.GetQuoteByIdAsync(id);
            if (quoteDto == null) return NotFound();

            var pdfBytes = await _commerceService.GenerateQuotePdfAsync(quoteDto);
            return File(pdfBytes, "application/pdf", $"Devis_{quoteDto.Reference}.pdf");
        }

        // --- Public Portal ---

        [HttpGet("public/quote/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicQuote(Guid token)
        {
            var result = await _commerceService.GetQuoteByTokenAsync(token);
            if (result == null) return NotFound("Quote not found or invalid token.");
            return Ok(result);
        }

        [HttpPost("public/quote/{token}/accept")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptPublicQuote(Guid token)
        {
            // Security: Retrieve ID first from Token
            var quote = await _commerceService.GetQuoteByTokenAsync(token);
            if (quote == null) return NotFound();

            if (quote.Status == QuoteStatus.Accepted || quote.Status == QuoteStatus.Rejected)
                return BadRequest("Quote already processed.");

            await _commerceService.UpdateQuoteStatusAsync(quote.Id, QuoteStatus.Accepted);
            return Ok();
        }

        [HttpPost("public/quote/{token}/reject")]
        [AllowAnonymous]
        public async Task<IActionResult> RejectPublicQuote(Guid token, [FromBody] string reason)
        {
             var quote = await _commerceService.GetQuoteByTokenAsync(token);
             if (quote == null) return NotFound();

             if (quote.Status == QuoteStatus.Accepted)
                 return BadRequest("Quote already accepted.");

             await _commerceService.UpdateQuoteStatusAsync(quote.Id, QuoteStatus.Rejected, reason);
             return Ok();
        }

        [HttpPost("public/quote/{token}/request-changes")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestChangesPublicQuote(Guid token, [FromBody] string comment)
        {
             var quote = await _commerceService.GetQuoteByTokenAsync(token);
             if (quote == null) return NotFound();

             if (quote.Status == QuoteStatus.Accepted)
                 return BadRequest("Quote already accepted.");

             // We use UpdateQuoteStatusAsync to handle status change and history logging
             // The service should ideally handle the "ChangeRequested" status and history note
             await _commerceService.UpdateQuoteStatusAsync(quote.Id, QuoteStatus.ChangeRequested, comment);
             return Ok();
        }
    }
}
