using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace StoryManagement.Plugins
{
    public class StoryCompletionPlugin : IPlugin
    {
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

                if (!target.Contains("sm_currentstatus"))
                    return;

                Entity preImage =
                    context.PreEntityImages["PreImage"];

                int oldStatus =
                    preImage.GetAttributeValue<OptionSetValue>(
                        "sm_currentstatus")?.Value ?? 0;

                int newStatus =
                    target.GetAttributeValue<OptionSetValue>(
                        "sm_currentstatus")?.Value ?? 0;

                // Only run when becoming Completed
                if (oldStatus == CompletedStatus ||
                    newStatus != CompletedStatus)
                {
                    return;
                }

                Guid storyId = context.PrimaryEntityId;

                Entity story = service.Retrieve(
                    "sm_story",
                    storyId,
                    new ColumnSet(
                        "sm_storyname",
                        "ownerid"));

                string storyName =
                    story.GetAttributeValue<string>(
                        "sm_storyname");

                EntityReference owner =
                    story.GetAttributeValue<EntityReference>(
                        "ownerid");

                // -----------------------------
                // Calculate Actual Hours
                // -----------------------------

                QueryExpression query =
                    new QueryExpression("sm_storyitem");

                query.ColumnSet =
                    new ColumnSet("sm_hoursrequired");

                query.Criteria.AddCondition(
                    "sm_story",
                    ConditionOperator.Equal,
                    storyId);

                EntityCollection storyItems =
                    service.RetrieveMultiple(query);

                decimal totalHours = 0;

                foreach (Entity storyItem in storyItems.Entities)
                {
                    totalHours +=
                        storyItem.GetAttributeValue<decimal>(
                            "sm_hoursrequired");
                }

                Entity storyUpdate =
                    new Entity("sm_story");

                storyUpdate.Id = storyId;

                storyUpdate["sm_actualhoursspent"] =
                    totalHours;

                service.Update(storyUpdate);

                // -----------------------------
                // Manager Lookup
                // -----------------------------

                if (owner == null)
                    return;

                Entity user = service.Retrieve(
                    "systemuser",
                    owner.Id,
                    new ColumnSet(
                        "parentsystemuserid"));

                EntityReference manager =
                    user.GetAttributeValue<EntityReference>(
                        "parentsystemuserid");

                if (manager == null)
                {
                    tracingService.Trace(
                        "Manager not found.");
                    return;
                }

                // -----------------------------
                // Create Email
                // -----------------------------

                Entity email =
                    new Entity("email");

                email["subject"] =
                    $"{storyName} is completed, please review";

                email["regardingobjectid"] = new EntityReference("sm_story", storyId);

                email["description"] =
                    "The Story has been successfully marked as completed. " +
                    "Please review the details and ensure everything is in order. " +
                    "If there are any discrepancies or further actions required, " +
                    "kindly address them at your earliest convenience.";

                email["from"] =
                    new EntityCollection(
                        new List<Entity>
                        {
                            new Entity("activityparty")
                            {
                                ["partyid"] = owner
                            }
                        });

                email["to"] =
                    new EntityCollection(
                        new List<Entity>
                        {
                            new Entity("activityparty")
                            {
                                ["partyid"] = manager
                            }
                        });

                Guid emailId = service.Create(email);

                tracingService.Trace(
                    "Email Created : " +
                    emailId);

                tracingService.Trace(
                    "Story completed successfully. Hours calculated and review email created.");
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.ToString());

                throw new InvalidPluginExecutionException(
                    "Error while processing Story completion. " +
                    ex.Message);
            }
        }
    }
}