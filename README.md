## IVSoftware.WinOS.MSTest.Extensions

The **MSTest Clipboard Utilities** package is designed to enhance the testing workflow by providing two primary extension methods:

1. **`ToClipboard()`**: Copies raw output to the clipboard for inspection or external use.
2. **`ToClipboardAssert()`**: Copies an escaped and formatted version of the output for direct embedding as a test limit in the code.

Additionally, the package includes optional Visual Studio code snippets that streamline the process of generating test limits on the fly, allowing developers to dynamically craft and validate test assertions during debugging sessions.

---

### Critical Setup

Before using this package, ensure the `.csproj` for your MSTest project is configured correctly. Modify your test project’s `.csproj` file to include the following properties:

```
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<!-- Critical configuration -->
		<TargetFramework>net8.0-windows</TargetFramework>
		<UseWindowsForms>True</UseWindowsForms>
		<!-- Win32 and STA threading is now enabled for this test project -->
	</PropertyGroup>

</Project>
```

Without this configuration, the `ToClipboard()` and `ToClipboardAssert()` methods will not function correctly, as they depend on Windows Forms for clipboard support.

---

### Key Features

#### 1. `ToClipboard()`
The `ToClipboard()` method copies the **actual output** (e.g., a serialized JSON string) to the clipboard for immediate inspection. This raw output can be pasted into external tools such as Notepad, JSON validators, or directly into your testing environment for manual review.

#### 2. `ToClipboardAssert()`
The `ToClipboardAssert()` method copies an **escaped and formatted version** of the output into the clipboard. This version is suitable for direct embedding into your test code as an `expected` test limit. The escaping ensures it is compatible with C# string literals, avoiding errors while preserving readability.

### Workflow Example

Below is a demonstration of how the extension methods integrate into the test workflow. The screenshots show the process in action.

##### Test Code Before
```
[TestMethod]
public void Test_RecordCreate()
{
    var record = new Record();
    var coc = new ChainOfCustodyEntry(record, Users[0], ChangeType.Create, MimeType.Class);
}
```

#### Visual Flow Example
When the templatized 'actt' code snippet is invoked, there are placeholders for "expected under test" and the assert message.

![Templatized Code Snippet](https://github.com/IVSoftware/IVSoftware.WinOS.MSTest.Extensions/raw/master/IVSoftware.WinOS.MSTest.Extensions/Images/templatized-code-snippet-first-look.png

In this arbitrary test method, the serialized 'coc' object will be the figure of merit, so the placeholders will be replaced as follows:

```
[TestMethod]
public void Test_RecordCreate()
{
    // Add string variables manually or using the 'actsu' code snippet.
    string actual, expected;

    var record = new Record();
    var coc = new ChainOfCustodyEntry(record, Users[0], ChangeType.Create, MimeType.Class);

    {
        actual = JsonConvert.SerializeObject(coc, Formatting.Indented);
        actual.ToClipboard();
        actual.ToClipboardAssert("Expecting json serialization to match.");
        { }
    }
}
```

Note that an empty code block provides a line that will bind to a breakpoint. When the debugger is paused on this line, the clipboard will contain an escaped version of the instance JSON and an assert to compart actual to expected. Before the paste operation, we are paused at the brackets:


![Edit and Continue Breakpoint](https://github.com/IVSoftware/IVSoftware.WinOS.MSTest.Extensions/raw/master/IVSoftware.WinOS.MSTest.Extensions/Images/breaking-on-empty-code-block.png)

Now just CTRL-V to paste the limit before continuing execution on the fly.

##### Test Code After
```
[TestMethod]
public void Test_RecordCreate()
{
    // Add string variables manually or using the 'actsu' code snippet.
    string actual, expected;

    var record = new Record();
    var coc = new ChainOfCustodyEntry(record, Users[0], ChangeType.Create, MimeType.Class);

    actual = JsonConvert.SerializeObject(coc, Formatting.Indented);
    actual.ToClipboard();
    actual.ToClipboardAssert("Expecting JSON serialization to match.");

    expected = @"
{
  ""Guid"": """",
  ""LocalId"": null,
  ""FirstModified"": ""0001-01-01T00:00:00+00:00"",
  ""FirstChangeTypeText"": ""Create"",
  ""LastModified"": ""0001-01-01T00:00:00+00:00"",
  ""LastChangeTypeText"": ""Create"",
  ""User"": {
    ""displayName"": ""Pat Prober"",
    ""emailAddress"": ""pat.prober@hotmail.com"",
    ""kind"": ""drive#user"",
    ""me"": false,
    ""permissionId"": ""0"",
    ""photoLink"": ""https://example.com/patricia.jpg"",
    ""ETag"": null
  },
  ""MimeTypeText"": ""Class""
}";

    Assert.AreEqual(
        expected.NormalizeResult(),
        actual.NormalizeResult(),
        "Expecting JSON serialization to match."
    );
}
```

---

### Optional Visual Studio Code Snippets

The package also includes pre-configured Visual Studio code snippets to simplify the process of setting up tests and generating test limits. These snippets are optional but can be installed to streamline your workflow.

#### Snippets Included

1. **`actsu`** - Inserts the basic structure for setting up `actual` and `expected` variables.
   ```
   string actual, expected;
   ```

2. **`act`** - Adds clipboard operations for the `actual` variable.
   ```
   actual.ToClipboard();
   actual.ToClipboardAssert();
   { }
   ```

3. **`actt`** - Generates a template for assigning `actual` a value under test, performing clipboard operations, and creating an escaped assertion.
   ```
   actual = $eut$;
   actual.ToClipboard();
   actual.ToClipboardAssert("Expecting $msg$");
   { }
   ```

#### Installing Snippets

To install the snippets:
1. Copy the provided XML snippet definitions into your Visual Studio snippets directory (typically located under `Documents\Visual Studio [Version]\Code Snippets\[Language]\My Code Snippets`).
2. Restart Visual Studio to load the new snippets.
3. Use the shortcuts (`actsu`, `act`, `actt`) within the IDE to quickly insert the snippet.

---

### First-Time Use of `ToClipboardAssert`

When using `ToClipboardAssert()` for the first time in a given test, the generated escaped JSON must be reviewed for correctness. Once verified, this JSON becomes the **gold standard** for this condition and serves as the `expected` value for future test runs.

---

### Summary

This extension package and the included code snippets provide a structured, efficient way to generate, inspect, and validate test limits on the fly. By integrating seamlessly into debugging workflows, the package supports dynamic test creation and reproducibility. This approach eliminates the arduous and error-prone process of manual copy-pasting while maintaining careful review of new or updated limits.

---
