using System.Runtime.InteropServices;

namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    public class STARunner : IDisposable
    {
        public STARunner(bool isVisible, Type? type = null)
        {
            if(type is not null)
            {
                ActivationType = type;
            }
            _uiThread = new(() =>
            {
                if (ActivationType.GetConstructor([typeof(bool)]) is { } ctorWithArg)
                {

                }
                else if (ActivationType.GetConstructor(Type.EmptyTypes) is { } ctorParameterless)
                {
                }
                else
                {
                    throw new InvalidOperationException("policy");
                }

                if (Activator.CreateInstance(ActivationType, [isVisible]) is Form success)
                {
                    MainForm = success;
                    if (MainForm.IsHandleCreated)
                    {
                        // Silent mode. Handle already exists.
                        localOnHandleCreated(MainForm, EventArgs.Empty);
                    }
                    else
                    {
                        MainForm.HandleCreated += localOnHandleCreated;
                    }

                    Application.Run(MainForm);

                    #region L o c a l F x 
                    void localOnHandleCreated(object? sender, EventArgs e)
                    {
                        MainForm.HandleCreated -= localOnHandleCreated;

                        _ = _tcsFormReady.TrySetResult(MainForm);
                        MainForm.BeginInvoke(() =>
                        {
                            // For example, if you do have two usings in one test
                            // method, the second instance might be underneath.
                            // NOTE: BringToFront has been shown to *not* be up to the task.
                            MainForm.CycleTopmost();
                        });
                    }
                    #endregion L o c a l F x
                }
                else
                {
                    throw new NullReferenceException($"{nameof(MainForm)} activation failed.");
                }
            });
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }

        private readonly Thread _uiThread;
        private readonly TaskCompletionSource<Form> _tcsFormReady = new();

        public Type ActivationType
        {
            get => _activationType;
            set
            {
                if (!Equals(_activationType, value))
                {
                    if (_mainForm is null)
                    {
                        if (value.IsAssignableTo(typeof(Form)))
                        {
                            _activationType = value;
                        }
                        else
                        {
                            throw new InvalidOperationException("Type must be assignable to Form.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Type cannot be changed once MainForm is instantiated.");
                    }
                }
            }
        }
        Type _activationType = typeof(SilentRunner);

        public Form MainForm
        {
            get => _mainForm!;
            private set
            {
                if (_mainForm is null)
                {
                    _mainForm = value;
                }
                else
                {
                    throw new InvalidOperationException("MainForm is immutable once instantiated.");
                }
            }
        }
        Form? _mainForm = null!;

        public async Task RunAsync(Func<Task> work)
        {
            var form = await _tcsFormReady.Task.ConfigureAwait(false);

            var tcs = new TaskCompletionSource();

            try
            {
                // Marshal the async work onto the UI thread
                form.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await work().ConfigureAwait(false);
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
            }
            catch (ObjectDisposedException)
            {
                // The form/thread died before we could dispatch
                tcs.SetCanceled();
            }

            // Await the completion on the caller's context
            await tcs.Task.ConfigureAwait(false);
        }

        public Task<T> RunAsync<T>(Func<Task<T>> work)
        {
            var form = _tcsFormReady.Task;

            var tcs = new TaskCompletionSource<T>();

            form.ContinueWith(_ =>
            {
                try
                {
                    MainForm.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            var result = await work().ConfigureAwait(false);
                            tcs.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                    tcs.SetCanceled();
                }
            });

            return tcs.Task;
        }


        public void Dispose()
        {
            // Wait for MainForm to exist in the first place.
            var form = _tcsFormReady.Task.Result;

            // PRE-CHECK: Is the form alive enough to accept BeginInvoke?
            if (form.IsHandleCreated && !form.IsDisposed)
            {
                try
                {
                    form.BeginInvoke(new Action(() =>
                    {
                        // POST-CHECK: The form might have died since posting
                        if (!form.IsDisposed)
                        {
                            form.Close();
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // The handle was destroyed between the outer check and BeginInvoke.
                    // Nothing more to do.
                }
            }
            _uiThread.Join();
        }
    }
}
