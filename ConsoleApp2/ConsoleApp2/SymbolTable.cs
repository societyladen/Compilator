using System.Collections.Generic;

namespace Компилятор
{
    public class Symbol
    {
        private string _name;
        private string _type;
        private uint _line;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string Type
        {
            get
            {
                return _type;
            }
        }

        public uint Line
        {
            get
            {
                return _line;
            }
        }

        public Symbol(string name, string type, uint line)
        {
            _name = name;
            _type = type;
            _line = line;
        }
    }

    public class SymbolTable
    {
        private Dictionary<string, Symbol> _symbols;

        public SymbolTable()
        {
            _symbols = new Dictionary<string, Symbol>();
        }

        public bool Add(string name, string type, uint line)
        {
            string key = name.ToLower();
            if (_symbols.ContainsKey(key))
            {
                return false;
            }
            _symbols[key] = new Symbol(name, type, line);
            return true;
        }

        public bool Exists(string name)
        {
            return _symbols.ContainsKey(name.ToLower());
        }

        public Symbol Get(string name)
        {
            _symbols.TryGetValue(name.ToLower(), out Symbol s);
            return s;
        }

        public string GetType(string name)
        {
            Symbol s = Get(name);
            return s != null ? s.Type : "";
        }
    }
}