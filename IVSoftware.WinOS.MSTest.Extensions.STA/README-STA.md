## IVSoftware.WinOS.MSTest.Extensions.STA

The STARunner package provides a deterministic, headless Single Threaded Apartment (STA) environment for MSTest. It is designed for scenarios where test code must run on a real WinForms UI thread with a real message pump, including:

- WinForms component testing
- APIs that require STA affinity
- UI automation or event sequencing
- Clipboard, drag-and-drop, and COM-based features
- Code paths that depend on WindowsFormsSynchronizationContext

By hosting a lightweight WinForms form on a dedicated STA thread, STARunner provides a small, isolated UI universe that behaves like a real desktop message loop.

---

### Why This Exists

Some Windows APIs require:

- the calling thread to be STA
- an active WinForms message pump
- a valid UI SynchronizationContext
- a fully created window handle
- a running dispatch loop

MSTest does not supply such an environment by default. STARunner fills this gap with a simple pattern:

```csharp
using (var sta = new STARunner())
{
    await sta.RunAsync(async () =>
    {
        // Code here executes on a real WinForms UI thread.
    });
}
```

Everything inside RunAsync executes on the isolated UI thread with proper dispatch, control creation, layout, and event behavior.

---

### Critical Setup

Your MSTest project must target Windows and enable WinForms:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>True</UseWindowsForms>
  </PropertyGroup>

</Project>
```

Without this configuration, the WinForms infrastructure required by STARunner cannot initialize.

---

### Key Features

#### 1. Real WinForms Message Pump

STARunner creates an internal UI thread, instantiates a form (typically SilentRunner), and calls Application.Run. This establishes:

- a Windows message queue
- a valid UI synchronization context
- handle creation guarantees
- proper WinForms lifecycle behavior

#### 2. Headless or Visible UI Environment

The default SilentRunner hosts a WinForms form whose handle exists but whose window remains hidden unless surfaced intentionally.

Reveal the window at any time:

```csharp
sta.MainForm.IsSilent = false;
```

#### 3. Transparent RunAsync Execution

UI-dependent code is marshaled using BeginInvoke and executed on the UI thread:

```csharp
await sta.RunAsync(async () =>
{
    var btn = new Button();
    btn.PerformClick();
    await Task.CompletedTask;
});
```

#### 4. Deterministic Teardown

At the end of the using block:

- the main form is closed on the UI thread
- the message pump exits
- the UI thread unwinds
- Dispose waits via Join

This prevents leaked threads or lingering pumps.

---

### Basic Usage

```csharp
[TestMethod]
public async Task Test_ButtonClick()
{
    using var sta = new STARunner();

    await sta.RunAsync(async () =>
    {
        var btn = new Button();
        bool clicked = false;

        btn.Click += (s, e) => clicked = true;
        btn.PerformClick();

        Assert.IsTrue(clicked);
        await Task.CompletedTask;
    });
}
```

Everything inside RunAsync runs on a true WinForms UI thread.

---

### SilentRunner

SilentRunner is the default form type. It:

- creates its handle even when not visible
- overrides SetVisibleCore to remain hidden while IsSilent is true
- can be displayed dynamically for debugging

This allows full UI behavior without interfering with automated test runs.

---

### Example: UI Updates Over Time

```csharp
[TestMethod]
public async Task Test_Countdown()
{
    using var sta = new STARunner();

    for (int i = 3; i >= 0; i--)
    {
        await Task.Delay(1000);

        await sta.RunAsync(async () =>
        {
            sta.MainForm.Text = $"Countdown: {i}";
            await Task.CompletedTask;
        });
    }
}
```

During execution, MainForm.Text is updated on the UI thread exactly as it would be in a real WinForms application.

---

### Summary

IVSoftware.WinOS.MSTest.Extensions.STA provides:

- a deterministic, isolated STA environment  
- a headless WinForms message pump  
- true UI thread semantics (Invoke, events, handles, layout)  
- a transparent async boundary through RunAsync  
- safe teardown with zero thread leakage  

It is designed for MSTest scenarios that depend on Windows Forms, COM STA components, or UI-thread behavior, without requiring any visible UI unless requested.
