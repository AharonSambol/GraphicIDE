namespace GraphicIDE;

public static class History {
    // todo each list should only have one cursorPos, selectedLine
    private const int historyAmount = 100;
    private static Stack<Change> 
        history = new(historyAmount),
        future = new(historyAmount); 
    
    public static void CtrlY(){
        if(future.IsEmpty()){  return; }
        var changes = future.Pop();
        (int, int) lastCursor = CursorPos.ToTuple();
        (int, int)? lastSelect = selectedLine;
        for (int i=0; i < changes.Count; i++) {
            var (typ, line, change, cursor, select) = changes[i];
            changes[i] = changes[i] with { cursor = lastCursor, select = lastSelect };
            lastCursor = cursor;
            lastSelect = select;
            switch (typ){
                case ChangeType.del:
                    linesText.RemoveAt(line);
                    break;
                case ChangeType.add:
                    linesText.Insert(line, change);
                    break;
                case ChangeType.change:
                    changes[i] = changes[i] with { change = linesText[line] };
                    linesText[line] = change;
                    break;
            }
        }
        changes.Reverse();
        history.Push(changes);
        CursorPos.ChangeBoth(lastCursor);
        selectedLine = lastSelect;
        nonStatic.Invalidate();
    }
    public static void CtrlZ(){
        if(history.IsEmpty()){  return; }
        var changes = history.Pop();
        (int, int) lastCursor = CursorPos.ToTuple();
        (int, int)? lastSelect = selectedLine;
        for (int i=0; i < changes.Count; i++) {
            var (typ, line, change, cursor, select) = changes[i];
            changes[i] = changes[i] with { cursor = lastCursor, select = lastSelect };
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
                    changes[i] = changes[i] with { change = linesText[line] };
                    linesText[line] = change;
                    break;
            }
        }
        changes.Reverse();
        future.Push(changes);
        CursorPos.ChangeBoth(lastCursor);
        selectedLine = lastSelect;
        nonStatic.Invalidate();
    }
    public static void AddChange(List<Change> val){
        history.Push(val);
        future.Empty();
        unsavedButton!.Show();
        Prediction.Predict();
    }
    public static void AddChange(Change val) => AddChange(new List<Change>(){ val });
}
public record struct Change(
    ChangeType typ, int line, string change, 
    (int line, int col) cursor, (int line, int col)? select
);
public enum ChangeType { del, add, change }
class Stack<T>{
    private List<T>?[] stack;
    private int pos, size;

    public Stack(int size){
        stack = new List<T>?[size];
        pos = size - 1;
        this.size = size;
    }
    public void Push(List<T> val){
        pos = (pos+1) % size;
        stack[pos] = val;
    }
    public List<T> Pop(){
        if(IsEmpty()){  throw new Exception(); }
        var val = stack[pos];
        stack[pos] = null;
        pos = pos == 0 ? size - 1 : pos - 1;
        return val!;
    }
    public List<T> Peek(){
        if(IsEmpty()){  throw new Exception(); }
        var val = stack[pos];
        return val!;
    }
    public bool IsEmpty() => stack[pos] is null;
    public void Empty() => stack[pos] = null;
}