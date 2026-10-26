using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace StoryManagement.Plugins
{
    public class StoryReopenPlugin : IPlugin
    {
        private const int CompletedStatus = 3;

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                factory.CreateOrganizationService(context.UserId);

            if (!context.InputParameters.Contains("Target"))
                return;

            Entity target = (Entity)context.InputParameters["Target"];

            if (!target.Contains("sm_currentstatus"))
                return;

            Entity preImage =
                context.PreEntityImages["PreImage"];

            int oldStatus =
                preImage.GetAttributeValue<OptionSetValue>("sm_currentstatus").Value;

            int newStatus =
                target.GetAttributeValue<OptionSetValue>("sm_currentstatus").Value;

            if (oldStatus != CompletedStatus)
                return;

            if (newStatus == CompletedStatus)
                return;

            QueryExpression query = new QueryExpression("sm_storyitem");
            query.ColumnSet = new ColumnSet(false);

            query.Criteria.AddCondition(
                "sm_story",
                ConditionOperator.Equal,
                context.PrimaryEntityId);

            EntityCollection items =
                service.RetrieveMultiple(query);

            ExecuteMultipleRequest batch =
                new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

            foreach (Entity item in items.Entities)
            {
                Entity updateItem =
                    new Entity("sm_storyitem");

                updateItem.Id = item.Id;

                updateItem["sm_currentstatus"] =
                    new OptionSetValue(newStatus);

                batch.Requests.Add(
                    new UpdateRequest()
                    {
                        Target = updateItem
                    });
            }

            if (batch.Requests.Count > 0)
            {
                service.Execute(batch);
            }
        }
    }
}