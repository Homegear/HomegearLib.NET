﻿using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib
{
    public class Building : IDisposable
    {
        internal RPCController _rpc = null;

        private ulong _id = 0;

        private Dictionary<string, RPCVariable> _metadata = null;
        public ulong ID { get { return _id; } internal set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { get { return _translations; } set { _translations = value; } }

        public Dictionary<string, RPCVariable> Metadata 
        { 
            get
            {
                if (_metadata == null)
                {
                    _metadata = _rpc?.GetBuildingMetadata(this);
                }

                return _metadata;
            }

            set
            {
                _rpc?.SetBuildingMetadata(this, value);
                _metadata = value;
            }
        }

        private Stories _stories = null;
        public Stories Stories { 
            get {
                if (_stories == null)
                    _stories = new Stories(_rpc, new Dictionary<ulong, Story>());
                return _stories; 
            } 
            internal set { _stories = value; } 
        }

        public Building(RPCController rpc, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _translations = translations;
        }

        public Building(RPCController rpc, ulong id, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _id = id;
            _translations = translations;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void AddStory(Story story)
        {
            if (story.ID == 0) return;
            _rpc.AddStoryToBuilding(this, story);
            if (_stories == null)
                _stories = new Stories(_rpc, new Dictionary<ulong, Story>());
            _stories._dictionary.Add(story.ID, story);
        }

        public void RemoveStory(Story story)
        {
            if (story.ID == 0) return;
            _rpc.RemoveStoryFromBuilding(this, story);
            if (_stories != null) _stories._dictionary.Remove(story.ID);
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
