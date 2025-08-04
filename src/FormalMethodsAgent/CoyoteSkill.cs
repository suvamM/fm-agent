using Microsoft.SemanticKernel;

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Microsoft.Extensions.VectorData;

namespace FormalMethodsAgent
{
    public class CoyoteSkill
    {
        [KernelFunction]
        [Description("Generates a plan for analyzing a C# project for concurrency issues using Coyote.")]
        [return: Description("A plan for analyzing the C# project using Coyote.")]
        public async Task<string> CoyoteAnalysisPlanner(
            Kernel kernel,
            [Description("The path to the C# project to analyze for concurrency issues.")] string projectPath
        )
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                return "Invalid project path provided.";
            }

            // Prompt the LLM to create a plan for Coyote analysis
            var result = await kernel.InvokePromptAsync($"""
            I am going to analyze the C# project at '{projectPath}' for concurrency issues using Coyote.
            In order to this, I need you to do the following:
            - Check if the project under test targets .NET 6.0 or older (you can find this by looking at the corresponding .csproj file). If it targets a newer .NET version, inform the user that Coyote does not support it and exit.
            - Read each C# file in the project to find methods that use concurrency features like async/await. Your objective is to write tests that exercise the concurrency behaviors of these methods. You MUST add tests that cover the following scenario: if a method M uses concurrency, then invoke M concurrently with other operations (including M itself). This ensures that we have tests that appropriately cover the concurrency behaviors of M.
            - Create a new C# xUnit project for testing (using the command dotnet new xunit -o <TestProjectPath>). Create the project in the same directory as the project under test, but ensure that the name of the test project is distinct from the project under test. Inform the user about the location and name of the new project.
            - Add the following references to the test project:
                - The project under test.
                - The Microsoft.Coyote NuGet package.
                - The FluentAssertions NuGet package.
            - In the new test project, create a new C# file containing the concurrency tests for the methods you identified in step 2. Each test should be defined as a public method with the [Microsoft.Coyote.SystematicTesting.Test] attribute. This is available via the Microsoft.Coyote package. You don't need to extend any class, and do NOT use attributes like [Fact] or [Test].
            - Compile the test project in Debug mode.
            - Rewrite the compiled binaries of the test project to include Coyote instrumentation. This will allow Coyote to analyze the concurrency behaviors of the methods. The test C# file, along with dlls pertaining to the prject under test, should be rewritten to include Coyote instrumentation.
            - Run the Coyote tool on the rewritten dll corresponding to the test .cs file, targeting each test method that you identified in step 1. Capture the output of the Coyote analysis.
            - Summarize the results of the Coyote analysis, indicating whether any concurrency issues were found, and if so, provide details about the issues.
            """);

            // Return the plan back to the agent
            return result.ToString();
        }

        [KernelFunction]
        [Description("Returns a list containing the paths of every C# file in the provided project path.")]
        private List<string> ListCSharpFiles(
            [Description("The project whose C# files needs to be enumerated")] string projectPath)
        {
            // Naive implementation: find methods with [Test] or similar attributes that use async/await or Task
            return Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories).ToList();
        }

        [KernelFunction]
        [Description("Rewrites the compiled binaries of the test project to include Coyote instrumentation.")]
        private async Task<string> RewriteBinariesForCoyote(
            [Description("The path where the binaries for the compiled test project is emitted.")] string binariesPath)
        {
            // Enumerate all DLLs in the bin directory and rewrite them for Coyote
            if (!Directory.Exists(binariesPath))
                return "No binaries found to rewrite.";
            var dllFiles = Directory.GetFiles(binariesPath, "*.dll");
            foreach (var dllFile in dllFiles)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "coyote",
                    Arguments = $"rewrite {dllFile}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psi);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (!string.IsNullOrWhiteSpace(error))
                    return $"{output}\nErrors:\n{error}";
            }

            return "Binaries rewritten for Coyote.";
        }

        [KernelFunction]
        [Description("Runs Coyote on a specified test method in the dll corresponding to the newly created test C# file and returns the output.")]
        private async Task<string> RunCoyoteOnTest(string dllUnderTest, string methodName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "coyote",
                Arguments = $"test {dllUnderTest} --method:{methodName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return string.IsNullOrWhiteSpace(error) ? output : $"{output}\nErrors:\n{error}";
        }
    }
}
