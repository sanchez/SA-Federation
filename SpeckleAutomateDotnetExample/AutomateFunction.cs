using Objects;
using Speckle.Automate.Sdk;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using SpeckleAutomate.Federation;

public static class AutomateFunction
{
  async static Task<Collection> GetFederatedModel(string objectId, ITransport serverTransport)
  {
    Base? receivedObject = await Operations.Receive(objectId, serverTransport);

    if (receivedObject == null)
      return new Collection()
      {
        collectionType = "federation"
      };

    if (receivedObject is Collection fedModel && fedModel.collectionType == "federation")
      return fedModel;

    Base instance = new Base();
    instance["item"] = receivedObject;
    instance["sourceName"] = "Main";

    return new Collection()
    {
      collectionType = "federation",
      elements = new List<Base>() {
        instance
      }
    };
  }

  public static async Task Run(
    AutomationContext automationContext,
    FunctionInputs functionInputs
  )
  {
    new Objects.Geometry.Line();
    string projectId = automationContext.AutomationRunData.ProjectId;
    string branchName = automationContext.AutomationRunData.BranchName;

    Base currentVersion = await automationContext.ReceiveVersion();
    Base fedObject = new();
    fedObject["item"] = currentVersion;
    fedObject["sourceName"] = branchName;

    ServerTransport serverTransport = new(automationContext.SpeckleClient.Account, projectId);

    Branch branch = await automationContext.SpeckleClient.BranchGet(projectId, functionInputs.TargetModelName);
    var commits = branch.commits.items;

    Collection federatedModel = new();
    if (commits.Count > 0)
    {
      string referencedObject = commits.First().referencedObject;
      federatedModel = await GetFederatedModel(referencedObject, serverTransport);
    }

    federatedModel.elements = federatedModel.elements.Where(x =>
    {
      var sourceName = x["sourceName"];
      if (sourceName is string s && s == branchName)
        return true;
      return false;
    }).ToList();
    federatedModel.elements.Add(fedObject);

    string message = "Automated Model Federation";
    if (commits.Count > 0)
    {
      message = commits.First().message;
    }

    new BaseObjectSerializerV2().PreserializeBase(federatedModel, true);
    await automationContext.CreateNewVersionInProject(federatedModel, functionInputs.TargetModelName, message);
  }
}
