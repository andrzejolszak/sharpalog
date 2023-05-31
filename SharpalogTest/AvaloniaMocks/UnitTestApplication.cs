﻿using System;
using System.Reflection;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.Utils;

namespace AvaloniaEdit.AvaloniaMocks
{
    public class UnitTestApplication : Application
    {
        public UnitTestApplication(TestServices services)
        {
            Services = services ?? new TestServices();
            RegisterServices();
        }

        public TestServices Services { get; }

        public static IDisposable Start(TestServices services = null)
        {
            AvaloniaLocator.Current = (AvaloniaLocator.CurrentMutable = new AvaloniaLocator());
            var app = new UnitTestApplication(services);
            AvaloniaLocator.CurrentMutable.BindToSelf<Application>(app);
            var updateServices = Dispatcher.UIThread.GetType().GetMethod("UpdateServices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            updateServices?.Invoke(Dispatcher.UIThread, null);
            return new Disposable(() =>
            {
                updateServices?.Invoke(Dispatcher.UIThread, null);
                AvaloniaLocator.CurrentMutable = null;
                AvaloniaLocator.Current = null;
            });
        }

        public override void RegisterServices()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
                .Bind<IFocusManager>().ToConstant(Services.FocusManager)
                .BindToSelf<IGlobalStyles>(this)
                .Bind<IInputManager>().ToConstant(Services.InputManager)
                .Bind<IKeyboardDevice>().ToConstant(Services.KeyboardDevice?.Invoke())
                .Bind<IKeyboardNavigationHandler>().ToConstant(Services.KeyboardNavigation)
                .Bind<ILayoutManager>().ToConstant(Services.LayoutManager)
                .Bind<IMouseDevice>().ToConstant(Services.MouseDevice?.Invoke())
                .Bind<IRuntimePlatform>().ToConstant(Services.Platform)
                .Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface)
                .Bind<IPlatformThreadingInterface>().ToConstant(Services.ThreadingInterface)
                .Bind<ICursorFactory>().ToConstant(Services.StandardCursorFactory)
                .Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform)
                .Bind<PlatformHotkeyConfiguration>().ToConstant(Services.PlatformHotkeyConfiguration)
                .Bind<IFontManagerImpl>().ToConstant(Services.FontManagerImpl)
                .Bind<ITextShaperImpl>().ToConstant(Services.TextShaperImpl);

            //var styles = Services.Theme?.Invoke();

            //if (styles != null)
            //{
            //    Styles.AddRange(styles);
            //}
        }
    }

    internal sealed class Disposable : IDisposable
    {
        private volatile Action _dispose;
        public Disposable(Action dispose)
        {
            _dispose = dispose;
        }
        public bool IsDisposed => _dispose == null;
        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}