using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Text;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace GraphicIDE {

    public partial class Form1: Form {
        List<Bitmap> lines = new();
        List<string> linesText = new();
        int curLine = 0, curCol = -1;
        Bitmap screen;
        Brush brush = new SolidBrush(Color.FromArgb(255 - 94, 94, 212, 240));
        SolidBrush textBrush = new SolidBrush(Color.FromArgb(255 - 94, 255, 255, 255));
        SolidBrush selectBrush = new SolidBrush(Color.FromArgb(100, 0, 100, 255));
        Pen pen = new Pen(Color.FromArgb(255 - 94, 93, 162, 240), 2);
        int LINE_HEIGHT = 50;
        TextBox textBox = new TextBox();
        ScriptEngine engine = Python.CreateEngine();
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

            Controls.Add(textBox);
            Text = "TextBox Example";
            textBox.TextChanged += new EventHandler(textBox_TextChanged);
            textBox.KeyDown += new KeyEventHandler(Form1_KeyDown);
            //textBox1.TextChanged += TextChanged();
            ResumeLayout(false);
            PerformLayout();
            Start();
        }

        async void Start() {
            await Task.Delay(10);
            lines.Add(MakeEmptyLine(Screen.PrimaryScreen.Bounds.Width));
            linesText.Add("");
            DrawNewScreen();
            Refresh();
        }
        private void Form1_Paint(object sender, PaintEventArgs e) {
            var graphics = e.Graphics;
            graphics.DrawImage(screen, 0, 0);
        }
        //private void Window_TextInput(object sender, TextCompositionEventArgs e)
        //{
        //    Console.WriteLine(e.Text);
        //}
        private void Execute() {

            //dynamic scope = engine.CreateScope();
            StringBuilder theScript = new(), res = new(), errs = new();
            foreach(var line in linesText) {
                theScript.AppendLine(line);
            }


            MemoryStream ms = new MemoryStream();
            EventRaisingStreamWriter outputWr = new EventRaisingStreamWriter(ms);
            outputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(sWr_StringWritten);

            MemoryStream ems = new MemoryStream();
            EventRaisingStreamWriter errOutputWr = new EventRaisingStreamWriter(ems);
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
        Keys lastPressed;
        int? lastCol = null;
        (int line, int col)? selectedLine = null;
        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            lastPressed = e.KeyCode;
            if(selectedLine is null && (Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
                selectedLine = (curLine, curCol);
            } else if(selectedLine is not null && (Control.ModifierKeys & Keys.Shift) != Keys.Shift) {
                switch(lastPressed) {
                    case Keys.CapsLock or Keys.Alt or Keys.Control:
                        break;
                    default:
                        selectedLine = null;
                        break;
                }
            }
            switch(lastPressed) {
                case Keys.CapsLock:
                    // todo display that alt is pressed \ not pressed
                    break;
                case Keys.End:
                    curCol = linesText[curLine].Length - 1;
                    break;
                case Keys.Home:
                    curCol = -1;
                    break;
                case Keys.Up:
                    curLine = Math.Max(curLine - 1, 0);
                    getClosestForCaret();
                    break;
                case Keys.Down:
                    curLine = Math.Min(curLine + 1, linesText.Count - 1);
                    getClosestForCaret();
                    break;
                case Keys.Right:
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
                case Keys.Left:
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
                default:
                    lastCol = null;
                    return;
            }
            DrawNewScreen();
            Refresh();
        }

        private void getClosestForCaret() {
            if(lastCol is not null) {
                curCol = Math.Min((int)lastCol, linesText[curLine].Length - 1);
            } else {
                lastCol = curCol;
                curCol = Math.Min(curCol, linesText[curLine].Length - 1);
            }
        }

        private Bitmap MakeEmptyLine(int? width = null) =>
            new Bitmap(width ?? this.Width, LINE_HEIGHT);
        //Font font = new Font("Arial", 16);
        Font font = new Font(FontFamily.GenericMonospace, 16);
        private void DrawNewScreen() {
            var newBitMap = new Bitmap(this.Width, LINE_HEIGHT * lines.Count);
            Graphics g = Graphics.FromImage(newBitMap);
            int end = 0;
            for(int i = 0; i < lines.Count; i++) {
                var line = lines[i];
                var lineText = linesText[i];

                if(i == curLine) {
                    var before = curCol == -1 ? "": lineText.Substring(0, curCol + 1);
                    //var after = curCol == lineText.Length ? "": lineText.Substring(curCol + 1, lineText.Length - 1 - curCol);
                    //lineText = before + '│' + after;
                    g.FillRectangle(
                        textBrush,
                        6.5f + g.MeasureString(before, font).Width,
                        end + line.Height / 3,
                        2,
                        line.Height / 2
                    );
                }
                g.DrawImage(line, 0, end);

                g.DrawString(lineText, font, textBrush, 10, end + 15);
                if(selectedLine is not null) {
                    (int line, int col) _selectedLine = ((int, int))selectedLine;
                    if((i < _selectedLine.line && i > curLine) || (i > _selectedLine.line && i < curLine)) {
                        g.FillRectangle(
                            selectBrush, 10, end,
                            g.MeasureString(lineText, font).Width, line.Height
                        );
                    } else if(i == _selectedLine.line || i == curLine) {
                        int smaller, bigger, cCol = curCol, sCol = _selectedLine.col;
                        if(i == _selectedLine.line) {
                            cCol = i == curLine ? curCol : (i > curLine ? -1 : lineText.Length-1);
                        } else {
                            sCol = i > _selectedLine.line ? -1 : lineText.Length - 1;
                        }
                        smaller = cCol < sCol ? cCol : sCol;
                        bigger = smaller == sCol ? cCol : sCol;
                        var startS = g.MeasureString(lineText.Substring(0, smaller + 1), font).Width;
                        var endS = g.MeasureString(lineText.Substring(0, bigger + 1), font).Width;
                        g.FillRectangle(selectBrush, 10 + startS, end, endS - startS, line.Height);
                    }
                }
                end += line.Height;
            }
            screen = newBitMap;
        }

        private void textBox_TextChanged(object sender, EventArgs e) {
            var txt = textBox.Text;
            string thisline = "";
            switch(lastPressed) {

                case Keys.Back:
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
                        linesText[curLine] = thisline.Substring(0, curCol) + thisline.Substring(curCol + 1);
                        curCol -= 1;
                    }
                    break;
                case Keys.Delete:
                    thisline = linesText[curLine];
                    if(curCol == thisline.Length - 1) {
                        if(curLine != linesText.Count - 1) {
                            var text = linesText[curLine+1];
                            lines.RemoveAt(curLine + 1);
                            linesText.RemoveAt(curLine + 1);
                            linesText[curLine] += text;
                        }
                    } else {
                        linesText[curLine] = thisline.Substring(0, curCol + 1) + thisline.Substring(curCol + 2);
                    }
                    break;

                case Keys.Insert:
                    // todo think of something interesting
                    break;
                case Keys.Enter:
                    var map = MakeEmptyLine();
                    lines.Add(map);
                    linesText.Add("");
                    curLine++;
                    curCol = -1;
                    break;
                case Keys.Tab:
                    Execute();
                    curCol += 1;
                    linesText[curLine] = txt.Split("\n")[curLine];
                    break;
                default:
                    //char key = (char)lastPressed;
                    //bool isCapsOn = IsKeyLocked(Keys.CapsLock);
                    //bool isShiftKeyPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
                    //if (isCapsOn == isShiftKeyPressed)
                    //{
                    //    key = char.ToLower(key);
                    //}
                    curCol += 1;
                    linesText[curLine] = txt.Split("\n")[curLine];
                    break;
            }
            DrawNewScreen();
            Refresh();

            //var line = txt.Split("\n")[curLine];
            //if (line.Length == 0)
            //{
            //    MessageBox.Show("Enter");
            //}
            //else
            //{
            //    try
            //    {
            //        MessageBox.Show(line[curCol].ToString());
            //    }
            //    catch(Exception ex)
            //    {
            //        MessageBox.Show("!!!" + txt.Split("\n").Length.ToString() + "!!"+ curLine);
            //    }
            //}
        }

    }
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
        if(StringWritten != null) {
            StringWritten(this, new MyEvtArgs<string>(txtWritten));
        }
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