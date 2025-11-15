## IVSoftware.WinOS.MSTest.Extensions.STA

STARunner provides a deterministic Single Threaded Apartment (STA) environment for MSTest. It hosts a lightweight WinForms message pump on a dedicated UI thread, giving tests access to real Windows UI semantics when required by WinForms, COM, or synchronization-context–dependent code.

---

### Test-Driven UI Component Development

Having a test container sandbox for UI components under development accelerates the cycle - as long as the UI thread and message loop are predictable and stable. STARunner provides that environment with minimal syntax overhead.

```csharp
[TestMethod]
public async Task Test_CanonicalPOC()
{
    using var sta = new STARunner(isVisible: false);
    await sta.RunAsync(localStaTest);

    // Encapsulate the local testing to be done on the STA thread.
    async Task localStaTest()
    {
        Assert.IsFalse(
            sta.MainForm.InvokeRequired,
            $"Expecting confirmation of UI thread context. No marshal is needed.");

        // Manipulate the UI
        sta.MainForm.Text = "Hello";
        Assert.IsInstanceOfType<SilentRunner>(sta.MainForm);
        Assert.IsTrue(sta.MainForm.IsHandleCreated);
        Assert.AreEqual("Hello", sta.MainForm.Text);

        await Task.CompletedTask;
    }
}
```
___

To test your app's Main Form, pass its Type into the constructor of STARunner.

```
[TestMethod]
public async Task Test_CustomUserForm()
{
    using var sta = new STARunner(isVisible: false, typeof(UserForm));
    await sta.RunAsync(localStaTest);

    #region L o c a l F x 
    async Task localStaTest()
    {
        Assert.IsFalse(
            sta.MainForm.InvokeRequired,
            $"Expecting confirmation of UI thread context. No marshal is needed.");

        // Manipulate the UI
        sta.MainForm.Text = "Hello";
        Assert.IsInstanceOfType<UserForm>(sta.MainForm);
        Assert.IsTrue(sta.MainForm.IsHandleCreated);
        Assert.AreEqual("Hello", sta.MainForm.Text);

        await Task.Delay(TimeSpan.FromSeconds(2.5));
    }
    #endregion L o c a l F x
}

class UserForm : Form 
{
    public UserForm()
    {
        Text = nameof(UserForm);
        BackColor = Color.AliceBlue;
        StartPosition = FormStartPosition.CenterScreen;
    }
}
```

Either way, everything inside `RunAsync` runs on the STA thread with normal WinForms behavior (layout, events, handle creation, etc.). 

---

### Project Setup

Your test project must target Windows and enable WinForms:

```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWindowsForms>True</UseWindowsForms>
</PropertyGroup>
```

---

### Core Concepts

#### Real Message Pump
STARunner creates an internal form and calls `Application.Run`, supplying:
- a UI thread with a message queue  
- proper synchronization context  
- predictable control initialization  

#### Silent or Visible Mode
By default the form is hidden but alive. You can surface it at any time:

```csharp
sta.MainForm.IsSilent = false;
```

#### RunAsync
`RunAsync` marshals work onto the STA thread:

```csharp
await sta.RunAsync(async () =>
{
    var btn = new Button();
    btn.PerformClick();
    await Task.CompletedTask;
});
```

#### Deterministic Teardown
Disposing STARunner closes the form, exits the loop, and joins the thread.  
No leaked UI threads, no phantom message pumps.

---

### Example: Direct UI Execution

```csharp
[TestMethod]
public async Task Test_MonolithicVisible()
{
    using var sta = new STARunner(isVisible: true);

    await sta.RunAsync(async () =>
    {
        sta.MainForm.Text = "Main Form";

        for (int n = 5; n >= 0; n--)
        {
            sta.MainForm.Text = $"Shutdown in {n}";
            await Task.Delay(1000);
        }
    });
}
```

Inside `RunAsync`, the test is effectively a tiny WinForms app.

---

### Example: Popup UI in a Silent Environment

```csharp
await sta.RunAsync(async () =>
{
    var popup = new Form
    {
        Size = new Size(300, 100),
        FormBorderStyle = FormBorderStyle.None
    };

    var label = new Label
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

    popup.Controls.Add(label);
    popup.Show();
    popup.PerformLayout();
    popup.Update();

    for (int n = 5; n >= 0; n--)
    {
        label.Text = $"Shutdown in {n}";
        await Task.Delay(1000);
    }
});
```

---

### Summary

STARunner supplies:

- a dedicated STA UI thread  
- an isolated WinForms message pump  
- true handle, layout, and event semantics  
- transparent async boundaries via `RunAsync`  
- deterministic teardown  

Ideal for MSTest scenarios requiring WinForms behavior, COM STA affinity, or UI-thread–dependent components, without requiring visible UI unless requested.
