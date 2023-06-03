﻿using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Moq;

namespace AvaloniaEdit.AvaloniaMocks
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new AssetLoader(),
            platform: new AppBuilder().RuntimePlatform,
            renderInterface: new MockPlatformRenderInterface(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            theme: () => CreateDefaultTheme(),
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            windowingPlatform: new MockWindowingPlatform(),
            fontManagerImpl: new MockFontManagerImpl(),
            textShaperImpl: new MockTextShaperImpl(),
            platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            renderInterface: new MockPlatformRenderInterface());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true));

        public static readonly TestServices MockWindowingPlatform = new TestServices(
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: new KeyboardNavigationHandler(),
            inputManager: new InputManager());

        public TestServices(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            ILayoutManager layoutManager = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderLoop renderLoop = null,
            ICursorFactory standardCursorFactory = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null,
            PlatformHotkeyConfiguration platformHotkeyConfiguration = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null)
        {
            AssetLoader = assetLoader;
            FocusManager = focusManager;
            InputManager = inputManager ?? new InputManager();
            KeyboardDevice = keyboardDevice ?? (() => (IKeyboardDevice)new KeyboardDevice());
            KeyboardNavigation = keyboardNavigation;
            LayoutManager = layoutManager;
            MouseDevice = mouseDevice;
            Platform = platform;
            RenderInterface = renderInterface;
            StandardCursorFactory = standardCursorFactory;
            Theme = theme;
            ThreadingInterface = threadingInterface;
            WindowImpl = windowImpl;
            WindowingPlatform = windowingPlatform;
            PlatformHotkeyConfiguration = platformHotkeyConfiguration;
            FontManagerImpl = fontManagerImpl;
            TextShaperImpl = textShaperImpl;
        }

        public IAssetLoader AssetLoader { get; }
        public IInputManager InputManager { get; }
        public IFocusManager FocusManager { get; }
        public Func<IKeyboardDevice> KeyboardDevice { get; }
        public IKeyboardNavigationHandler KeyboardNavigation { get; }
        public ILayoutManager LayoutManager { get; }
        public Func<IMouseDevice> MouseDevice { get; }
        public IRuntimePlatform Platform { get; }
        public IPlatformRenderInterface RenderInterface { get; }
        public ICursorFactory StandardCursorFactory { get; }
        public Func<Styles> Theme { get; }
        public IPlatformThreadingInterface ThreadingInterface { get; }
        public IWindowImpl WindowImpl { get; }
        public IWindowingPlatform WindowingPlatform { get; }
        public PlatformHotkeyConfiguration PlatformHotkeyConfiguration { get; }
        public IFontManagerImpl FontManagerImpl { get; }

        public ITextShaperImpl TextShaperImpl { get; }

        public TestServices With(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            ILayoutManager layoutManager = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderLoop renderLoop = null,
            ICursorFactory standardCursorFactory = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null,
            PlatformHotkeyConfiguration platformHotkeyConfiguration = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null)
        {
            return new TestServices(
                assetLoader: assetLoader ?? AssetLoader,
                focusManager: focusManager ?? FocusManager,
                inputManager: inputManager ?? InputManager,
                keyboardDevice: keyboardDevice ?? KeyboardDevice,
                keyboardNavigation: keyboardNavigation ?? KeyboardNavigation,
                layoutManager: layoutManager ?? LayoutManager,
                mouseDevice: mouseDevice ?? MouseDevice,
                platform: platform ?? Platform,
                renderInterface: renderInterface ?? RenderInterface,
                standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
                theme: theme ?? Theme,
                threadingInterface: threadingInterface ?? ThreadingInterface,
                windowingPlatform: windowingPlatform ?? WindowingPlatform,
                windowImpl: windowImpl ?? WindowImpl,
                platformHotkeyConfiguration: platformHotkeyConfiguration ?? PlatformHotkeyConfiguration,
                fontManagerImpl: fontManagerImpl ?? FontManagerImpl,
                textShaperImpl: textShaperImpl ?? TextShaperImpl);
        }

        private static Styles CreateDefaultTheme()
        {
            var result = new Styles
            {
                new SimpleTheme()
            };

            return result;
        }

        private static IPlatformRenderInterface CreateRenderInterfaceMock()
        {
            return Mock.Of<IPlatformRenderInterface>(x =>
                x.CreateStreamGeometry() == Mock.Of<IStreamGeometryImpl>(
                    y => y.Open() == Mock.Of<IStreamGeometryContextImpl>()));
        }
    }
}