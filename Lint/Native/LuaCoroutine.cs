using System;
using Lint.Exceptions;
using Lint.ObjectTranslation;
using Lint.Native.LuaHooks;

namespace Lint.Native
{
    /// <summary>
    ///     Specifies the status of a Lua coroutine.
    /// </summary>
    public enum CoroutineStatus
    {
        /// <summary>
        ///     The coroutine is currently running.
        /// </summary>
        Running = 0,

        /// <summary>
        ///     The coroutine has either finished its execution or encountered an error.
        /// </summary>
        Dead = 1,

        /// <summary>
        ///     The coroutine yielded.
        /// </summary>
        Suspended = 2,

        /// <summary>
        ///     The coroutine has invoked a subroutine.
        /// </summary>
        Normal = 3
    }

    /*
     * TODO:
     *      [] Fix LuaXMove failing to do its job properly when moving results between states
     */

    /// <summary>
    ///     Represents a Lua couroutine.
    /// </summary>
    /// <remarks>
    ///     Lua's couroutines are similar to C#'s threads. The difference between the two is that threads work in parallel,
    ///     whereas coroutines are collaborative and run one at a time.
    /// </remarks>
    public sealed class LuaCoroutine : LuaObject
    {
        /// <inheritdoc />
        internal LuaCoroutine(IntPtr state, int reference) : base(state, reference)
        {
        }

        /// <summary>
        ///     Gets the state associated with this coroutine.
        /// </summary>
        public IntPtr CoroutineState
        {
            get
            {
                PushToStack();
                var handle = LuaLibrary.LuaToThread(State, -1);
                LuaLibrary.LuaPop(State, 1);
                return handle;
            }
        }

        /// <summary>
        ///     Gets the coroutine's status.
        /// </summary>
        public CoroutineStatus Status
        {
            get
            {
                var coroutineState = LuaLibrary.LuaToThread(State, 1);
                if (State == coroutineState)
                {
                    return CoroutineStatus.Running;
                }

                var status = (LuaThreadStatus) LuaLibrary.LuaStatus(State);
                switch (status)
                {
                    case LuaThreadStatus.LUA_OK:
                		LuaDebug _temp;
                        if (LuaLibrary.LuaGetStack(State, 0, out _temp) == 1)
                        {
                            return CoroutineStatus.Normal;
                        }

                        if (LuaLibrary.LuaGetTop(State) == 0)
                        {
                            return CoroutineStatus.Dead;
                        }

                        return CoroutineStatus.Suspended;
                    case LuaThreadStatus.LUA_YIELD:
                        return CoroutineStatus.Suspended;
                    default:
                        return CoroutineStatus.Dead;
                }
            }
        }

        public struct ResumeTuple
        {
        	public bool success; 
        	public string errorMsg; 
        	public object[] res;
        	
        	public ResumeTuple(bool success, string errorMsg, object[] res)
        	{
        		this.success = success;
        		this.errorMsg = errorMsg;
        		this.res = res;
        	}
        }
        
        /// <summary>
        ///     Resumes (or starts) the coroutine.
        /// </summary>
        /// <param name="nargs">The number of arguments to be passed to the coroutine from the main state.</param>
        /// <returns>
        ///     A <see cref="ValueTuple{T1, T2, T3}" /> denoting the status, the error message and the results. If the
        ///     coroutine finishes its execution without errors the method returns <c>true</c> and the coroutine's results. The
        ///     error message is <c>null</c> in this case. If the coroutine encounters an error during its execution the method
        ///     returns <c>false</c> and the error message describing the error. The results are empty in this case.
        /// </returns>
        /// <exception cref="LuaException">The callee's stack does not have enough space to fit the arguments.</exception>
        /// <exception cref="LuaException">The coroutine is dead.</exception>
        /// <exception cref="LuaException">The callee's stack does not have enough space to fit the results.</exception>
        public ResumeTuple Resume(int nargs = 0)
        {
        	bool success = false; string errorMsg = null; object[] res = new object[0];
            if (!LuaLibrary.LuaCheckStack(State, nargs))
            {
                throw new LuaException("The stack does not have enough space to fit that many arguments.");
            }

            if (LuaLibrary.LuaStatus(State) == (int) LuaThreadStatus.LUA_OK && LuaLibrary.LuaGetTop(State) == 0)
            {
                throw new LuaException("Cannot resume a dead coroutine.");
            }

            LuaLibrary.LuaXMove(State, State, nargs); // Exchange the requested arguments between threads
            int _temp;
            var threadStatus = (LuaThreadStatus) LuaLibrary.LuaResume(State, IntPtr.Zero, nargs, out _temp);
            var oldStackTop = LuaLibrary.LuaGetTop(State);
            if (threadStatus == LuaThreadStatus.LUA_OK || threadStatus == LuaThreadStatus.LUA_YIELD)
            {
                var numberOfResults = LuaLibrary.LuaGetTop(State); // The results are all that's left on the stack
                if (!LuaLibrary.LuaCheckStack(State, numberOfResults + 1)) // Check the stack
                {
                    LuaLibrary.LuaPop(State, numberOfResults);
                    throw new LuaException("The stack does not have enough space to fit that many results.");
                }

                LuaLibrary.LuaXMove(State, State, numberOfResults); // Propagate the results back to the caller

                res = new object[numberOfResults];
                var newStackTop = LuaLibrary.LuaGetTop(State);
                for (var i = oldStackTop + 1; i <= newStackTop; ++i)
                {
                    res[i - oldStackTop - 1] = ObjectTranslator.GetObject(State, i);
                }

                LuaLibrary.LuaPop(State, numberOfResults);
                success = true;
            }
            else
            {
                LuaLibrary.LuaXMove(State, State, 1); // Propagate the error message back to the caller
                errorMsg = (string) ObjectTranslator.GetObject(State, -1); // Get the error message
                LuaLibrary.LuaPop(State, 1); // Pop the error message
                success = false;
            }

            return new ResumeTuple(success, errorMsg, res);
        }

        /// <summary>
        ///     Resumes (or starts) the coroutine.
        /// </summary>
        /// <param name="arguments">The arguments to be pushed directly to the coroutine's stack.</param>
        /// <returns>
        ///     A <see cref="ValueTuple{T1, T2, T3}" /> denoting the status, the error message and the results. If the
        ///     coroutine finishes its execution without errors the method returns <c>true</c> and the coroutine's results. The
        ///     error message is <c>null</c> in this case. If the coroutine encounters an error during its execution the method
        ///     returns <c>false</c> and the error message describing the error. The results are empty in this case.
        /// </returns>
        /// <exception cref="LuaException">The callee's stack does not have enough space to fit the arguments.</exception>
        /// <exception cref="LuaException">The coroutine is dead.</exception>
        /// <exception cref="LuaException">The callee's stack does not have enough space to fit the results.</exception>
        public ResumeTuple Resume(object[] arguments = null)
        {
            var args = arguments ?? new object[0];
            bool success = false; string errorMsg = null; object[] res = new object[0];
            if (!LuaLibrary.LuaCheckStack(State, args.Length))
            {
                throw new LuaException("The stack does not have enough space to fit that many arguments.");
            }

            if (LuaLibrary.LuaStatus(State) == (int) LuaThreadStatus.LUA_OK && LuaLibrary.LuaGetTop(State) == 0)
            {
                throw new LuaException("Cannot resume a dead coroutine.");
            }

            foreach (var arg in args)
            {
                ObjectTranslator.PushToStack(State, arg);
            }
			
            int _temp;
            var threadStatus = (LuaThreadStatus) LuaLibrary.LuaResume(State, IntPtr.Zero, args.Length, out _temp);
            var oldStackTop = LuaLibrary.LuaGetTop(State);
            if (threadStatus == LuaThreadStatus.LUA_OK || threadStatus == LuaThreadStatus.LUA_YIELD)
            {
                var numberOfResults = LuaLibrary.LuaGetTop(State); // The results are all that's left on the stack
                if (!LuaLibrary.LuaCheckStack(State, numberOfResults + 1)) // Check the stack
                {
                    LuaLibrary.LuaPop(State, numberOfResults);
                    throw new LuaException("The stack does not have enough space to fit that many results.");
                }

                LuaLibrary.LuaXMove(State, State, numberOfResults); // Propagate the results back to the caller

                res = new object[numberOfResults];
                var newStackTop = LuaLibrary.LuaGetTop(State);
                for (var i = oldStackTop + 1; i <= newStackTop; ++i)
                {
                    res[i - oldStackTop - 1] = ObjectTranslator.GetObject(State, i);
                }

                LuaLibrary.LuaPop(State, numberOfResults);
                success = true;
            }
            else
            {
                LuaLibrary.LuaXMove(State, State, 1); // Propagate the error message back to the caller
                errorMsg = (string) ObjectTranslator.GetObject(State, -1); // Get the error message
                LuaLibrary.LuaPop(State, 1); // Pop the error message
                success = false;
            }

            return new ResumeTuple(success, errorMsg, res);
        }
    }
}