﻿using UglyToad.PdfPig;

namespace TagCloudGenerator.TextReaders
{
    public class PdfReader : ITextReader
    {
        public string GetFileExtension() => ".pdf";

        Result<IEnumerable<string>> ITextReader.ReadTextFromFile(string filePath)
        {
            try
            {
                var text = new List<string>();
                using (var pdf = PdfDocument.Open(filePath))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        text = page.Text.Split(' ').ToList();
                        text.Remove("");
                    }
                    return Result<IEnumerable<string>>.Success(text);
                }
            }
            catch
            {
                return Result<IEnumerable<string>>.Failure($"Could not find file {filePath}");
            }
        }
    }
}