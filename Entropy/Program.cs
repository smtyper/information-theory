using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var textFile = configuration["TextFile"];
var bookText = await File.ReadAllTextAsync(textFile);
var characherArray = bookText.ToCharArray();

var (textEntropy, textInformation) = await GetInformationAsync(characherArray);

var imageFile = configuration["ImageFile"];
await using var imageFileStream = File.OpenRead(imageFile);
using var sourceImage = await Image.LoadAsync<L8>(Configuration.Default, imageFileStream);
using var grayscaleImage = sourceImage.Clone(context => context.Grayscale());
await grayscaleImage.SaveAsJpegAsync("grayscale.jpg");
var intensities = GetImagePixels(grayscaleImage)
    .Select(pixel => pixel.PackedValue)
    .ToArray();

var (imageEntropy, imageInformation) = await GetInformationAsync(intensities);


static IReadOnlyCollection<TPixel> GetImagePixels<TPixel>(Image<TPixel> image)
    where TPixel : unmanaged, IPixel<TPixel> => image.GetPixelMemoryGroup()
    .Single()
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

    File.Delete(savePath);
    await File.AppendAllLinesAsync(savePath, elementGroups
        .Select(elementGroup => string.Join('\t', elementGroup.Key, elementGroup.Count, elementGroup.Chance))
        .Append($"entropy: {entropy}\tinformation: {information}"));
    return (entropy, information);
}
