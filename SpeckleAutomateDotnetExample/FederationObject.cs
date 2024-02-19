using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace SpeckleAutomate.Federation;

public class FederationObject : Base
{
    public string SourceName { get; set; }
    public Base Document { get; set; }

    public FederationObject() { }
}