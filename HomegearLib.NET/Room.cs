using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomegearLib
{
    public class Room : IDisposable
    {
        private ulong _id = 0;
        public ulong ID { get { return _id; } set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { get { return _translations; } set { _translations = value; } }

        public int Level
        {
            get
            {
                if ((_id / 10000) * 10000 == _id) return 0;
                else if ((_id / 100) * 100 == _id) return 1;
                else return 2;
            }
        }

        public Room(ulong id, Dictionary<string, string> translations)
        {
            _id = id;
            _translations = translations;
        }

        public void Dispose()
        {

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
