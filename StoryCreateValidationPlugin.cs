using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace StoryManagement.Plugins
{
    public class StoryCreateValidationPlugin : IPlugin
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
                if (!context.InputParameters.Contains("Target") ||
                    !(context.InputParameters["Target"] is Entity))
                {
                    return;
                }

                Entity target = (Entity)context.InputParameters["Target"];

                // Replace logical names with your actual column names
                string storyName = target.GetAttributeValue<string>("sm_storyname");
                decimal estHours = target.GetAttributeValue<decimal>("sm_esthoursrequired");
                int noOfDays = target.GetAttributeValue<int>("sm_noofdaysrequired");

                #region Validations

                if (string.IsNullOrWhiteSpace(storyName))
                {
                    throw new InvalidPluginExecutionException(
                        "Story Name is required.");
                }

                if (estHours <= 0)
                {
                    throw new InvalidPluginExecutionException(
                        "Estimated Hours Required must be greater than zero.");
                }

                if (noOfDays <= 0)
                {
                    throw new InvalidPluginExecutionException(
                        "Number of Days Required must be greater than zero.");
                }

                #endregion

                #region Duplicate Check

                string fetchXml = $@"
                <fetch top='1'>
                  <entity name='sm_story'>
                    <attribute name='sm_storyuniqueid' />
                    <filter>
                      <condition attribute='sm_storyname'
                                 operator='eq'
                                 value='{System.Security.SecurityElement.Escape(storyName)}' />

                      <condition attribute='sm_esthoursrequired'
                                 operator='eq'
                                 value='{estHours}' />

                      <condition attribute='sm_noofdaysrequired'
                                 operator='eq'
                                 value='{noOfDays}' />
                    </filter>
                  </entity>
                </fetch>";

                EntityCollection existingStories =
                    service.RetrieveMultiple(new FetchExpression(fetchXml));

                if (existingStories.Entities.Count > 0)
                {
                    throw new InvalidPluginExecutionException(
                        "A Story with the same Story Name, Estimated Hours Required, and Number of Days Required already exists.");
                }

                #endregion

                #region Initialize Values

                target["sm_openstoryitems"] = noOfDays;
                target["sm_inprogressstoryitems"] = 0;
                target["sm_completedstoryitems"] = 0;
                target["sm_actualhoursspent"] = 0m;

                #endregion
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracingService.Trace("StoryCreateValidationPlugin Error: " + ex.ToString());

                throw new InvalidPluginExecutionException(
                    "An error occurred while creating the Story. " +
                    ex.Message);
            }
        }
    }
}
