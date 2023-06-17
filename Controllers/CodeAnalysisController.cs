using Microsoft.AspNetCore.Mvc;
using OnlineLinter.Interfaces;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace OnlineLinter.Controllers
{
  [Route("api/v1/[controller]")]
  [ApiController]
  public class CodeAnalysisController : ControllerBase
  {
    private readonly ICodeAnalysisService _codeAnalysisService;

    public CodeAnalysisController(ICodeAnalysisService linterService)
    {
      this._codeAnalysisService = linterService;
    }

    /// <summary>
    /// Formats and returns a c sharp code snippet
    /// </summary>
    /// <param name="codeSnippet"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A newly formatted code</returns>
    [HttpPost()]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> FormatCode(CancellationToken cancellationToken)
    {
      string? content;
      using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
      {
        content = await reader.ReadToEndAsync();
      }
      ArgumentNullException.ThrowIfNull(content, "Error reading request body");

      var formattedCode = await this._codeAnalysisService.GetFormattedCode(content, cancellationToken);

      return Ok(formattedCode);
    }
  }
}
