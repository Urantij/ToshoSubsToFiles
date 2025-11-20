using System.Text.RegularExpressions;

namespace ToshoSubsToFiles;

internal class PathData(string path, int? num)
{
    public string Path { get; } = path;
    public int? Num { get; } = num;
}

internal class Program
{
    private static readonly Regex DigitRegex = new(@"\d+", RegexOptions.Compiled);

    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("Первый аргумент - путь до папок с сабами");
            Console.WriteLine("Второй аргумент - путь до папок с видиками");
            Console.WriteLine("Если расширение видео не mkv, добавь \"-ext расширение\"");
            return;
        }

        string videoExtension = "mkv";
        {
            int veArgIndex = Array.IndexOf(args, "-ext");
            if (veArgIndex != -1)
            {
                videoExtension = args[veArgIndex + 1];
            }
        }

        // первая меняющая цифра, самая популярная. если не меняется... 
        // 720 будет меняться, если 1 а не 01 юзается... 
        // можно просто забанить 720). а первая меняющаяся. тада норм.
        // если канеш 720 не будет перед названием серии... а. оно не будет меняться...
        
        string toshoPath = args[0];
        string videopath = args[1];

        // [Vivid] The World God Only Knows - Goddesses Arc - 01 [BD 720p AAC] [7988E011].mkv

        PathData[] vids;
        {
            string[] videos = Directory.GetFiles(videopath)
                .Where(f => Path.GetExtension(f) == $".{videoExtension}")
                .ToArray();
            vids = DigitilizePathes(videos)
                .Where(p => p.Num != null)
                .ToArray();   
        }

        PathData[] toshos;
        {
            string[] toshoTargets = Directory.GetDirectories(toshoPath);
            toshos = DigitilizePathes(toshoTargets)
                .Where(p => p.Num != null)
                .ToArray();    
        }

        string? choice = null;
        foreach (PathData target in toshos.OrderBy(p => p.Num))
        {
            // chapters.xml
            // track3.ass

            // [Winter] Kami nomi zo Shiru Sekai - Megami Hen 01 [BDrip 1280x720 x264 Vorbis].mkv

            var vid = vids.FirstOrDefault(v => v.Num == target.Num);
            if (vid == null)
            {
                Console.WriteLine($"Видева {target.Num} нет");
                continue;
            }

            string[] filesWithPathes = Directory.GetFiles(target.Path);

            string file;
            if (filesWithPathes.Any(f => Path.GetFileName(f) == choice))
            {
                file = choice!;
            }
            else
            {
                var options = filesWithPathes.Select(f => new
                    {
                        f,
                        length = new FileInfo(f).Length
                    })
                    .OrderByDescending(a => a.length)
                    .ToArray();

                for (int i = 0; i < options.Length; i++)
                {
                    var a = options[i];

                    Console.WriteLine($"[{i + 1}] {Path.GetFileName(a.f)} ({a.length})");
                }

                file = choice = Path.GetFileName(options[int.Parse(Console.ReadLine()) - 1].f);
            }

            string ext = GetAllExtensions(file) ?? "sub";

            string resultPath = Path.ChangeExtension(vid.Path, ext);

            if (File.Exists(resultPath))
            {
                Console.WriteLine($"скипаем {target.Num} уже есть");
                continue;
            }

            File.Copy(Path.Combine(target.Path, file), resultPath);

            Console.WriteLine($"Видево {target.Num} добавлено");
        }
    }

    /// <summary>
    /// Найти все екстешены пути. file.1.2.3 вернёт 1.2.3
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static string? GetAllExtensions(string path)
    {
        string a = Path.GetFileNameWithoutExtension(path);

        int l;
        do
        {
            l = a.Length;

            a = Path.GetFileNameWithoutExtension(a);
        } while (a.Length != l);

        return path.Substring(l);
    }

    static PathData[] DigitilizePathes(string[] pathes)
    {
        // Находим все числа в путях

        var pathesNumData = pathes.Select(p =>
        {
            MatchCollection matches = DigitRegex.Matches(p);

            return new
            {
                path = p,
                data = matches.Select(m => new
                {
                    m.Index,
                    value = int.Parse(m.Value)
                }).ToArray()
            };
        }).ToArray();

        // Берём все числа всех путей, берём их индекс

        var numsData = pathesNumData.SelectMany(p => p.data);

        var indexes = numsData.Select(n => n.Index).Distinct().ToArray();

        // Находим самое раннее число, которое есть во всх путях, и которое всегда меняется

        List<(int index, int popularity)> yes = [];
        foreach (int index in indexes)
        {
            int popularity = 0;

            bool repeated = false;

            HashSet<int> values = [];
            foreach (var pathNumData in pathesNumData)
            {
                var m = pathNumData.data.FirstOrDefault(d => d.Index == index);

                if (m == null)
                    continue;

                if (!values.Add(m.value))
                {
                    repeated = true;
                    break;
                }

                popularity++;
            }

            if (repeated)
                continue;

            yes.Add((index, popularity));
        }

        int winner = yes.OrderBy(y => y.index).First().index;

        return pathesNumData.Select(p =>
        {
            var data = p.data.FirstOrDefault(d => d.Index == winner);

            return new PathData(p.path, data?.value);
        }).ToArray();
    }
}