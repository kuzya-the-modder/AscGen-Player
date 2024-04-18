using System.Drawing;
using Pastel;
using Kuzya;
using C = System.Console;
// only for windows now
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class App {
    // gradients
    // "  .,:ilwW@@";
    // ".,\"-:il4GW@";
    
    // IN -> argvs, processing, OUT -> MakeFile
    public static async Task Main(string[] argvs) {
        C.CursorVisible = false;
        string gradient;
        string saveTo;
        string[] frames;
        string[] names;
        short div;
        short delta;
        bool colorize;
        string? ttasks; // null -> no flag, else -> get content as number of tasks
        // IN LOGIC
        #region handle_args
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
        gradient = args.GetArg("gradient", "g", "gr") ?? ".,\"-:il4GW@";
        var tdiv = args.GetArg("size","s") ?? "48";
        div = Convert.ToInt16(tdiv);
        var search = path.Split("\\")[^1];
        names = Directory.GetFiles(path[0..(path.Length-search.Length)], search);
        var tdelta = args.GetArg("delta","d") ?? "50";
        delta = Convert.ToInt16(tdelta);
        var tsave = args.GetArg("save","s");
        var force = args.HasFlag("force");
        if (tsave is null || File.Exists(tsave) && !force)
        { // idk what it does
            C.WriteLine("No save param or save file already exists and can't be overriden");
            return;
        }
        saveTo = tsave;
        colorize = args.HasFlag("colorize", "color", "c");
        ttasks = args.GetArg("tasks");
        frames = new string[names.Length];
        #endregion
        // tasks logic is there
        #region async_render
        if (ttasks is not null)
        { // build threads/task
            Queue<(int, string)> namesQuery = new();
            // fill
            for (int i = 0; i < names.Length; i++) namesQuery.Enqueue((i, names[i]));  
            var tasksLen = Convert.ToByte(ttasks);
            Task[] tasks = new Task[tasksLen];
            #region init_tasks
            for (byte t = 0; t < tasksLen; t++)
            { // do one thread/task 
                C.WriteLine($"Initialized [{t}] task ");
                tasks[t] = Task.Factory.StartNew(() => {
                    while (namesQuery.Count != 0)
                    { //item : (int, string) : (pos, name) 
                        int frameId;
                        string name;
                        lock (namesQuery)
                        { // lock query to prevent rendering one frame by multiple tasks
                            (frameId, name) = namesQuery.Dequeue();
                        };
                        C.WriteLine($"Rendering {frameId}...");
                        Render(name, out var result, div, gradient, colorize);
                        frames[frameId] = result;
                    }
                });
            }
            #endregion
            // wait for all tasks to finish
            await Task.WhenAll(tasks);
            MakeFile(saveTo, in frames, delta);
            return;
        }
        #endregion
        // syncronized logic is there
        #region sync_render
        int f = 0; // id of frame current frame
        foreach (var name in names) {
            C.SetCursorPosition(0, 0);
            C.Write($"Rendering {name}");
            Render(name, out var frame, div, gradient, colorize);
            frames[f] = frame;
            f++;
            // buf = string.Empty;
            // C.WriteLine(name);
        }
        MakeFile(saveTo,in frames, delta);
        #endregion
    }
    // OUT LOGIC
    static void MakeFile(string path, in string[] frames, short delta)
    { // just making output file and writing frames to it
        var file = File.CreateText(path + ".asc");
        file.WriteLine(delta);
        foreach (var frame in frames)
        { // write to file
            file.WriteLine(frame);
        }
        file.Close();
    }
    static void Render(string name, out string result, short div, string gradient, bool colorize)
    { // not made with "using" because its fucking dumb
        Bitmap tempimg = new(name);
        var divideBy = tempimg.Width / div;
        Bitmap img = new(tempimg, new Size(tempimg.Width / divideBy * 2, tempimg.Height / divideBy));
        string buf = string.Empty;
        // 
        for (int i = 0; i < img.Height; i++)
        {
            for (int j = 0; j < img.Width; j++)
            {
                var color = img.GetPixel(j, i);
                var avg = (color.R + color.G + color.B) / 3;
                var ch = gradient[avg * gradient.Length / 255 % gradient.Length];
                buf += colorize ? ch.ToString().Pastel(color) : ch;
            }
            buf += '\n';
        }
        // DISPOSE!!!
        tempimg.Dispose();
        img.Dispose();
        result = buf;
    }
}