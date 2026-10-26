using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace StoryManagement.Plugins
{
    public class StoryDeletePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                if (!context.InputParameters.Contains("Target"))
                    return;

                EntityReference storyReference =
                    (EntityReference)context.InputParameters["Target"];

                Guid storyId = storyReference.Id;

                QueryExpression query =
                    new QueryExpression("sm_storyitem");

                query.ColumnSet =
                    new ColumnSet(false);

                query.Criteria.AddCondition(
                    "sm_story",
                    ConditionOperator.Equal,
                    storyId);

                EntityCollection storyItems =
                    service.RetrieveMultiple(query);

                if (storyItems.Entities.Count == 0)
                {
                    tracingService.Trace(
                        "No related Story Items found.");

                    return;
                }

                ExecuteMultipleRequest batchRequest =
                    new ExecuteMultipleRequest()
                    {
                        Settings = new ExecuteMultipleSettings()
                        {
                            ContinueOnError = false,
                            ReturnResponses = true
                        },
                        Requests = new OrganizationRequestCollection()
                    };

                foreach (Entity storyItem in storyItems.Entities)
                {
                    DeleteRequest deleteRequest =
                        new DeleteRequest()
                        {
                            Target =
                                new EntityReference(
                                    "sm_storyitem",
                                    storyItem.Id)
                        };

                    batchRequest.Requests.Add(deleteRequest);
                }

                ExecuteMultipleResponse response =
                    (ExecuteMultipleResponse)service.Execute(batchRequest);

                foreach (ExecuteMultipleResponseItem item in response.Responses)
                {
                    if (item.Fault != null)
                    {
                        throw new InvalidPluginExecutionException(
                            "Failed to delete one or more Story Items. " +
                            item.Fault.Message);
                    }
                }

                tracingService.Trace(
                    $"{storyItems.Entities.Count} Story Items deleted successfully.");
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.ToString());

                throw new InvalidPluginExecutionException(
                    "Unable to delete related Story Items. " +
                    ex.Message);
            }
        }
    }
}