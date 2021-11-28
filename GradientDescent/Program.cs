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

var theta = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
var l = GetLikehoodFunction(theta);
var step = 0.000005;

while (true)
{
    var gradient = GetGradient(theta);
    var oldL = l;

    theta = theta
        .Zip(gradient.Select(value => value * step).ToArray())
        .Select(pair => pair.First + pair.Second)
        .ToArray();
    l = GetLikehoodFunction(theta);

    if (l < oldL)
        break;
}

var result = vectors
    .Skip(trainingSampleSize)
    .Select(pair =>
    {
        var (y, vector) = pair;

        var probability = GetProbability(theta, vector, -1);
        var realResult = y is 1;

        return (probability, realResult);
    })
    .Where(pair => (pair.probability >= 0.5) == pair.realResult)
    .ToArray();

var successPercent = (double)result.Length / (vectors.Length - trainingSampleSize);
Console.WriteLine($@"{nameof(theta)} =
[
	{string.Join(",\n\t", theta)}
]
{nameof(l)} = {l}
{nameof(successPercent)} = {successPercent}
");

double GetLikehoodFunction(double[] currentTheta) => vectors
    .Sum(pair =>
    {
        var (y, vector) = pair;

        var value = Math.Pow(GetProbability(currentTheta, vector, -1), y) *
                    Math.Pow(GetProbability(currentTheta, vector, 1), 1 - y);

        var logValue = Math.Log(value);

        return logValue;
    });

double[] GetGradient(double[] currentTheta) => trainingVectors
    .Select(pair =>
    {
        var (y, vector) = pair;

        var probability = GetProbability(currentTheta, vector, -1);

        var resultVector = vector
            .Select(x => x * (y - probability))
            .ToArray();

        return resultVector;

    })
    .Aggregate((first, second) => first
        .Zip(second)
        .Select(pair => pair.First + pair.Second)
        .ToArray());

static double GetProbability(double[] currentTheta, double[] vector, int unit) =>
    1 / (1 + Math.Pow(Math.E, unit * Multiply(currentTheta, vector).Sum()));

static double[] Multiply(IEnumerable<double> first, IEnumerable<double> second) => first
    .Zip(second)
    .Select(pair => pair.First * pair.Second)
    .ToArray();
