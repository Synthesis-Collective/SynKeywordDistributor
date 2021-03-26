using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using KeywordDistributor.Types;
using System.IO;
using System.Linq;
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
                foreach (var item in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
                {
                    if (data.ContainsKey(item.EditorID ?? "") && item.Keywords != null && data[item.EditorID ?? ""].Mod.Equals(item.FormKey.ModKey))
                    {
                        var viakey = data[item.EditorID ?? ""].Keywords;
                        var keys = keywords.Where(x => viakey.Keys.Contains(x.Key)).Select(x => x.Value);
                        var DoWork = keys.Except(item.Keywords).Any();
                        if (DoWork)
                        {
                            var copy = state.PatchMod.Weapons.GetOrAddAsOverride(item);
                            foreach (var kyd in data[copy.EditorID ?? ""].Keywords)
                            {
                                switch (kyd.Value)
                                {
                                    case Action.R:
                                        if (copy?.Keywords?.Contains(keywords[kyd.Key]) ?? false)
                                        {
                                            copy.Keywords?.Remove(keywords[kyd.Key]);
                                        }
                                        break;
                                    case Action.A:
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
                foreach (var item in state.LoadOrder.PriorityOrder.Armor().WinningOverrides())
                {
                    if (data.ContainsKey(item.EditorID ?? "") && item.Keywords != null && data[item.EditorID ?? ""].Mod.Equals(item.FormKey.ModKey))
                    {
                        var viakey = data[item.EditorID ?? ""].Keywords;
                        var keys = keywords.Where(x => viakey.Keys.Contains(x.Key)).Select(x => x.Value);
                        var DoWork = keys.Except(item.Keywords).Any();
                        if (DoWork)
                        {
                            var copy = state.PatchMod.Armors.GetOrAddAsOverride(item);
                            foreach (var kyd in data[copy.EditorID ?? ""].Keywords)
                            {
                                switch (kyd.Value)
                                {
                                    case Action.R:
                                        if (copy?.Keywords?.Contains(keywords[kyd.Key]) ?? false)
                                        {
                                            copy.Keywords?.Remove(keywords[kyd.Key]);
                                        }
                                        break;
                                    case Action.A:
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