using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var sourceFilePath = configuration["TextFile"];

await using (var sourceFileStream = File.OpenRead(sourceFilePath))
{
    await using var writeStream = File.OpenWrite($"{sourceFilePath}.gz");
    await using var compressionStream = new GZipStream(writeStream, CompressionMode.Compress);

    await sourceFileStream.CopyToAsync(compressionStream);
}
