namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    public class STARunner : IDisposable
    {
        public STARunner(Type? type = null)
        {
            if(type is not null)
            {
                ActivationType = type;
            }
            _uiThread = new(() => 
            {
                if(Activator.CreateInstance(ActivationType) is Form success)
                {
                    MainForm = success;
                    MainForm.HandleCreated += (sender, e) =>
                    {
                        _ = _tcsFormReady.TrySetResult(MainForm);
                    };
                    Application.Run(MainForm);
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
