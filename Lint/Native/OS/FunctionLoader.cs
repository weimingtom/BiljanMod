using System;
using System.Runtime.InteropServices;

namespace Lint.Native.OS
{
    /// <summary>
    ///     Represents a function loader. This class provides a convenient way of importing functions from unmanaged .dll files
    ///     when <see cref="DllImportAttribute" /> does not suffice.
    /// </summary>
    internal sealed class FunctionLoader
    {
        /// <summary>
        ///     Gets the handle of a specific DLL file.
        /// </summary>
        private static readonly Func<string, IntPtr> GetLibrary;

        /// <summary>
        ///     Gets the handle of a specific procedure.
        /// </summary>
        private static readonly Func<IntPtr, string, IntPtr> GetProcAddress;

        /// <summary>
        ///     Gets the cached handle of a native DLL file.
        /// </summary>
        private readonly IntPtr _fileHandle;

        /// <summary>
        ///     Gets the static constructor. This constructor is used to assign loading functions based on the OS the app is
        ///     running on.
        /// </summary>
        static FunctionLoader()
        {
            if (IsLinux)
            {
                GetLibrary = s => NativeMethods.dlopen(s);
                GetProcAddress = NativeMethods.dlsym;
            }
            else
            {
                GetLibrary = NativeMethods.LoadLibrary;
                GetProcAddress = NativeMethods.GetProcAddress;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionLoader" /> class with the specified native library path.
        /// </summary>
        /// <param name="nativeFilePath">The path to the native DLL file, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nativeFilePath" /> is <c>null</c>.</exception>
        /// <exception cref="BadImageFormatException">The <paramref name="nativeFilePath" /> is invalid.</exception>
        public FunctionLoader(string nativeFilePath)
        {
            if (nativeFilePath == null)
            {
                throw new ArgumentNullException("nativeFilePath");
            }

            _fileHandle = GetLibrary(nativeFilePath);
            if (_fileHandle == IntPtr.Zero)
            {
                throw new BadImageFormatException("Cannot load image at path: '" + nativeFilePath + "'");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the application is running on a Linux distro.
        /// </summary>
        private static bool IsLinux {get{ return false; /*RuntimeInformation.IsOSPlatform(OSPlatform.Linux);*/ }}

        ///// <summary>
        /////     The finalizer.
        ///// </summary>
        //~NativeFileHandler()
        //{
        //    if (IsLinux)
        //    {
        //        NativeMethods.dlclose(_fileHandle);
        //    }
        //    else
        //    {
        //        NativeMethods.FreeLibrary(_fileHandle);
        //    }
        //}

        /// <summary>
        ///     Gets a function with the specified name from the cached file handle and maps it to the specified type of a Lua
        ///     function delegate.
        /// </summary>
        /// <typeparam name="T">The type of Lua function delegate to map to.</typeparam>
        /// <param name="functionName">The function name, which must not be <c>null</c>.</param>
        /// <returns>The resulting delegate.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="functionName" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The provided <paramref name="functionName" /> is invalid.</exception>
        public Delegate LoadFunction(string functionName, Type type)
        {
            if (functionName == null)
            {
                throw new ArgumentNullException("functionName");
            }

            var procedureHandle = GetProcAddress(_fileHandle, functionName);
            if (procedureHandle == IntPtr.Zero)
            {
                throw new MissingMethodException("Function '" + functionName + "' does not exist.");
            }

            return Marshal.GetDelegateForFunctionPointer(procedureHandle, type);
        }
    }
}