using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json.Linq;

using KeywordDistributor.Structs;
using Noggog;

namespace KeywordDistributor
{
    class Program
    {
        public static Dictionary<string, Entry>? data = null;
        public static Dictionary<string, FormLink<IKeywordGetter>> keywords = new();
        public static JsonMergeSettings merge = new() { MergeArrayHandling = MergeArrayHandling.Union, MergeNullValueHandling = MergeNullValueHandling.Merge };
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynKeywordDistributor.esp")
                .Run(args);
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var files = Directory.GetFiles(state.DataFolderPath).Where(x => x.EndsWith("_KEYWORDS.json"));
            var JObj = JObject.Parse("{}");
            foreach (var f in files)
            {
                JObj.Merge(JObject.Parse(File.ReadAllText(Path.Combine(state.DataFolderPath, f))), merge);
            }
            data = JObj.ToObject<Dictionary<string, Entry>>();
            if (data != null)
            {
                var keywds = data.SelectMany(x => x.Value.AKeywords).Concat(data.SelectMany(x => x.Value.RKeywords)).ToHashSet().ToList();
                foreach (var kywd in state.LoadOrder.PriorityOrder.Keyword().WinningOverrides())
                {
                    if (keywds?.Contains(kywd.EditorID ?? "") ?? false)
                    {
                        keywords[kywd.EditorID ?? ""] = new FormLink<IKeywordGetter>(kywd);
                    }
                }
                foreach (var obj in state.LoadOrder.PriorityOrder.SkyrimMajorRecord().WinningContextOverrides(state.LinkCache))
                {
                    if (data.ContainsKey(obj.Record.EditorID ?? "") && obj.Record is IKeywordedGetter<IKeywordGetter> item && item.Keywords != null && data[obj.Record.EditorID ?? ""].Mod.Equals(obj.Record.FormKey.ModKey))
                    {
                        var addWord = data[obj.Record.EditorID ?? ""].AKeywords;
                        var remWord = data[obj.Record.EditorID ?? ""].RKeywords;
                        var addKeys = keywords.Where(x => addWord.Contains(x.Key)).Select(x => x.Value);
                        var remKeys = keywords.Where(x => remWord.Contains(x.Key)).Select(x => x.Value);
                        var DoAdd = addKeys.Any(x => !item.Keywords.Contains(x));
                        var DoRem = remKeys.Any(x => item.Keywords.Contains(x));
                        if (DoAdd || DoRem)
                        {
                            var copy = obj.GetOrAddAsOverride(state.PatchMod) as IKeyworded<IKeywordGetter>;
                            if (DoRem)
                            {
                                foreach (var key in remKeys)
                                {
                                    if (copy?.Keywords?.Contains(key) ?? false)
                                    {
                                        copy.Keywords?.Remove(key);
                                    }
                                    break;
                                }
                            }
                            if (DoAdd)
                            {
                                foreach (var key in addKeys)
                                {
                                    if (!(copy?.Keywords?.Contains(key) ?? false))
                                    {
                                        copy?.Keywords?.Add(key);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
