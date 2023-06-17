using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OnlineLinter.Interfaces;
using OnlineLinter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineLinter.Test
{
  [TestFixture]
  public class CodeAnalysisServiceTests
  {
    [TestCaseSource(nameof(CodeSnippetTestData))]
    public async Task ShouldFormatValidCode(string expectedCode, string inputCode)
    {
      //Arrange
      //Act
      string actualCode = await new CodeAnalysisService(NullLogger<ICodeAnalysisService>.Instance).GetFormattedCode(inputCode);

      //Assert
      actualCode.Should().NotBeEmpty().And.BeEquivalentTo(expectedCode);
    }

    private static IEnumerable<object[]> CodeSnippetTestData()
    {
      var expectedFilePaths = Directory.EnumerateFiles(@"Resources\Expected").ToList();
      var inputFilePaths = Directory.EnumerateFiles(@"Resources\Input").ToList();

      if (expectedFilePaths.Count != inputFilePaths.Count)
      {
        throw new InvalidOperationException("The directories must contain the same number of files.");
      }

      for (int i = 0; i < expectedFilePaths.Count; i++)
      {
        var expectedFileContent = File.ReadAllText(expectedFilePaths[i]) ?? throw new Exception($"Error retrieving expected file path: {expectedFilePaths[i]}");
        var inputFileContent = File.ReadAllText(inputFilePaths[i]) ?? throw new Exception($"Error retrieving input file path: {inputFilePaths[i]}");

        yield return new object[] { expectedFileContent, inputFileContent };
      }
    }
  }
}