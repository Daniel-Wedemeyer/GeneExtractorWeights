using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GeneExtractorWeights;

public class GeneWeight : IExposable
{
    public float Weight;


    public GeneWeight(string geneDefName, float weight)
    {
        GeneDefName = geneDefName;
        Weight = weight;
    }

    public GeneWeight()
    {
    }

    public string GeneDefName { get; set; }

    public void ExposeData()
    {
        var geneDefName = GeneDefName;
        Scribe_Values.Look(ref geneDefName, nameof(GeneDefName));
        GeneDefName = geneDefName;
        var weight = Weight;
        Scribe_Values.Look(ref weight, nameof(Weight), forceSave: true);
        Weight = weight;
    }
}

public class GeneExtractorWeightsSettings : ModSettings
{
    private Dictionary<string, GeneWeight> _genes = new();

    internal bool _ignoreMetabolismLimit;
    internal bool _isInitialized;

    public IReadOnlyDictionary<string, GeneWeight> Genes => _genes;

    public bool IgnoreMetabolismLimit => _ignoreMetabolismLimit;

    internal void AddGene(GeneWeight gene)
    {
        _genes[gene.GeneDefName] = gene;
    }

    public void InitializeGenes()
    {
        foreach (var gene in GeneExtractorWeights.GenesInOrder)
        {
            if (Genes.ContainsKey(gene.defName))
                continue;

            AddGene(new GeneWeight(gene.defName, GetDefaultWeight(gene)));
        }

        foreach (var gene in Genes.Values.ToList())
            if (GeneExtractorWeights.GenesInOrder.All(g => g.defName != gene.GeneDefName))
            {
                Log.Warning(
                    $"GeneExtractorWeights: Gene {gene.GeneDefName} is not in the list of genes. Removing it from the list.");
                _genes.Remove(gene.GeneDefName);
            }

        _genes = _genes
            .OrderBy(g => GeneExtractorWeights.GenesInOrder.IndexOf(GeneExtractorWeights.GenesInOrder.First(gg =>
                string.Equals(gg.defName, g.Value.GeneDefName, StringComparison.Ordinal))))
            .ToDictionary(g => g.Key, g => g.Value);

        _isInitialized = true;
    }

    private static int GetDefaultWeight(GeneDef gene)
    {
        return gene.biostatArc > 0 ? 0 : gene.biostatCpx > 0 ? 3 : 1;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref _ignoreMetabolismLimit, nameof(IgnoreMetabolismLimit));

        var genes = Genes.Values.ToList();
        Scribe_Collections.Look(ref genes, nameof(Genes), LookMode.Deep);

        _genes = genes.ToDictionary(g => g.GeneDefName);

        base.ExposeData();
    }

    public void ResetToDefault()
    {
        foreach (var gene in Genes.Values)
        {
            var geneDef = DefDatabase<GeneDef>.GetNamed(gene.GeneDefName);
            gene.Weight = GetDefaultWeight(geneDef);
        }
    }
}