using System.Collections.Generic;

using Mutagen.Bethesda.Plugins;

namespace KeywordDistributor.Structs
{
    struct Entry
    {
        public ModKey Mod;
        public List<string> AKeywords;
        public List<string> RKeywords;
    }
}