using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using KeywordDistributor.Structs;
using System.IO;
using System.Linq;
using System;

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
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynKeyworder.esp")
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
            var keywds = data?.SelectMany(x => x.Value.Keywords.Select(x => x.Key)).ToHashSet().ToList();
            foreach (var kywd in state.LoadOrder.PriorityOrder.Keyword().WinningOverrides())
            {
                if (keywds?.Contains(kywd.EditorID ?? "") ?? false)
                {
                    keywords[kywd.EditorID ?? ""] = new FormLink<IKeywordGetter>(kywd);
                }
            }
            if (data != null)
            {
                foreach (var obj in state.LoadOrder.PriorityOrder.SkyrimMajorRecord().WinningContextOverrides(state.LinkCache))
                {
                    if (data.ContainsKey(obj.Record.EditorID ?? "") && obj.Record is IKeywordedGetter<IKeywordGetter> item && item.Keywords != null && data[obj.Record.EditorID ?? ""].Mod.Equals(obj.Record.FormKey.ModKey))
                    {
                        var viakey = data[obj.Record.EditorID ?? ""].Keywords;
                        var keys = keywords.Where(x => viakey.Keys.Contains(x.Key)).Select(x => x.Value);
                        var DoWork = keys.Except(item.Keywords).Any();
                        DoWork |= viakey.Where(x => x.Value == ToDo.R).Any();
                        if (DoWork)
                        {
                            var copy = obj.GetOrAddAsOverride(state.PatchMod) as IKeyworded<IKeywordGetter>;
                            foreach (var kyd in data[obj.Record.EditorID ?? ""].Keywords)
                            {
                                switch (kyd.Value)
                                {
                                    case ToDo.R:
                                        if (copy?.Keywords?.Contains(keywords[kyd.Key]) ?? false)
                                        {
                                            copy.Keywords?.Remove(keywords[kyd.Key]);
                                        }
                                        break;
                                    case ToDo.A:
                                        if (!(copy?.Keywords?.Contains(keywords[kyd.Key]) ?? false))
                                        {
                                            copy?.Keywords?.Add(keywords[kyd.Key]);
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