using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomegearLib
{
    public class Stories : ReadOnlyDictionary<ulong, Story>, IDisposable
    {
        RPCController _rpc = null;

        public Stories(RPCController rpc, Dictionary<ulong, Story> stories) : base(stories)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<ulong, Story> room in _dictionary)
            {
                room.Value.Dispose();
            }
        }

        public void Reload()
        {
            _rpc.Stories = null;
            _dictionary = _rpc.Stories;
        }

        public void Create(Story story)
        {
            story.ID = _rpc.CreateStory(story);
        }

        public Story ByName(string name)
        {
            foreach (var story in _dictionary)
            {
                if (story.Value.HasName(name)) return story.Value;
            }
            return null;
        }
    }
}
