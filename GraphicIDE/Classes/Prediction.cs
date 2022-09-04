namespace GraphicIDE;

/*
? ****************************************************************
? This is a mess that I copied from a different one of my projects
? For now it works but definetly could use a cleanup...
? ****************************************************************
*/

public static class Prediction {
    private readonly static HashSet<string> pyKeyWords = new(){
        "and", "try", "with", "yield", "or", "for", "while", "return", "from",
        "as", "assert", "del", "pass", "except", "finally", "nonlocal", "global",
        "def", "class", "break", "continue", "if", "elif", "else", "else:", "lambda", "import",
        "None", "False", "not", "raise", "True", "is", "in", 
    };
    private readonly static string[] pyBuiltinFunctions = new[]{
        "abs(", "delattr(", "hash(", "memoryview(", "set(", "all(", "dict(", "help(", "min(", "setattr(", 
        "any(", "dir(", "hex(", "next(", "slice(", "ascii(", "divmod(", "id(", "object(", "sorted(", "bin(", 
        "enumerate(", "input(", "oct(", "staticmethod(", "bool(", "eval(", "int(", "open(", "str(", 
        "breakpoint(", "exec(", "isinstance(", "ord(", "sum(", "bytearray(", "filter(", "issubclass(",
        "pow(", "super(", "bytes(", "float(", "iter(", "print(", "tuple(", "callable(", "format(", "len(", 
        "property(", "type(", "chr(", "frozenset(", "list(", "range(", "vars(", "classmethod(", "getattr(", 
        "locals(", "repr(", "zip(", "compile(", "globals(", "map(", "reversed(", "complex(", 
        "hasattr(", "max(", "round("
    }, 
    strBuiltinFunctions = new[]{
        "capitalize(", "casefold(", "center(", "count(", "encode(", "endswith(", "expandtabs(",
        "find(", "format(", "format_map(", "index(", "isalnum(", "isalpha(", "isascii(", "isdecimal(",
        "isdigit(", "isidentifier(", "islower(", "isnumeric(", "isprintable(", "isspace(", "istitle(",
        "isupper(", "join(", "ljust(", "lower(", "lstrip(", "maketrans(", "partition(", "replace(",
        "rfind(", "rindex(", "rjust(", "rpartition(", "rsplit(", "rstrip(", "split(", "splitlines(",
        "startswith(", "strip(", "swapcase(", "title(", "translate(", "upper(", "zfill" 
    }, 
    lstBuiltinFunctions = new[]{
        "append(", "clear(", "copy(", "count(", "extend(", "index(", "insert(", "pop(",
        "remove(", "reverse(", "sort"
    }, 
    dictBuiltinFunctions = new[]{
        "clear(", "copy(", "fromkeys(", "get(", "items(", "keys(", "pop(", "popitem(",
        "setdefault(", "update(", "values"
    }, 
    setBuiltinFunctions = new[]{
        "add(", "clear(", "copy(", "difference(", "difference_update(", "discard(", 
        "intersection(", "intersection_update(", "isdisjoint(", "issubset(", "issuperset(",
        "pop(", "remove(", "symmetric_difference(", "symmetric_difference_update(",
        "union(", "update("
    };
    private static HashSet<string> allKeyWords = new();
    private static readonly Tri 
        afterSpaceRecomendations = new(null), afterPeriodRecomendations = new(null);
    private static readonly Regex variableName = new(@"[a-zA-Z]\w*\(?", RegexOptions.Compiled);
    private const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Func<bool> IsStrLitteral = () 
        => CountTillCursor('\'') % 2 != 0
        || CountTillCursor('"') % 2 != 0;
    private static readonly Func<int, int, bool> 
        IsOpenBracket = (line, col) => col > -1 && linesText[line][col] == '(',
        IsPeriod = (line, col) => col > -1 && linesText[line][col] == '.',
        IsSpace = (line, col) => col > -1 && linesText[line][col] == ' ',
        IsLetter = (line, col) => col > -1 && letters.Contains(linesText[line][col]);
    public static void Start(){
        foreach (string item in pyKeyWords){
            if(item.Length > 2){
                afterSpaceRecomendations.AddWord(item);
            }
        }
        foreach (string item in pyBuiltinFunctions  ){  afterSpaceRecomendations.AddWord(item);  allKeyWords.Add(item); }
        foreach (string item in strBuiltinFunctions ){  afterPeriodRecomendations.AddWord(item); allKeyWords.Add(item); }
        foreach (string item in lstBuiltinFunctions ){  afterPeriodRecomendations.AddWord(item); allKeyWords.Add(item); }
        foreach (string item in dictBuiltinFunctions){  afterPeriodRecomendations.AddWord(item); allKeyWords.Add(item); }
        foreach (string item in setBuiltinFunctions ){  afterPeriodRecomendations.AddWord(item); allKeyWords.Add(item); }
        foreach (string item in pyKeyWords)          {                                             allKeyWords.Add(item); }
    } 
    
    private static void PosMinus1(ref int line, ref int col){
        col--;
        if(col < -1){
            line--;
            col = linesText[line].Length - 1;
        }
    }
    private static int CountTillCursor(char ch){
        int count = 0;
        for (int i = 0; i < CursorPos.line; i++){
            count += linesText[i].Count(x => x == ch);
        }
        return count + linesText[CursorPos.line][..CursorPos.col].Count(x => x == ch);
    }
    public static (bool period, string? word) GetPreviosWord(int toSkip=0){
        // ? it's a mess, but it works... And I don't really want to write it from scratch...
        try{
            if(IsStrLitteral()){    return (false, null); }
            (int l, int c) = CursorPos.ToTuple();
            while(IsSpace(l, c)){   PosMinus1(ref l, ref c); }
            var end = (l, c);
            for(int x=0; x<toSkip; x++){
                if   (IsOpenBracket(l, c))  {   PosMinus1(ref l, ref c); }
                while(IsLetter(l, c))       {   PosMinus1(ref l, ref c); }
                while(IsSpace(l, c))        {   PosMinus1(ref l, ref c); }
                if((l, c) == end){  return (false, null); }
                end = (l, c);
            }
            if   (IsOpenBracket(l, c)) {   PosMinus1(ref l, ref c); }
            while(IsLetter(l, c))      {   PosMinus1(ref l, ref c); }
            if((l, c) == end){  return (false, null); }
            string word = linesText[l].Substring(c + 1, end.c - c).Trim();
            if(word.Equals("")){    return (false, null); }
            return (IsPeriod(l, c), word);
        } catch (Exception){
            return (false, null);
        }
    }
    public static LinkedList<string> GetAllOptionsFromTree(Tri tree, string? prefix){
        if(prefix is null || prefix.Equals("")){ return new(); }

        LinkedList<(Tri pos, Tri parent, string prefix)> allPosses = new();
        GetAllPrefixes(tree, tree, prefix.ToCharArray(), new(), 0, allPosses);

        LinkedList<string> ll = new();
        foreach (var item in allPosses){
            var (treePos, parent, prfx) = item;
            prfx = prfx.Remove(prfx.Length-1);
            StringBuilder sb = new(prfx);
            GetAllOptionsFromTree(sb, ll, treePos, parent);
        }
        return ll;
    }
    private static void GetAllOptionsFromTree(StringBuilder word, LinkedList<string> ll, Tri tree, Tri parent){
        word.Append(tree.Val);
        if(tree.IsWord){
            ll.AddLast(word.ToString());
        }
        foreach (var val in tree.children.Values){
            GetAllOptionsFromTree(word, ll, val, tree);
        }
        word.Length--;
    }
    private static void GetAllPrefixes(Tri parent, Tri pos, char[] prefix, StringBuilder prfx, int i, LinkedList<(Tri, Tri, string)> ll){
        if(i == prefix.Length){
            ll.AddLast((pos, parent, prfx.ToString()));  return;
        }
        var newParent = pos;
        foreach(char c in new char[]{
                Char.ToUpper(prefix[i]),
                Char.ToLower(prefix[i])
            }
        ){
            if(pos.TryGetChild(c, out var newPos)){
                prfx.Append(c);
                GetAllPrefixes(newParent, newPos!, prefix, prfx, i+1, ll);
                prfx.Length--;
            }
        }
    }

    public static string[]? GetPredictions(){
        if(CursorPos.col == -1 && (CursorPos.line == 0 || linesText[CursorPos.line-1].Length == 0)){ return null; }
        var letter = CursorPos.col == -1 
            ? linesText[CursorPos.line-1][^1] 
            : linesText[CursorPos.line][CursorPos.col];
        if (letters.Contains(letter)){
            var (period, prefix) = GetPreviosWord();
            return GetAllOptionsFromTree(
                period ? afterPeriodRecomendations : afterSpaceRecomendations,
                prefix
            ).Select(x => x.Substring(prefix!.Length))
                .Where(x => !x.Equals(""))
                .ToArray();
        } else if(letter == ' '){
            return GetPreviosWord().word switch {
                "is" => new[]{ "not", "None", "not None" },
                "in" => new[] { "range(", "range(len(", "enumerate(", "zip(" },
                "not" => new[] { "in" },
                "raise" => new[] { "Exception(" },
                "except" => new[] { "Exception:" },
                "def" => new[] { "__init__(self," },
                "if" => new[] { "__name__ == \'__main__\':" },
                "return" => new[] { "None", "True", "False" },
                "lambda" => new[] { "x:", "num:", "x, y:", "num1, num2:" },
                "for" => new[] { "i", "num", "_", "arr" },
                _ => GetPreviosWord(1).word switch {
                    "for" => new[]{ "in", "," },
                    "if" or "elif" => new[]{ "is", "in", "==", "!=", "is not", "not in", "% 2 == 0", "% 2 != 0"},
                    "and" or "or" or "while" => new[]{ ">", "<", "==", "!=", "<=", ">=", "is", "in"},
                    "from" => new[]{ "import" },
                    "import" => new[]{ "as" },
                    _ => null
                },
            };
        } 
        return letter switch {
            '[' => new[]{"x for x in"},
            '{' => new[]{"x for x in"},
            '+' => new[]{ "= 1" },
            '-' => new[]{ "= 1" },
            _ => null
        };
    }

    public static void Predict(){
        var predictions = GetPredictions();
        if(predictions is null || predictions.Length == 0){    
            predictionMenu = null;
            return; 
        }
        MakePredictionMenu(predictions);
        nonStatic.Invalidate();
    } 

    public static (Bitmap bm, string[] predictions)? predictionMenu;
    private static void MakePredictionMenu(string[] predictions){
        if(predictions.Length > 10){
            predictions = predictions[..10];
        }
        var (width, height) = (0, txtHeight * predictions.Length);
        foreach(var pred in predictions) {
            width = Max(width, MeasureWidth(pred, boldFont));
        }

        predictionMenu = (new(width, height), predictions);
        RedrawPredictionMenu();
    }
    public static void RedrawPredictionMenu(){
        int end = 0;
        using(var g = Graphics.FromImage(predictionMenu!.Value.bm)){
            g.Clear(Color.Gray);
            foreach(var pred in predictionMenu!.Value.predictions) {
                g.DrawString(pred, boldFont, blackBrush, 0, end);
                end += txtHeight;
            }
        }
    }
}

public class Tri {
    public char? Val { get; }
    public bool IsWord { get; set; } = false;
    public Dictionary<char, Tri> children { get; }
    public Tri(char? val){
        this.Val = val;
        children = new();
    }
    public Tri AddChild(char val) => AddChild(new Tri(val));
    public Tri AddChild(Tri child){
        if(children.TryGetValue((char)child.Val!, out var oldChild)){
            return oldChild;
        }
        children[(char)child.Val!] = child;
        return child;
    }
    public Tri GetChild(char val) => children[val];
    public bool TryGetChild(char val, out Tri? res) => children.TryGetValue(val, out res);
    public void AddWord(string word, bool isVarName=false){
        var tree = this;
        foreach (char c in word){    tree = tree.AddChild(c); }
        tree.IsWord = true;
    }
}