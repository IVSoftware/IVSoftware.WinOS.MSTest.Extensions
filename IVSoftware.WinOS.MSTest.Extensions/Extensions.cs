using System.Diagnostics;
using System.Xml.Linq;

namespace IVSoftware.WinOS.MSTest.Extensions
{
    public static class Extensions
    {
        public static void ToClipboard(this string text, bool log = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLineIf(log, $"ADVISORY: Illegal attempt to copy empty text to clipboard.");
            }
            else
            {
                var thread = new Thread(() => Clipboard.SetText(text ?? string.Empty));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }
        public static void ToClipboard(this XElement xel)
        {
            var thread = new Thread(() => Clipboard.SetText(xel?.ToString() ?? string.Empty));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        public static string FromClipboard()
        {
            string result = string.Empty;

            var thread = new Thread(() =>
            {
                if (Clipboard.ContainsText())
                {
                    result = Clipboard.GetText();
                }
                else
                {
                    Debug.Fail("Expecting clipboard to contain text.");
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        /// <summary>
        /// Copies a generated assertion script for the specified string to the clipboard.
        /// </summary>
        /// <param name="limit">The string to generate an assertion script for.</param>
        public static void ToClipboardAssert(this string limit, string? message = null) => limit.makeAssert(message).ToClipboard();

        /// <summary>
        /// Copies a generated assertion script for the specified XML element to the clipboard.
        /// </summary>
        /// <param name="limit">The XML element to generate an assertion script for.</param>
        public static void ToClipboardAssert(this XElement limit, string? message = null) => limit.makeAssert(message).ToClipboard();

        /// <summary>
        /// Copies an escaped and formatted version of the output for updating a test limit in the code.
        /// </summary>
        /// <param name="limit">The string to generate an assertion script for.</param>
        public static void ToClipboardExpected(this string limit) => limit.makeExpected().ToClipboard();

        [Obsolete("Use ToClipboardExpected for consistency with generated output 'expected = '.")]
        public static void ToClipboardExpecting(this string limit) => limit.ToClipboardExpected();

        /// <summary>
        /// Copies an escaped and formatted version of the output for updating a test limit in the code.
        /// </summary>
        /// <param name="limit">The XML element to generate an assertion script for.</param>
        public static void ToClipboardExpected(this XElement limit) => limit.makeAssert().ToClipboard();


        [Obsolete("Use ToClipboardExpected for consistency with generated output 'expected = '.")]
        public static void ToClipboardExpecting(this XElement limit) => limit.ToClipboardExpected();

        /// <summary>
        /// Converts a string containing XML into a normalized XML string format.
        /// </summary>
        /// <param name="parseableXml">The XML string to normalize.</param>
        /// <returns>A normalized XML string representation.</returns>
        public static string ToNormalizedXML(this string parseableXml) =>
            XElement.Parse(parseableXml).ToString();


        /// Generates an assertion script for the specified string.
        /// </summary>
        /// <param name="limit">The string to generate an assertion script for.</param>
        /// <returns>A generated assertion script as a string.</returns>
        private static string makeAssert(this string limit, string? message = null)
        {
            // Remove a leading newline only, preserving all other indentation
            limit = limit.StartsWith("\r\n") ?
                        limit.Substring(2) :
                        (limit.StartsWith("\n") ? limit.Substring(1) : limit);

            // Escape double quotes within the string
            limit = limit.Replace(@"""", @"""""");

            return $@"
expected = @"" 
{limit}"";

Assert.AreEqual(
    expected.NormalizeResult(),
    actual.NormalizeResult(),
    ""{message ?? "Expecting values to match."}""
);";
        }

        /// <summary>
        /// Generates an assertion script for the specified XML element.
        /// </summary>
        /// <param name="limit">The XML element to generate an assertion script for.</param>
        /// <returns>A generated assertion script as a string.</returns>
        private static string makeExpected(this string limit) => $@"
expected = @"" 
{limit.ToString().Replace(@"""", @"""""")}""
;";

        /// <summary>
        /// Generates an assertion script for the specified XML element.
        /// </summary>
        /// <param name="limit">The XML element to generate an assertion script for.</param>
        /// <returns>A generated assertion script as a string.</returns>
        private static string makeAssert(this XElement limit, string? message = null) => $@"
expected = @""
{limit.ToString().Replace(@"""", @"""""")}
"";
Assert.AreEqual(
    expected.NormalizeResult(),
    actual.NormalizeResult(),
    ""{message ?? "Expecting values to match."}""
);";

        /// <summary>
        /// Generates an assertion script for the specified XML element.
        /// </summary>
        /// <param name="limit">The XML element to generate an assertion script for.</param>
        /// <returns>A generated assertion script as a string.</returns>
        private static string makeExpected(this XElement limit) => $@"
expected = @""
{limit.ToString().Replace(@"""", @"""""")}
"";
);";

        // <summary>
        /// Normalizes a path by removing all preceding directories up to and including the "OnePage" directory.
        /// </summary>
        /// <param name="path">The path to be normalized.</param>
        /// <returns>A normalized path starting from the "OnePage" directory.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided path is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the path does not contain a 'OnePage' directory.</exception>

        static string NormalizePathFromOnePageRoot(this string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            var split = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int onePageIndex = Array.FindIndex(split, dir => dir.Equals("OnePage", StringComparison.OrdinalIgnoreCase));

            if (onePageIndex == -1)
                throw new InvalidOperationException("The path does not contain a 'OnePage' directory.");

            var normalizedPath = string.Join(Path.DirectorySeparatorChar, split.Skip(onePageIndex));
            return normalizedPath;
        }

        /// <summary>
        /// Normalizes a collection of paths by invoking <see cref="NormalizePathFromOnePageRoot"/> on each path.
        /// </summary>
        /// <param name="paths">The collection of paths to be normalized.</param>
        /// <returns>A list of normalized paths. If the input is a list, it is modified in place.</returns>

        public static List<string> NormalizePathFromOnePageRoot(this IEnumerable<string> paths)
        {
            var tmp = paths.Select(_ => _.NormalizePathFromOnePageRoot()).ToArray();
            if (paths is List<string> list)
            {
                list.Clear();
                list.AddRange(tmp);
                return list;
            }
            else
            {
                return tmp.ToList();
            }
        }
        /// <summary>
        /// Diagnoses differences between the expected and actual strings, reporting the first mismatch found.
        /// </summary>
        /// <param name="expected">The expected string value.</param>
        /// <param name="actual">The actual string value to compare against the expected value.</param>
        /// <returns>The index of the first mismatch found in the strings.</returns>

        public static int DiagnoseAssert(this string expected, string actual)
        {
            int index = 0;

            while (index < expected.Length && index < actual.Length)
            {
                char e = expected[index], a = actual[index];

                if (e != a)
                {
                    var msg = $"Character mismatch at {index} where Expected='{e}' and Actual='{a}'.";

                    string estr = expected.Substring(index);
                    string astr = actual.Substring(index);

                    $"{expected}{Environment.NewLine}{Environment.NewLine}{actual}".ToClipboard();

                    Debug.Fail(msg);
                    break;
                }
                index++;
            }
            if (index < expected.Length)
            {
                Debug.Fail("Expected string is longer.");
            }
            else if (index < actual.Length)
            {
                Debug.Fail("Actual string is longer.");
            }
            return index;
        }

        /// <summary>
        /// Normalizes a string by removing unnecessary whitespace and line breaks, returning a single-line representation.
        /// </summary>
        /// <param name="input">The input string to be normalized.</param>
        /// <returns>A normalized string with excess whitespace removed.</returns>
        public static string NormalizeResult(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Split by any newline or whitespace character, trim each part, and filter out empty entries.
            return string.Join(" ", input
                .Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrEmpty(part)))
                .Trim();
        }
        public static async Task<DialogResult> MessageBoxAsync(
            this string message,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
            MessageBoxIcon icon = MessageBoxIcon.None,
            string caption = "Alert")
        {
            return await Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<DialogResult>();

                var thread = new Thread(() =>
                {
                    try
                    {
                        var dialogResult = MessageBox.Show(message, caption, buttons, icon, defaultButton);
                        tcs.SetResult(dialogResult);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                return tcs.Task;
            });
        }
    }
}
