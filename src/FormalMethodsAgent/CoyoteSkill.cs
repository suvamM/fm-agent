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
                Arguments = $"test {dllUnderTest} --method:{methodName} -i:10000",
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
