using System;

namespace GCAnalyzer
{
    /// <summary>
    /// Helper class to centralize resource access.
    /// We don't technically need this since MSBuild will auto-generate a Resources class,
    /// but this demonstrates how you can create a wrapper if needed.
    /// </summary>
    internal static class ResourceHelper
    {
        /// <summary>
        /// Example of how to access the resources from the auto-generated resource class
        /// </summary>
        /// <returns>The title for the GCA001 rule</returns>
        public static string GetAvoidGCCollectTitle()
        {
            return Resources.AvoidUsingGCCollectTitle;
        }

        /// <summary>
        /// Example of how to format a message with placeholders
        /// </summary>
        /// <param name="typeName">The name of the type to include in the message</param>
        /// <returns>A formatted resource message</returns>
        public static string FormatProperResourceDisposalMessage(string typeName)
        {
            return string.Format(Resources.ProperResourceDisposalMessageFormat, typeName);
        }
    }
} 