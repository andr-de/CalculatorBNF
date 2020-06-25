using System;
using System.Collections.Generic;
using System.Text;

namespace CalculatorBack
{
    public class ExpressionProcessor
    {
        private int tokenNumber = 0;
        private List<string> tokens = new List<string>();
        private List<ErrorClass> errors = new List<ErrorClass>();
        private int positionInExpression = 0;
        public string Expr { get; private set; }
        public double Result { get; private set; }
        public int FirstInfPosition { get; private set; }
        public int FirstNaNPosition { get; private set; }
        
        public double ComputeExpression(string expression)
        {
            expression = String.Concat("(", expression, ")");
            ClearInfo();
            Tokenize(expression);
            BracketsCheck();
            if (errors.Count == 0)
                Parse();
            else 
                return Double.NaN;
            return Result;
        }
        
        private void Tokenize(string expression)
        {
            Expr = expression;
            var i = 0;
            var expressionLength = expression.Length;

            while (i < expressionLength)
            {
                var currentSymbol = expression[i];
                if ((currentSymbol == ')') || (currentSymbol == '('))
                {
                    tokens.Add(currentSymbol.ToString());
                    i++;
                }
                else if (IsOperand(currentSymbol))
                {
                    // Символ опирации считается некорректным, если идёт после открывающей скобки и не является минусом,
                    // если идёт перед закрывающей скобкой
                    // или если идёт сразу после другого символа операции.
                    if (((expression[i - 1] == '(') && (currentSymbol != '-')) ||
                        expression[i + 1] == ')' ||
                        IsOperand(expression[i - 1]))
                    {
                        errors.Add(new ErrorClass(ErrorType.UnexpectedOperator, currentSymbol, i));
                    }
                    tokens.Add(currentSymbol.ToString());
                    i++;
                }
                else if (char.IsDigit(currentSymbol))
                {
                    var pointed = false;
                    var tempDouble = new StringBuilder();
                    while (char.IsDigit(expression[i]) || (expression[i] == ','))
                    {
                        if (expression[i] == ',')
                            if (!pointed)
                                pointed = true;
                            else
                                errors.Add(new ErrorClass(ErrorType.SecondPoint, i));
                        tempDouble.Append(expression[i]);
                        i++;
                        if (i >= expressionLength)
                            break;
                    }
                    tokens.Add(tempDouble.ToString());
                }
                else
                {
                    errors.Add(new ErrorClass(ErrorType.UnsopportedSymbol, currentSymbol, i));
                    i++;
                }
            }
        }

        private void Parse()
        {
            var res = Expression();
            if (tokenNumber != tokens.Count)
                errors.Add(new ErrorClass(ErrorType.UnknownError, positionInExpression));
            Result = res;
        }

        private double Expression()
        {
            var left = Term();
            var currentPositionInExpression = positionInExpression;
            while (tokenNumber < tokens.Count)
            {
                var oper = tokens[tokenNumber];
                if ((oper != "+") && (oper != "-"))
                    break;
                
                tokenNumber++;
                positionInExpression += oper.Length;

                var right = Term();
                if (oper == "+")
                    left += right;
                else
                    left -= right;
            }
            CheckIfNanOrInf(left, currentPositionInExpression);
            return left;
        }

        private double Term()
        {
            var left = SignedFactor();
            var currentPositionInExpression = positionInExpression;
            while (tokenNumber < tokens.Count)
            {
                var oper = tokens[tokenNumber];
                if ((oper != "*") && (oper != "/"))
                    break;
                tokenNumber++;
                positionInExpression += oper.Length;
                var right = SignedFactor();
                if (oper == "*")
                    left *= right;
                else
                    left /= right;
            }
            CheckIfNanOrInf(left, currentPositionInExpression);
            return left;
        }

        private double SignedFactor()
        {
            var currentPositionInExpression = positionInExpression;
            var token = tokens[tokenNumber];
            double res;
            switch (token)
            {
                case "-":
                    tokenNumber++;
                    positionInExpression += token.Length;
                    res = -Factor();
                    break;
                case "+":
                    tokenNumber++;
                    positionInExpression += token.Length;
                    res = Factor();
                    break;
                default:
                    res = Factor();
                    break;
            }
            CheckIfNanOrInf(res, currentPositionInExpression);
            return res;
        }

        private double Factor()
        {
            var next = tokens[tokenNumber];
            double res;
            if (next == "(") {
                tokenNumber++;
                positionInExpression += next.Length;
                res = Expression();
                if ((tokenNumber < tokens.Count) && (tokens[tokenNumber] == ")")) {
                    tokenNumber++;
                    positionInExpression += next.Length;
                    CheckIfNanOrInf(res, positionInExpression);
                } else {
                    errors.Add(new ErrorClass(ErrorType.UnknownError, positionInExpression));
                    return Double.NaN;
                }
            }

            int currentPositionInExpression = positionInExpression;
            tokenNumber++;
            positionInExpression += next.Length;
            if (Double.TryParse(next, out res))
            {
                CheckIfNanOrInf(res, currentPositionInExpression);
                return res;
            }
            errors.Add(new ErrorClass(ErrorType.UnexpectedOperator, next[0], currentPositionInExpression));
            return Double.NaN;
        }

        private static bool IsOperand(char c)
        {
            return (c == '+') || (c == '-') || (c == '*') || (c == '/');
        }
        private void CheckIfNanOrInf(double res, int pos)
        {
            if (Double.IsInfinity(res) && (FirstInfPosition == -1))
                FirstInfPosition = pos;
            else if (Double.IsNaN(res) && (FirstNaNPosition == -1))
                FirstNaNPosition = pos;
        }

        public int GetErrorSize()
        {
            return errors.Count;
        }
        
        public string CombineErrors()
        {
            var res = new StringBuilder();
            foreach (var i in errors)
                res.Append(i.PrintError());
            return res.ToString();
        }

        public ErrorClass GetError(int pos)
        {
            if (pos < errors.Count)
                return errors[pos];
            return null;
        }

        private void ClearInfo()
        {
            errors.Clear();
            tokens.Clear();
            FirstInfPosition = -1;
            FirstNaNPosition = -1;
            tokenNumber = 0;
            positionInExpression = 0;
            Expr = String.Empty;
            Result = 0;
        }

        private void BracketsCheck()
        {
            // -1 так как нужно проигнорировать добавленную нами первую скобку, 
            // чтобы не ломалось позиционирование ошибки закрывающих скобок
            var openBracketsCounter = -1;
            var closeBracketsCounter = 0;
            var isBracketErrorReported = false;
            var currentPositionInExpression = 0;
            var i = 0;
            while (i < tokens.Count)
            {
                var currentToken = tokens[i];
                switch (currentToken)
                {
                    case "(":
                        openBracketsCounter++;
                        break;
                    case ")":
                    {
                        closeBracketsCounter++;
                        if ((closeBracketsCounter > openBracketsCounter) && (!isBracketErrorReported) && (i != tokens.Count - 1))
                        {
                            errors.Add(new ErrorClass(ErrorType.ClosingBrakets, currentPositionInExpression));
                            isBracketErrorReported = true;
                        }

                        break;
                    }
                }
                currentPositionInExpression += currentToken.Length;
                i++;
            }
            if (((openBracketsCounter + 1) > closeBracketsCounter) && (!isBracketErrorReported))
                errors.Add(new ErrorClass(ErrorType.OpeningBrakets, openBracketsCounter + 1 - closeBracketsCounter));
        }
    }
}