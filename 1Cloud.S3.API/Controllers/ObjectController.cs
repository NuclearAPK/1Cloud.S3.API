using Microsoft.AspNetCore.Mvc;
using OneCloud.S3.API.Infrastructure.Interfaces;
using OneCloud.S3.API.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace OneCloud.S3.API.Controllers
{
    /// <summary>
    /// �������������� � ��������
    /// </summary>
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Route("api/storage/object")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ObjectController : ControllerBase
    {
        private readonly ILogger<ObjectController> _logger;
        private readonly IStorageObjectRepository _storageRepository;

        public ObjectController(ILogger<ObjectController> logger, IStorageRepository storageRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageRepository = storageRepository ?? throw new ArgumentNullException(nameof(storageRepository));
        }

        /// <summary>
        /// �������� ������
        /// </summary>
        /// <param name="bucket">������������ ����������</param>
        /// <param name="filePath">������������ ��� ���� � �������</param>
        /// <param name="contentType">MIME-��� �������</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <response code="200">������ ����</response>
        /// <response code="404">���� ������ �� ������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpGet("{bucket}/{filePath}", Name = "GetObject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetObject(string bucket, string filePath, [Required] string contentType, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("�������� �� ���������� {Bucket} ������ {Object} � ����� {ContentType}", bucket,
                    filePath, contentType);

                var result = await _storageRepository.GetObjectAsync(bucket, filePath, cancellationToken);
                if (result.Length == 0) return NotFound();

                return File(result, filePath, Path.GetFileName(filePath));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// ��������� ��������� ������ �� ������
        /// </summary>
        /// <param name="bucket">������������ ����������</param>
        /// <param name="filePath">������������ ��� ���� � �������</param>
        /// <param name="expires">���� ��������� �������� ������</param>
        /// <returns></returns>
        /// <response code="200">������ ������ �� ������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpGet("url/{bucket}/{filePath}", Name = "GetObjectUrl")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetObjectUrl(string bucket, string filePath, [Required] DateTime expires)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("Get public link for {Object} from {Bucket} expired {UrlExpires}", filePath, bucket, expires);

                var result = _storageRepository.GetPreSignedUrl(bucket, filePath, expires);

                return Ok(new ObjectUrlDto
                {
                    Url = result,
                    Expires = expires,
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// ��������� ������
        /// </summary>
        /// <param name="bucket">������������ ����������</param>
        /// <param name="filePath"></param>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <response code="201">������ ������ ���� � ������� � bool ������� �������� ��������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpPost("{bucket}/{filePath}", Name = "CreateObject")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostObject(string bucket, string filePath, [Required] IFormFile file, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("Save to {Bucket} object {Object}", bucket, filePath);

                await _storageRepository.PutObjectAsync(bucket, filePath, file, cancellationToken);

                return CreatedAtAction("GetObject", new { bucket, filePath, file.ContentType });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// ������ ������ � �������
        /// </summary>
        /// <param name="bucket">������������ ����������</param>
        /// <param name="filePath">������������ ��� ���� � �������</param>
        /// <param name="isPublicRead">������� ���������� �������</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <response code="200">������ � ������� �������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpPut("permission/{bucket}/{filePath}", Name = "ChangeObjectPermissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutObjectPermission(string bucket, string filePath, bool isPublicRead, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("Change permissions on object {Object} from {Bucket} to public = {IsPublicRead}", filePath, bucket, isPublicRead);

                await _storageRepository.PutAclAsync(bucket, filePath, isPublicRead, cancellationToken);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// ���������� ������
        /// </summary>
        /// <param name="srcBucket">������������ ��������� ����������</param>
        /// <param name="srcFilePath">������������ ��� ���� ��������� �������</param>
        /// <param name="destBucket">������������ ���������� ����������</param>
        /// <param name="destFilePath">������������ ��� ���� ������� ����������</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <response code="200">������ ����������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpPut("copy/{srcBucket}/{srcFilePath}", Name = "CopyObject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutObjectCopy(string srcBucket, string srcFilePath, [Required] string destBucket, [Required] string destFilePath, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("Copy from bucket {Bucket} object {Object} to bucket {DestinationBucket} in object {DestinationObject}", srcBucket, srcFilePath, destBucket, destFilePath);

                await _storageRepository.CopyObjectAsync(srcBucket, srcFilePath, destBucket, destFilePath, cancellationToken);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// ������� ������
        /// </summary>
        /// <param name="bucket">������������ ����������</param>
        /// <param name="filePath">������������ ��� ���� � �������</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <response code="200">������ ������</response>
        /// <response code="400">������������ ��������� �������</response>
        /// <response code="500">���-�� ����� �� ���</response>
        [HttpDelete("{bucket}/{filePath}", Name = "DeleteObject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteObject(string bucket, string filePath, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                _logger.LogInformation("Delete from {Bucket} object {Object}", bucket, filePath);

                await _storageRepository.DeleteObjectAsync(bucket, filePath, cancellationToken);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}