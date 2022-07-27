using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Text;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Runtime.InteropServices;
using System;

namespace GraphicIDE;
public partial class Form1: Form {
    private readonly List<Bitmap> lines = new();
    private readonly List<Button> linesButton = new();
    private readonly List<string> linesText = new();
    private int curLine = 0, curCol = -1;
    private Bitmap screen;
    private static readonly SolidBrush 
        /*brush = new(Color.FromArgb(255 - 94, 94, 212, 240)),*/
        textBrush = new(Color.FromArgb(255 - 94, 255, 255, 255)),
        selectBrush = new(Color.FromArgb(100, 0, 100, 255));
    /*private static readonly Pen pen = new(Color.FromArgb(255 - 94, 93, 162, 240), 2);*/
    private const int LINE_HEIGHT = 30, LINE_START = 4;
    private readonly TextBox textBox = new();
    /*private readonly ScriptEngine engine = Python.CreateEngine();*/
    private static readonly StringFormat stringFormat = new();
    private Keys lastPressed;
    private int? lastCol = null;
    private (int line, int col)? selectedLine = null;
    private static readonly Font font = new(FontFamily.GenericMonospace, 15);
    private static bool isMouseDown = false;
    private static bool iChanged = false;
    public Form1() {
        InitializeComponent();
        this.WindowState = FormWindowState.Maximized;
        this.screen = new Bitmap(this.Width, this.Height);
        this.BackColor = Color.Black;
        this.DoubleBuffered = true;

        textBox.AcceptsReturn = true;
        textBox.AcceptsTab = true;
        textBox.Dock = DockStyle.None;
        textBox.Size = new Size(0, 0);
        textBox.Multiline = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.Text = "()";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        textBox.Focus();
        SetTabWidth(textBox, 4);

        Controls.Add(textBox);
        Text = "TextBox Example";
        textBox.TextChanged += new EventHandler(textBox_TextChanged!);
        textBox.KeyDown += new KeyEventHandler(Form1_KeyDown!);

        stringFormat.SetTabStops(0, new float[] { 4 });
        ResumeLayout(false);
        PerformLayout();
        Start();
    }
    #region setTabSize
    private const int EM_SETTABSTOPS = 0x00CB;
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);
    public static void SetTabWidth(TextBox textbox, int tabWidth) {
        Graphics graphics = textbox.CreateGraphics();
        var characterWidth = (int)graphics.MeasureString("M", textbox.Font).Width;
        SendMessage(textbox.Handle, EM_SETTABSTOPS, 1, new int[] { tabWidth * characterWidth });
    }
    #endregion
    async void Start() {
        await Task.Delay(10);
        lines.Add(MakeEmptyLine(Screen.PrimaryScreen.Bounds.Width));
        linesText.Add("");
        DrawNewScreen();
        Refresh();
    }
    private void Form1_Paint(object sender, PaintEventArgs e) => e.Graphics.DrawImage(screen, 0, 0);
    private void Execute() {
        StringBuilder theScript = new(), res = new(), errs = new();
        foreach(var line in linesText) {
            theScript.AppendLine(line);
        }
        MemoryStream ms = new();
        EventRaisingStreamWriter outputWr = new(ms);
        outputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(sWr_StringWritten);

        MemoryStream ems = new();
        EventRaisingStreamWriter errOutputWr = new(ems);
        errOutputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(errSWr_StringWritten);

        var engine = Python.CreateEngine();
        engine.Runtime.IO.SetOutput(ms, outputWr);
        engine.Runtime.IO.SetErrorOutput(ems, errOutputWr);
        try {
            engine.CreateScriptSourceFromString(theScript.ToString()).Execute();
            MessageBox.Show(res.ToString());
        } catch(Exception err) {
            MessageBox.Show(err.Message);
        }

        void sWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            res.AppendLine(e.Value);
        void errSWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            errs.AppendLine(e.Value);
    }
    private void GetClosestForCaret() {
        if(lastCol is not null) {
            curCol = Math.Min((int)lastCol, linesText[curLine].Length - 1);
        } else {
            lastCol = curCol;
            curCol = Math.Min(curCol, linesText[curLine].Length - 1);
        }
    }
    private Bitmap MakeEmptyLine(int? width = null) => new(width ?? this.Width, LINE_HEIGHT);
    private void BtnClick(int i) {
        /*if(!isMouseDown) { return; }*/
            
        curLine = i;
        curCol = BinarySearch(linesText[curLine].Length, Cursor.Position.X);
        textBox.Focus();
        DrawNewScreen();
        Refresh();
    }
    /*async void DragMouse() {
        var prevPos = Cursor.Position;

        var curButtom = 0;  //find
        var curIndex = 0;   //find
        while(isMouseDown) {
            var curMousePos = Cursor.Position;
            if(curMousePos.X != prevPos.X) {
            }
            if(curMousePos.Y != prevPos.Y) {
            }

            prevPos = curMousePos;
            await Task.Delay(2);
        }
    }*/
    private float GetDist(Graphics g, int i) {
        return LINE_START + MeasureWidth(g, linesText[curLine].AsSpan(0, i + 1), font);
    }
    readonly Graphics plainGraphics = Graphics.FromImage(new Bitmap(1,1));
    public int BinarySearch(int len, float item) {
        if(len == 0) { return -1; }
        int first = 0, mid;
        int last = len - 1;
        do {
            mid = first + (last - first) / 2;
            var pos = GetDist(plainGraphics, mid);
            if(item > pos)  {   first = mid + 1;    } 
            else            {   last = mid - 1;     }
            if(pos == item) {   return mid;         }
        } while(first <= last);

        var cur = Abs(item - GetDist(plainGraphics, mid));
        if(mid > -1) {
            if(Abs(item - GetDist(plainGraphics, mid - 1)) < cur) {
                return mid - 1;
            }
        }
        if(mid < len - 1) {
            if(Abs(item - GetDist(plainGraphics, mid + 1)) < cur) {
                return mid + 1;
            }
        }
        return mid;
    }
    private void DrawNewScreen() {
        foreach(var b in linesButton) {
            this.Controls.Remove(b);
        }
        linesButton.Clear();
            
        var newBitMap = new Bitmap(this.Width, LINE_HEIGHT * lines.Count);
        Graphics g = Graphics.FromImage(newBitMap);
        int end = 0;
        for(int i = 0; i < lines.Count; i++) {
            var line = lines[i];
            var lineText = linesText[i];

            if(i == curLine) {
                var before = curCol == -1 ? "": lineText.AsSpan(0, curCol + 1);
                g.FillRectangle(
                    textBrush,
                    LINE_START + MeasureWidth(g, before, font),
                    end + line.Height / 3,
                    2, line.Height / 2
                );
            }
            g.DrawImage(line, 0, end);
            g.DrawString(lineText.Replace("\t", "    "), font, textBrush, LINE_HEIGHT / 4, end + LINE_HEIGHT / 4);
                
            NewButton(end, i, line); // not efficient

            if(selectedLine is not null) {
                (int line, int col) _selectedLine = ((int, int))selectedLine;
                if((i < _selectedLine.line && i > curLine) || (i > _selectedLine.line && i < curLine)) {
                    g.FillRectangle(
                        selectBrush, LINE_START, end,
                        MeasureWidth(g, lineText, font), line.Height
                    );
                } else if(i == _selectedLine.line || i == curLine) {
                    int cCol = curCol, sCol = _selectedLine.col;
                    if(i == _selectedLine.line) {
                        cCol = i == curLine ? curCol : (i > curLine ? -1 : lineText.Length - 1);
                    } else {
                        sCol = i > _selectedLine.line ? -1 : lineText.Length - 1;
                    }
                    var (smaller, bigger) = cCol < sCol ? (cCol, sCol) : (sCol, cCol);
                    var startS = MeasureWidth(g, lineText.AsSpan(0, smaller + 1), font);
                    var endS = MeasureWidth(g, lineText.AsSpan(0, bigger + 1), font);
                    g.FillRectangle(selectBrush, LINE_START + startS, end, endS - startS, line.Height);
                }
            }
            end += line.Height;
        }
        screen = newBitMap;
    }
    private void NewButton(int end, int i, Bitmap line) {
        Button b = new() {
            Location = new Point(0, end),
            Size = line.Size,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.Transparent
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.Transparent;
        b.FlatAppearance.MouseDownBackColor = Color.Transparent;
        b.FlatAppearance.BorderColor = Color.Black;
        b.MouseClick += new MouseEventHandler(((object sender, MouseEventArgs e) => BtnClick(i))!);
        this.Controls.Add(b);
    }
    private static float MeasureWidth(Graphics g, ReadOnlySpan<char> st, Font ft) {
        // used || so that trailing\leading spaces get included too
        // you might wonder why theres an "a" in here... me too... it just doesn't work without it...
        st = $"|a{st}|".Replace("\t", "    ");
        return g.MeasureString(st.ToString(), ft).Width - g.MeasureString("|", ft).Width * 2;
    }
    private void DeleteSelection() {
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(curLine == selectedLine_.line) {
            (int smaller, int bigger)
                = curCol < selectedLine_.col
                    ? (curCol, selectedLine_.col)
                    : (selectedLine_.col, curCol);
            linesText[curLine] = string.Concat(
                linesText[curLine].AsSpan(0, smaller + 1),
                linesText[curLine].AsSpan(bigger + 1)
            );
            curCol = smaller;
        } else {
            ((int line, int col) smaller, (int line, int col) bigger) 
                = curLine > selectedLine_.line 
                    ? (selectedLine_, (curLine, curCol))
                    : ((curLine, curCol), selectedLine_);
            linesText[smaller.line] = string.Concat(
                linesText[smaller.line].AsSpan(0, smaller.col + 1),
                linesText[bigger.line].AsSpan(bigger.col + 1));
            for(int i = smaller.line + 1; i <= bigger.line; i++) {
                lines.RemoveAt(smaller.line + 1);
                linesText.RemoveAt(smaller.line + 1);
            }
            (curLine, curCol) = smaller;
        }
    }
    private (int line, int col, char val)? GetNextR() {
        if(curCol != linesText[curLine].Length - 1) {
            return (curLine, curCol + 1, linesText[curLine][curCol + 1]);
        }
        if(curLine == linesText.Count - 1) {
            return null;
        }
        return (curLine + 1, -1, '\n');
    }
    private (int line, int col, char val)? GetNextL() {
        if(curCol != - 1) {
            return (curLine, curCol - 1, linesText[curLine][curCol]);
        }
        if(curLine == 0) {
            return null;
        }
        return (curLine - 1, linesText[curLine - 1].Length - 1, '\n');
    }
    private static bool IsNumeric(char val) => val == '_' || char.IsLetter(val) || char.IsDigit(val);
    private static bool IsAltNumeric(char val) => char.IsLower(val) || char.IsDigit(val);
    /*private char GetChar((int line, int col)? ps) => linesText[ps.Value.line][ps.Value.col + 1];*/
    private void GoInDirCtrl(Func<(int line, int col, char val)?> GetNext, bool isAlt) {
        var next = GetNext();
        char? cur = next?.val;
        if(cur is null) { } 
        else if(" \n\t".Contains(cur.Value)) {
            Move(() => " \n\t".Contains(next!.Value.val));
        } else if(IsNumeric(cur!.Value)) {
            if(isAlt) {
                Move(() => IsAltNumeric(next!.Value.val));
            } else {
                Move(() => IsNumeric(next!.Value.val));
            }
        } else {
            Move(() => !" \n\t".Contains(next!.Value.val) && !IsNumeric(next!.Value.val));
            while(next is not null && " \n\t".Contains(next.Value.val)) {
                (curLine, curCol, _) = next!.Value;
                next = GetNext();
            }
        }
        void Move(Func<bool> Condition) {
            do {
                (curLine, curCol, _) = next!.Value;
                next = GetNext();
            } while(
                next is not null && Condition()
            );
        }
    }
    private string GetSelectedText() {
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(curLine == selectedLine_.line) {
            (int smaller, int bigger)
                = curCol < selectedLine_.col
                    ? (curCol, selectedLine_.col)
                    : (selectedLine_.col, curCol);
            return linesText[curLine].Substring(smaller + 1, bigger - smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger)
                = curLine > selectedLine_.line
                    ? (selectedLine_, (curLine, curCol))
                    : ((curLine, curCol), selectedLine_);
            res.AppendLine(linesText[smaller.line][(smaller.col + 1)..]);
            for(int i = smaller.line + 1; i < bigger.line; i++) {
                res.AppendLine(linesText[i]);
            }
            res.Append(linesText[bigger.line].AsSpan(0, bigger.col + 1));
        }
        return res.ToString();
    }

    private void Form1_MouseDown(object sender, MouseEventArgs e) {
        isMouseDown = true;
    }
    private void Form1_MouseUp(object sender, MouseEventArgs e) {
        isMouseDown = false;
    }

    private (int, int) AddString(ReadOnlySpan<char> change, (int line, int col) pos) {
        if(change.Contains("\r\n", StringComparison.Ordinal)) { // todo but not the litteral
            var newLines = change.ToString().Split("\r\n");
            var newCol = newLines[^1].Length - 1;
            if(pos.col != linesText[pos.line].Length - 1) {
                newLines[^1] = string.Concat(newLines[^1], linesText[pos.line].AsSpan(pos.col + 1));    
            }
            linesText[pos.line] = string.Concat(linesText[pos.line].AsSpan(0, pos.col + 1), newLines[0]);
            for(int i = 1; i < newLines.Length; i++) {
                linesText.Insert(pos.line + 1, newLines[i]);
                lines.Insert(pos.line + 1, MakeEmptyLine());
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

    private void Form1_KeyDown(object sender, KeyEventArgs e) {
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        lastPressed = e.KeyCode;
        bool isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        if(selectedLine is null && isShift) {
            selectedLine = (curLine, curCol);
        }
        bool isCtrlKeyPressed = (ModifierKeys & Keys.Control) == Keys.Control;
        bool isAltlKeyPressed = (ModifierKeys & Keys.Alt) == Keys.Alt;
        switch(lastPressed) {
            case Keys.CapsLock:
                // todo display that caps is pressed \ not pressed
                break;
            case Keys.Insert:
                Execute();
                break;
            #region End
            case Keys.End:
                if(!isShift) { selectedLine = null; }
                if(isCtrlKeyPressed) { curLine = linesText.Count - 1; }
                curCol = linesText[curLine].Length - 1;
                break;
            #endregion
            #region Home
            case Keys.Home:
                if(!isShift) { selectedLine = null; }
                if(isCtrlKeyPressed) {
                    (curLine, curCol) = (0, -1);
                    break;
                }
                int spaces = linesText[curLine].Length - linesText[curLine].TrimStart().Length;
                if(curCol == spaces - 1) {
                    curCol = -1;
                } else {
                    curCol = spaces - 1;
                }
                break;
            #endregion
            #region Up
            case Keys.Up:
                if(!isShift) {
                    if(selectedLine is not null && selectedLine.Value.line < curLine) {
                        curLine = selectedLine.Value.line;
                    }
                    selectedLine = null;
                }
                if(isCtrlKeyPressed) {
                    // TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO 
                }
                if(curLine == 0) {
                    curCol = -1;
                } else {
                    curLine--;
                    GetClosestForCaret();
                }
                break;
            #endregion
            #region Down
            case Keys.Down:
                if(!isShift) {
                    if(selectedLine is not null && selectedLine.Value.line > curLine) {
                        curLine = selectedLine.Value.line;
                    }
                    selectedLine = null; 
                }
                if(isCtrlKeyPressed) {
                    // TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO 
                }
                if(curLine == linesText.Count - 1) {
                    curCol = linesText[^1].Length - 1;
                } else {
                    curLine++;
                    GetClosestForCaret();
                }
                break;
            #endregion
            #region Right
            case Keys.Right:
                if(!isShift) { 
                    if(selectedLine is not null && 
                        (selectedLine.Value.line > curLine || 
                            (selectedLine.Value.line == curLine && selectedLine.Value.col > curCol)
                        )
                    ) {
                        (curLine, curCol) = selectedLine.Value;
                        lastCol = null;
                        break;
                    }
                    selectedLine = null; 
                }
                if(isCtrlKeyPressed) {
                    GoInDirCtrl(GetNextR, isAltlKeyPressed);
                    lastCol = null;
                    break;
                }
                if(linesText[curLine].Length == curCol + 1) {
                    if(linesText.Count > curLine + 1) {
                        curLine++;
                        curCol = -1;
                    }
                } else {
                    curCol++;
                }
                lastCol = null;
                break;
            #endregion
            #region Left
            case Keys.Left:
                if(!isShift) {
                    if(selectedLine is not null &&
                        (selectedLine.Value.line < curLine ||
                            (selectedLine.Value.line == curLine && selectedLine.Value.col < curCol)
                        )
                    ) {
                        (curLine, curCol) = selectedLine.Value;
                        lastCol = null;
                        break;
                    }
                    selectedLine = null; 
                }
                if(isCtrlKeyPressed) {
                    GoInDirCtrl(GetNextL, isAltlKeyPressed);
                    lastCol = null;
                    break;
                }
                if(curCol == -1) {
                    if(curLine != 0) {
                        curLine--;
                        curCol = linesText[curLine].Length - 1;
                    }
                } else {
                    curCol--;
                }
                lastCol = null;
                break;
            #endregion
            default:
                if(isCtrlKeyPressed) {
                    string txt;
                    switch(e.KeyCode) {
                        #region Copy
                        case Keys.C:
                            if(selectedLine is null)    {  txt = linesText[curLine];    } 
                            else                        {  txt = GetSelectedText();     }
                            if(isAltlKeyPressed) {  txt = txt.Trim(); }
                            Clipboard.SetText(txt);
                            break;
                        #endregion
                        #region Cut
                        case Keys.X:
                            if(selectedLine is null) {  
                                txt = linesText[curLine];
                                lines.RemoveAt(curLine);
                                linesText.RemoveAt(curLine);
                                GetClosestForCaret();
                            } else {
                                var select = selectedLine;
                                txt = GetSelectedText();
                                selectedLine = select;
                                DeleteSelection();
                            }
                            if(isAltlKeyPressed) {  txt = txt.Trim(); }
                            Clipboard.SetText(txt);
                            break;
                        #endregion
                        #region Duplicate
                        case Keys.D:
                            if(selectedLine is null) { 
                                txt = "\r\n" + linesText[curLine];
                                if(isAltlKeyPressed) { txt = txt.Trim(); }
                                AddString(txt, (curLine, linesText[curLine].Length - 1));
                            } 
                            else {
                                var caretPos = (curLine, curCol);
                                if(selectedLine.Value.line > curLine ||
                                        (selectedLine.Value.line == curLine && selectedLine.Value.col > curCol)
                                    ){
                                    caretPos = selectedLine.Value;
                                }
                                txt = GetSelectedText();
                                if(isAltlKeyPressed) { txt = txt.Trim(); }
                                (curLine, curCol) = AddString(txt, caretPos);
                            }
                            break;
                        #endregion
                    }
                }
                lastCol = null;
                DrawNewScreen();
                Refresh();
                return;
        }
        DrawNewScreen();
        Refresh();
    }
    private void textBox_TextChanged(object sender, EventArgs e) {
        if(iChanged) {
            iChanged = false;
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

        if(selectedLine == (curLine, curCol)) {
            selectedLine = null;
        }
        string thisline;
        /*bool isCtrlKeyPressed = (ModifierKeys & Keys.Control) == Keys.Control;*/
        switch(lastPressed) {
            #region BackSpace
            case Keys.Back:
                if(selectedLine is not null) {
                    DeleteSelection();
                    break;
                }
                thisline = linesText[curLine];
                if(thisline.Length == 0) {
                    if(curLine != 0) {
                        lines.RemoveAt(curLine);
                        linesText.RemoveAt(curLine);
                        curLine--;
                        curCol = linesText[curLine].Length - 1;
                    }

                } else if(curCol == -1) {
                    if(curLine != 0) {
                        var text = linesText[curLine];
                        lines.RemoveAt(curLine);
                        linesText.RemoveAt(curLine);
                        curLine--;
                        curCol = linesText[curLine].Length - 1;
                        linesText[curLine] += text;
                    }
                } else {
                    linesText[curLine] = string.Concat(thisline.AsSpan(0, curCol), thisline.AsSpan(curCol + 1));
                    curCol -= 1;
                }
                break;
            #endregion
            #region Delete
            case Keys.Delete:
                if(selectedLine is not null) {
                    DeleteSelection();
                    break;
                }
                thisline = linesText[curLine];
                if(curCol == thisline.Length - 1) {
                    if(curLine != linesText.Count - 1) {
                        var text = linesText[curLine+1];
                        lines.RemoveAt(curLine + 1);
                        linesText.RemoveAt(curLine + 1);
                        linesText[curLine] += text;
                    }
                } else {
                    linesText[curLine] = string.Concat(thisline.AsSpan(0, curCol + 1), thisline.AsSpan(curCol + 2));
                }
                break;
            #endregion
            #region Enter
            case Keys.Enter:
                if(selectedLine is not null) {
                    DeleteSelection();
                    selectedLine = null;
                    break;
                }
                if(curCol == linesText[curLine].Length - 1) {
                    var map = MakeEmptyLine();
                    lines.Insert(curLine + 1, map);
                    linesText.Insert(curLine + 1, "");
                } else {
                    var map = MakeEmptyLine(); // todo
                    lines.Insert(curLine + 1, map);
                    linesText.Insert(curLine + 1, linesText[curLine][(curCol + 1)..]);
                    linesText[curLine] = linesText[curLine][..(curCol + 1)];
                }
                curLine++;
                curCol = -1;
                break;
            #endregion
            #region Default
            default:
                if(change == null) { throw new Exception("input is null?"); }
                if(selectedLine is not null) {
                    DeleteSelection();
                    selectedLine = null;
                }
                (curLine, curCol) = AddString(change, (curLine, curCol));
                break;
            #endregion
        }
        DrawNewScreen();
        Refresh();
    }
    private static float Abs(float num) => num > 0 ? num : -num;
}


public class MyEvtArgs<T>: EventArgs {
    public T Value {
        get;
        private set;
    }
    public MyEvtArgs(T value) {
        this.Value = value;
    }
}

public class EventRaisingStreamWriter: StreamWriter {
    #region Event
    public event EventHandler<MyEvtArgs<string>> StringWritten;
    #endregion

    #region CTOR
    public EventRaisingStreamWriter(Stream s) : base(s) { }
    #endregion

    #region Private Methods
    private void LaunchEvent(string txtWritten) {
        // invoke just calls it. so this checks if its null and then calls it
        StringWritten?.Invoke(this, new MyEvtArgs<string>(txtWritten));
    }
    #endregion

    #region Overrides

    public override void Write(string value) {
        base.Write(value);
        LaunchEvent(value);
    }
    public override void Write(bool value) {
        base.Write(value);
        LaunchEvent(value.ToString());
    }
    // here override all writing methods...

    #endregion
}