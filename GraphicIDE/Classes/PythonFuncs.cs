using IronPython;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;

namespace GraphicIDE;

public static class PythonFuncs{
    public static string? errLink = null;
    public static PythonAst ToAST() {
        StringBuilder theScript = new();
        if(!curFunc.name.StartsWith(".")){
            theScript.Append("def ").Append(curFunc.name).AppendLine(":");
            foreach(var line in linesText) {
                theScript.Append("    ").AppendLine(line);
            }
        } else {
            foreach(var line in linesText) {
                theScript.AppendLine(line);
            }
        }
        var engine = Python.CreateEngine();

        ScriptSource source = engine.CreateScriptSourceFromString(
            theScript.ToString(),
            SourceCodeKind.InteractiveCode
        );

        SourceUnit unit = HostingHelpers.GetSourceUnit(source);
        Parser p = Parser.CreateParser(
            new CompilerContext(unit, new PythonCompilerOptions(), ErrorSink.Default),
            new PythonOptions()
        );

        return p.ParseFile(false);
    }
    public static void ExecuteBtn(object from, EventArgs e) {
        Execute();
        textBox.Focus();
    }
    public static async void Execute() {
        Settings.Save(false);
        if(Settings.savePath is null){  return; }

        // ? to show that it's running
        console.function.displayImage = new(1, 1);
        nonStatic.Refresh();

        Func<((string txt, ConsoleTxtType typ)[], string, string)> func = () => {
            ProcessStartInfo psi = new ProcessStartInfo("cmd", $"/c python { Settings.savePath }") { 
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Stopwatch sw = new();
            sw.Start();
            Process? p = Process.Start(psi);
            sw.Stop();

            string output = p!.StandardOutput.ReadToEnd();
            string err = p!.StandardError.ReadToEnd();

            p.WaitForExit();
            var exec = $"Execute Time: { sw.ElapsedMilliseconds} ms";
            Func<string, string> MakeLink = (str) => @"https://www.google.com/search?q=Python" + WebUtility.UrlEncode(" " + str.TrimEnd().Split('\n')[^1]);
            if(!output.Equals("") && !err.Equals("")){
                return (new[]{ 
                    (output, ConsoleTxtType.text), (err, ConsoleTxtType.error)
                }, exec, MakeLink(err));
            } else if(!err.Equals("")){
                return (new[]{ (err, ConsoleTxtType.error)}, exec,  MakeLink(err));
            } else if(!output.Equals("")){
                return (new[]{ (output, ConsoleTxtType.text)}, exec, "");
            }
            return (new (string, ConsoleTxtType)[0], exec, "");
            
        };
        Task<((string txt, ConsoleTxtType typ)[] txt, string time, string link)> t = Task.Run(func);

        while(!(t.IsCanceled || t.IsCompleted || t.IsCompletedSuccessfully || t.IsFaulted)){
            await Task.Delay(100);
        }
        executedTime = t.Result.time;
        consoleTxt = t.Result.txt;
        errLink = t.Result.link;
        RefreshTimeBtn();
        ShowConsole(); 
    }
    public static string GetPythonStr(){
        StringBuilder theScript = new(), main = new();
        foreach(var func in nameToFunc.Values) {
            if(func.name.Equals(".Main")){
                foreach(var line in func.linesText) {
                    main.AppendLine(line);
                }
            } else if(func.name.Equals(".console")){
                continue;
            } else {
                bool changed = false;
                theScript.Append("def ").Append(func.name).Append(":\n");
                foreach(var line in func.linesText) {
                    if(!changed && !line.Trim().Equals("")){
                        changed = true;
                    }
                    theScript.Append("    ").AppendLine(line);
                }
                if(!changed){
                    theScript.AppendLine("    pass");
                }
            }
        }
        theScript.Append("\n\n").Append(main);
        return theScript.ToString();
    }
}

public class MyEvtArgs<T>: EventArgs {
    public T Value { get; private set; }
    public MyEvtArgs(T value) {
        this.Value = value;
    }
}
public class EventRaisingStreamWriter: StreamWriter {
    public event EventHandler<MyEvtArgs<string>> StringWritten = null!;
    public EventRaisingStreamWriter(Stream s) : base(s) { }
    private void LaunchEvent(string txtWritten) {
        // invoke just calls it. so this checks if its null and then calls it
        StringWritten?.Invoke(this, new MyEvtArgs<string>(txtWritten));
    }
    public override void Write(string? value) {
        base.Write(value);
        LaunchEvent(value!);
    }
    public override void Write(bool value) {
        base.Write(value);
        LaunchEvent(value.ToString());
    }
    // here override all writing methods...
}