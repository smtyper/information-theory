using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var textPath = configuration["TextPath"];
var imagePath = configuration["ImagePath"];


var bookText = await File.ReadAllTextAsync(textPath);
var textEntropy = GetEntropy(bookText.ToCharArray());
var textInformation = bookText.Length * textEntropy;

await using var imageFileStream = File.OpenRead(imagePath);

using var sourceImage = await Image.LoadAsync<L8>(Configuration.Default, imageFileStream);
using var grayscaleImage = sourceImage.Clone(context => context.Grayscale());

var pixels = GetImagePixels(grayscaleImage);
var imageEntropy = GetEntropy(pixels);
var imageInformation = pixels.Count * imageEntropy;

Console.WriteLine($"{nameof(textEntropy)}: {textEntropy}\t{nameof(imageEntropy)}: {imageEntropy}");
Console.WriteLine($"{nameof(textInformation)}: {textInformation}\t{nameof(imageInformation)}: {imageInformation}");

foreach (var compression in new[] { 2, 4, 8 })
{
    using var compressedImage = grayscaleImage.Clone(context =>
        context.Pad(grayscaleImage.Width / compression, grayscaleImage.Height / compression));
    var compressedImagePixels = GetImagePixels(compressedImage);

    var compressedImageEntropy = GetEntropy(compressedImagePixels);
    var compressedImageInformation = compressedImageEntropy * compressedImagePixels.Count;

    Console.Write($"compression: 1 / {compression}\t");
    Console.Write($"entropy:{compressedImageEntropy}\t");
    Console.Write($"information: {compressedImageInformation}\t");
    Console.Write($"comparison: {imageInformation / compressedImageInformation}\n");
}

Console.WriteLine();

static IReadOnlyCollection<TPixel> GetImagePixels<TPixel>(Image<TPixel> image)
    where TPixel : unmanaged, IPixel<TPixel> => image.GetPixelMemoryGroup()
    .Single()
    .ToArray();

static double GetEntropy<TElement>(IReadOnlyCollection<TElement> elements) => elements
    .GroupBy(element => element)
    .Select(group => group.Count() / (double)elements.Count)
    .Sum(frequency => frequency * Math.Log2(1 / frequency));
