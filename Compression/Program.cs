using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

var fileChars = (await File.ReadAllTextAsync(sourceFilePath)).ToCharArray();
var charGroups = fileChars
    .GroupBy(chr => chr)
    .Select(group => (group.Key,
        Count: group.Count(),
        Chance: group.Count() / (double)fileChars.Length))
    .ToArray();
var entropy = charGroups.Sum(group => group.Chance * Math.Log2(1 / group.Chance));
var redundancy = Math.Log2(charGroups.Length) - entropy;

Console.WriteLine($"entropy: {entropy}\nredundancy: {redundancy}");
