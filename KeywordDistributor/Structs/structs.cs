using System.Collections.Generic;
using Mutagen.Bethesda;

namespace KeywordDistributor.Structs {
    struct Entry{
        public ModKey Mod;
        public Dictionary<string, ToDo> Keywords;
    }
    enum ToDo {
        A, R
    }
}