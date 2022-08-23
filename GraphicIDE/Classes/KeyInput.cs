using System.Text;

using static GraphicIDE.Form1;
using static GraphicIDE.MyMath;
using static GraphicIDE.Tabs;
using static GraphicIDE.DrawScreen;
using static GraphicIDE.Helpers;

namespace GraphicIDE;

public static class KeyInput {
    public static Keys lastPressed;

    public static bool iChanged = false, forgetThisOne = false;

    public static void Form1_KeyDown(object? sender, KeyEventArgs e) {
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        lastPressed = e.KeyCode;
        bool isShift = isShiftPressed();
        if(selectedLine is null && isShift) {
            selectedLine = (CursorPos.Line, CursorPos.Col);
        }
        bool isAltl = isAltlPressed();
        bool isCtrl = isCtrlPressed();
        // todo bool isCaps = isCapsPressed();
        var refresh = true;
        ((Action)(lastPressed switch {
            Keys.CapsLock => () => refresh = false, // todo display that caps is pressed \ not pressed
            Keys.Insert => () => DrawPicScreen(),
            Keys.End => () => EndKey(isShift, isCtrl),
            Keys.Home => () => HomeKey(isShift, isCtrl),
            Keys.Up => () => UpKey(isShift, isAltl),
            Keys.Down => () => DownKey(isShift, isAltl),
            Keys.Right => () => RightKey(isShift, isAltl, isCtrl),
            Keys.Left => () => LeftKey(isShift, isAltl, isCtrl),
            Keys.F1 => () => OpenErrLink(null, new()),
            Keys.Tab => () => Tab(),
            _ => () => {lastCol = null; refresh = false;}
        }))();

        // if(isCaps){
        //     textBox.Text = "()";
        //     MessageBox.Show("!");
        // }
        
        if(refresh){
            DrawTextScreen();
            nonStatic.Invalidate();
        }
    }
    public static void TextBox_TextChanged(object? sender, EventArgs e) {
        if(iChanged) {
            iChanged = false;
            return;
        } if(forgetThisOne){
            forgetThisOne = false;
            iChanged = true;
            textBox.Text = "()";
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 0;
            return;
        }
        ReadOnlySpan<char> change = null;
        try {
            change = textBox.Text.AsSpan(1, textBox.Text.Length - 2);
        } catch(IndexOutOfRangeException) { } catch(ArgumentOutOfRangeException) { }
        iChanged = true;
        textBox.Text = "()";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;

        if(selectedLine == (CursorPos.Line, CursorPos.Col)) {
            selectedLine = null;
        }
        var changeSt = change.ToString();
        ((Action)(lastPressed switch {
            Keys.Back => () => BackSpaceKey(),
            Keys.Delete => () => DeleteKey(),
            Keys.Enter => () => EnterKey(),
            _ => () => CharKey(changeSt)
        }))();
        DrawTextScreen();
        nonStatic.Invalidate();
    }
    public static void Tab(){
        if(isShiftPressed()){
            var selectLine = selectedLine is null ? CursorPos.Line : selectedLine.Value.line;
            var (bigger, smaller) = MaxMin(CursorPos.Line, selectLine);
            for (int i=smaller; i <= bigger; i++) {
                if(linesText[i].StartsWith("    ")){
                    linesText[i] = linesText[i][4..];
                    if(i == CursorPos.Line && CursorPos.Col != -1){
                        CursorPos.ChangeCol(Max(-1, CursorPos.Col - 4));
                    }
                    if(selectedLine is (int line, int col) sl && i == sl.line && sl.col != -1){
                        selectedLine = (sl.line, Max(-1, sl.col - 4));
                    }
                }
            }
        } else if(selectedLine is (int line, int col) sl && sl.line != CursorPos.Line){
            var (bigger, smaller) = MaxMin(CursorPos.Line, sl.line);
            for (int i=smaller; i <= bigger; i++) {
                linesText[i] = $"    { linesText[i] }";
                if(i == CursorPos.Line){
                    CursorPos.ChangeCol(CursorPos.Col + 4);
                }
                if(i == sl.line){
                    selectedLine = (sl.line, sl.col + 4);
                }
            }
        } else {
            textBox.Text = "(    )";
            textBox.SelectionStart = 5;
            textBox.SelectionLength = 0;
            //? here it will call TextBox_TextChanged
        }
        forgetThisOne = true;
    }
    public static void LeftKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null &&
                (selectedLine.Value.line < CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col < CursorPos.Col)
                )
            ) {
                CursorPos.ChangeCol(selectedLine.Value.col);
                CursorPos.ChangeLine(selectedLine.Value.line);
                lastCol = null;
                return;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            GoInDirCtrl(GetNextL, isAltlKeyPressed);
            lastCol = null;
            return;
        }
        if(CursorPos.Col == -1) {
            if(CursorPos.Line != 0) {
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
            }
        } else {
            CursorPos.ChangeCol(CursorPos.Col-1);
        }
        lastCol = null;
    }
    public static void RightKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null &&
                (selectedLine.Value.line > CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col > CursorPos.Col)
                )
            ) {
                CursorPos.ChangeLine(selectedLine.Value.line); 
                CursorPos.ChangeCol(selectedLine.Value.col);
                lastCol = null;
                return;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            GoInDirCtrl(GetNextR, isAltlKeyPressed);
            lastCol = null;
            return;
        }
        if(linesText[CursorPos.Line].Length == CursorPos.Col + 1) {
            if(linesText.Count > CursorPos.Line + 1) {
                CursorPos.ChangeLine(CursorPos.Line+1);
                CursorPos.ChangeCol(-1);
            }
        } else {
            CursorPos.ChangeCol(CursorPos.Col+1);
        }
        lastCol = null;
    }
    public static void DownKey(bool isShift, bool isAltlKeyPressed) {
        if(isAltlKeyPressed) {
            if(selectedLine is (int, int) sl) {
                var (maxLine, minLine) = MaxMin(sl.line, CursorPos.Line);
                if(maxLine == linesText.Count - 1) { return; }
                var prev = linesText[maxLine + 1];
                for(int i = maxLine; i >= minLine; i--) {
                    linesText[i + 1] = linesText[i];
                }
                linesText[minLine] = prev;
                selectedLine = (sl.line + 1, sl.col);
            } else {
                if(CursorPos.Line == linesText.Count - 1) { return; }
                (linesText[CursorPos.Line], linesText[CursorPos.Line + 1]) = (linesText[CursorPos.Line + 1], linesText[CursorPos.Line]);
            }
            CursorPos.ChangeLine(CursorPos.Line + 1);
            return;
        }
        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line > CursorPos.Line) {
                CursorPos.ChangeLine(selectedLine.Value.line);
            }
            selectedLine = null;
        }
        if(CursorPos.Line == linesText.Count - 1) {
            CursorPos.ChangeCol(linesText[^1].Length - 1);
        } else {
            CursorPos.ChangeLine(CursorPos.Line+1);
            GetClosestForCaret();
        }
    }
    public static void UpKey(bool isShift, bool isAltlKeyPressed) {
        if(isAltlKeyPressed) {
            if(selectedLine is (int, int) sl) {
                var (maxLine, minLine) = MaxMin(sl.line, CursorPos.Line);
                if(minLine == 0) { return; }
                var prev = linesText[minLine - 1];
                for(int i = minLine; i <= maxLine; i++) {
                    linesText[i - 1] = linesText[i];
                }
                linesText[maxLine] = prev;
                selectedLine = (sl.line - 1, sl.col);
            } else {
                if(CursorPos.Line == 0) {  return; }
                (linesText[CursorPos.Line], linesText[CursorPos.Line - 1]) = (linesText[CursorPos.Line - 1], linesText[CursorPos.Line]);
            }
            CursorPos.ChangeLine(CursorPos.Line - 1);
            return;
        }

        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line < CursorPos.Line) {
                CursorPos.ChangeLine(selectedLine.Value.line);
            }
            selectedLine = null;
        }

        if(CursorPos.Line == 0) {
            CursorPos.ChangeCol(-1);
        } else {
            CursorPos.ChangeLine(CursorPos.Line-1);
            GetClosestForCaret();
        }
    }
    public static void HomeKey(bool isShift, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) {
            CursorPos.ChangeLine(0);
            CursorPos.ChangeCol(-1);
        } else {
            int spaces = linesText[CursorPos.Line].Length - linesText[CursorPos.Line].TrimStart().Length;
            if(CursorPos.Col == spaces - 1) {
                CursorPos.ChangeCol(-1);
            } else {
                CursorPos.ChangeCol(spaces - 1);
            }
        }
    }
    public static void EndKey(bool isShift, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) { CursorPos.ChangeLine(linesText.Count - 1); }
        CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
    }

    public static void CharKey(ReadOnlySpan<char> change) {
        if(change == null) { throw new Exception("input is null?"); }
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        CursorPos.ChangeBoth(AddString(change, (CursorPos.Line, CursorPos.Col)));
    }
    public static void EnterKey(bool isCtrl=false) {
        if(isCtrl) {
            CursorPos.ChangeCol(-1);
            selectedLine = null;
        }
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        if(CursorPos.Col == linesText[CursorPos.Line].Length - 1) {
            linesText.Insert(CursorPos.Line + 1, "");
        } else {
            linesText.Insert(CursorPos.Line + 1, linesText[CursorPos.Line][(CursorPos.Col + 1)..]);
            linesText[CursorPos.Line] = linesText[CursorPos.Line][..(CursorPos.Col + 1)];
        }
        if(!isCtrl) {
            CursorPos.ChangeLine(CursorPos.Line+1);
            CursorPos.ChangeCol(-1);
        }
    }
    public static void DeleteKey(bool isAlt=false, bool isCtrl=false) {
        if(isCtrl) {
            if(selectedLine is not null) {
                DeleteSelection();
            }
            selectedLine = (CursorPos.Line, CursorPos.Col);
            GoInDirCtrl(GetNextR, isAlt);
        }
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[CursorPos.Line];
        if(CursorPos.Col == thisline.Length - 1) {
            if(CursorPos.Line != linesText.Count - 1) {
                var text = linesText[CursorPos.Line+1];
                linesText.RemoveAt(CursorPos.Line + 1);
                linesText[CursorPos.Line] += text;
            }
        } else {
            linesText[CursorPos.Line] = string.Concat(thisline.AsSpan(0, CursorPos.Col + 1), thisline.AsSpan(CursorPos.Col + 2));
        }
    }
    public static void BackSpaceKey(bool isAlt=false, bool isCtrl=false) {
        if(isCtrl) {
            if(selectedLine is not null) {
                DeleteSelection();
            }
            selectedLine = (CursorPos.Line, CursorPos.Col);
            GoInDirCtrl(GetNextL, isAlt);
        }
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[CursorPos.Line];
        if(thisline.Length == 0) {
            if(CursorPos.Line != 0) {
                linesText.RemoveAt(CursorPos.Line);
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
            }

        } else if(CursorPos.Col == -1) {
            if(CursorPos.Line != 0) {
                var text = linesText[CursorPos.Line];
                linesText.RemoveAt(CursorPos.Line);
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
                linesText[CursorPos.Line] += text;
            }
        } else {
            linesText[CursorPos.Line] = string.Concat(thisline.AsSpan(0, CursorPos.Col), thisline.AsSpan(CursorPos.Col + 1));
            CursorPos.ChangeCol(CursorPos.Col - 1);
        }
    }
    
    
    #region ShortCuts
    public static void CtrlTab() {
        for(int i = 0; i < windows.Count; i++) {
            var item = windows[i];
            if(item.Function.Equals(curFunc)) {
                curWindow = windows[(i + 1) % windows.Count];
                ChangeTab(curWindow.Function.Name);
                return;
            }
        }
    }
    public static void SelectAll() {
        selectedLine = (0, -1);
        CursorPos.ChangeLine(linesText.Count - 1);
        CursorPos.ChangeCol(linesText[^1].Length - 1);
    }
    public static void Duplicate(bool isAltlKeyPressed) {
        if(selectedLine is null) {
            var txt = "\r\n" + linesText[CursorPos.Line];
            AddString(txt, (CursorPos.Line, linesText[CursorPos.Line].Length - 1));
        } else {
            var caretPos = (CursorPos.Line, CursorPos.Col);
            if(selectedLine.Value.line > CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col > CursorPos.Col)
                ) {
                caretPos = selectedLine.Value;
            }
            var txt = GetSelectedLines();
            CursorPos.ChangeBoth(AddString($"\r\n{ txt }", caretPos));
        }
    }
    public static void Cut(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) {
            txt = linesText[CursorPos.Line].Trim();
            linesText.RemoveAt(CursorPos.Line);
            GetClosestForCaret();
        } else {
            var select = selectedLine;
            txt = GetSelectedText();
            selectedLine = select;
            DeleteSelection();
        }
        if(!txt.Equals("")) {
            Clipboard.SetText(txt);
        }
    }
    public static void Paste() {
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        var txt = Clipboard.GetText();
        if(CursorPos.Col > -1){
            var line = linesText[CursorPos.Line];
            var before = line.AsSpan(0, CursorPos.Col);
            var tabCount = (line.Length - line.TrimStart(' ').Length) / 4;
            if(tabCount > 0){
                var tabs = new string(' ', tabCount * 4);
                txt = String.Join($"\r\n{ tabs }", txt.Split("\n"));
            }
        }
        CursorPos.ChangeBoth(AddString(txt, (CursorPos.Line, CursorPos.Col)));
    }
    public static void Copy(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) { txt = linesText[CursorPos.Line].Trim(); } else { txt = GetSelectedText(); }
        if(!txt.Equals("")) {
            Clipboard.SetText(txt);
        }
    }
    #endregion

    public static void DeleteSelection() {
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(CursorPos.Col, selectedLine_.col);
                
            linesText[CursorPos.Line] = string.Concat(
                linesText[CursorPos.Line].AsSpan(0, smaller + 1),
                linesText[CursorPos.Line].AsSpan(bigger + 1)
            );
            CursorPos.ChangeCol(smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger) 
                = CursorPos.Line > selectedLine_.line 
                    ? (selectedLine_, (CursorPos.Line, CursorPos.Col))
                    : ((CursorPos.Line, CursorPos.Col), selectedLine_);
            linesText[smaller.line] = string.Concat(
                linesText[smaller.line].AsSpan(0, smaller.col + 1),
                linesText[bigger.line].AsSpan(bigger.col + 1));
            for(int i = smaller.line + 1; i <= bigger.line; i++) {
                linesText.RemoveAt(smaller.line + 1);
            }
            CursorPos.ChangeBoth(smaller);
        }
    }
    public static void GetClosestForCaret() {
        if(lastCol is not null) {
            CursorPos.ChangeCol(Min((int)lastCol, linesText[CursorPos.Line].Length - 1));
        } else {
            lastCol = CursorPos.Col;
            CursorPos.ChangeCol(Min(CursorPos.Col, linesText[CursorPos.Line].Length - 1));
        }
    }
    public static string GetSelectedText() {
        if(selectedLine is null) {  return ""; }
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(CursorPos.Col, selectedLine_.col);
            return linesText[CursorPos.Line].Substring(smaller + 1, bigger - smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger)
                = CursorPos.Line > selectedLine_.line
                    ? (selectedLine_, (CursorPos.Line, CursorPos.Col))
                    : ((CursorPos.Line, CursorPos.Col), selectedLine_);
            res.AppendLine(linesText[smaller.line][(smaller.col + 1)..]);
            for(int i = smaller.line + 1; i < bigger.line; i++) {
                res.AppendLine(linesText[i]);
            }
            res.Append(linesText[bigger.line].AsSpan(0, bigger.col + 1));
        }
        return res.ToString();
    }
    public static string GetSelectedLines() {
        if(selectedLine is null) {  return ""; }
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            return linesText[CursorPos.Line];
        } 
        (int bigger, int smaller) = MaxMin(CursorPos.Line, selectedLine_.line);
        for(int i = smaller; i < bigger; i++) {
            res.AppendLine(linesText[i]);
        }
        res.Append(linesText[bigger]);
        return res.ToString();
    }
    public static (int, int) AddString(ReadOnlySpan<char> change, (int line, int col) pos) {
        bool containsRN = change.Contains("\r\n", StringComparison.Ordinal);
        bool containsN = change.Contains("\n", StringComparison.Ordinal);
        if(containsRN || containsN) {
            string[] newLines;
            if(containsRN){
                newLines = change.ToString().Split("\r\n");
            } else {
                newLines = change.ToString().Split("\n");
            }
            var newCol = newLines[^1].Length - 1;
            if(pos.col != linesText[pos.line].Length - 1) {
                newLines[^1] = string.Concat(newLines[^1], linesText[pos.line].AsSpan(pos.col + 1));    
            }
            linesText[pos.line] = string.Concat(linesText[pos.line].AsSpan(0, pos.col + 1), newLines[0]);
            for(int i = 1; i < newLines.Length; i++) {
                linesText.Insert(pos.line + 1, newLines[i]);
                pos.line++;
            }
            pos.col = newCol;
        } else {
            var line = linesText[pos.line];
            var start = pos.col == -1 ? "" : line.AsSpan(0, pos.col+1);
            linesText[pos.line] = $"{start}{change}{line.AsSpan(pos.col + 1)}";
            pos.col += change.Length;
        }
        return (pos.line, pos.col);
    }
    
}