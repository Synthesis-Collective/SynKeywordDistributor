using System.Collections.Generic;
using Mutagen.Bethesda;

namespace KeywordDistributor.Types {
    struct Entry{
        public ModKey Mod;
        public Dictionary<string, Action> Keywords;
    }
    enum Action {
        A, R
    }
}