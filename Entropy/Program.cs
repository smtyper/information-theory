using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

var bookText = await File.ReadAllTextAsync("source.txt");
var textEntropy = GetEntropy(bookText.ToCharArray());
var textInformation = bookText.Length * textEntropy;

await using var imageStream = File.OpenRead("source.jpg");
using var image = await Image.LoadAsync<Rgba32>(Configuration.Default, imageStream);
var pixels = image.GetPixelMemoryGroup()
    .Single()
    .ToArray();
var imageEntropy = GetEntropy(pixels);
var imageInformation = pixels.Length * imageEntropy;

Console.WriteLine();

static double GetEntropy<T>(IReadOnlyCollection<T> elements) => elements
    .GroupBy(element => element)
    .Select(group => group.Count() / (double)elements.Count)
    .Sum(frequency => frequency * Math.Log2(1 / frequency));
