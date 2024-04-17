using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using Pastel;
using Kuzya;
using C = System.Console;
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class App {
    // "  .,:ilwW@@";
    // ".,\"-:il4GW@";
    static Queue<(int,string)> namesQuery;
    public static async Task Main(string[] argvs) {
        C.CursorVisible = false;
        var args = Args.Parse(argvs);
        // foreach (var kv in args) C.WriteLine(kv);
        if (args.HasFlag("help", "h")) {
            // Help
            C.WriteLine("{help_article}");
            return;
        }
        if (args.HasFlag("version", "v")) {
            C.WriteLine("AsciiGenerator (by KuzyaTheModder) 1.0v");
            return;
        }
        var path = args.GetArg("path", "p");
        if (path is null) {
            C.WriteLine("no path"+"\n"+"Press any key to exit...");
            C.Read();
            return;
        }
        var gradient = args.GetArg("gradient", "g", "gr") ?? ".,\"-:il4GW@";
        var tdiv = args.GetArg("size","s") ?? "48";
        var div = Convert.ToInt16(tdiv);
        var search = path.Split("\\")[^1];
        var names = Directory.GetFiles(path[0..(path.Length-search.Length)], search);
        var tdelta = args.GetArg("delta","d") ?? "50";
        var delta = Convert.ToInt16(tdelta);
        var save = args.GetArg("save","s");
        var force = args.HasFlag("force");
        if (save is null && File.Exists(save) && !force) {
            C.WriteLine("No save param or save file already exists and can't be overriden");
            return;
        }
        var colorize = args.HasFlag("colorize", "color", "c");
        var ttasks = args.GetArg("tasks");
        string[] frames = new string[names.Length];
        if (ttasks is not null)
        { // build threads/task
            namesQuery = new();
            // fill
            for (int i = 0; i < names.Length; i++) namesQuery.Enqueue((i, names[i]));  
            var tasksLen = Convert.ToByte(ttasks);
            Task[] tasks = new Task[tasksLen];
            for (byte t = 0; t < tasksLen; t++)
            { // do one thread/task 
                C.WriteLine($"Initialized [{t}] task ");
                tasks[t] = Task.Factory.StartNew(() => {
                    while (namesQuery.Count != 0)
                    { //item : (int, string) : (pos, name) 
                        int frameId;
                        string name;
                        lock (namesQuery)
                        {
                            (frameId, name) = namesQuery.Dequeue();
                        };
                        C.WriteLine($"Rendering {frameId}...");
                        frames[frameId] = Render(name);
                    }
                });
            }
            await Task.WhenAll(tasks);
            MakeFile();
            return;
        }
        int f = 0;
        string Render(string name) {
            Bitmap tempimg = new(name);
            var divideBy = tempimg.Width / div;
            Bitmap img = new(tempimg, new Size(tempimg.Width / divideBy * 2, tempimg.Height / divideBy));
            // using Bitmap img = new(name);
            string buf = string.Empty;
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    var pixel = img.GetPixel(j, i);
                    var avg = (pixel.R + pixel.G + pixel.B) / 3;
                    var ch = gradient[avg * gradient.Length / 255 % gradient.Length];
                    buf += colorize ? ch.ToString().Pastel(Color.FromArgb(pixel.R, pixel.G, pixel.B)) : ch;
                    // buf += gradient[avg * gradient.Length / 255 % gradient.Length];
                    // buf += ' ';
                }
                buf += '\n';
                // buf += '\n';
            }
            tempimg.Dispose();
            img.Dispose();
            return buf;
        }
        foreach (var name in names) {
            C.SetCursorPosition(0, 0);
            C.Write($"Rendering {name}");
            frames[f] = Render(name);
            f++;
            // buf = string.Empty;
            // C.WriteLine(name);
        }
        MakeFile();
        void MakeFile() {
            var file = File.CreateText(save + ".asc");
            file.WriteLine(delta);
            foreach (var frame in frames)
            { // write to file
                file.WriteLine(frame);
            }
            file.Close();
        }
    }
}