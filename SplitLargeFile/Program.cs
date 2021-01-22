using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplitLargeFile
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var inputPath = GetInputPath(args);
            var splitSize = GetSplitSize(args);
            Console.WriteLine($"split {inputPath} into files of {splitSize}B");
            Console.WriteLine();

            var outputs = GetOutputSizes(inputPath, splitSize)
                .Select((t, i) => (GetOutputPath(inputPath, i), t))
                .ToArray();

            Console.WriteLine("going to output:");
            foreach (var (outputPath, outputSize) in outputs)
            {
                Console.WriteLine($"{outputPath} {outputSize}B");
            }
            Console.WriteLine();

            Console.WriteLine("start to output:");
            using var inputStream = File.OpenRead(inputPath);
            foreach (var (outputPath, outputSize) in outputs)
            {
                Console.Write(outputPath);

                using var outputStream = File.OpenWrite(outputPath);
                await CopyToAsync(inputStream, outputStream, outputSize);

                Console.WriteLine($" {outputSize}B");
            }
            Console.WriteLine();
        }

        private static string GetInputPath(string[] arguments)
        {
            if (arguments.Length >= 1)
            {
                return arguments[0];
            }
            else
            {
                Console.Write("input path? ");
                return Console.ReadLine();
            }
        }
        private static int GetSplitSize(string[] arguments)
        {
            string text;
            if (arguments.Length >= 2)
            {
                text = arguments[1];
            }
            else
            {
                Console.Write("split size? ");
                text = Console.ReadLine();
            }

            var match = Regex.Match(text, @"(?<value>\d+)\s*(?<unit>[KkMm])?B?");
            if (!match.Success)
            {
                throw new FormatException($"cannot get split size from \"{text}\"");
            }

            var valueText = match.Groups["value"].Value;
            var valueValue = int.Parse(valueText);

            var unitGroup = match.Groups["unit"];
            if (unitGroup.Success)
            {
                return unitGroup.Value switch
                {
                    "K" => valueValue * 1024,
                    "k" => valueValue * 1000,
                    "M" => valueValue * 1024 * 1024,
                    "m" => valueValue * 1000 * 1000,
                    _ => throw new Exception(),
                };
            }
            else
            {
                return valueValue;
            }
        }

        private static IEnumerable<int> GetOutputSizes(string inputPath, int splitSize)
        {
            var fileInfo = new FileInfo(inputPath);

            var sizes = Enumerable.Repeat(splitSize, (int)(fileInfo.Length / splitSize));

            if (fileInfo.Length % splitSize > 0)
            {
                return sizes.Append((int)(fileInfo.Length - fileInfo.Length / splitSize * splitSize));
            }
            else
            {
                return sizes;
            }
        }
        private static string GetOutputPath(string path, int index)
        {
            var extension = Path.GetExtension(path);

            var body = path.Substring(0, path.Length - extension.Length);

            return $"{body}.{index++:000}{extension}";
        }

        private static async Task CopyToAsync(FileStream inputStream, FileStream outputStream, int size)
        {
            var buffer = new byte[100 * 1024 * 1024];
            var copied = 0;

            while (true)
            {
                var memory = new Memory<byte>(buffer, 0, Math.Min(size - copied, buffer.Length));
                
                var read = await inputStream.ReadAsync(memory);
                if (read <= 0)
                {
                    break;
                }

                if (read >= memory.Length)
                {
                    await outputStream.WriteAsync(memory);
                }
                else
                {
                    var slice = memory.Slice(0, read);
                    await outputStream.WriteAsync(slice);
                }

                copied += read;
            }
        }
    }
}
