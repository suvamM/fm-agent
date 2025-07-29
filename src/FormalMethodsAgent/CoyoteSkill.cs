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

public class CoyoteSkill
{
    [Description("Analyzes C# programs for concurrency issues using Coyote.")]
    [KernelFunction("RunCoyoteAnalysis")]
    public async Task<string> RunCoyoteAnalysisAsync(string projectPath)
    {
        var testMethods = FindConcurrencyTests(projectPath);
        if (!testMethods.Any())
            return "No concurrency test methods found.";

       await CompileDebugBinaries(projectPath);
       await RewriteBinariesForCoyote(projectPath);

        var resultBuilder = new StringBuilder();

        foreach (var testMethod in testMethods)
        {
            var output = await RunCoyoteOnTest(projectPath, testMethod);
            resultBuilder.AppendLine($"### Test: {testMethod} ###\n{output}\n");
        }

        return SummarizeOutput(resultBuilder.ToString());
    }

    private List<string> FindConcurrencyTests(string projectPath)
    {
        // Naive implementation: find methods with [Test] or similar attributes that use async/await or Task
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        var testMethods = new List<string>();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            bool inTest = false;
            foreach (var line in lines)
            {
                if (line.Contains("[TestMethod]") || line.Contains("[Fact]") || line.Contains("[Microsoft.Coyote.SystematicTesting.Test]")) inTest = true;

                if (inTest && (line.Contains("async") || line.Contains("Task")))
                {
                    var methodMatch = Regex.Match(line, @"\b(\w+)\s*\(");
                    if (methodMatch.Success)
                        testMethods.Add(methodMatch.Groups[1].Value);

                    inTest = false;
                }
            }
        }

        return testMethods;
    }

    private async Task<string> CompileDebugBinaries(string projectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build {projectPath} -c Debug -o {projectPath}\\bin",
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

    private async Task<string> RewriteBinariesForCoyote(string projectPath)
    {
        // Enumerate all DLLs in the bin directory and rewrite them for Coyote
        if (!Directory.Exists($"{projectPath}\\bin"))
            return "No binaries found to rewrite.";
        var dllFiles = Directory.GetFiles($"{projectPath}\\bin", "*.dll");
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

    private async Task<string> RunCoyoteOnTest(string projectPath, string methodName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "coyote",
            Arguments = $"test {projectPath}\\bin --method:{methodName}",
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

    private string SummarizeOutput(string rawOutput)
    {
        if (rawOutput.Contains("bug found"))
            return "Coyote found concurrency issues.\n\n" + rawOutput;
        else if (rawOutput.Contains("No bug found"))
            return "No concurrency issues found.";
        return "Coyote output:\n" + rawOutput;
    }
}
