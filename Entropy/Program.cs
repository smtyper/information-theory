using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var destinationFolder = configuration["DestinationFolder"];

var textFilePath = configuration["TextFile"];
var bookText = await File.ReadAllTextAsync(textFilePath);
var characherArray = bookText
    .Where(chr => chr is not '\r')
    .Select(chr => chr is '\n' ? @"\n" : chr.ToString())
    .ToArray();

var (textEntropy, textInformation) = await GetInformationAsync(characherArray, Path.Combine(destinationFolder,
    $"{Path.GetFileName(textFilePath)}.results.tsv"));

var imageFilePath = configuration["ImageFile"];
await using var imageFileStream = File.OpenRead(imageFilePath);
using var sourceImage = await Image.LoadAsync<L8>(Configuration.Default, imageFileStream);
using var grayscaleImage = sourceImage.Clone(context => context.Grayscale());
var intensities = GetImagePixelIntensities(grayscaleImage);

var (imageEntropy, imageInformation) = await GetInformationAsync(intensities, Path.Combine(destinationFolder,
    $"{Path.GetFileName(imageFilePath)}.results.tsv"));
await grayscaleImage.SaveAsJpegAsync(Path.Combine(destinationFolder, "grayscale.jpg"));

var compressionImageResults = await new[] { 2, 4, 8, 16 }
    .ToAsyncEnumerable()
    .SelectAwait(async compression =>
    {
        var compressedImageName = $"{Path.GetFileNameWithoutExtension(imageFilePath)}x{compression}";
        var resultSavePath = Path.Combine(destinationFolder, "compressed", $"{compressedImageName}.result.tsv");
        var imageSavePath = Path.Combine(destinationFolder, "compressed", $"{compressedImageName}.jpeg");

        using var compressedImage = grayscaleImage.Clone(context => context
            .Pad(grayscaleImage.Width / compression, grayscaleImage.Height / compression));

        var compressedImageIntensities = GetImagePixelIntensities(compressedImage);
        var (entropy, information) = await GetInformationAsync(compressedImageIntensities, resultSavePath);
        await compressedImage.SaveAsJpegAsync(imageSavePath);

        return (entropy, information);
    })
    .ToArrayAsync();

static IReadOnlyCollection<byte> GetImagePixelIntensities(Image<L8> image) => image.GetPixelMemoryGroup().Single()
    .ToArray()
    .Select(pixel => pixel.PackedValue)
    .ToArray();

static async Task<(double Entropy, double Information)> GetInformationAsync<TElement>(
    IReadOnlyCollection<TElement> elements, string savePath = null)
{
    var elementGroups = elements
        .GroupBy(element => element)
        .Select(group => (group.Key, Count: group.Count(), Chance: group.Count() / (double)elements.Count))
        .ToArray();
    var entropy = elementGroups.Sum(group => group.Chance * Math.Log2(1 / group.Chance));
    var information = entropy * elements.Count;

    if (savePath is null)
        return (entropy, information);

    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
    File.Delete(savePath);
    await File.AppendAllLinesAsync(savePath, elementGroups
        .OrderBy(elementGroup => elementGroup.Key)
        .Select(elementGroup => string.Join('\t', elementGroup.Key, elementGroup.Count, elementGroup.Chance))
        .Append($"entropy: {entropy}\tinformation: {information}"), Encoding.Unicode);
    return (entropy, information);
}
