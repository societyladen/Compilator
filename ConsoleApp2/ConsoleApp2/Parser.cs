using System;
using System.Collections.Generic;

namespace Компилятор
{
    public class Parser
    {
        private LexicalAnalyzer _lexer;
        private SymbolTable _symbols;
        private byte _current;

        private static HashSet<byte> _statementStartSymbols;
        private static HashSet<byte> _stopSymbols;

        static Parser()
        {
            _statementStartSymbols = new HashSet<byte>
            {
                LexicalAnalyzer.Ident,
                LexicalAnalyzer.BeginSy,
                LexicalAnalyzer.WhileSy,
                LexicalAnalyzer.RepeatSy,
                LexicalAnalyzer.ForSy,
                LexicalAnalyzer.WritelnSy,
                LexicalAnalyzer.IfSy,
            };

            _stopSymbols = new HashSet<byte>
            {
                LexicalAnalyzer.Semicolon,
                LexicalAnalyzer.EndSy,
                LexicalAnalyzer.Eof,
                LexicalAnalyzer.Point,
            };
        }

        public Parser(LexicalAnalyzer lex)
        {
            _lexer = lex;
            _symbols = new SymbolTable();
            _current = 0;
            Advance();
        }

        private void Advance()
        {
            _current = _lexer.NextSym();
        }

        private bool Match(byte expected)
        {
            if (_current == expected)
            {
                Advance();
                return true;
            }
            InputOutput.Error(104, _lexer.Token);
            return false;
        }

        private bool MatchColon()
        {
            if (_current == LexicalAnalyzer.Colon)
            {
                Advance();
                return true;
            }
            InputOutput.Error(108, _lexer.Token);
            return false;
        }

        private bool MatchAssign()
        {
            if (_current == LexicalAnalyzer.Assign)
            {
                Advance();
                return true;
            }
            InputOutput.Error(109, _lexer.Token);
            return false;
        }

        private void SkipTo(HashSet<byte> stop)
        {
            while (!stop.Contains(_current) && _current != LexicalAnalyzer.Eof)
            {
                Advance();
            }
        }

        private void SkipToStatementEnd()
        {
            SkipTo(_stopSymbols);
        }

        public void Parse()
        {
            Program();
        }

        private void Program()
        {
            if (!Match(LexicalAnalyzer.ProgramSy))
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.Ident,
                    LexicalAnalyzer.Semicolon,
                    LexicalAnalyzer.VarSy,
                    LexicalAnalyzer.BeginSy
                });
            }

            if (_current == LexicalAnalyzer.Ident)
            {
                Advance();
            }
            else
            {
                InputOutput.Error(104, _lexer.Token);
            }

            if (!Match(LexicalAnalyzer.Semicolon))
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.VarSy,
                    LexicalAnalyzer.BeginSy,
                    LexicalAnalyzer.Eof
                });
            }

            if (_current == LexicalAnalyzer.VarSy)
            {
                VarDeclarations();
            }

            if (!Match(LexicalAnalyzer.BeginSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> { LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof });
                if (_current == LexicalAnalyzer.BeginSy)
                {
                    Advance();
                }
            }

            StatementSequence();

            if (!Match(LexicalAnalyzer.EndSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> { LexicalAnalyzer.Point, LexicalAnalyzer.Eof });
            }

            Match(LexicalAnalyzer.Point);
        }

        private void VarDeclarations()
        {
            Match(LexicalAnalyzer.VarSy);

            while (_current == LexicalAnalyzer.Ident)
            {
                TextPosition declPos = _lexer.Token;
                List<string> names = IdentifierList();

                if (!MatchColon())
                {
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.IntegerSy,
                        LexicalAnalyzer.RealSy,
                        LexicalAnalyzer.BooleanSy,
                        LexicalAnalyzer.CharSy,
                        LexicalAnalyzer.Semicolon,
                        LexicalAnalyzer.BeginSy
                    });
                }

                string type = GetCurrentType();
                if (type == "unknown")
                {
                    InputOutput.Error(105, _lexer.Token);
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.Semicolon,
                        LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.Eof
                    });
                }
                else
                {
                    Advance();
                }

                if (!Match(LexicalAnalyzer.Semicolon))
                {
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.Ident,
                        LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.Eof
                    });
                }

                if (type != "unknown")
                {
                    foreach (string name in names)
                    {
                        if (!_symbols.Add(name, type, declPos.LineNumber))
                        {
                            InputOutput.Error(204, declPos);
                        }
                    }
                }
            }
        }

        private string GetCurrentType()
        {
            if (_current == LexicalAnalyzer.IntegerSy)
            {
                return "integer";
            }
            if (_current == LexicalAnalyzer.RealSy)
            {
                return "real";
            }
            if (_current == LexicalAnalyzer.BooleanSy)
            {
                return "boolean";
            }
            if (_current == LexicalAnalyzer.CharSy)
            {
                return "char";
            }
            return "unknown";
        }

        private List<string> IdentifierList()
        {
            var list = new List<string>();

            if (_current == LexicalAnalyzer.Ident)
            {
                list.Add(_lexer.AddrName);
                Advance();
            }
            else
            {
                InputOutput.Error(104, _lexer.Token);
            }

            while (_current == LexicalAnalyzer.Comma)
            {
                Advance();
                if (_current == LexicalAnalyzer.Ident)
                {
                    list.Add(_lexer.AddrName);
                    Advance();
                }
                else
                {
                    InputOutput.Error(104, _lexer.Token);
                }
            }
            return list;
        }

        private void StatementSequence()
        {
            Statement();
            while (_current == LexicalAnalyzer.Semicolon)
            {
                Advance();
                if (_current == LexicalAnalyzer.EndSy || _current == LexicalAnalyzer.UntilSy)
                {
                    break;
                }
                Statement();
            }
        }

        private void Statement()
        {
            switch (_current)
            {
                case LexicalAnalyzer.Ident:
                    {
                        Assignment();
                        break;
                    }
                case LexicalAnalyzer.BeginSy:
                    {
                        CompoundStatement();
                        break;
                    }
                case LexicalAnalyzer.WhileSy:
                    {
                        WhileLoop();
                        break;
                    }
                case LexicalAnalyzer.RepeatSy:
                    {
                        RepeatLoop();
                        break;
                    }
                case LexicalAnalyzer.ForSy:
                    {
                        ForLoop();
                        break;
                    }
                case LexicalAnalyzer.IfSy:
                    {
                        IfStatement();
                        break;
                    }
                case LexicalAnalyzer.WritelnSy:
                    {
                        Writeln();
                        break;
                    }
                default:
                    {
                        if (!_stopSymbols.Contains(_current) && !_statementStartSymbols.Contains(_current))
                        {
                            InputOutput.Error(107, _lexer.Token);
                            SkipToStatementEnd();
                        }
                        break;
                    }
            }
        }

        private void CompoundStatement()
        {
            Match(LexicalAnalyzer.BeginSy);
            StatementSequence();
            if (!Match(LexicalAnalyzer.EndSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Semicolon,
                    LexicalAnalyzer.Point,
                    LexicalAnalyzer.Eof
                });
                if (_current == LexicalAnalyzer.EndSy)
                {
                    Advance();
                }
            }
        }

        private void Assignment()
        {
            string name = _lexer.AddrName;
            TextPosition pos = _lexer.Token;

            if (!_symbols.Exists(name))
            {
                InputOutput.Error(201, pos);
            }

            Advance();

            if (!MatchAssign())
            {
                SkipToStatementEnd();
                return;
            }

            string exprType = Expression();

            if (_symbols.Exists(name))
            {
                string varType = _symbols.GetType(name);
                CheckTypeCompatibility(varType, exprType, pos);
            }
        }

        private void WhileLoop()
        {
            Match(LexicalAnalyzer.WhileSy);
            Condition();

            if (!Match(LexicalAnalyzer.DoSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte>(_statementStartSymbols)
                {
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
                if (!_statementStartSymbols.Contains(_current))
                {
                    return;
                }
            }

            Statement();
        }

        private void RepeatLoop()
        {
            Match(LexicalAnalyzer.RepeatSy);
            StatementSequence();

            if (!Match(LexicalAnalyzer.UntilSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.UntilSy,
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
                if (_current == LexicalAnalyzer.UntilSy)
                {
                    Advance();
                }
                else
                {
                    return;
                }
            }

            Condition();
        }

        private void ForLoop()
        {
            Match(LexicalAnalyzer.ForSy);

            string name = _lexer.AddrName;
            TextPosition pos = _lexer.Token;

            if (_current == LexicalAnalyzer.Ident)
            {
                if (!_symbols.Exists(name))
                {
                    InputOutput.Error(201, pos);
                }

                string varType = _symbols.GetType(name);
                if (varType != "integer" && varType != "char" && varType != "")
                {
                    InputOutput.Error(206, pos);
                }

                Advance();
            }
            else
            {
                InputOutput.Error(104, _lexer.Token);
            }

            if (!MatchAssign())
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.ToSy,
                    LexicalAnalyzer.DownToSy,
                    LexicalAnalyzer.DoSy,
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
            }
            else
            {
                Expression();
            }

            if (_current == LexicalAnalyzer.ToSy || _current == LexicalAnalyzer.DownToSy)
            {
                Advance();
            }
            else
            {
                InputOutput.Error(106, _lexer.Token);
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.DoSy,
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
            }

            Expression();

            if (!Match(LexicalAnalyzer.DoSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte>(_statementStartSymbols)
                {
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
                if (!_statementStartSymbols.Contains(_current))
                {
                    return;
                }
            }

            Statement();
        }

        private void IfStatement()
        {
            Match(LexicalAnalyzer.IfSy);
            Condition();

            if (!Match(LexicalAnalyzer.ThenSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte>(_statementStartSymbols)
                {
                    LexicalAnalyzer.EndSy,
                    LexicalAnalyzer.Eof
                });
                if (!_statementStartSymbols.Contains(_current))
                {
                    return;
                }
            }

            Statement();

            if (_current == LexicalAnalyzer.ElseSy)
            {
                Advance();
                Statement();
            }
        }

        private void Condition()
        {
            Expression();

            if (_current == LexicalAnalyzer.Equal ||
                _current == LexicalAnalyzer.Later ||
                _current == LexicalAnalyzer.Greater ||
                _current == LexicalAnalyzer.LaterEqual ||
                _current == LexicalAnalyzer.GreaterEqual ||
                _current == LexicalAnalyzer.LaterGreater)
            {
                Advance();
                Expression();
            }
        }

        private void Writeln()
        {
            Match(LexicalAnalyzer.WritelnSy);

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance();
                if (_current != LexicalAnalyzer.RightPar)
                {
                    Expression();
                    while (_current == LexicalAnalyzer.Comma)
                    {
                        Advance();
                        Expression();
                    }
                }
                if (!Match(LexicalAnalyzer.RightPar))
                {
                    InputOutput.Error(104, _lexer.Token);
                    SkipToStatementEnd();
                }
            }
        }

        private string Expression()
        {
            string t = Term();
            while (_current == LexicalAnalyzer.Plus || _current == LexicalAnalyzer.Minus)
            {
                Advance();
                string t2 = Term();
                t = MergeNumericTypes(t, t2);
            }
            return t;
        }

        private string Term()
        {
            string t = Factor();
            while (_current == LexicalAnalyzer.Star ||
                   _current == LexicalAnalyzer.Slash ||
                   _current == LexicalAnalyzer.DivSy ||
                   _current == LexicalAnalyzer.ModSy)
            {
                Advance();
                string t2 = Factor();
                t = MergeNumericTypes(t, t2);
            }
            return t;
        }

        private string Factor()
        {
            if (_current == LexicalAnalyzer.Minus)
            {
                Advance();
                return Factor();
            }

            if (_current == LexicalAnalyzer.NotSy)
            {
                Advance();
                string inner = Factor();
                if (inner != "boolean")
                {
                    InputOutput.Error(206, _lexer.Token);
                }
                return "boolean";
            }

            if (_current == LexicalAnalyzer.Ident)
            {
                string name = _lexer.AddrName;
                TextPosition pos = _lexer.Token;
                if (!_symbols.Exists(name))
                {
                    InputOutput.Error(201, pos);
                }
                Advance();
                return _symbols.GetType(name);
            }

            if (_current == LexicalAnalyzer.IntC)
            {
                Advance();
                return "integer";
            }

            if (_current == LexicalAnalyzer.FloatC)
            {
                Advance();
                return "real";
            }

            if (_current == LexicalAnalyzer.StringSy)
            {
                string sv = _lexer.StringValue;
                Advance();
                return sv != null && sv.Length == 1 ? "char" : "string";
            }

            if (_current == LexicalAnalyzer.TrueSy || _current == LexicalAnalyzer.FalseSy)
            {
                Advance();
                return "boolean";
            }

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance();
                string t = Expression();
                if (!Match(LexicalAnalyzer.RightPar))
                {
                    InputOutput.Error(104, _lexer.Token);
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.RightPar,
                        LexicalAnalyzer.Semicolon,
                        LexicalAnalyzer.EndSy,
                        LexicalAnalyzer.Eof
                    });
                    if (_current == LexicalAnalyzer.RightPar)
                    {
                        Advance();
                    }
                }
                return t;
            }

            InputOutput.Error(104, _lexer.Token);
            SkipToStatementEnd();
            return "";
        }

        private static string MergeNumericTypes(string a, string b)
        {
            if (a == "real" || b == "real")
            {
                return "real";
            }
            return a;
        }

        private static void CheckTypeCompatibility(string varType, string exprType, TextPosition pos)
        {
            if (exprType == "" || varType == "")
            {
                return;
            }

            if (varType == "real" && exprType == "integer")
            {
                return;
            }
            if (varType == exprType)
            {
                return;
            }

            InputOutput.Error(205, pos);
        }
    }
}