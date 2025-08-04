using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormalMethodsAgent
{
    public class DotnetSkill
    {
        [KernelFunction]
        [Description("Compiles the C# project at the specified path in Debug mode and outputs the binaries to the bin directory.")]
        private async Task<string> CompileDebugBinaries(
       [Description("The C# project (.csproj) to compile")] string projectPath,
       [Description("The path to output the compiled binaries.")] string binariesPath)
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
        [Description("Creates a new xUnit test project at the specified path with the given name.")]
        private async Task<string> CreateNewTestProject(
            [Description("The path where the new test project will be created.")] string testProjectPath,
            [Description("The name of the new test project.")] string testProjectName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new xunit -o {testProjectPath} -n {testProjectName}",
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
        [Description("Adds a reference to the specified project in the test project.")]
        private async Task<string> AddNuGetPackage(
            [Description("The path to the project where the NuGet package will be added.")] string projectPath,
            [Description("The name of the NuGet package to add.")] string packageName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"add {projectPath} package {packageName}",
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
        [Description("Adds a project reference to the specified project in the test project.")]
        private async Task<string> AddProjectReference(
            [Description("The path to the test project where the reference will be added.")] string testProjectPath,
            [Description("The path to the project that will be referenced.")] string projectPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"add {testProjectPath} reference {projectPath}",
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
