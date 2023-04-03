using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.AspNetCore.Mvc;

namespace s3_bucket_practice.Controllers;

[Controller]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public FilesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<List<string>>> RetrieveFilesList()
    {
        var accessKey = Environment.GetEnvironmentVariable("AccessKey", EnvironmentVariableTarget.Machine);
        var secretKey = Environment.GetEnvironmentVariable("SecretKey", EnvironmentVariableTarget.Machine);
        var bucketName = _configuration.GetValue<string>("BucketName");
        
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        try
        {
            using var s3Client = new AmazonS3Client(credentials, config);
        
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
            {
                return NotFound("Bucket was not found!");
            }

            var response = await s3Client.ListObjectsAsync(new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = "images"
            });

            var files = response.S3Objects.Select(obj => Path.GetFileName(obj.Key)).ToList();

            return Ok(files);
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("download/{imageName}")]
    public async Task<IActionResult> DownloadFile(string imageName)
    {
        var accessKey = Environment.GetEnvironmentVariable("AccessKey", EnvironmentVariableTarget.Machine);
        var secretKey = Environment.GetEnvironmentVariable("SecretKey", EnvironmentVariableTarget.Machine);
        var bucketName = _configuration.GetValue<string>("BucketName");
        
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        try
        {
            using var s3Client = new AmazonS3Client(credentials, config);
        
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
            {
                return NotFound("Bucket was not found!");
            }

            var imagesNames = (await s3Client.ListObjectsAsync(new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = "images"
            })).S3Objects.Select(obj => Path.GetFileName(obj.Key));

            var neededImage = imagesNames.FirstOrDefault(img => img.Contains(imageName));

            if (neededImage == null) return NotFound("The specified image was not found!");

            var getObjectRequest = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = $"images/{neededImage}"
            };

            var file = await s3Client.GetObjectAsync(getObjectRequest);

            var memoryStream = new MemoryStream();

            await file.ResponseStream.CopyToAsync(memoryStream);

            return File(memoryStream.ToArray(), file.Headers.ContentType, Path.GetFileName(file.Key));
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var accessKey = Environment.GetEnvironmentVariable("AccessKey", EnvironmentVariableTarget.Machine);
        var secretKey = Environment.GetEnvironmentVariable("SecretKey", EnvironmentVariableTarget.Machine);
        var bucketName = _configuration.GetValue<string>("BucketName");
        
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1
            };

            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = file.OpenReadStream(),
                    Key = "images/" + file.FileName,
                    BucketName = bucketName,
                    CannedACL = S3CannedACL.NoACL
                };
        
                using var s3Client = new AmazonS3Client(credentials, config);
        
                if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
                {
                    var bucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };
        
                    await s3Client.PutBucketAsync(bucketRequest);
                }
        
                var transferUtility = new TransferUtility(s3Client);
        
                await transferUtility.UploadAsync(uploadRequest);

                return NoContent();
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
    }
}