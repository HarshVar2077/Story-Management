using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace StoryManagement.Plugins
{
    public class StoryItemsSummaryPlugin : IPlugin
    {
        private const int OpenStatus = 1;
        private const int InProgressStatus = 2;
        private const int CompletedStatus = 3;

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

                Entity target =
                    (Entity)context.InputParameters["Target"];

                Guid storyItemId = context.PrimaryEntityId;

                // Retrieve Story Item
                Entity storyItem = service.Retrieve(
                    "sm_storyitem",
                    storyItemId,
                    new ColumnSet("sm_story"));

                EntityReference storyRef =
                    storyItem.GetAttributeValue<EntityReference>("sm_story");

                if (storyRef == null)
                    return;

                QueryExpression query =
                    new QueryExpression("sm_storyitem");

                query.ColumnSet =
                    new ColumnSet("sm_currentstatus");

                query.Criteria.AddCondition(
                    "sm_story",
                    ConditionOperator.Equal,
                    storyRef.Id);

                EntityCollection storyItems =
                    service.RetrieveMultiple(query);

                int openCount = 0;
                int inProgressCount = 0;
                int completedCount = 0;

                foreach (Entity item in storyItems.Entities)
                {
                    OptionSetValue statusValue =
                        item.GetAttributeValue<OptionSetValue>(
                            "sm_currentstatus");

                    if (statusValue == null)
                        continue;

                    switch (statusValue.Value)
                    {
                        case OpenStatus:
                            openCount++;
                            break;

                        case InProgressStatus:
                            inProgressCount++;
                            break;

                        case CompletedStatus:
                            completedCount++;
                            break;
                    }
                }

                Entity storyUpdate =
                    new Entity("sm_story");

                storyUpdate.Id = storyRef.Id;

                storyUpdate["sm_openstoryitems"] =
                    openCount;

                storyUpdate["sm_inprogressstoryitems"] =
                    inProgressCount;

                storyUpdate["sm_completedstoryitems"] =
                    completedCount;

                service.Update(storyUpdate);

                tracingService.Trace(
                    $"Story updated. Open={openCount}, InProgress={inProgressCount}, Completed={completedCount}");
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.ToString());

                throw new InvalidPluginExecutionException(
                    "Failed to update Story Item summary counts. " +
                    ex.Message);
            }
        }
    }
}