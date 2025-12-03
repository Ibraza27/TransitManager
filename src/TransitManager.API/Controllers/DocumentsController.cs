using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        // GET: api/documents/entity/vehicule/{id}
        [HttpGet("entity/{entityType}/{entityId}")]
        public async Task<ActionResult<IEnumerable<Document>>> GetByEntity(string entityType, Guid entityId)
        {
            try
            {
                var docs = await _documentService.GetDocumentsByEntityAsync(entityId, entityType);
                return Ok(docs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents.");
                return StatusCode(500, "Erreur interne.");
            }
        }

        // GET: api/documents/{id}/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            try
            {
                var result = await _documentService.GetFileStreamAsync(id);
                if (result == null) return NotFound("Fichier introuvable.");

                return File(result.Value.FileStream, result.Value.ContentType, result.Value.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement.");
                return StatusCode(500, "Erreur interne.");
            }
        }

        // POST: api/documents/upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file, 
            [FromForm] string typeDocStr, // On reçoit l'enum en string
            [FromForm] Guid? clientId,
            [FromForm] Guid? vehiculeId,
            [FromForm] Guid? colisId,
            [FromForm] Guid? conteneurId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier fourni.");

            if (!Enum.TryParse<TypeDocument>(typeDocStr, out var typeDoc))
                typeDoc = TypeDocument.Autre;

            try
            {
                using var stream = file.OpenReadStream();
                var doc = await _documentService.UploadDocumentAsync(
                    stream, 
                    file.FileName, 
                    file.ContentType, 
                    typeDoc, 
                    clientId, 
                    colisId, 
                    vehiculeId, 
                    conteneurId
                );

                return Ok(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload.");
                return StatusCode(500, $"Erreur interne : {ex.Message}");
            }
        }

        // DELETE: api/documents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
		
		// GET: api/documents/{id}/preview
		[HttpGet("{id}/preview")]
		public async Task<IActionResult> Preview(Guid id)
		{
			try
			{
				var result = await _documentService.GetFileStreamAsync(id);
				if (result == null) return NotFound("Fichier introuvable.");

				// "inline" dit au navigateur : essaie d'afficher ça (PDF, Image) au lieu de télécharger
				var contentDisposition = new System.Net.Mime.ContentDisposition
				{
					FileName = result.Value.FileName,
					Inline = true 
				};
				Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

				return File(result.Value.FileStream, result.Value.ContentType);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de la prévisualisation.");
				return StatusCode(500, "Erreur interne.");
			}
		}
		
    }
}