﻿using System.Drawing;
using System.Reflection;
using TagsCloudVisualization;
using System.Drawing.Imaging;
using TagCloudGenerator.TextReaders;
using TagCloudGenerator.TextProcessors;

namespace TagCloudGenerator
{
    public class TagCloudDrawer
    {
        private ITextProcessor[] textProcessors;
        private ITextReader[] textReaders;
        private WordCounter wordCounter;

        public TagCloudDrawer(WordCounter wordCounter, IEnumerable<ITextProcessor> textProcessors, IEnumerable<ITextReader> textReaders)
        {
            this.textProcessors = textProcessors.ToArray();
            this.textReaders = textReaders.ToArray();
            this.wordCounter = wordCounter;
        }

        public Result<Bitmap> DrawWordsCloud(string filePath, VisualizingSettings visualizingSettings)
        {
            if (filePath == null)
                return new Result<Bitmap>(null, "There is no path to the file");

            var words = new List<string>();
            var extension = Path.GetExtension(filePath);
            var uncorrectedExtension = true;

            foreach (var textReader in textReaders)
            {
                if (extension == textReader.GetFileExtension())
                {
                    uncorrectedExtension = false;
                    var text = textReader.ReadTextFromFile(filePath);
                    if (text.IsSuccess)
                    {
                        words = textReader.ReadTextFromFile(filePath).Value.ToList();
                        break;
                    }
                    else
                        return new Result<Bitmap>(null, text.Error);
                }
            }

            if (uncorrectedExtension)
                return new Result<Bitmap>(null, string.Format("The file is empty or contains an unsuitable format for reading - {0}", extension));

            if (words.Count == 0 && !uncorrectedExtension)
                return new Result<Bitmap>(null, string.Format("The file is empty"));


            foreach (var processor in textProcessors)
                words = processor.ProcessText(words).Value.ToList();

            var wordsWithCount = wordCounter.CountWords(words);
            ImageScaler imageScaler = new ImageScaler(wordsWithCount);
            var rectangles = GetRectanglesToDraw(wordsWithCount, visualizingSettings);

            var smallestSizeOfRectangles = imageScaler.GetMinPoints(rectangles);
            var unscaledImageSize = imageScaler.GetImageSizeWithRealSizeRectangles(rectangles, smallestSizeOfRectangles);

            if (imageScaler.NeedScale(visualizingSettings, unscaledImageSize))
            {
                var bitmap = imageScaler.DrawScaleCloud(visualizingSettings, rectangles, unscaledImageSize, smallestSizeOfRectangles);

                if (bitmap == null)
                    return new Result<Bitmap>(null, "Failed to draw an image");

                Console.WriteLine("The tag cloud is drawn");
                return new Result<Bitmap>(bitmap, null);
            }

            var image = Draw(wordsWithCount, visualizingSettings, rectangles);
            if (image == null)
                return new Result<Bitmap>(null, "Failed to draw an image");

            return new Result<Bitmap>(Draw(wordsWithCount, visualizingSettings, rectangles), null);
        }

        public Result<bool> SaveImage(Bitmap bitmap, VisualizingSettings visualizingSettings)
        {
            if (bitmap == null)
                return new Result<bool>(false, "Bitmap is null");

            var extension = Path.GetExtension(visualizingSettings.ImageName);
            var format = GetImageFormat(extension);

            if (!format.IsSuccess)
            {
                Console.WriteLine(format.Error);
                return new Result<bool>(false, "Uncorrected Image Format");
            }

            bitmap.Save(visualizingSettings.ImageName, format.Value);
            Console.WriteLine($"The image is saved, the path to the image: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)}");
            return new Result<bool>(true, null);
        }

        private Result<ImageFormat> GetImageFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrEmpty(extension))
                return new Result<ImageFormat>(null, string.Format("Unable to determine file extension for fileName: {0}", fileName));

            switch (extension.ToLower())
            {
                case @".bmp":
                    return new Result<ImageFormat>(ImageFormat.Bmp, null);

                case @".gif":
                    return new Result<ImageFormat>(ImageFormat.Gif, null);

                case @".ico":
                    return new Result<ImageFormat>(ImageFormat.Icon, null);

                case @".jpg":
                case @".jpeg":
                    return new Result<ImageFormat>(ImageFormat.Jpeg, null);

                case @".png":
                    return new Result<ImageFormat>(ImageFormat.Png, null);

                case @".tif":
                case @".tiff":
                    return new Result<ImageFormat>(ImageFormat.Tiff, null);

                case @".wmf":
                    return new Result<ImageFormat>(ImageFormat.Wmf, null);

                default:
                    return new Result<ImageFormat>(null, string.Format("Unable to determine file extension for fileName: {0}", fileName));
            }
        }

        private Bitmap Draw(Dictionary<string, int> tags, VisualizingSettings settings, RectangleF[] rectangles)
        {
            var bitmap = new Bitmap(settings.ImageSize.Width, settings.ImageSize.Height);
            using var brush = new SolidBrush(settings.PenColor);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(settings.BackgroundColor);

            for (var i = 0; i < rectangles.Length; i++)
                foreach (var tag in tags)
                {
                    var rectangle = rectangles[i];
                    var font = new Font(settings.Font, 24 + (tag.Value * 6));
                    graphics.DrawString(tag.Key, font, brush, rectangle.X, rectangle.Y);
                    i++;
                }

            Console.WriteLine("The tag cloud is drawn");

            return bitmap;
        }

        private RectangleF[] GetRectanglesToDraw(Dictionary<string, int> text, VisualizingSettings settings)
        {

            using var bitmap = new Bitmap(settings.ImageSize.Width, settings.ImageSize.Height);
            using var graphics = Graphics.FromImage(bitmap);
            var layouter = new CircularCloudLayouter(settings.PointDistributor);
            var rectangles = new List<RectangleF>();
            foreach (var line in text)
            {
                using var font = new Font(settings.Font, 24 + (line.Value * 6));
                SizeF size = graphics.MeasureString(line.Key, font);
                var rectangle = layouter.PutNextRectangle(size.ToSize());

                rectangles.Add(rectangle);
            }

            return rectangles.ToArray();
        }
    }
}