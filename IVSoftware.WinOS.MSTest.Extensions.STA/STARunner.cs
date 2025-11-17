using System.Reflection;
using System.Runtime.InteropServices;

namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    public class STARunner<T> : STARunner where T : Form
    {
        public STARunner(bool isVisible = true, Type? type = null) : base(isVisible, type) { }

        public new T MainForm
        {
            get => (T) base.MainForm;
        }
    }
    public class STARunner : IDisposable
    {
        public static STARunner CreateThread(bool isVisible = true) => new STARunner(isVisible);
        public static STARunner<T> CreateThread<T>(bool isVisible = true) where T : Form => new STARunner<T>(isVisible, typeof(T));
        public STARunner(bool isVisible=true, Type? type = null)
        {
            if(type is not null)
            {
                ActivationType = type;
            }
            _uiThread = new(() =>
            {
                // Determine constructor policy up front.
                ConstructorInfo? ctor =
                    ActivationType.GetConstructor(new[] { typeof(bool) }) ??
                    ActivationType.GetConstructor(Type.EmptyTypes);

                if (ctor is null)
                {
                    throw new InvalidOperationException(
                        "ActivationType must expose either a (bool) constructor or a parameterless constructor.");
                }

                // Activate using the chosen constructor.
                Form? form = ctor.GetParameters().Length switch
                {
                    1 => ctor.Invoke(new object[] { isVisible }) as Form,
                    0 => ctor.Invoke(null) as Form,
                    _ => null
                };

                if (form is null)
                {
                    throw new InvalidOperationException(
                        $"Activation of {ActivationType.Name} did not return a Form instance.");
                }

                MainForm = form;

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
            });
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }

        private readonly Thread _uiThread;
        public TaskCompletionSource<Form> _tcsFormReady = new();

        public async Task WaitReadyAsync() => await _tcsFormReady.Task;

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
