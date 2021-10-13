using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var bookText = await File.ReadAllTextAsync("source.txt");
var textEntropy = GetEntropy(bookText.ToCharArray());
var textInformation = bookText.Length * textEntropy;

await using var imageFileStream = File.OpenRead("source.jpg");

using var sourceImage = await Image.LoadAsync<L8>(Configuration.Default, imageFileStream);
using var blackWhiteImage = sourceImage.Clone(context => context.BlackWhite());

var pixels = GetImagePixels(blackWhiteImage);
var imageEntropy = GetEntropy(pixels);
var imageInformation = pixels.Count * imageEntropy;

Console.WriteLine();

static IReadOnlyCollection<TPixel> GetImagePixels<TPixel>(Image<TPixel> image)
    where TPixel : unmanaged, IPixel<TPixel> => image.GetPixelMemoryGroup()
    .Single()
    .ToArray();

static double GetEntropy<TElement>(IReadOnlyCollection<TElement> elements) => elements
    .GroupBy(element => element)
    .Select(group => group.Count() / (double)elements.Count)
    .Sum(frequency => frequency * Math.Log2(1 / frequency));
