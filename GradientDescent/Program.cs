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
var trainingSampleSize = int.Parse(configuration["TrainingSampleSize"]);

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
var trainingVectors = vectors.Take(trainingSampleSize).ToArray();


double GetLikehoodFunction(double[] currentTeta) => vectors
    .Sum(pair =>
    {
        var (y, vector) = pair;

        var value = Math.Pow(GetProbability(currentTeta, vector, -1), y) *
                    Math.Pow(GetProbability(currentTeta, vector, 1), 1 - y);

        var logValue = Math.Log(value);

        return logValue;
    });

double[] GetGradient(double[] currentTeta) => trainingVectors
    .Select(pair =>
    {
        var (y, vector) = pair;

        var probability = GetProbability(currentTeta, vector, -1);

        var resultVector = vector
            .Select(x => x * (y - probability))
            .ToArray();

        return resultVector;

    })
    .Aggregate((first, second) => first
        .Zip(second)
        .Select(pair => pair.First + pair.Second)
        .ToArray());

static double GetProbability(double[] currentTeta, double[] vector, int unit) =>
    1 / (1 + Math.Pow(Math.E, unit * Multiply(currentTeta, vector).Sum()));

static double[] Multiply(IEnumerable<double> first, IEnumerable<double> second) => first
    .Zip(second)
    .Select(pair => pair.First * pair.Second)
    .ToArray();
