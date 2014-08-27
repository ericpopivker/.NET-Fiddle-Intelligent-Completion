namespace DotNetFiddle.IntelligentCompletion
{
    /// <summary>
    /// Options for language service
    /// </summary>
    public class LanguageServiceOptions
    {
        public LanguageServiceOptions()
        {
            ParseDocumenation = true;
        }

        /// <summary>
        /// Do we need to parse xml documentation. It may affect perfomance a bit
        /// </summary>
        public bool ParseDocumenation { get; set; }
    }
}