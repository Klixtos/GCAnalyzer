namespace GCAnalyzer
{
    /// <summary>
    /// Constants used throughout the analyzer.
    /// </summary>
    public static class AnalyzerConstants
    {
        /// <summary>
        /// Base URL for help documentation. Change this to your actual documentation location.
        /// </summary>
        public const string HelpLinkBaseUrl = "https://github.com/Devoo-Consulting/GCAnalyzer/blob/main/docs/rules/";
        
        /// <summary>
        /// Format to use when creating a help link for a specific rule
        /// </summary>
        /// <param name="ruleId">The rule ID (e.g. "GCA001")</param>
        /// <returns>Formatted help link URL</returns>
        public static string GetHelpLink(string ruleId)
        {
            return $"{HelpLinkBaseUrl}{ruleId}.md";
        }
    }
} 