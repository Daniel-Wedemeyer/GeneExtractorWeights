using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneExtractorWeights;

public class GeneExtractorWeights : Mod
{
    private static List<GeneDef> cachedGeneDefsInOrder;
    private readonly GeneExtractorWeightsSettings _settings;
    //private List<GeneWeight> _genes;
    private float _scrollHeight;

    private Vector2 _scrollPosition = new(0, 0);

    public GeneExtractorWeights(ModContentPack pack) : base(pack)
    {
        var harmony = new Harmony("ObiVayneKenobi.GeneExtractorWeights");
        harmony.PatchAll();

        _settings = GetSettings<GeneExtractorWeightsSettings>();


        Log.Message("GeneExtractorWeights loaded");
    }

    internal static List<GeneDef> GenesInOrder
    {
        get
        {
            if (cachedGeneDefsInOrder == null)
            {
                cachedGeneDefsInOrder = new List<GeneDef>();
                foreach (var allDef in DefDatabase<GeneDef>.AllDefs)
                    //if (allDef.endogeneCategory != EndogeneCategory.Melanin)
                    cachedGeneDefsInOrder.Add(allDef);
                cachedGeneDefsInOrder.SortBy(x => -x.displayCategory.displayPriorityInXenotype,
                    x => x.displayCategory.label, x => x.displayOrderInCategory);
            }

            return cachedGeneDefsInOrder;
        }
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        if (!_settings._isInitialized) _settings.InitializeGenes();

        const float geneIconSize = 90;
        const float rowMargin = 6;
        const float rowHeight = 96;

        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.CheckboxLabeled("Ignore metabolism limit", ref _settings._ignoreMetabolismLimit, height: 24,
            tooltip:
            "by default, the gene extractor will not add a gene to the extracted genepack if it would exceed the metabolism limit of the pawn. This option will ignore that limit.");
        Widgets.BeginScrollView(inRect with { y = 40, height = inRect.height - 76 }, ref _scrollPosition,
            new Rect(0.0f, 0.0f, inRect.width - 16f, _scrollHeight));

        var left = true;
        var startRow = (((int)(_scrollPosition.y * 4 / (geneIconSize + rowMargin)))/ 2) * 2;
        for (int row = startRow; row < startRow + 2 * inRect.height / (geneIconSize + rowMargin) && row < _settings.Genes.Count; row++)
        {
            
            var gene = _settings.Genes[row];
            var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(gene.GeneDefName);
            if (geneDef == null) continue;
            GeneUIUtility.DrawGeneDef(geneDef,
                new Rect(left ? 0 : inRect.width / 2, row / 2 * rowHeight - _scrollPosition.y, geneIconSize, geneIconSize),
                GeneType.Endogene, null);
            Widgets.HorizontalSlider(
                new Rect(geneIconSize + 24 + (left ? 0 : inRect.width / 2),
                    row / 2 * rowHeight + geneIconSize / 2 - 12 - _scrollPosition.y, inRect.width / 2 - geneIconSize - 48, 24),
                ref gene.Weight, new FloatRange(0, 10), roundTo: .1f);
            Widgets.Label(
                new Rect(geneIconSize + 24 + (left ? 0 : inRect.width / 2),
                    row / 2 * rowHeight + geneIconSize / 2 + 12 - _scrollPosition.y, inRect.width / 2 - geneIconSize - 48, 24),
                gene.Weight.ToString("0.0"));
            
            left = !left;
        }

        if (Event.current.type == EventType.Layout)
            _scrollHeight = (_settings.Genes.Count * (geneIconSize + rowMargin) / 2 + inRect.height) / 2;
        Widgets.EndScrollView();

        if (Widgets.ButtonText(new Rect(inRect.width - 100, inRect.height - 30, 100, 30), "Reset"))
        {
            _settings.ResetToDefault();
        }

        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "Gene Extractor Weights";
    }
}