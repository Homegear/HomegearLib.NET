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

        public void Update(out bool storiesRemoved, out bool storiesAdded)
        {
            storiesRemoved = false;
            storiesAdded = false;
            Dictionary<ulong, Story> stories = _rpc.GetStories();
            foreach (KeyValuePair<ulong, Story> storyPair in stories)
            {
                if (!_dictionary.ContainsKey(storyPair.Key))
                {
                    storiesAdded = true;
                    continue;
                }
                Story story = _dictionary[storyPair.Value.ID];
                story.Translations = storyPair.Value.Translations;
            }
            foreach (KeyValuePair<ulong, Story> storyPair in _dictionary)
            {
                if (!stories.ContainsKey(storyPair.Key))
                {
                    storiesRemoved = true;
                    break;
                }
            }
        }

        public void Create(Story story)
        {
            _rpc.CreateStory(story);
        }

        public Story StoryByName(string name)
        {
            foreach (var story in _dictionary)
            {
                if (story.Value.HasName(name)) return story.Value;
            }
            return null;
        }
    }
}
