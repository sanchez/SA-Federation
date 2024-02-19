using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace SpeckleAutomate.Federation;

public class FederationModel : Base
{
    [DetachProperty] public List<FederationObject> Items { get; set; } = new List<FederationObject>();

    [JsonConstructor]
    public FederationModel() { }

}