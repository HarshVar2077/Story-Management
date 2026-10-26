using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;


namespace StoryManagement.Plugins
{
    public class StoryItemBatchCreatePlugin : IPlugin
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


                Entity story = (Entity)context.InputParameters["Target"];


                Guid storyId = story.Id;


                string storyName =
                    story.GetAttributeValue<string>("sm_storyname");


                decimal estimatedHours =
                    story.GetAttributeValue<decimal>("sm_esthoursrequired");


                int noOfDays =
                    story.GetAttributeValue<int>("sm_noofdaysrequired");


                EntityReference owner =
                    story.GetAttributeValue<EntityReference>("ownerid");


                if (noOfDays <= 0)
                    return;


                decimal hoursPerItem =
                    estimatedHours / noOfDays;


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


                for (int i = 1; i <= noOfDays; i++)
                {
                    Entity storyItem = new Entity("sm_storyitem");


                    storyItem["sm_storyitemname"] =
                        $"{storyName} - Day {i}";


                    storyItem["sm_hoursrequired"] =
                        hoursPerItem;


                    storyItem["sm_story"] =
                        new EntityReference("sm_story", storyId);


                    storyItem["ownerid"] =
                        owner;


                    storyItem["sm_currentstatus"] =
                        new OptionSetValue(1); // Open


                    CreateRequest createRequest =
                        new CreateRequest()
                        {
                            Target = storyItem
                        };


                    batchRequest.Requests.Add(createRequest);
                }


                service.Execute(batchRequest);
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.ToString());


                throw new InvalidPluginExecutionException(
                    "Unable to create Story Items. " +
                    ex.Message);
            }
        }
    }
}