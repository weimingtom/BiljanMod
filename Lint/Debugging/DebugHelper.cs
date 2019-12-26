using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Lint.Native;
using DebugConsole = System.Diagnostics.Debug;

namespace Lint.Debugging
{
    /// <summary>
    ///     Represents the debug helper.
    /// </summary>
    internal static class DebugHelper
    {
        /// <summary>
        ///     Dumps the stack contents of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="caller">The caller.</param>
        public static void DumpStack(IntPtr luaState, string caller = "")
        {
            var table = new DebugTable("Stack Index", "Type", "Value");
            DebugConsole.WriteLine("--------------- STACK (" + caller + ") ---------------");
            for (var i = 1; i <= LuaLibrary.LuaGetTop(luaState); ++i)
            {
                var value = string.Empty;
                var luaType = LuaLibrary.GetLuaType(luaState, i);
                switch (luaType)
                {
                    case LuaType.Nil:
                        value = "NULL";
                        break;
                    case LuaType.Boolean:
                        value = LuaLibrary.LuaToBoolean(luaState, i).ToString();
                        break;
                    case LuaType.LightUserdata:
                    case LuaType.Table:
                    case LuaType.Function:
                    case LuaType.Userdata:
                    case LuaType.Thread:
                        value = LuaLibrary.LuaToPointer(luaState, i).ToString();
                        break;
                    case LuaType.Number:
                        IntPtr _temp, _temp2;
                        value = LuaLibrary.LuaIsInteger(luaState, i)
                            ? LuaLibrary.LuaToIntegerX(luaState, i, out _temp).ToString()
                            : LuaLibrary.LuaToNumberX(luaState, i, out _temp2).ToString(CultureInfo.CurrentCulture);
                        break;
                    case LuaType.String:
                        value = LuaLibrary.LuaToString(luaState, i);
                        break;
                }

                table.AddRow(i, luaType, value);
            }

            DebugConsole.WriteLine(table.GetOutput());
            DebugConsole.WriteLine("--------------- STACK ---------------");
        }
    }
}