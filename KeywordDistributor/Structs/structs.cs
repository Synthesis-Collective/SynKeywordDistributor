using System;
using System.Collections.Generic;
using Mutagen.Bethesda;

namespace KeywordDistributor.Structs {
    struct Entry{
        public ModKey Mod;
        public List<string> AKeywords;
        public List<string> RKeywords;
    }
}