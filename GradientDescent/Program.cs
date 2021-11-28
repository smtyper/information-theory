using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var dbFilePath = configuration["DbFile"];
var vectors = (await File.ReadAllLinesAsync(dbFilePath))
    .Skip(2)
    .Select(line =>
    {
        var splittedLine = line.Split('\t');

        var y = double.Parse(splittedLine[0], CultureInfo.InvariantCulture);
        var vector = splittedLine[1..]
            .Select(numberString => double.Parse(numberString, CultureInfo.InvariantCulture))
            .Prepend(1)
            .ToArray();

        return (y, vector);
    })
    .ToArray();
