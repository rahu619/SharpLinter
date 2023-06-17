namespace OnlineLinter.Interfaces
{
    /// <summary>
    /// The interface for the code analyis service.
    /// </summary>
    public interface ICodeAnalysisService
    {
        /// <summary>
        /// Formats and indents the <paramref name="codeSnippet"/> passed in
        /// </summary>
        /// <param name="codeSnippet">The incoming code</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        Task<string> GetFormattedCode(string codeSnippet, CancellationToken cancellationToken = default);
    }
}
