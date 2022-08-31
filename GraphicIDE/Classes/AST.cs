using IronPython.Compiler.Ast;
using System.Drawing.Drawing2D;
using System.Text;

using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.MyImages;
using static GraphicIDE.MyMath;

namespace GraphicIDE;

public static class AST {
    public static BM_Middle MakeImg(Node ast){
        try {
            return ((Func<BM_Middle>)(ast switch {
                PythonAst pa => () => MainModule(pa),
                SuiteStatement ss => () => SuiteStatement(ss),
                ExpressionStatement es => () => MakeImg(es.Expression),
                PrintStatement ps => () => PrintStatement(ps),
                ParenthesisExpression pe => () => ParenthesisExpression(pe),
                ConstantExpression ce => () => Literal(ce),
                TupleExpression te => () => TupleExpression(te),
                NameExpression ne => () => MakeTxtBM(ne.Name.ToString(), brush: lblueBrush),
                AssignmentStatement as_ => () => AssignmentStatement(as_),
                CallExpression ce => () => FunctionCall(ce),
                BinaryExpression be => () => BinaryExpression(be),
                UnaryExpression ue => () => UnaryExpression(ue),
                IfStatement is_ => () => IfExpression(is_),
                WhileStatement ws => () => WhileStatement(ws),
                EmptyStatement _ => () => EmptyStatement(),
                ListExpression le => () => ListExpression(le),
                AndExpression ae => () => AndExpression(ae),
                OrExpression oe => () => OrExpression(oe),
                FromImportStatement fis => () => FromImportStatement(fis),
                ImportStatement is_ => () => ImportStatement(is_),
                AugmentedAssignStatement aas => () => AugmentedAssignStatement(aas),
                FunctionDefinition fd => () => MainModule(fd),
                ReturnStatement rs => () => ReturnStatement(rs),
                ListComprehension lc => () => ListComprehension(lc),
                SetComprehension sc => () => SetComprehension(sc),
                DictionaryComprehension dc => () => DictionaryComprehension(dc),
                ForStatement fs => () => ForStatement(fs),
                ConditionalExpression ce => () => ShortIfElse(ce),
                MemberExpression me => () => MemberExpression(me),
                _ => () => throw new Exception(),
            }))();
        } catch(Exception) { 
            throw new Exception();
        }
    }
    private static BM_Middle MemberExpression(MemberExpression ast){
        Bitmap img = MakeImg(ast.Target).Img;
        string name = $".{ ast.Name }";
        int nameW = MeasureWidth(name, boldFont);
        Bitmap res = new(img.Width + nameW, Max(txtHeight, img.Height));
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(img, 0, 0);
            g.DrawString(name, boldFont, lblueBrush, img.Width, 0);
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle ShortIfElse(ConditionalExpression ast){
        Bitmap test = MakeImg(ast.Test).Img;
        Bitmap trueExp = MakeImg(ast.TrueExpression).Img;
        Bitmap falseExp = MakeImg(ast.FalseExpression).Img;
        int gap = 4;
        int space = txtHeight * 2;
        Bitmap res = new(
            test.Width + Max(trueExp.Width, falseExp.Width) 
            + gap * 10 + space
            , Max(test.Height / 2, trueExp.Height + 3 * gap)
            + Max(test.Height / 2, falseExp.Height + 3 * gap)
            + 4 * gap
        );
        int middle = Max(test.Height / 2, trueExp.Height + 3 * gap) + 2 * gap;
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(test, gap * 2, middle - test.Height / 2);
            g.DrawRectangle(
                whiteP, gap, middle - test.Height / 2 - gap, 
                test.Width + gap*2, test.Height + gap*2
            );
            int end = test.Width + gap * 4;

            var arr = new(Bitmap, Bitmap, int, Pen)[]{
                (trueExp , shortArrowG, -1, shortIfElseGP),
                (falseExp, shortArrowR, +1, shortIfElseRP)
            };
            for (int i=0; i < arr.Length; i++) {
                var (exp, arrowUnsized, dir, pen) = arr[i];
                Bitmap arrow = new(arrowUnsized, txtHeight/2, txtHeight/2);
                int y = middle + dir * (test.Height / 4);
                int startX = end;
                int endX = end + txtHeight;
                Point startP = new(startX, y);
                Point endP   = new(endX, y);
                g.DrawLine(pen, startP, endP);
                startP = endP;
                endP.Y = middle + dir * (exp.Height/2 + gap * 3);
                g.DrawLine(pen, startP, endP);
                startP = endP;
                endP.X = end + space - arrow.Width;
                g.DrawLine(pen, startP, endP);
                endP.Y -= arrow.Height/2;
                g.DrawImage(arrow, endP);
                endP.Y += arrow.Height/2 - (exp.Height/2 + gap);
                endP.X += arrow.Width;
                g.DrawRectangle(pen, endP.X, endP.Y, exp.Width + gap * 5, exp.Height + gap * 2);
                endP.X += 2 * gap;
                endP.Y += gap;
                g.DrawImage(exp, endP);
            }
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle Comprehension(Comprehension ast, string open, string close, Brush brush, Color bg, Bitmap item){
        int indent = indentW / 2;
        LinkedList<Bitmap> iterPics = new();
        int totalHeight = item.Height, totalWidth  = item.Width + indent;
        foreach(var iter in ast.Iterators) {
            if(iter is ComprehensionFor cf){
                Bitmap bm = ForTopPart(cf.Left, cf.List);
                totalHeight += bm.Height;
                totalWidth = Max(totalWidth, bm.Width + indent);
                iterPics.AddLast(bm);
            } else if(iter is ComprehensionIf ci){
                Bitmap condition = MakeImg(ci.Test).Img;
                Bitmap bm = new(condition.Width + qWidth, condition.Height);
                totalHeight += bm.Height;
                totalWidth = Max(totalWidth, bm.Width + indent);
                using(var g = Graphics.FromImage(bm)){
                    g.DrawString("¿", boldFont, keyOrangeB, 0, bm.Height / 2 - qHeight / 2);
                    g.DrawImage(condition, upSideDownW, 0);
                    g.DrawString(
                        "?", boldFont, keyOrangeB, 
                        condition.Width + upSideDownW, 
                        bm.Height / 2 - qHeight / 2
                    );
                }
                iterPics.AddLast(bm);
            } else {
                throw new("What is this comprehension iterator?");
            }
        }
        int bracketHieght = txtHeight * 2;
        Bitmap res = new(totalWidth, totalHeight + bracketHieght);
        using (var g = Graphics.FromImage(res)) {
            g.Clear(bg);
            g.DrawString(open, boldFont, brush, 0, 0);
            int end = txtHeight;
            foreach(var iter in iterPics) {
                g.DrawImage(iter, indent, end);
                end += iter.Height;
            }
            g.DrawImage(item, indent, end);
            end += item.Height;
            g.DrawString(close, boldFont, brush, 0, end);

        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle ListComprehension(ListComprehension ast){
        return Comprehension(ast, "[", "]", keyOrangeB, opaqekeyOrange, MakeImg(ast.Item).Img);
    }
    private static BM_Middle SetComprehension(SetComprehension ast){
        return Comprehension(ast, "{", "}", mathPurpleB, opaqeMathPurple, MakeImg(ast.Item).Img);
    }
    private static BM_Middle DictionaryComprehension(DictionaryComprehension ast){
        Bitmap key = MakeImg(ast.Key).Img;
        Bitmap val = MakeImg(ast.Value).Img;
        int colW = MeasureWidth(":", boldFont);
        Bitmap item = new(key.Width + val.Width + colW, Max(key.Height, val.Height));
        using(var g = Graphics.FromImage(item)){
            g.DrawImage(key, 0, 0);
            g.DrawString(":", boldFont, mathPurpleB, key.Width, 0);
            g.DrawImage(val, key.Width + colW, 0);
        }
        return Comprehension(ast, "{", "}", forBlueB, opaqeForBlue, item);
    }
    private static BM_Middle? scaledNone;
    private static BM_Middle None(){
        if(scaledNone is null){
            scaledNone = new(new(noneImg, txtHeight, txtHeight), txtHeight/2);
        }
        return (BM_Middle)scaledNone;
    }
    private static Bitmap? scaledReturn;
    private static BM_Middle ReturnStatement(ReturnStatement ast){
        var val = MakeImg(ast.Expression).Img;
        if(scaledReturn is null){
            scaledReturn = new(returnImg, returnImg.Width / (returnImg.Height / txtHeight), txtHeight);
        }

        Bitmap res = new(val.Width + scaledReturn.Width, Max(val.Height, scaledReturn.Height));
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(val, res.Width - val.Width, res.Height/2-val.Height/2);
            g.DrawImage(scaledReturn, 0, res.Height/2-scaledReturn.Height/2);
        }
        return new(res, res.Height/2);
    }
    // todo relative import (from .. import func)
    private static BM_Middle FromImportStatement(FromImportStatement ast){
        string fromName = string.Join('.', ast.Root.Names);
        int fromNameW = MeasureWidth(fromName, boldFont);
        int fromNameH = MeasureHeight(fromName, boldFont);
        Bitmap import;
        if(ast.AsNames is not null){
            import = ImportStatement(null, ast.Names.ToArray(), ast.AsNames.ToArray()).Img;
        } else {
            import = new(MeasureWidth("EVERYTHING", boldFont), MeasureHeight("EVERYTHING", boldFont));
            using(var g = Graphics.FromImage(import)){
                g.DrawString("EVERYTHING", boldFont, keyOrangeB, 0, 0);
            }
        }
        Bitmap res = new(
            import.Width + fromNameW + fromW + colW, 
            Max(txtHeight, Max(import.Height, fromNameH))
        );
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(import, res.Width - import.Width, 0);
            g.DrawString("from", boldFont, keyOrangeB, 0, res.Height / 2 - txtHeight / 2);
            g.DrawString(fromName, boldFont, lblueBrush, fromW, res.Height / 2 - fromNameH / 2);
            g.DrawString(":", boldFont, whiteBrush, fromW + fromNameW, res.Height / 2 - txtHeight / 2);
        }
        return new(res, res.Width/2);
    }
    private static BM_Middle ImportStatement(ImportStatement? ast, string[]? names=null, string?[]? asNames=null){
        names ??= ast!.Names.Select((x) => string.Join('.', x.Names)).ToArray();
        asNames ??= ast!.AsNames.ToArray();
        int gap = 6;
        var cars = new Bitmap[names.Length];
        var (height, width) = (0, 0);
        for (int i=0; i < cars.Length; i++) {
            cars[i] = MakeCar(MakeName(names[i], asNames[i]));
            height = Max(height, cars[i].Height);
            width += cars[i].Width + gap;
        }

        
        Bitmap truck = new(truckImg, height + 2 * gap, height + 2 * gap);
        Bitmap bm = new(width + truck.Width, truck.Height);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(truck, bm.Width-truck.Width, 0);
            int end = 0;
            foreach(var car in cars) {
                g.DrawImage(car, end, gap);
                end += car.Width;
                g.DrawLine(truckP, end, bm.Height - 14, end + gap, bm.Height - 14);
                end += gap;
            }
        }
        return new(bm, bm.Height / 2);
        Bitmap MakeName(string name, string? asName){
            if(asName is null){
                Bitmap res_ = new(MeasureWidth(name, boldFont), MeasureHeight(name, boldFont));
                using(var g = Graphics.FromImage(res_)){
                    g.DrawString(name, boldFont, blackBrush, 0, 0);
                }
                return res_;
            }
            int nameW = MeasureWidth(name, boldFont);
            int asW = MeasureWidth(" as ", boldFont);
            int asNameW = MeasureWidth(asName, boldFont);
            int nameH = MeasureHeight(name, boldFont);
            int asH = MeasureHeight(" as ", boldFont);
            int asNameH = MeasureHeight(asName, boldFont);
            Bitmap res = new(nameW + asW + asNameW, Max(nameH, Max(asH, asNameH)));
            using(var g = Graphics.FromImage(res)){
                g.DrawString(name, boldFont, blackBrush, 0, res.Height/2 - nameH/2);
                g.DrawString(" as", boldFont, keyOrangeB, nameW, res.Height/2 - asH/2);
                g.DrawString(asName, boldFont, blackBrush, nameW + asW, res.Height/2 - asNameH/2);
            }
            return res;
        }
        Bitmap MakeCar(Bitmap name){
            var (w, h) = (name.Width, name.Height);
            int outGap = (int)(truckP.Width/2);
            int inGap = 4;
            Bitmap res = new(w + outGap * 2 + inGap * 2, h + outGap * 2 + inGap * 2);
            using(var g = Graphics.FromImage(res)){
                g.FillRectangle(truckIBrush, outGap, outGap, res.Width - outGap * 2, res.Height - outGap * 2);
                g.DrawRectangle(truckP, outGap, outGap, res.Width - outGap * 2, res.Height - outGap * 2);
                g.DrawImage(name, outGap + inGap, outGap + inGap);
            }
            return res;
        }
    }
    private static BM_Middle AndExpression(AndExpression ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int andWidth = MeasureWidth("and", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + andWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, andWidth + 10, 10);
            g.DrawImage(r.Img, andWidth + 10, l.Img.Height + 20);
            int andPosY = l.Img.Height - txtHeight / 2;
            g.DrawLine(yellowP, 2, l.Img.Height + 15, 10, l.Img.Height + 15);
            g.DrawLine(yellowP, 10 + andWidth, l.Img.Height + 15, bm.Width - 2, l.Img.Height + 15);
            
            g.DrawString("and", boldFont, yellowB, 10, andPosY + 15);
            // the twos are brush size / 2
            g.DrawLine(yellowP, 2, 2, 2, bm.Height - 2);
            g.DrawLine(yellowP, 2, 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, 2, bm.Height - 2);
        }
        return new(bm, bm.Height / 2);
    }
    private static BM_Middle OrExpression(OrExpression ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int orWidth = MeasureWidth("or", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + orWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, orWidth + 10, 10);
            g.DrawImage(r.Img, orWidth + 10, l.Img.Height + 20);
            int andPosY = l.Img.Height - txtHeight / 2;
            g.DrawLine(yellowP, 2, l.Img.Height + 15, 10, l.Img.Height + 15);
            g.DrawLine(yellowP, 10 + orWidth, l.Img.Height + 15, bm.Width - 2, l.Img.Height + 15);
            g.DrawString("or", boldFont, yellowB, 10, andPosY + 15);
            // the twos are brush size / 2
            g.DrawLine(yellowP, 2, 2, 2, bm.Height - 2);
            g.DrawLine(yellowP, 2, 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, 2, bm.Height - 2);
        }
        return new(bm, bm.Height / 2);
    }
    private static BM_Middle? emptyListScaled = null;
    private static BM_Middle ListExpression(ListExpression ast){
        if(ast.Items.Count == 0) {
            if(emptyListScaled is BM_Middle bm && bm.Img.Height == txtHeight + 15) {
                return bm;
            }
            Bitmap emptyList = new(emptyListImg, emptyListImg.Width / (emptyListImg.Height / (txtHeight + 15)), txtHeight + 15);
            Bitmap padded = new(emptyList.Width + 5, emptyList.Height);
            using(var pg = Graphics.FromImage(padded)) {
                pg.DrawImage(emptyList, 5, 0);
            }
            emptyListScaled = new(padded, padded.Height / 2);
            return (BM_Middle)emptyListScaled;
        }
        int lineLen = 5;
        int gap = 5;
        LinkedList<BM_Middle> elements = new();
        var (width, height) = (lineLen + gap, 0);
        foreach(var item in ast.Items) {
            var img = MakeImg(item);
            var middle = img.Middle;
            elements.AddLast(img);
            width += lineLen + img.Img.Width + 2 * gap;
            height = Max(height, img.Img.Height);
        }
        Bitmap res = new(width, height + (lineLen + gap) * 2);
        using(var g = Graphics.FromImage(res)) {
            var end = 2;
            foreach(var (img, _) in elements) {
                end += gap;
                g.DrawLine(redListP, end, 5, end, res.Height - 5);
                end += lineLen;
                g.DrawImage(img, end + (int)redListP.Width/2, res.Height / 2 - img.Height / 2);
                end += img.Width + gap;
            }
            end += gap;
            g.DrawLine(redListP, end, 5, end, res.Height - 5);
            g.DrawLine(redListP, 5, 5, res.Width, 5);
            g.DrawLine(redListP, 5, res.Height - 5, res.Width, res.Height - 5);
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle? passPic = null;
    private static BM_Middle EmptyStatement(){
        if(passPic is BM_Middle bm && bm.Img.Height == txtHeight) {   return bm; }
        Bitmap img = new(passImg, passImg.Width / (passImg.Height / txtHeight), txtHeight);
        passPic = new(img, img.Height / 2);
        return (BM_Middle)passPic;
    }
    private static Bitmap ForTopPart(Expression left, Expression list){
        Bitmap var;
        try{
            if(left is NameExpression 
                || (
                    linesText[left.Start.Line - 1][left.Start.Column - 2] == '('
                    && linesText[left.End.Line - 1][left.End.Column - 1] == ')'
                    // TODO could be space...?
                )
            ){
                var = MakeImg(left).Img;
            } else {
                throw new IndexOutOfRangeException();
            }
        } catch (IndexOutOfRangeException){
            LinkedList<BM_Middle> args = new();
            if(left is TupleExpression tuple){
                foreach(var item in tuple.Items) {
                    var bm = MakeImg(item);
                    args.AddLast(bm);    
                }
                var = NonTupleTuple(args).Img;
            } else {
                throw new Exception();
            }
        }
        Bitmap iter = MakeImg(list).Img;
        int forW = MeasureWidth("for ", boldFont);
        int inW = MeasureWidth(" in ", boldFont);
        int iterHeight = Max(Max(var.Height, iter.Height), txtHeight);
        Bitmap condition = new(var.Width + iter.Width + forW + inW, iterHeight);
        using(var g = Graphics.FromImage(condition)){
            g.DrawString("for", boldFont, keyOrangeB, 0, condition.Height/2-txtHeight/2);
            int end = forW;
            g.DrawImage(var, end, condition.Height/2-var.Height/2);
            end += var.Width;
            g.DrawString(" in", boldFont, mathPurpleB, end, condition.Height/2-txtHeight/2);
            end += inW;
            g.DrawImage(iter, end, condition.Height/2-iter.Height/2);
        }
        return condition;
    }
    private static BM_Middle ForStatement(ForStatement ast){
        Bitmap body = MakeImg(ast.Body).Img;
        Bitmap condition = ForTopPart(ast.Left, ast.List);
        return WhileAndFor(condition, body, forBlueP, forArrowUpImg, forArrowDownImg);
        // todo else
    }
    private static BM_Middle WhileStatement(WhileStatement ast){
        Bitmap condition = MakeImg(ast.Test).Img;
        int whileW = MeasureWidth("while ", boldFont);
        Bitmap withWhile = new(
            condition.Width + whileW, 
            Max(txtHeight, condition.Height)
        );
        using(var g = Graphics.FromImage(withWhile)){
            g.DrawString("while", boldFont, keyOrangeB, 0, withWhile.Height/2-txtHeight/2);
            g.DrawImage(condition, whileW, withWhile.Height/2-condition.Height/2);
        }
        Bitmap body = MakeImg(ast.Body).Img;
        return WhileAndFor(withWhile, body, whileOrangeP, whileArrowUpImg, whileArrowDownImg);
    }
    private static BM_Middle WhileAndFor(Bitmap condition, Bitmap body, Pen pen, Bitmap arUp, Bitmap arDown){
        int indent = 8, gap = 8;
        Bitmap border = new(
            Max(condition.Width + 8, body.Width + indent),
            condition.Height + 8
        );
        using (var g = Graphics.FromImage(border)) {
            g.DrawRectangle(pen, 2, 2, border.Width - 4, border.Height - 4);
            g.DrawImage(condition, 4, 4);
        }
        condition = border;
        Bitmap arrowUp = new(arUp, txtHeight * 2, body.Height + condition.Height);
        Bitmap arrowDown = new(arDown, txtHeight * 2, body.Height + condition.Height);
        Bitmap res = new(
            width: Max(condition.Width, body.Width + indent) + arrowDown.Width + arrowUp.Width,
            height: condition.Height + body.Height + gap * 2 + 6
        );
        using (var g = Graphics.FromImage(res)) {
            g.DrawImage(arrowDown, 0, gap);
            g.DrawImage(arrowUp, res.Width - arrowUp.Width, gap);

            g.DrawImage(condition, arrowDown.Width, gap);
            g.DrawImage(body, indent + arrowDown.Width, condition.Height + gap);

            g.DrawLine(pen, arrowDown.Width, res.Height - 4, res.Width - arrowUp.Width, res.Height - 4);
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle IfExpression(IfStatement ast){
        static Graphics JoinIfAndElse(Pen lastColor, ref Bitmap res, Bitmap img) {
            var (prevH, prevW) = (res.Height, res.Width);
            var prevImg = res;
            res = new(Max(res.Width, img.Width), res.Height + img.Height);
            var resG = Graphics.FromImage(res);
            resG.DrawImage(prevImg, 0, 0);
            resG.DrawLine(lastColor, 4, prevH + 1, prevW, prevH + 1);
            resG.DrawImage(img, 0, prevH);
            return resG;
        }

        static Bitmap MakeIfOrElif(Bitmap condition, Bitmap body, Pen pen) {
            Bitmap res = new(
                width: Max(condition.Width + qWidth, body.Width + indentW),
                height: condition.Height + body.Height + 5 + (int)pen.Width
            );
            var g = Graphics.FromImage(res);
            g.DrawString("¿", boldFont, keyOrangeB, 0, (int)(pen.Width + condition.Height / 2 - qHeight / 2));
            g.DrawImage(condition, upSideDownW, pen.Width);
            g.DrawString(
                "?", boldFont, keyOrangeB, 
                condition.Width + upSideDownW, 
                (int)(pen.Width + condition.Height / 2 - qHeight / 2)
            );
            g.DrawLine(pen, 1, 0, 1, res.Height);
            g.DrawImage(body, indentW, condition.Height);
            return res;
        }
        
        var mainIf = ast.Tests[0];
        Pen lastColor = greenDashed;
        var ifCond = MakeImg(mainIf.Test).Img;
        var ifBody = MakeImg(mainIf.Body).Img;
        var res = MakeIfOrElif(ifCond, ifBody, greenOpaqeP);
        var resG = Graphics.FromImage(res);
        resG.DrawLine(lastColor, 4, (int)lastColor.Width/2, res.Width, (int)lastColor.Width/2);

        if(ast.Tests.Count > 1) {
            lastColor = orangeDashed;
            for(int i = 1; i < ast.Tests.Count; i++) {
                var item = ast.Tests[i];
                var cond = MakeImg(item.Test).Img;
                var body = MakeImg(item.Body).Img;
                var img = MakeIfOrElif(cond, body, orangeOpaqeP);
                resG = JoinIfAndElse(lastColor, ref res, img);
            }
        }

        if(ast.ElseStatement is not null) {
            var elseBody = MakeImg(ast.ElseStatement).Img;
            Bitmap elseImg = new(
                width: elseBody.Width + indentW,
                height: elseBody.Height + 7
            );
            using(var eg = Graphics.FromImage(elseImg)) {
                eg.DrawLine(redOpaqeP, 1, 0, 1, elseImg.Height);
                eg.DrawImage(elseBody, indentW, 0);
            }
            resG = JoinIfAndElse(lastColor, ref res, elseImg);
            lastColor = redDashed;
        }
        resG.DrawLine(lastColor, 4, res.Height - 3, res.Width, res.Height - 3);
        Bitmap addPad = new(res.Width, res.Height + 5);
        using(var g = Graphics.FromImage(addPad)) {
            g.DrawImage(res, 0, 0);
        }
        return new(addPad, res.Height / 2);
    }
    private static BM_Middle NonTupleTuple(LinkedList<BM_Middle> args){
        int commaW = MeasureWidth(",", boldFont);
        int hTop = 0, hBottom = 0, width = -commaW;
        foreach(var item in args) {
            width += commaW + item.Img.Width;
            hTop = Max(hTop, item.Middle);
            hBottom = Max(hBottom, item.Img.Height - item.Middle);
        }
        Bitmap argsBm = new(width, hTop + hBottom);
        using(var g = Graphics.FromImage(argsBm)){
            int end = 0;
            foreach(var item in args) {
                g.DrawImage(item.Img, end, hTop - item.Middle);
                end += item.Img.Width;
                if(!item.Equals(args.Last!.Value)){
                    g.DrawString(",", boldFont, whiteBrush, end, hTop - txtHeight/2);
                    end += commaW;
                }
            }
        }
        return new(argsBm, argsBm.Height/2);
    }
    private static Func<CallExpression, Bitmap> FirstToImg = (ast) => MakeImg(ast.Args[0].Expression).Img;
    private static Func<CallExpression, Bitmap> argsToTuple = (ast) => NonTupleTuple(new(ast.Args.Select((x) => MakeImg(x.Expression)))).Img;
    private static BM_Middle FunctionCall(CallExpression ast){   
        string? name = null;
        if (ast.Target is NameExpression tar1) { name = tar1.Name; }
        if (ast.Target is MemberExpression tar2) { name = tar2.Name; }
        if (name is null) { return DefaultFuncCall(ast); }
        try {
            return name switch {
                "input" => Input(ast),
                "sqrt" => Sqrt(ast),
                "pow" => Pow(ast),
                "abs" => Abs(ast),
                "hash" => Func(FirstToImg(ast), hashtagImg, hashP),
                "sum" => Func(FirstToImg(ast), sumImg, sumP),    // todo start
                "len" => Func(FirstToImg(ast), rulerImg, lenP),
                "filter" => Func(argsToTuple(ast), filterImg, sumP),
                "divmod" => Func(argsToTuple(ast), divmodImg, divmodP),
                "round" => Func(FirstToImg(ast), roundImg, divmodP),    // todo digits (precision)
                "int" => Func(FirstToImg(ast), intImg, intFuncP),
                "str" => Func(FirstToImg(ast), strImg, strFuncP),
                "bool" => Func(FirstToImg(ast), boolImg, boolFuncP),
                "float" => Func(FirstToImg(ast), floatImg, intFuncP),
                "list" => Func(FirstToImg(ast), listImg, listP),
                "tuple" => Func(FirstToImg(ast), tupleImg, tupleP),
                "max" => MaxMin(ast, maxImg),
                "min" => MaxMin(ast, minImg),
                _ => DefaultFuncCall(ast),
            };
        } catch (Exception) {   return DefaultFuncCall(ast); }
    }

    private static BM_Middle DefaultFuncCall(CallExpression ast) {
        Bitmap argsBm;
        if (ast.Args.Count > 0) {
            LinkedList<BM_Middle> args = new();
            foreach (var item in ast.Args) {
                var bm = MakeImg(item.Expression);
                args.AddLast(bm);
            }
            argsBm = NonTupleTuple(args).Img;
        } else {
            argsBm = new(5, txtHeight);
        }
        Bitmap par = ParenthesisExpression(null, inside_: argsBm, pen: lblueP).Img;
        Bitmap func = MakeImg(ast.Target).Img;
        Bitmap res = new(par.Width + func.Width, Max(par.Height, func.Height));
        using (var g = Graphics.FromImage(res)) {
            g.DrawImage(func, 0, res.Height / 2 - func.Height / 2);
            g.DrawImage(par, func.Width, res.Height / 2 - par.Height / 2);
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle MaxMin(CallExpression ast, Bitmap img) {        
        var inside = NonTupleTuple(new(ast.Args.Select((x) => MakeImg(x.Expression)))).Img;
        int gap = 2;
        Bitmap res = new(img, inside.Width + 2 * gap, 2 * inside.Height + 2 * gap);
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(inside, gap, inside.Height + gap);
        }
        return new(res, res.Height/2);
    }
    private static BM_Middle Input(CallExpression ast) {
        var val = ast.Args[0].Expression;
        var inside = MakeImg(val).Img;
        int gap = 10;
        Bitmap res = new(keyboardImg, inside.Width + 2 * gap, inside.Height + 2 * gap);
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(inside, gap, gap);
        }
        return new(res, res.Height/2);
    }
    private static BM_Middle Abs(CallExpression ast) {
        var val = ast.Args[0].Expression;
        var inside = MakeImg(val).Img;
        Bitmap res_ = new(
            width: inside.Width + 30,
            height: inside.Height + 10
        );
        using (var g = Graphics.FromImage(res_)) {
            g.DrawLine(mathPurpleP, 10, 0, 10, res_.Height);
            g.DrawLine(mathPurpleP, res_.Width - 5, 0, res_.Width - 5, res_.Height);
            g.DrawImage(inside, 15, 5);
        }
        return new(res_, res_.Height / 2);
    }
    private static BM_Middle Pow(CallExpression ast) {
        var bottom = MakeImg(ast.Args[0].Expression);
        var top = MakeImg(ast.Args[1].Expression);
        var pow = Power(bottom, top);
        if (ast.Args.Count > 2) {
            var mod = MakeImg(ast.Args[2].Expression);
            var parenthesis = ParenthesisExpression(null, pow.Img);
            return BinaryExpression(null, "%", parenthesis, mod);
        }
        return pow;
    }
    private static BM_Middle Sqrt(CallExpression ast) {
        var val = ast.Args[0].Expression;
        var inside = MakeImg(val).Img;
        Bitmap res_ = new(
            width: inside.Width + 30,
            height: inside.Height + 15
        );
        using (var g = Graphics.FromImage(res_)) {
            g.DrawImage(inside, 20, 15);
            g.DrawLine(mathPurpleP, 15, 5, res_.Width, 5);
            g.DrawLine(mathPurpleP, 15, 5, 15, res_.Height);
            g.DrawLine(mathPurpleP, 5, res_.Height - 15, 15, res_.Height);
        }
        return new(res_, res_.Height / 2);
    }
    private static BM_Middle Func(Bitmap inside, Bitmap icon, Pen pen){
        int gap = 4;
        int penW = (int)pen.Width / 2;
        icon = new(icon, icon.Width / (icon.Height / (inside.Height + 2 * penW)), inside.Height + 2 * penW);
        Bitmap res_ = new(
            width: inside.Width + icon.Width + 5 * gap + penW,
            height: inside.Height + 2 * penW + 2 * gap
        );
        using (var g = Graphics.FromImage(res_)){
            g.DrawImage(icon, gap, gap + penW);
            int start = gap + icon.Width + gap;
            g.DrawImage(inside, start + gap, penW + gap);
            g.DrawRectangle(pen, start, gap + penW, res_.Width-start-gap-penW, res_.Height-2*gap);
        }
        return new(res_, res_.Height / 2);
    }

    private static BM_Middle UnaryExpression(UnaryExpression ast) {
        var op = PythonOperatorToString(ast.Op);
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont);
        var opHeight = MeasureHeight(op, boldFont);
        var img = MakeImg(ast.Expression);
        (Bitmap bmap, int middle) = (img.Img, img.Middle);
        var top = middle;
        Bitmap res = new(
            width: bmap.Width + opWidth + gap,
            height: bmap.Height
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawString(op, boldFont, mathPurpleB, x: gap, y: middle - opHeight / 2);
            g.DrawImage(bmap, x: opWidth + gap, y: middle - top);
        }
        return new(res, middle);
    }
    private static BM_Middle BinaryExpression(BinaryExpression? ast, string? op_=null, BM_Middle? imgL_=null, BM_Middle? imgR_=null) {
        var op = op_ ?? PythonOperatorToString(ast!.Operator);
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(op, boldFont);
        var l = imgL_ ?? MakeImg(ast!.Left);
        (Bitmap left, int lmiddle) = (l.Img, l.Middle);
        var r = imgR_ ?? MakeImg(ast!.Right);
        (Bitmap right, int rmiddle) = (r.Img, r.Middle);
        switch(op){
            case "**":
                return Power(l, r);
            case "/":
                return Divide(right, left);
            case "//":
                return FloorDivide(right, left);
        }
        var rBottom = right.Height - rmiddle;
        var rTop = rmiddle;
        var lBottom = left.Height - lmiddle;
        var lTop = lmiddle;
        Bitmap res = new(
            width: left.Width + right.Width + opWidth,
            height: Max(lTop, rTop) + Max(lBottom, rBottom)
        );
        var resMiddle = Max(lTop, rTop);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(left, 0, y: resMiddle - lTop);
            g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: resMiddle - opHeight / 2);
            g.DrawImage(right, x: left.Width + opWidth, y: resMiddle - rTop);
        }
        return new(res, resMiddle);
    }

    private static BM_Middle Power(BM_Middle bottom, BM_Middle top) {
        int topGap = top.Img.Height - top.Middle;
        Bitmap res = new(
            width: top.Img.Width + bottom.Img.Width - 5,
            height: Max(topGap, bottom.Img.Height) + top.Middle
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(bottom.Img, 0, y: res.Height - bottom.Img.Height);
            g.DrawImage(top.Img, x: bottom.Img.Width - 5, y: 0);
        }
        return new(res, res.Height - bottom.Img.Height / 2);
    }
    private static BM_Middle FloorDivide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 22
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (res.Width - top.Width) / 2, y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawLine(mathPurpleP, 5, top.Height + 11, res.Width, top.Height + 11);
            g.DrawImage(bottom, x: (res.Width - bottom.Width) / 2, y: top.Height + 22);
        }
        return new(res, top.Height + 11);
    }
    private static BM_Middle Divide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 15
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (res.Width - top.Width) / 2, y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawImage(bottom, x: (res.Width - bottom.Width) / 2, y: top.Height + 15);
        }
        return new(res, top.Height + 7);
    }
    private static BM_Middle Literal(ConstantExpression ast) {
        if("None".Equals(ast.NodeName)){
            return None();
        }
        return MakeTxtBM(
            ast.Type.Name.Equals("String") ? $"'{ast.Value}'": ast.Value.ToString()!, 
            ast.Type.Name switch {
                "String" => stringBrush,
                "Int32" or "Double" or "BigInteger" => intBrush,
                _ => textBrush
            }
        );
    }
    private static BM_Middle SuiteStatement(SuiteStatement ast) {
        LinkedList<Bitmap> resses = new();
        var (height, width) = (0, 0);
        foreach(var statement in ast.Statements) {
            try {
                Bitmap img = MakeImg(statement).Img;
                resses.AddLast(img);
                height += img.Size.Height;
                width = Max(width, img.Size.Width);
            } catch(Exception) {
                // put just the text in this line
                throw;
            }
            
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        using(var g = Graphics.FromImage(res)) {
            foreach(var item in resses) {
                g.DrawImage(item, 0, end);
                end += item.Height;
            }
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle MainModule(ScopeStatement ast) {
        LinkedList<Bitmap> resses = new();
        var (height, width) = (0, 0);
        IList<Statement> statements;
        if(ast is PythonAst pa){
            statements = ((SuiteStatement)pa.Body).Statements;
        } else if(ast is FunctionDefinition fd) {
            statements = ((SuiteStatement)fd.Body).Statements;
        } else {
            throw new Exception();
        }
        for(int j = 0; j < statements.Count; j++) {
            var statement = statements[j];
            (int line, int col) startI, endI;
            if(j == 0) {
                startI = (0, 0);
            } else {
                var start = statements[j - 1].End;
                startI = (start.Line - 1, start.Column - 1);
            }
            if(j == statements.Count - 1) {
                endI = (linesText.Count - 1, linesText[^1].Length);
                for(int i = 1; i <= linesText.Count; i++) {
                    endI = (linesText.Count - i, linesText[^i].Length);
                    if(endI.col != 0) {
                        break;
                    }
                }
            } else {
                var start = statements[j + 1].Start;
                endI = (start.Line - 1, start.Column - 1);
            }
            try {
                var img = MakeImg(statement).Img;
                if(img is null) {
                    throw new Exception();
                }
                resses.AddLast(img);
            } catch(Exception) {
                StringBuilder sb = new();
                for(int i = startI.line; i <= endI.line; i++) {
                    if(i == startI.line) {
                        if(i == endI.line) {
                            if(startI.col != endI.col) {
                                sb.Append(linesText[i].AsSpan(startI.col, endI.col - startI.col));
                            }
                        } else {
                            sb.Append(linesText[i].AsSpan(startI.col));
                        }
                    } else if (i == endI.line) {
                        if(endI.col != 0) {
                            sb.Append(linesText[i].AsSpan(0, endI.col));
                        }
                    } else {
                        sb.Append(linesText[i]);
                    }
                    if(i != endI.line) {    sb.Append('\n'); }
                }
                var st = sb.ToString();
                var len = MeasureWidth(st, boldFont);
                var lenH = MeasureHeight(st, boldFont);
                Bitmap bm = new(len, lenH);
                using(var bg = Graphics.FromImage(bm)) {
                    bg.DrawString(st, boldFont, textBrush, 0, 0);
                }
                resses.AddLast(bm);
            }
            height += resses.Last!.Value.Size.Height;
            width = Max(width, resses.Last!.Value.Size.Width);
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        using(var g = Graphics.FromImage(res)) {
            foreach(var item in resses) {
                g.DrawImage(item, 0, end);
                end += item.Height;
            }
        }
        return new(res, res.Height / 2);
    }
    // todo keywords (end, sep)
    private static BM_Middle PrintStatement(PrintStatement ast) {
        int gap = 2;
        LinkedList<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Expressions) {
            if(statement is TupleExpression te) {
                LinkedList<BM_Middle> args = new();
                foreach(var item in te.Items) {
                    var bm = MakeImg(item);
                    args.AddLast(bm);    
                }
                resses.AddLast(NonTupleTuple(args).Img);
            } else if (statement is ParenthesisExpression pe) {
                resses.AddLast(MakeImg(pe.Expression).Img);
            } else {
                throw new Exception();
            }
            width += resses.Last!.Value.Width;
            height = Max(height, resses.Last!.Value.Height);
        }
        var printer = new Bitmap(printerImg, new Size(printerImg.Width / (printerImg.Height / (height + gap * 4)), height + gap * 4));
        var res = new Bitmap(width + printer.Width + gap * 3, printer.Height);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(printer, 0, 0);
            var end = printer.Width;
            g.DrawRectangle(new Pen(Color.White, 5), end, gap, res.Width - end - gap, res.Height - 2 * gap);

            end += gap;
            foreach(var item in resses) {
                g.DrawImage(item, end, gap * 2);
                end += item.Width;
            }
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle ParenthesisExpression(ParenthesisExpression? ast, Bitmap? inside_=null, Pen? pen=null) {
        Bitmap inside = inside_ ?? MakeImg(ast!.Expression).Img;
        int curveWidth = (inside.Height)/4;
        pen = pen ?? mathPurpleP;
        int pW = (int)pen.Width/2;
        Bitmap res = new(inside.Width + curveWidth * 2 + pW*2, inside.Height);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(inside, curveWidth + pW, 0);
            
            g.SmoothingMode = SmoothingMode.HighQuality;
            var (startX, startY) = (curveWidth + pW, 0);
            var (endX, endY) = (curveWidth + pW, res.Height);
            Point p1 = new(startX, startY);
            Point p2 = new(startX - curveWidth, startY + curveWidth);
            Point p3 = new(endX - curveWidth, endY - curveWidth);
            Point p4 = new(endX, endY);
            g.DrawBezier(pen, p1, p2, p3, p4);
            
            (startX, startY) = (res.Width - curveWidth - pW, 0);
            (endX, endY) = (res.Width - curveWidth - pW, res.Height);
            p1 = new(startX, startY);
            p2 = new(startX + curveWidth, startY + curveWidth);
            p3 = new(endX + curveWidth, endY - curveWidth);
            p4 = new(endX, endY);
            g.DrawBezier(pen, p1, p2, p3, p4);
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle? emptyTupleScaled = null;
    private static BM_Middle TupleExpression(TupleExpression ast) {
        if(ast.Items.Count == 0){
            if(emptyTupleScaled is BM_Middle bm && bm.Img.Height == txtHeight + 15) {
                return bm;
            }
            Bitmap emptyTuple = new(emptyTupleImg, emptyTupleImg.Width / (emptyTupleImg.Height / (txtHeight + 15)), txtHeight + 15);
            Bitmap padded = new(emptyTuple.Width + 5, emptyTuple.Height);
            using(var pg = Graphics.FromImage(padded)) {
                pg.DrawImage(emptyTuple, 5, 0);
            }
            emptyTupleScaled = new(padded, padded.Height / 2);
            return (BM_Middle)emptyTupleScaled;
        }
        int gap = 5;
        int lineLen = 5;
        LinkedList<BM_Middle> resses = new();
        var (width, height) = (lineLen + gap, 0);
        foreach(var statement in ast.Items) {
            BM_Middle img = MakeImg(statement);
            resses.AddLast(img);
            width += lineLen + img.Img.Width + 2 * gap;
            height = Max(height, img.Img.Height);
        }
        int resH = height + (lineLen + gap) * 2;
        int curveWidth = (resH)/4;
        Bitmap res = new(width + curveWidth * 2, resH);
        int pW = (int)lblueP.Width/2;
        using(var g = Graphics.FromImage(res)) {
            var end = curveWidth;
            foreach(var (img, _) in resses) {
                end += gap;
                if(!img.Equals(resses.First!.Value.Img)){
                    g.DrawLine(lblueP, end + pW, 5, end + pW, res.Height - 5);
                }
                end += lineLen + gap;
                g.DrawImage(img, end, res.Height / 2 - img.Height / 2);
                end += img.Width;
            }
            g.DrawLine(lblueP,  curveWidth, 5, res.Width - curveWidth + pW, 5);
            g.DrawLine(lblueP, curveWidth, res.Height - 5, res.Width - curveWidth + pW, res.Height - 5);
            
            // ? drawing the curves
            g.SmoothingMode = SmoothingMode.HighQuality;
            var (startX, startY) = (curveWidth, 5);
            var (endX, endY) = (curveWidth, res.Height-5);
            Point p1 = new(startX, startY);
            Point p2 = new(startX - curveWidth, startY + curveWidth);
            Point p3 = new(endX - curveWidth, endY - curveWidth);
            Point p4 = new(endX, endY);
            g.DrawBezier(lblueP, p1, p2, p3, p4);
            
            (startX, startY) = (res.Width - curveWidth, 5);
            (endX, endY) = (res.Width - curveWidth, res.Height-5);
            
            p1 = new(startX, startY);
            p2 = new(startX + curveWidth, startY + curveWidth);
            p3 = new(endX + curveWidth, endY - curveWidth);
            p4 = new(endX, endY);
            g.DrawBezier(lblueP, p1, p2, p3, p4);
            
            g.SmoothingMode = SmoothingMode.Default;
            int size = Math.Clamp((int)(curveWidth*1.5f), txtHeight, txtHeight*2);
            Bitmap padlock = new(lockImg, size, size);
            g.DrawImage(padlock, 0, res.Height-padlock.Height);
        }
        return new(res, res.Height / 2);
    }
    
    //todo cleanup
    private static BM_Middle AssignmentStatement(AssignmentStatement ast) {
        LinkedList<Bitmap> assignmentNames = new();
        var (h1, w1) = (0, 0);
        int lineWidth = 5;
        int gap = 5;

        if(ast.Left.Count != 1) {
            throw new Exception("It's not len 1??");
        }
        IList<Expression> leftVal;
        if(ast.Left[0] is TupleExpression tuple) {
            leftVal = tuple.Items;
        } else {
            leftVal = new List<Expression> { ast.Left[0] };
        }
        foreach(var item in leftVal) {
            Bitmap img = MakeImg(item).Img;
            assignmentNames.AddLast(img);
            if(h1 != 0) { h1 += gap; }
            h1 += img.Height;
            w1 = Max(w1, img.Width);
        }
        Bitmap valueName = MakeImg(ast.Right).Img;
        var (h2, w2) = (valueName.Height, valueName.Width);

        var height = Max(h1, h2) + lineWidth * 2 + gap * 2;
        var width = w1 + w2 + lineWidth * 3 + gap * 4;
        Bitmap res = new(width, height);

        using(var g = Graphics.FromImage(res)){
            var end = (height - h1) / 2  - lineWidth;
            foreach(var item in assignmentNames) {
                g.DrawImage(
                    item,
                    x: lineWidth + gap,
                    y: end,
                    item.Width, item.Height
                );
                end += item.Height + gap;
            }

            g.DrawImage(
                image: valueName,
                x: w1 + lineWidth * 2 + gap * 3,
                y: (height - h2) / 2 - lineWidth,
                valueName.Width, valueName.Height
            );

            g.DrawRectangle(
                new(Color.WhiteSmoke, lineWidth), 0, 0,
                width: w1 + gap * 2 + lineWidth * 2,
                height: height - lineWidth * 2
            );
            g.DrawRectangle(
                new(Color.WhiteSmoke, lineWidth), 0, 0,
                width: w1 + w2 + gap * 4 + lineWidth * 3,
                height: height - lineWidth * 2
            );
        }
        return new(res, res.Height / 2);
    }
    private static BM_Middle AugmentedAssignStatement(AugmentedAssignStatement ast) {
        int lineWidth = 4, gap = 4;
        string op = PythonOperatorToString(ast.Operator);
        int opHeight = MeasureHeight(op, boldFont);
        int opWidth = MeasureWidth(op, boldFont);
        var leftImg = MakeImg(ast.Left).Img;
        var h1 = leftImg.Height;
        var w1 = leftImg.Width;
        
        Bitmap valueName = MakeImg(ast.Right).Img;
        var (h2, w2) = (valueName.Height, valueName.Width);

        var height = Max(h1, h2) + lineWidth * 2 + gap * 2;
        var width = w1 + w2 + lineWidth * 3 + gap * 4 + opWidth;
        Bitmap res = new(width, height);

        using(var g = Graphics.FromImage(res)){
            g.DrawRectangle(
                new Pen(Color.WhiteSmoke, lineWidth), 
                lineWidth/2, lineWidth/2,
                width: w1 + gap * 2 + lineWidth + opWidth/2,
                height: height - lineWidth
            );

            int opX = w1 + gap * 2 + lineWidth;
            int opY = res.Height / 2 - opHeight / 2;
            g.FillRectangle(blackBrush, opX, opY, opWidth, opHeight);
            g.DrawString(op, boldFont, mathPurpleB, opX, opY);

            g.DrawRectangle(
                new Pen(Color.WhiteSmoke, lineWidth), 
                lineWidth/2, lineWidth/2,
                width: res.Width - lineWidth,
                height: res.Height - lineWidth
            );
            g.DrawImage(
                leftImg,
                lineWidth + gap,
                res.Height / 2 - leftImg.Height / 2,
                leftImg.Width, leftImg.Height
            );

            g.DrawImage(
                image: valueName,
                x: w1 + lineWidth + gap * 3 + opWidth,
                y: lineWidth + gap,
                width: valueName.Width, height: valueName.Height
            );

        }
        return new(res, res.Height / 2);
    }
}