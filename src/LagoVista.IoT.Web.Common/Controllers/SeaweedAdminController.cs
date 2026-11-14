using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage.StorageProviders.File;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// System Admin read-only SeaweedFS S3 browser. Uses the existing host configuration
    /// and never accepts storage credentials, host names, or writable operations.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [ApiController]
    [Route("api/sysadmin/storage/seaweed")]
    public sealed class SeaweedAdminController : ControllerBase
    {
        private readonly IS3AdminRepo _repo;
        private readonly ICloudFileStorageClient _files;

        public SeaweedAdminController(IS3AdminRepo repo, ICloudFileStorageClient files)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _files = files ?? throw new ArgumentNullException(nameof(files));
        }

        [McpEndpoint("seaweed-buckets", FamilyId = "sys",
            Description = "List SeaweedFS S3 buckets visible to the configured storage account. SystemAdmin only; read-only.")]
        [HttpGet("buckets")]
        public async Task<InvokeResult<List<string>>> GetBucketsAsync(CancellationToken ct = default)
        {
            try
            {
                var buckets = await _repo.GetBucketsAsync(ct);
                return InvokeResult<List<string>>.Create(buckets.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<string>>.FromError($"SeaweedFS bucket discovery failed: {ex.Message}");
            }
        }

        [McpEndpoint("seaweed-objects", FamilyId = "sys",
            Description = "List up to 200 objects and folder prefixes under an S3 bucket and prefix. SystemAdmin only; read-only.")]
        [HttpGet("objects")]
        public async Task<InvokeResult<S3AdminObjectList>> GetObjectsAsync(
            [FromQuery] string bucket, [FromQuery] string prefix = null, CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<S3AdminObjectList>.Create(await _repo.ListObjectsAsync(bucket, prefix, ct));
            }
            catch (Exception ex)
            {
                return InvokeResult<S3AdminObjectList>.FromError($"SeaweedFS object discovery failed: {ex.Message}");
            }
        }

        [McpEndpoint("seaweed-object", FamilyId = "sys",
            Description = "Get one SeaweedFS S3 object's metadata without reading its content. Supply bucket and key. SystemAdmin only.")]
        [HttpGet("object")]
        public async Task<InvokeResult<S3AdminObjectInfo>> GetObjectAsync(
            [FromQuery] string bucket, [FromQuery] string key, CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<S3AdminObjectInfo>.Create(await _repo.GetObjectAsync(bucket, key, ct));
            }
            catch (Exception ex)
            {
                return InvokeResult<S3AdminObjectInfo>.FromError($"SeaweedFS object metadata failed: {ex.Message}");
            }
        }

        [McpEndpoint("seaweed-read-url", FamilyId = "sys",
            Description = "Return a short-lived, read-only signed URL for one SeaweedFS object. SystemAdmin only; the URL grants temporary object access. Never share it publicly.")]
        [HttpGet("read-url")]
        public async Task<InvokeResult<string>> GetReadUrlAsync(
            [FromQuery] string bucket, [FromQuery] string key, CancellationToken ct = default)
        {
            try
            {
                S3AdminRepo.ValidateBucket(bucket);
                S3AdminRepo.ValidateKey(key);
                ct.ThrowIfCancellationRequested();
                var result = await _files.CreateReadUrlAsync(bucket, key, TimeSpan.FromMinutes(2));
                return result.Successful && result.Result != null
                    ? InvokeResult<string>.Create(result.Result.ToString())
                    : InvokeResult<string>.FromError("Unable to generate a read-only URL for this object.");
            }
            catch (Exception ex)
            {
                return InvokeResult<string>.FromError($"SeaweedFS object read link failed: {ex.Message}");
            }
        }
    }
}
