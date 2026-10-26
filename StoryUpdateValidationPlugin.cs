using Microsoft.Xrm.Sdk;
using System;

namespace StoryManagement.Plugins
{
    public class StoryUpdateValidationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            try
            {
                if (!context.InputParameters.Contains("Target"))
                    return;

                Entity target =
                    (Entity)context.InputParameters["Target"];

                if (!context.PreEntityImages.Contains("PreImage"))
                    return;

                Entity preImage =
                    context.PreEntityImages["PreImage"];

                //----------------------------------------------------
                // Est Hours Validation
                //----------------------------------------------------

                if (target.Contains("sm_esthoursrequired"))
                {
                    decimal oldHours =
                        preImage.GetAttributeValue<decimal>(
                            "sm_esthoursrequired");

                    decimal newHours =
                        target.GetAttributeValue<decimal>(
                            "sm_esthoursrequired");

                    if (oldHours != newHours)
                    {
                        if (newHours <= 0)
                        {
                            throw new InvalidPluginExecutionException(
                                "Estimated Hours Required must be greater than zero.");
                        }
                    }
                }

                //----------------------------------------------------
                // Days Validation
                //----------------------------------------------------

                if (target.Contains("sm_noofdaysrequired"))
                {
                    int oldDays =
                        preImage.GetAttributeValue<int>(
                            "sm_noofdaysrequired");

                    int newDays =
                        target.GetAttributeValue<int>(
                            "sm_noofdaysrequired");

                    if (oldDays != newDays)
                    {
                        if (newDays <= 0)
                        {
                            throw new InvalidPluginExecutionException(
                                "Number of Days Required must be greater than zero.");
                        }

                        if (newDays > 90)
                        {
                            throw new InvalidPluginExecutionException(
                                "Number of Days Required cannot exceed 90.");
                        }
                    }
                }

                //----------------------------------------------------
                // Status Change Detection
                //----------------------------------------------------

                if (target.Contains("sm_currentstatus"))
                {
                    int oldStatus =
                        preImage.GetAttributeValue<OptionSetValue>(
                            "sm_currentstatus").Value;

                    int newStatus =
                        target.GetAttributeValue<OptionSetValue>(
                            "sm_currentstatus").Value;

                    if (oldStatus != newStatus)
                    {
                        tracingService.Trace(
                            $"Story Status changed from {oldStatus} to {newStatus}");

                        // Place future status-related logic here
                    }
                    else
                    {
                        tracingService.Trace(
                            "Story Status not changed.");
                    }
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.ToString());

                throw new InvalidPluginExecutionException(
                    "Story update validation failed. " +
                    ex.Message);
            }
        }
    }
}