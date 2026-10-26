using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace StoryManagement.Plugins
{
    public class StoryStatusValidationPlugin : IPlugin
    {
        private const int OpenStatus = 1;
        private const int InProgressStatus = 2;
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

            int newStatus =
                target.GetAttributeValue<OptionSetValue>("sm_currentstatus").Value;

            if (newStatus != CompletedStatus)
                return;

            QueryExpression query = new QueryExpression("sm_storyitem");
            query.ColumnSet = new ColumnSet("sm_currentstatus");

            query.Criteria.AddCondition(
                "sm_story",
                ConditionOperator.Equal,
                context.PrimaryEntityId);

            EntityCollection items =
                service.RetrieveMultiple(query);

            foreach (Entity item in items.Entities)
            {
                int status =
                    item.GetAttributeValue<OptionSetValue>("sm_currentstatus").Value;

                if (status == OpenStatus ||
                    status == InProgressStatus)
                {
                    throw new InvalidPluginExecutionException(
                        "All Story Items must be completed before the Story can be marked as Completed.");
                }
            }
        }
    }
}