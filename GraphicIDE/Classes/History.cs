using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyImages;
using static GraphicIDE.Start;

namespace GraphicIDE;

public static class History {
    #region Stack
    private const int historyAmount = 50;
    private static List<Change>?[] history = new List<Change>?[historyAmount];
    private static int pos = historyAmount-1;
    private static void Push(List<Change> val){
        pos = (pos+1) % historyAmount;
        history[pos] = val;
    }
    private static List<Change> Pop(){
        if(IsEmpty()){  throw new Exception(); }
        var val = history[pos];
        history[pos] = null;
        pos = pos == 0 ? historyAmount - 1 : pos - 1;
        return val!;
    }
    private static List<Change> Peek(){
        if(IsEmpty()){  throw new Exception(); }
        var val = history[pos];
        return val!;
    }
    private static bool IsEmpty(){
        return history[pos] is null;
    }

    #endregion

    public static void CtrlZ(){
        // todo pop into ctrl y
        if(IsEmpty()){  return; }
        var changes = Pop();
        (int, int) lastCursor = CursorPos.ToTuple();
        (int, int)? lastSelect = selectedLine;
        LinkedList<int> linesRemoved = new();
        foreach(var (typ, line, change, cursor, select) in changes) {
            lastCursor = cursor;
            lastSelect = select;
            switch (typ){
                case ChangeType.del:
                    linesText.Insert(line, change);
                    break;
                case ChangeType.add:
                    linesText.RemoveAt(line);
                    break;
                case ChangeType.change:
                    linesText[line] = change;
                    break;
            }
        }
        CursorPos.ChangeBoth(lastCursor);
        selectedLine = lastSelect;
        nonStatic.Invalidate();
    }
    public static void AddChange(List<Change> val){
        Push(val);
    }
    public static void AddChange(Change val){
        Push(new(){ val });
    }
}
public record class Change(
    ChangeType typ, int line, string change, 
    (int line, int col) cursor, (int line, int col)? select);
public enum ChangeType { del, add, change }