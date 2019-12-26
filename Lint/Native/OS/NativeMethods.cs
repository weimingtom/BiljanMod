using System;
using System.Runtime.InteropServices;

namespace Lint.Native.OS
{
    /// <summary>
    ///     Provides native methods required to call unmanaged DLLs from a managed environment at runtime.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        ///     Frees the file handle returned by the <see cref="dlopen(string, int)" /> method.
        /// </summary>
        /// <param name="hModule">The file handle.</param>
        /// <returns><c>true</c> if the handle is released successfully; otherwise, <c>false</c>.</returns>
        [DllImport("libdl.so")]
        public static extern int dlclose(IntPtr hModule);

        /// <summary>
        ///     Maps the specified DLL file into the address space of the calling process.
        /// </summary>
        /// <param name="fileName">The name of the DLL file.</param>
        /// <param name="flag">The resolution flag. See <see cref="https://linux.die.net/man/3/dlopen" />.</param>
        /// <returns>The handler for the DLL file.</returns>
        [DllImport("libdl.so", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flag = 2);

        /// <summary>
        ///     Gets the procedure address bound to the specified name and handle.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="symbol">The procedure name.</param>
        /// <returns>The handler for the procedure.</returns>
        [DllImport("libdl.so", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        /// <summary>
        ///     Frees the file handle returned by the <see cref="LoadLibrary(string)" /> method.
        /// </summary>
        /// <param name="hModule">The file handle.</param>
        /// <returns><c>true</c> if the handle is released successfully; otherwise, <c>false</c>.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        ///     Gets the procedure address bound to the specified name and handle.
        /// </summary>
        /// <param name="hModule">The handle.</param>
        /// <param name="procName">The procedure name.</param>
        /// <returns>The handle for the procedure.</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        ///     Maps the specified DLL file into the address space of the calling process.
        /// </summary>
        /// <param name="lpFileName">The name of the DLL file.</param>
        /// <returns>The handle for the DLL file.</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);
    }
}