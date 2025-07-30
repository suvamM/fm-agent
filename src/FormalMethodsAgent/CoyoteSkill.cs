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
            1. Read each C# file in the project to find methods that use concurrency features like async/await. Your objective is to write tests that exercise the concurrency behaviors of these methods.
            2. Once you have identified the usage of concurrency, create a new C# test project. This new project should include the Xunit framework, and reference the appropriate C# projects under test. Add the necessary test methods in a single .cs file, each method having attribute [Test].
            3. Clearly inform the user where the new test project is created, and where the test methods are defined.
            4. Compile the test project in Debug mode.
            5. Rewrite the compiled binaries of the test project to include Coyote instrumentation. This will allow Coyote to analyze the concurrency behaviors of the methods.
            6. Run the Coyote tool on the rewritten dll corresponding to the test .cs file, targeting each test method that you identified in step 1. Capture the output of the Coyote analysis.
            7. Summarize the results of the Coyote analysis, indicating whether any concurrency issues were found, and if so, provide details about the issues.
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
    [Description("Return the contents of the file from the provided path.")]
    public string ReadFileContents(
        [Description("The path to the file to read.")] string filePath)
    {
        if (!File.Exists(filePath))
            return $"File not found: {filePath}";

        return File.ReadAllText(filePath);
    }

    [KernelFunction]
    [Description("Writes the provided content to the specified file path.")]
    public string WriteFileContents(
        [Description("The path to the file to write.")] string filePath,
        [Description("The content to write to the file.")] string content)
    {
        try
        {
            File.WriteAllText(filePath, content);
            return $"Successfully wrote to {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error writing to file {filePath}: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Compiles the C# project at the specified path in Debug mode and outputs the binaries to the bin directory.")]
    private async Task<string> CompileDebugBinaries(
        [Description("The C# project (.csproj) to compile")] string projectPath,
        [Description("The path to output the compiled binaries. Should be completely distinct from where other binaries of the project are emitted")] string binariesPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build {projectPath} -c Debug -o {binariesPath}",
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

    [KernelFunction]
    [Description("Rewrites the compiled binaries of the test project to include Coyote instrumentation.")]
    private async Task<string> RewriteBinariesForCoyote(
        [Description("The path where the binaries for the compiled test project is emitted.")] string binariesPath)
    {
        // Enumerate all DLLs in the bin directory and rewrite them for Coyote
        if (!Directory.Exists(binariesPath))
            return "No binaries found to rewrite.";
        var dllFiles = Directory.GetFiles(binariesPath, "*.dll");
        foreach ( var dllFile in dllFiles) {
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
