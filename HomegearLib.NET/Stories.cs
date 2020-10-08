using HomegearLib.RPC;
using System;
using System.Collections.Generic;

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
            if (story._rpc == null) story._rpc = _rpc;
            _dictionary.Add(story.ID, story);
        }

        public Story ByName(string name)
        {
            foreach (var story in _dictionary)
            {
                if (story.Value.HasName(name)) return story.Value;
            }
            return null;
        }

        // This will need reloading things that contain stories, directly or indirectly
        // the story won't be removed from all collections just by calling this
        // use it carefully
        public void Delete(Story story)
        {
            if (_dictionary.ContainsKey(story.ID))
            {
                _rpc?.DeleteStory(story);
                _dictionary.Remove(story.ID);
            }
        }
    }
}
