using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BoxPushing.Tests
{
    /// <summary>
    /// Runs the EditMode test suite through the standard TestRunnerApi and writes a text
    /// summary to a file. Exists because this project pins com.unity.test-framework 1.1.33,
    /// which predates the newer UnityTestApi helper.
    /// </summary>
    public static class EditModeTestRunner
    {
        public static void RunAsync(string outputPath)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCallbacks(outputPath));
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            private readonly string outputPath;

            public ResultCallbacks(string outputPath)
            {
                this.outputPath = outputPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                    File.WriteAllText(outputPath, Summarize(result));
                }
                catch (Exception e)
                {
                    File.WriteAllText(outputPath, "Failed to write results: " + e);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static string Summarize(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"ResultState: {result.ResultState}");
                sb.AppendLine($"Passed: {result.PassCount}");
                sb.AppendLine($"Failed: {result.FailCount}");
                sb.AppendLine($"Skipped: {result.SkipCount}");
                sb.AppendLine($"Inconclusive: {result.InconclusiveCount}");
                sb.AppendLine();

                var failures = new List<ITestResultAdaptor>();
                CollectFailures(result, failures);
                sb.AppendLine($"Failures ({failures.Count}):");
                foreach (ITestResultAdaptor failure in failures)
                {
                    sb.AppendLine($"- {failure.FullName}");
                    sb.AppendLine($"  {failure.ResultState}: {failure.Message}");
                    if (!string.IsNullOrEmpty(failure.StackTrace))
                        sb.AppendLine($"  {failure.StackTrace.Replace("\n", "\n  ")}");
                }

                return sb.ToString();
            }

            private static void CollectFailures(ITestResultAdaptor node, List<ITestResultAdaptor> failures)
            {
                if (node.TestStatus == TestStatus.Failed && !node.HasChildren)
                {
                    failures.Add(node);
                    return;
                }

                if (node.HasChildren)
                {
                    foreach (ITestResultAdaptor child in node.Children)
                        CollectFailures(child, failures);
                }
            }
        }
    }
}
