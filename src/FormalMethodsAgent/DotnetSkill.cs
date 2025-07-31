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
    }
}
