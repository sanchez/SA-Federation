using Objects;
using Speckle.Automate.Sdk;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Transports;
using SpeckleAutomate.Federation;



public static class AutomateFunction
{
  async static Task<FederationModel> GetFederatedModel(string objectId, ITransport serverTransport)
  {
    Base? receivedObject = await Operations.Receive(objectId, serverTransport);

    if (receivedObject == null)
      return new FederationModel();

    if (receivedObject is FederationModel fedModel)
      return fedModel;

    return new FederationModel()
    {
      Items = new List<FederationObject>() {
        new FederationObject() {
          SourceName = "Main",
          Document = receivedObject
        }
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
    FederationObject fedObject = new FederationObject()
    {
      SourceName = branchName,
      Document = currentVersion,
    };

    ServerTransport serverTransport = new(automationContext.SpeckleClient.Account, projectId);

    Branch branch = await automationContext.SpeckleClient.BranchGet(projectId, functionInputs.TargetModelName);
    var commits = branch.commits.items;

    FederationModel federatedModel = new();
    if (commits.Count > 0)
    {
      string referencedObject = commits.First().referencedObject;
      federatedModel = await GetFederatedModel(referencedObject, serverTransport);
    }

    federatedModel.Items = federatedModel.Items.Where(x => x.SourceName != branchName).ToList();
    federatedModel.Items.Add(fedObject);

    string message = "Automated Model Federation";
    if (commits.Count > 0)
    {
      message = commits.First().message;
    }

    await automationContext.CreateNewVersionInProject(federatedModel, functionInputs.TargetModelName, message);
  }
}
