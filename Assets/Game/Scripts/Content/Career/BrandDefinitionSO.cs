using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Brand", fileName = "Brand")]
    public sealed class BrandDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string tagline;
        [SerializeField] private string colorHex = "#65FFE1";
        [SerializeField] private BrandCategory category;
        [SerializeField] private TeamPhilosophy philosophy;
        [SerializeField] private List<BrandProduct> products = new List<BrandProduct>();
        [SerializeField] private List<BrandContractTemplate> contracts = new List<BrandContractTemplate>();
        public BrandDefinition CreateDefinition() => new BrandDefinition(id, displayName, tagline, colorHex, category, philosophy, products, contracts);
    }
}
