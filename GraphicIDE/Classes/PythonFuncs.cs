using System.Net;
using System.Text;
using System.Diagnostics;

using IronPython;
using IronPython.Hosting;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.CSharp.RuntimeBinder;

using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Form1;
using static GraphicIDE.Console;

namespace GraphicIDE;

public static class PythonFuncs{
    public static PythonAst ToAST() {
        StringBuilder theScript = new();
        foreach(var line in linesText) {
            theScript.AppendLine(line);
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
    public static void Execute() {
        StringBuilder theScript = new(), res = new(), errs = new();
        foreach(var line in linesText) {
            theScript.AppendLine(line);
        }

        var engine = Python.CreateEngine();
        MemoryStream ms = new();
        EventRaisingStreamWriter outputWr = new(ms);
        outputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(sWr_StringWritten!);

        MemoryStream ems = new();
        EventRaisingStreamWriter errOutputWr = new(ems);
        errOutputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(errSWr_StringWritten!);


        engine.Runtime.IO.SetOutput(ms, outputWr);
        engine.Runtime.IO.SetErrorOutput(ems, errOutputWr);
        console.Function.CurCol = -1;
        console.Function.CurLine = 0;
        try {
            var source = engine.CreateScriptSourceFromString(theScript.ToString(), SourceCodeKind.File);
            Stopwatch sw = new();
            sw.Start();
            source.Execute();
            sw.Stop();
            executedTime = $"Execute Time: { sw.ElapsedMilliseconds} ms";
            consoleTxt = (res.ToString(), ConsoleTxtType.text);
            console.txtBrush = textBrush;
            ShowConsole();
        } catch(Exception err) {
            try {
                consoleTxt = (
                    $"Error in line {((dynamic)err).Line}:\n{(err.Message.Equals("unexpected EOF while parsing") ? "unclosed bracket\\parentheses\\quote" : err.Message)}",
                    ConsoleTxtType.error
                );
            }catch(RuntimeBinderException) {
                consoleTxt = (
                    err.Message.Equals("unexpected EOF while parsing") ? "unclosed bracket\\parentheses\\quote" : err.Message,
                    ConsoleTxtType.error
                );
            }

            errLink = @"https://www.google.com/search?q=Python" + WebUtility.UrlEncode(" " + err.Message);
            console.txtBrush = redBrush;
            ShowConsole();
        }
        if(execTimeBtn!.BackgroundImage is not null){
            RefreshTimeBtn();
        }

        void sWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            res.Append(e.Value.Replace("\r\n", "\n"));
        void errSWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            errs.Append(e.Value.Replace("\r\n", "\n"));
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