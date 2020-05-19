﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomegearLib
{
    public class Building : IDisposable
    {
        private ulong _id = 0;
        public ulong ID { get { return _id; } internal set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { get { return _translations; } set { _translations = value; } }

        public Stories _stories = null;
        public Stories Stories { get { return _stories; } internal set { _stories = value; } }

        public Building(Dictionary<string, string> translations)
        {
            _translations = translations;
        }

        internal Building(ulong id, Dictionary<string, string> translations)
        {
            _id = id;
            _translations = translations;
        }

        public void Dispose()
        {

        }

        public bool HasName(string name)
        {
            foreach (var translation in _translations)
            {
                if (translation.Value == name) return true;
            }
            return false;
        }

        public string Name(string languageCode)
        {
            if (_translations.Count == 0) return "";

            if (_translations.ContainsKey(languageCode)) return _translations[languageCode];
            else if (_translations.ContainsKey("en-US")) return _translations["en-US"];
            else return _translations.First().Value;
        }
    }
}
