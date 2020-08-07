using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GCLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location); ;
            string[] folderFilePaths = Directory.GetFiles(folderPath);

            List<string> filePathsTrimmed = new List<string>();
            var bannedFiles = new List<string>() { "TempChatSessionFile.log" };
            foreach (var filePath in folderFilePaths)
            {
                if (System.IO.Path.GetExtension(filePath) != ".log") continue;
                if (bannedFiles.Exists(bf => bf == Path.GetFileName(filePath))) continue;
                if (filePath.Contains("Current.log")) continue;
                try
                {
                    using (StreamReader r = new StreamReader(filePath))
                    {
                        string firstLine = r.ReadLine();
                        if (string.IsNullOrEmpty(firstLine)) continue;
                        if (!firstLine.Contains(" - Log: Log file open,"))
                        {
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    continue;
                }

                filePathsTrimmed.Add(filePath);
            }

            if (filePathsTrimmed.Count == 0)
            {
                Console.WriteLine("No log files found.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"Getting average DevGarbage MS for {filePathsTrimmed.Count} files.\n");

            ConcurrentBag<(string, List<float>)> results = new ConcurrentBag<(string, List<float>)>();

            Parallel.ForEach(filePathsTrimmed, (fileName) =>
            {
                results.Add((fileName, GetAverageGCTime(fileName)));
            });

            var resultsList = results.ToList();
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            var orderedList = resultsList.OrderBy(ele => ele.Item1);

            List<string> linesToWrite = new List<string>();

            foreach (var line in orderedList)
            {
                string printedLine = ResultToString(line);
                linesToWrite.Add(printedLine);
                Console.WriteLine(printedLine);
            }

            File.WriteAllLines(folderPath + "\\" + dateNow + ".result",linesToWrite);

            Console.WriteLine($"\nFinished & wrote result to {dateNow}.result\nquick&dirty gc tool by rooq");
            Console.ReadLine();
        }

        private static string ResultToString((string, List<float>) result)
        {
            if (result.Item1 == null || result.Item2 == null) return "";
            string fileName = System.IO.Path.GetFileName(result.Item1);
            if (result.Item2.Count <= 3)
            {
                return $"Result ({result.Item2.Count} measures) '{fileName}'\n    Too little (<=3) or no DevGarbage information.\n";
            }
            float min = result.Item2.Min();
            float avg = result.Item2.Average();
            float max = result.Item2.Max();

            StringBuilder sb = new StringBuilder();
            sb.Append($"Result ({result.Item2.Count} measures) '{fileName}'\n");
            sb.Append($"    Min: {min} ms\n");
            sb.Append($"    Avg: {avg} ms\n");
            sb.Append($"    Max: {max} ms\n");

            return sb.ToString();
        }

        private static List<float> GetAverageGCTime(string path)
        {
            List<float> measures = new List<float>();
            using (StreamReader r = new StreamReader(path))
            {
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine();
                    if (!line.Contains("ms for realtime GC")) continue;
                    float ms = float.Parse(line.Split(' ')[3]);
                    measures.Add(ms);
                }
            }
            return measures;
        }
    }
}
