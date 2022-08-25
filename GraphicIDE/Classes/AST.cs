using System.Text;

using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.MyImages;
using static GraphicIDE.MyMath;


namespace GraphicIDE;

public static class AST {
    public static BM_Middle? MakeImg(dynamic ast) {
        try {
            return ((Func<BM_Middle?>)(ast.NodeName switch {
                "PythonAst" => () => MainModule(ast),
                "SuiteStatement" => () => SuiteStatement(ast),
                "ExpressionStatement" => () => MakeImg(ast.Expression),
                "PrintStatement" => () => PrintStatement(ast),
                "ParenthesisExpression" => () => ParenthesisExpression(ast),
                "literal" => () => Literal(ast),
                "TupleExpression" => () => TupleExpression(ast),
                "NameExpression" => () => MakeTxtBM(ast.Name.ToString(), brush: lblueBrush),
                "AssignmentStatement" => () => AssignmentStatement(ast),
                "operator" => () => Operator(ast),
                "function call" => () => FunctionCall(ast),
                "comparison" => () => Comparison(ast),
                "UnaryExpression" => () => UnaryExpression(ast),
                "IfStatement" => () => IfExpression(ast),
                "WhileStatement" => () => WhileStatement(ast),
                "EmptyStatement" => () => EmptyStatement(),
                "ListExpression" => () => ListExpression(ast),
                "AndExpression" => () => AndExpression(ast),
                "OrExpression" => () => OrExpression(ast),
                "ImportStatement" => () => ImportStatement(ast),
                "AugmentedAssignStatement" => () => AugmentedAssignStatement(ast),
                "FunctionDefinition" => () => MainModule(ast),
                "ReturnStatement" => () => ReturnStatement(ast),
                "None" => () => None(ast),
                "list comprehension" => () => ListComprehension(ast),
                "set comprehension" => () => SetComprehension(ast),
                "dict comprehension" => () => DictionaryComprehension(ast),
                "ForStatement" => () => ForStatement(ast),
                _ => () => null
            }))();
        } catch(Exception) { 
            return null;
        }
    }
    private static BM_Middle Comprehension(dynamic ast, string open, string close, Brush brush, Color bg, Bitmap? ditem=null){
        int indent = indentW / 2;
        Bitmap item = ditem ?? MakeImg(ast.Item).Img;
        LinkedList<Bitmap> iterPics = new();
        int totalHeight = 0, totalWidth = 0;
        foreach(var iter in ast.Iterators) {
            if("ComprehensionFor".Equals(iter.NodeName)){
                // todo make for image
                Bitmap bm = ForTopPart(iter);
                totalHeight += bm.Height;
                totalWidth = Max(totalWidth, bm.Width + indent);
                iterPics.AddLast(bm);
            } else if("ComprehensionIf".Equals(iter.NodeName)){
                Bitmap condition = MakeImg(iter.Test).Img;
                Bitmap bm = new(condition.Width + qWidth, condition.Height);
                totalHeight += bm.Height;
                totalWidth = Max(totalWidth, bm.Width + indent);
                using(var g = Graphics.FromImage(bm)){
                    g.DrawString("¿", boldFont, keyOrangeB, 0, (int)(bm.Height / 2 - qHeight / 2));
                    g.DrawImage(condition, upSideDownW, 0);
                    g.DrawString(
                        "?", boldFont, keyOrangeB, 
                        condition.Width + upSideDownW, 
                        (int)(bm.Height / 2 - qHeight / 2)
                    );
                }
                iterPics.AddLast(bm);
            } else {
                throw new("What is this comprehension iterator?");
            }
        }
        int bracketHieght = txtHeight * 2;
        Bitmap res = new(totalWidth, totalHeight + bracketHieght + item.Height);
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
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle ListComprehension(dynamic ast){
        return Comprehension(ast, "[", "]", keyOrangeB, opaqekeyOrange);
    }
    private static BM_Middle SetComprehension(dynamic ast){
        return Comprehension(ast, "{", "}", mathPurpleB, opaqeMathPurple);
    }
    private static BM_Middle DictionaryComprehension(dynamic ast){
        Bitmap key = MakeImg(ast.Key).Img;
        Bitmap val = MakeImg(ast.Value).Img;
        int colW = MeasureWidth(":", boldFont);
        Bitmap item = new(key.Width + val.Width + colW, Max(key.Height, val.Height));
        using(var g = Graphics.FromImage(item)){
            g.DrawImage(key, 0, 0);
            g.DrawString(":", boldFont, mathPurpleB, key.Width, 0);
            g.DrawImage(val, key.Width + colW, 0);
        }
        return Comprehension(ast, "{", "}", forBlueB, opaqeForBlue, ditem: item);
    }
    private static BM_Middle? scaledNone;
    private static BM_Middle None(dynamic ast){
        if(scaledNone is null){
            scaledNone = new(new(noneImg, txtHeight, txtHeight), (int)(txtHeight/2));
        }
        return (BM_Middle)scaledNone;
    }
    private static Bitmap? scaledReturn;
    private static BM_Middle ReturnStatement(dynamic ast){
        var val = MakeImg(ast.Expression).Img;
        if(scaledReturn is null){
            scaledReturn = new(returnImg, returnImg.Width / (returnImg.Height / txtHeight), txtHeight);
        }

        Bitmap res = new(val.Width + scaledReturn.Width, Max(val.Height, scaledReturn.Height));
        using(var g = Graphics.FromImage(res)){
            g.DrawImage(val, res.Width - val.Width, (int)(res.Height/2-val.Height/2));
            g.DrawImage(scaledReturn, 0, (int)(res.Height/2-scaledReturn.Height/2));
        }
        return new(res, (int)res.Height/2);
    }
    private static BM_Middle ImportStatement(dynamic ast){
        string name = ast.Names[0].Names[0];
        int width = MeasureWidth(name, boldFont);
        int gap = 6;
        Bitmap truck = new(truckImg, txtHeight + gap * 2, txtHeight + gap * 2);
        Bitmap res = new(gap * 2 + width + truck.Width, truck.Height + 8);
        using(var g = Graphics.FromImage(res)){
            g.FillRectangle(truckIBrush, 4, 6, res.Width - truck.Width - 6, res.Height - 4 - 8);
            g.DrawRectangle(truckP, 2, 6, res.Width - truck.Width - 4, res.Height - 4 - 8);
            g.DrawString(name, boldFont, blackBrush, gap, gap * 2);
            g.DrawImage(truck, res.Width - truck.Width, gap);
        }
        return new(res, res.Height / 2);

    }
    private static BM_Middle AndExpression(dynamic ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int andWidth = MeasureWidth("and", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + andWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, andWidth + 10, 10);
            g.DrawImage(r.Img, andWidth + 10, l.Img.Height + 20);
            int andPosY = (int)(l.Img.Height - txtHeight / 2);
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
    private static BM_Middle OrExpression(dynamic ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int orWidth = MeasureWidth("or", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + orWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, orWidth + 10, 10);
            g.DrawImage(r.Img, orWidth + 10, l.Img.Height + 20);
            int andPosY = (int)(l.Img.Height - txtHeight / 2);
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
    private static BM_Middle ListExpression(dynamic ast) {
        if(ast.Items.Length == 0) {
            if(emptyListScaled is BM_Middle bm && bm.Img.Height == txtHeight + 15) {
                return bm;
            }
            Bitmap emptyList = new(emptyListImg, (int)(emptyListImg.Width / (emptyListImg.Height / (txtHeight + 15))), txtHeight + 15);
            Bitmap padded = new(emptyList.Width + 5, emptyList.Height);
            using(var pg = Graphics.FromImage(padded)) {
                pg.DrawImage(emptyList, 5, 0);
            }
            emptyListScaled = new(padded, (int)(padded.Height / 2));
            return (BM_Middle)emptyListScaled;
        }
        int lineLen = 5;
        int gap = 5;
        LinkedList<BM_Middle> elements = new();
        var (width, heightT, heightB) = (lineLen + gap, 0, 0);
        foreach(var item in ast.Items) {
            var img = MakeImg(item);
            var middle = img.Middle;
            elements.AddLast(img);
            width += lineLen + img.Img.Width + 2 * gap;
            heightT = Max(heightT, middle);
            heightB = Max(heightB, img.Img.Height - middle);
        }
        Bitmap res = new(width, heightB + heightT + 20);
        using(var g = Graphics.FromImage(res)) {
            var end = 2;
            foreach(var (img, _) in elements) {
                end += gap;
                g.DrawLine(redListP, end, 5, end, res.Height - 5);
                end += lineLen;
                g.DrawImage(img, end, (int)(res.Height / 2 - img.Height / 2));
                end += img.Width + gap;
            }
            end += gap;
            g.DrawLine(redListP, end, 5, end, res.Height - 5);
            g.DrawLine(redListP, 5, 5, res.Width, 5);
            g.DrawLine(redListP, 5, res.Height - 5, res.Width, res.Height - 5);
        }
        return new(res, (int)(res.Height / 2));

    }
    private static BM_Middle? passPic = null;
    private static BM_Middle EmptyStatement() {
        if(passPic is BM_Middle bm && bm.Img.Height == txtHeight) {   return bm; }
        Bitmap img = new(passImg, (int)(passImg.Width / (passImg.Height / txtHeight)), txtHeight);
        passPic = new(img, (int)(img.Height / 2));
        return (BM_Middle)passPic;
    }
    private static Bitmap ForTopPart(dynamic ast){
        Bitmap var = MakeImg(ast.Left).Img;
        Bitmap iter = MakeImg(ast.List).Img;
        int forW = MeasureWidth("for ", boldFont);
        int inW = MeasureWidth(" in ", boldFont);
        int iterHeight = Max(Max(var.Height, iter.Height), txtHeight);
        Bitmap condition = new(var.Width + iter.Width + forW + inW, iterHeight);
        // todo center the var and iter?
        using(var g = Graphics.FromImage(condition)){
            g.DrawString("for", boldFont, keyOrangeB, 0, 0);
            int end = forW;
            g.DrawImage(var, end, 0);
            end += var.Width;
            g.DrawString(" in", boldFont, mathPurpleB, end, 0);
            end += inW;
            g.DrawImage(iter, end, 0);
        }
        return condition;
    }
    private static BM_Middle ForStatement(dynamic ast){
        Bitmap body = MakeImg(ast.Body).Img;
        Bitmap condition = ForTopPart(ast);
        return WhileAndFor(condition, body, forBlueP, forArrowUpImg, forArrowDownImg);
        // todo else
    }
    private static BM_Middle WhileStatement(dynamic ast){
        Bitmap condition = MakeImg(ast.Test).Img;
        Bitmap body = MakeImg(ast.Body).Img;
        return WhileAndFor(condition, body, whileOrangeP, whileArrowUpImg, whileArrowDownImg);
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
        return new(res, (int)(res.Height / 2));
    }

    private static BM_Middle IfExpression(dynamic ast) {
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
                height: condition.Height + body.Height + 5
            );
            var g = Graphics.FromImage(res);
            g.DrawString("¿", boldFont, keyOrangeB, 0, (int)(condition.Height / 2 - qHeight / 2));
            g.DrawImage(condition, upSideDownW, 0);
            g.DrawString(
                "?", boldFont, keyOrangeB, 
                condition.Width + upSideDownW, 
                (int)(condition.Height / 2 - qHeight / 2)
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
        resG.DrawLine(lastColor, 4, 1, res.Width, 1);

        if(ast.Tests.Length > 1) {
            lastColor = orangeDashed;
            for(int i = 1; i < ast.Tests.Length; i++) {
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
        return new(addPad, (int)(res.Height/2));
    }
    private static BM_Middle? FunctionCall(dynamic ast) {
        if(ast.Target.Name.Equals("sqrt") && ast.Target.Target.Name.Equals("math")) {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap res = new(
                width: inside.Width + 30, 
                height: inside.Height + 15
            );
            using(var g = Graphics.FromImage(res)) {
                g.DrawImage(inside, 20, 15);
                g.DrawLine(mathPurpleP, 15, 5, res.Width, 5);
                g.DrawLine(mathPurpleP, 15, 5, 15, res.Height);
                g.DrawLine(mathPurpleP, 5, res.Height - 15, 15, res.Height);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name.Equals("sum")) {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap sum = new(sumImg, sumImg.Width / (sumImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + sum.Width + 10,
                height: inside.Height
            );
            using(var g = Graphics.FromImage(res)) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 255, 255)), 0, 0, res.Width, res.Height);
                g.DrawImage(sum, 5, 0);
                g.DrawImage(inside, sum.Width + 5, 0);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name.Equals("len")) {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap ruler = new(rulerImg, rulerImg.Width / (rulerImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + ruler.Width + 10,
                height: inside.Height
            );
            using(var g = Graphics.FromImage(res)) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 215, 206, 180)), 5 + ruler.Width, 0, res.Width, res.Height);
                g.DrawImage(ruler, 5, 0);
                g.DrawImage(inside, ruler.Width + 5, 0);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name.Equals("abs")) {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap res = new(
                width: inside.Width + 30,
                height: inside.Height + 10
            );
            using(var g = Graphics.FromImage(res)) {
                g.DrawLine(mathPurpleP, 10, 0, 10, res.Height);
                g.DrawLine(mathPurpleP, res.Width - 5, 0, res.Width - 5, res.Height);
                g.DrawImage(inside, 15, 5);
            }
            return new(res, (int)(res.Height / 2));
        }
        return null;
    }
    private static BM_Middle UnaryExpression(dynamic ast) {
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
            g.DrawString(op, boldFont, mathPurpleB, x: gap, y: (int)(middle - opHeight / 2));
            g.DrawImage(bmap, x: opWidth + gap, y: (int)(middle - top));
        }
        return new(res, middle);
    }
    private static BM_Middle Comparison(dynamic ast) {
        var op = PythonOperatorToString(ast.Operator);
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Img, l.Middle);
        var r = MakeImg(ast.Right);
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
            g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
            g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
            g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
        }
        return new(res, resMiddle);
    }
    private static BM_Middle Operator(dynamic ast) {
        #region OperatorSwitch
        var op = PythonOperatorToString(ast.Operator);
        #endregion
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Img, l.Middle);
        var r = MakeImg(ast.Right);
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
            g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
            g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
            g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
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
        return new(res, res.Height - (int)(bottom.Img.Height / 2));
    }
    private static BM_Middle FloorDivide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 22
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawLine(mathPurpleP, 5, top.Height + 11, res.Width, top.Height + 11);
            g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 22);
        }
        return new(res, top.Height + 11);
    }
    private static BM_Middle Divide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 15
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 15);
        }
        return new(res, top.Height + 7);
    }
    private static BM_Middle Literal(dynamic ast) =>
        MakeTxtBM(ast.Type.Name.Equals("String") ? $"'{ast.Value}'": ast.Value.ToString(), 
            ast.Type.Name switch {
                "String" => stringBrush,
                "Int32" or "Double" or "BigInteger" => intBrush,
                _ => textBrush
            }
        );
    private static BM_Middle SuiteStatement(dynamic ast) {
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
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle MainModule(dynamic ast) {
        LinkedList<Bitmap> resses = new();
        var (height, width) = (0, 0);
        var statements = ast.Body.Statements;
        for(int j = 0; j < statements.Length; j++) {
            var statement = statements[j];
            (int line, int col) startI, endI;
            if(j == 0) {
                startI = (0, 0);
            } else {
                var start = statements[j - 1].End;
                startI = (start.Line - 1, start.Column - 1);
            }
            if(j == statements.Length - 1) {
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
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle PrintStatement(dynamic ast) {
        LinkedList<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Expressions) {
            if(statement.NodeName.Equals("TupleExpression")) {
                resses.AddLast(MakeImg(statement).Img);
            } else {
                resses.AddLast(MakeImg(statement.Expression).Img);
            }
            width += resses.Last!.Value.Width;
            height = Max(height, resses.Last!.Value.Height);
        }
        var printer = new Bitmap(printerImg, new Size(printerImg.Width / (printerImg.Height / (height + 20)), height + 20));
        var res = new Bitmap(width + printer.Width + 20, printer.Height);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(printer, 0, 0);
            var end = printer.Width;
            g.DrawRectangle(new Pen(Color.White, 5), end, 2.5f, res.Width - printer.Width - 10, res.Height - 5);

            end += 5;
            foreach(var item in resses) {
                g.DrawImage(item, end, 12);
                end += item.Width;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle ParenthesisExpression(dynamic ast) {
        Bitmap inside = MakeImg(ast.Expression).Img;
        var width = inside.Width;
        var height = inside.Height;
        var parHeight = height + 10;
        var parWidth = parHeight / 5;
        Bitmap res = new(
            width + 20 + (parWidth + 5) * 2,
            height + 20
        );

        using(var g = Graphics.FromImage(res)) {
            var end = 10;
            g.DrawArc(mathPurpleP, new Rectangle(end, 5, parWidth, parHeight), 90, 180);
            end += parWidth + 5;
            g.DrawImage(inside, end, 10);
            g.DrawArc(mathPurpleP, new Rectangle(
                end + width + 5, 5, parWidth, parHeight
            ), 270, 180);
        }
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle TupleExpression(dynamic ast) {
        if(ast.Items.Length == 0){
            return new(new(1, txtHeight), (int)(txtHeight/2));
        }
        LinkedList<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Items) {
            resses.AddLast(MakeImg(statement).Img);
            width += resses.Last!.Value.Width;
            height = Max(height, resses.Last!.Value.Height);
        }
        int commaLen = MeasureWidth(",", boldFont);
        var res = new Bitmap(width + commaLen * (resses.Count - 1), height);

        using(var g = Graphics.FromImage(res)) {
            bool putComma = false;
            var end = 0;
            foreach(var item in resses) {
                if(putComma) {
                    g.DrawString(",", boldFont, textBrush, end, 0);
                    end += commaLen;
                } else { putComma = true; }
                g.DrawImage(item, end, 0);
                end += item.Width;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    //todo cleanup
    private static BM_Middle AssignmentStatement(dynamic ast) {
        LinkedList<Bitmap> assignmentNames = new();
        var (h1, w1) = (0, 0);
        int lineWidth = 5;
        int gap = 5;

        if(ast.Left.Length != 1) {
            throw new Exception("It's not len 1??");
        }
        var leftVal = ast.Left[0];
        if(leftVal.NodeName.Equals("TupleExpression")) {
            leftVal = leftVal.Items;
        } else {
            leftVal = new List<dynamic> { leftVal };
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
        return new(res, (int)(res.Height / 2));
    }
    private static BM_Middle AugmentedAssignStatement(dynamic ast) {
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
                width: (int)(w1 + gap * 2 + lineWidth + opWidth/2),
                height: height - lineWidth
            );

            int opX = (int)(w1 + gap * 2 + lineWidth);
            int opY = (int)(res.Height / 2 - opHeight / 2);
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
                lineWidth + gap,
                leftImg.Width, leftImg.Height
            );

            g.DrawImage(
                image: valueName,
                x: w1 + lineWidth + gap * 3 + opWidth,
                y: lineWidth + gap,
                width: valueName.Width, height: valueName.Height
            );

        }
        return new(res, (int)(res.Height / 2));
    }
}