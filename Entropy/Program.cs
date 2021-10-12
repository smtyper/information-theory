using System;
using System.IO;
using System.Linq;

var bookText = await File.ReadAllTextAsync("source.txt");
var textEntropy = bookText
    .GroupBy(chr => chr)
    .Select(group => group.Count() / (double)bookText.Length)
    .Sum(frequency => frequency * Math.Log2(1 / frequency));

Console.WriteLine(textEntropy);
