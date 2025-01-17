﻿using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using TagCloudGenerator;
using TagCloudGenerator.TextProcessors;
using TagCloudGenerator.TextReaders;
using TagsCloudVisualization;
using TagsCloudVisualization.PointDistributors;

public class Program
{
    private static readonly string toDicPath = "../../../Dictionaries/English (American).dic";
    private static readonly string toAffPath = "../../../Dictionaries/English (American).aff";

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
                 .WithParsed(RunOptions)
                 .WithNotParsed(HandleParseError);
    }

    private static void HandleParseError(IEnumerable<Error> enumerable)
    {
        foreach (var error in enumerable)
            Console.WriteLine($"Error: {error}");
    }

    private static void RunOptions(Options o)
    {
        var drawer = InitializeDrawer();
        var visualizingSettings = new VisualizingSettings();

        AddSettings(o, visualizingSettings);
        var image = drawer.DrawWordsCloud(o.Path, visualizingSettings);
        if (image.IsSuccess)
        {
            var savingImage = drawer.SaveImage(image.Value, visualizingSettings);
            if (!savingImage.IsSuccess)
                Console.WriteLine(savingImage.Error);
        }
    }

    public static TagCloudDrawer InitializeDrawer()
    {
        var services = new ServiceCollection();
        services.AddTransient<TagCloudDrawer>();
        services.AddTransient<WordCounter>();
        services.AddTransient<ITextProcessor, WordsLowerTextProcessor>();
        services.AddTransient<ITextProcessor>(x => new BoringWordsTextProcessor(toDicPath, toAffPath));
        services.AddTransient<ITextReader, TxtReader>();
        services.AddTransient<ITextReader, PdfReader>();
        services.AddTransient<ITextReader, DocxReader>();
        var container = services.BuildServiceProvider();

        return container.GetRequiredService<TagCloudDrawer>();
    }

    private static void AddSettings(Options options, VisualizingSettings visualizingSettings)
    {
        if (options.ImageName != null)
            visualizingSettings.ImageName = options.ImageName;

        if (!options.Size.IsEmpty)
            visualizingSettings.ImageSize = options.Size;

        if (!options.ForegroundColor.IsEmpty)
            visualizingSettings.PenColor = options.ForegroundColor;

        if (!options.BackgroundColor.IsEmpty)
            visualizingSettings.BackgroundColor = options.BackgroundColor;

        if (options.Font != null)
            visualizingSettings.Font = options.Font;

        var center = new Point(visualizingSettings.ImageSize.Width / 2, visualizingSettings.ImageSize.Height / 2);
        var step = 1;
        var deltaAngle = 0.1;

        if (!options.Center.IsEmpty)
            center = options.Center;
        if (options.Step != 0)
            step = options.Step;
        if (options.DeltaAngle != 0)
            deltaAngle = options.DeltaAngle;

        visualizingSettings.PointDistributor = new Spiral(center, step, deltaAngle);
    }
}