// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System;

namespace Utilities.Extensions.Editor
{
    /// <summary>
    /// Result from a completed asynchronous process.
    /// </summary>
    public class ProcessResult
    {
        public string Arguments { get; }

        /// <summary>
        /// Exit code from completed process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Errors from completed process.
        /// </summary>
        public string[] Errors { get; }

        /// <summary>
        /// Output from completed process.
        /// </summary>
        public string[] Output { get; }

        /// <summary>
        /// Constructor for Process Result.
        /// </summary>
        /// <param name="arguments">The process into arguments.</param>
        /// <param name="exitCode">Exit code from completed process.</param>
        /// <param name="errors">Errors from completed process.</param>
        /// <param name="output">Output from completed process.</param>
        public ProcessResult(string arguments, int exitCode, string[] errors, string[] output)
        {
            Arguments = arguments;
            ExitCode = exitCode;
            Errors = errors;
            Output = output;
        }

        /// <summary>
        /// Checks <see cref="ExitCode"/> and throws an exception if it is not zero.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void ThrowIfNonZeroExitCode()
        {
            if (ExitCode != 0)
            {
                var messageBuilder = new StringBuilder($"[{ExitCode}] Failed to run: \"{Arguments}\"");

                if (Output != null)
                {

                    foreach (var line in Output)
                    {
                        messageBuilder.Append($"\n{line}");
                    }
                }

                if (Errors != null)
                {
                    foreach (var line in Errors)
                    {
                        messageBuilder.Append($"\n{line}");
                    }
                }

                throw new Exception(messageBuilder.ToString());
            }
        }
    }
}
