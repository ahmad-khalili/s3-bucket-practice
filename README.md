# Assignment 3: S3 Bucket Practice
## Running the app locally
You need the .NET 7 SDK installed
- Navigate to the project's base directory
- Execute dotnet run in the terminal (or run the project using your IDE of choice)
- Open `localhost:5119/swagger` in your browser
Note: You need to add `AccessKey` and `SecretKey` to your machine's environment variables or the operations won't be authorized

## Configurations
- `GET files`: This lists all files names inside the `image/` directory in the bucket specified in `appsettings.json`
- `GET files/download/{imageName}`: This downloads the specified file name that is only under the `images/` directory. No need to specify the type of the image,
it's already handled.
- `POST files`: This uploads a specified file to the bucket, and it creates a new bucket with the specified bucket name if no bucket was found under that name, then
uploads the file. Returns a 204 (No content) if it was successfully uploaded, otherwise, it'll return any issue found by the S3 SDK.

## Code Explanation

```ruby
var accessKey = Environment.GetEnvironmentVariable("AccessKey", EnvironmentVariableTarget.Machine);
var secretKey = Environment.GetEnvironmentVariable("SecretKey", EnvironmentVariableTarget.Machine);
var bucketName = _configuration.GetValue<string>("BucketName");
```

- The credentials keys are fetched from environment variables on the host machine, to avoid having exposing delicate info.
- the bucket name can be specified in the `appsettings.json` and fetched from there.

```ruby
var credentials = new BasicAWSCredentials(accessKey, secretKey);
        
var config = new AmazonS3Config
{
  RegionEndpoint = Amazon.RegionEndpoint.USEast1
};
using var s3Client = new AmazonS3Client(credentials, config);
```

- This creates a new S3 client to handle communication with Amazon's S3 using the AWS.SDK Nuget package
- You can also specify which region this client would be doing the communication with

```ruby
if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
{
  var bucketRequest = new PutBucketRequest
    {
      BucketName = bucketName,
      UseClientRegion = true
    };
        
  await s3Client.PutBucketAsync(bucketRequest);
 }
```

- The `DoesS3BucketExistV2Async` checks if the specified bucket name exists in the specified region and config and returns a boolean. This is used in the upload API, to
create a bucket with the specified name if it doesn't exist.
- The `PutBucketAsync` requires a `PutBucketRequest` object to specify the needed config, like access priviliges, bucketname, and region of the bucket to create.

```ruby
var uploadRequest = new TransferUtilityUploadRequest
{
  InputStream = file.OpenReadStream(),
  Key = "images/" + file.FileName,
  BucketName = bucketName,
  CannedACL = S3CannedACL.NoACL
};
var transferUtility = new TransferUtility(s3Client);
await transferUtility.UploadAsync(uploadRequest);
```

- The transfer utility is usually used in performing operations from the host machine to S3 and vice versa, such as uploading and downloading
- The `UploadAsync` method requires a `TransferUtilityUploadRequest` object which takes the needed configuration as properties, such as the bucket name, the path of file
you want to upload, the access level of that uploaded, and a stream of the file you want to upload.

```ruby
await s3Client.ListObjectsAsync(new ListObjectsRequest
{
  BucketName = bucketName,
  Prefix = "images"
});

var getObjectRequest = new GetObjectRequest
{
BucketName = bucketName,
Key = $"images/{neededImage}"
};
var file = await s3Client.GetObjectAsync(getObjectRequest);
```

- The `ListObjectsAsync` method requires a `ListObjectsRequest` object, which specifies which bucke to pull from, and the prefix of the files to match
- The `GetObjectAsync` method requires a `GetObjectRequest` object which takes the key, and the bucket of the needed file, and retrieves relevant info for downloading, like
a response stream of that file, which can be used to download.
