﻿// 
// Copyright © Microsoft Corporation. All rights reserved.
// 
// Microsoft Public License (MS-PL)
// 
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
// 
// 1. Definitions
// 
//   The terms "reproduce," "reproduction," "derivative works," and
//   "distribution" have the same meaning here as under U.S. copyright law. A
//   "contribution" is the original software, or any additions or changes to
//   the software. A "contributor" is any person that distributes its
//   contribution under this license. "Licensed patents" are a contributor's
//   patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
//   (A) Copyright Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free copyright license
//       to reproduce its contribution, prepare derivative works of its
//       contribution, and distribute its contribution or any derivative works
//       that you create.
// 
//   (B) Patent Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free license under its
//       licensed patents to make, have made, use, sell, offer for sale,
//       import, and/or otherwise dispose of its contribution in the software
//       or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
//   (A) No Trademark License- This license does not grant you rights to use
//       any contributors' name, logo, or trademarks.
// 
//   (B) If you bring a patent claim against any contributor over patents that
//       you claim are infringed by the software, your patent license from such
//       contributor to the software ends automatically.
// 
//   (C) If you distribute any portion of the software, you must retain all
//       copyright, patent, trademark, and attribution notices that are present
//       in the software.
// 
//   (D) If you distribute any portion of the software in source code form, you
//       may do so only under this license by including a complete copy of this
//       license with your distribution. If you distribute any portion of the
//       software in compiled or object code form, you may only do so under a
//       license that complies with this license.
// 
//   (E) The software is licensed "as-is." You bear the risk of using it. The
//       contributors give no express warranties, guarantees or conditions. You
//       may have additional consumer rights under your local laws which this
//       license cannot change. To the extent permitted under your local laws,
//       the contributors exclude the implied warranties of merchantability,
//       fitness for a particular purpose and non-infringement.
//       

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.ClearScript.Util;

namespace Microsoft.ClearScript
{
    /// <summary>
    /// Provides the base implementation for all script engines.
    /// </summary>
    public abstract class ScriptEngine : IDisposable
    {
        #region constructors

        // ReSharper disable EmptyConstructor

        /// <summary>
        /// Initializes a new script engine instance.
        /// </summary>
        protected ScriptEngine()
        {
            // the help file builder (SHFB) insists on an empty constructor here
        }

        // ReSharper restore EmptyConstructor

        #endregion

        #region public members

        /// <summary>
        /// Gets the script engine's recommended file name extension for script files.
        /// </summary>
        public abstract string FileNameExtension { get; }

        /// <summary>
        /// Allows script code to access non-public host resources.
        /// </summary>
        /// <remarks>
        /// By setting this property to a type you declare that script code running in the current
        /// script engine is to be treated as if it were part of that type's implementation. Doing
        /// so does not expose any host resources to script code, but it affects which host
        /// resources are importable and which members of exposed resources are accessible.
        /// </remarks>
        public Type AccessContext { get; set; }

        /// <summary>
        /// Gets or sets a callback that can be used to halt script execution.
        /// </summary>
        /// <remarks>
        /// During script execution the script engine periodically invokes this callback to
        /// determine whether it should continue. If the callback returns <c>false</c>, the script
        /// engine terminates script execution and throws an exception.
        /// </remarks>
        public ContinuationCallback ContinuationCallback { get; set; }

        /// <summary>
        /// Allows the host to access script resources directly.
        /// </summary>
        /// <remarks>
        /// The value of this property is an object that is bound to the script engine's root
        /// namespace. It dynamically supports properties and methods that correspond to global
        /// script objects and functions.
        /// </remarks>
        public abstract dynamic Script { get; }

        /// <summary>
        /// Exposes a host object to script code.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the object.</param>
        /// <param name="target">The object to expose.</param>
        /// <remarks>
        /// For information about the mapping between host members and script-callable properties
        /// and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </remarks>
        public void AddHostObject(string itemName, object target)
        {
            AddHostObject(itemName, HostItemFlags.None, target);
        }

        /// <summary>
        /// Exposes a host object to script code with the specified options.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the object.</param>
        /// <param name="flags">A value that selects options for the operation.</param>
        /// <param name="target">The object to expose.</param>
        /// <remarks>
        /// Once a host object is exposed to script code, its members are accessible via the script
        /// language's native syntax for member access. The following table provides details about
        /// the mapping between host members and script-accessible properties and methods.
        /// <para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Member&#xA0;Type</term>
        ///         <term>Exposed&#xA0;As</term>
        ///         <description>Remarks</description>
        ///     </listheader>
        ///     <item>
        ///         <term><b>Constructor</b></term>
        ///         <term>N/A</term>
        ///         <description>
        ///         To invoke a constructor from script code, call
        ///         <see cref="HostFunctions.newObj{T}">HostFunctions.newObj(T)</see>.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><b>Property/Field</b></term>
        ///         <term><b>Property</b></term>
        ///         <description>N/A</description>
        ///     </item>
        ///     <item>
        ///         <term><b>Method</b></term>
        ///         <term><b>Method</b></term>
        ///         <description>
        ///         Overloaded host methods are merged into a single script-callable method. At
        ///         runtime the correct host method is selected based on the argument types.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><b>Generic&#xA0;Method</b></term>
        ///         <term><b>Method</b></term>
        ///         <description>
        ///         The ClearScript library supports dynamic C#-like type inference when invoking
        ///         generic methods. However, some methods require explicit type arguments. To call
        ///         such a method from script code, you must place the required number of
        ///         <see cref="AddHostType(string, HostItemFlags, Type)">host type objects</see>
        ///         at the beginning of the argument list. Doing so for methods that do not require
        ///         explicit type arguments is optional.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><b>Extension&#xA0;Method</b></term>
        ///         <term><b>Method</b></term>
        ///         <description>
        ///         Extension methods are available if the type that implements them has been
        ///         exposed in the current script engine.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><b>Indexer</b></term>
        ///         <term><b>Property</b></term>
        ///         <description>
        ///         Indexers appear as properties named "Item" that accept one or more index values
        ///         as arguments. In addition, objects that implement <see cref="IList"/> expose
        ///         properties with numeric names that match their valid indices. This includes
        ///         one-dimensional host arrays and other collections. Multidimensional host arrays
        ///         do not expose functional indexers; you must use
        ///         <see href="http://msdn.microsoft.com/en-us/library/system.array.getvalue.aspx">Array.GetValue</see>
        ///         and
        ///         <see href="http://msdn.microsoft.com/en-us/library/system.array.setvalue.aspx">Array.SetValue</see>
        ///         instead.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><b>Event</b></term>
        ///         <term><b>Property</b></term>
        ///         <description>
        ///         Events are exposed as read-only properties of type <see cref="EventSource{T}"/>.
        ///         </description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        public void AddHostObject(string itemName, HostItemFlags flags, object target)
        {
            MiscHelpers.VerifyNonNullArgument(target, "target");
            AddHostItem(itemName, flags, target);
        }

        /// <summary>
        /// Exposes a host type to script code.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="type">The type to expose.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, Type type)
        {
            AddHostType(itemName, HostItemFlags.None, type);
        }

        /// <summary>
        /// Exposes a host type to script code with the specified options.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="flags">A value that selects options for the operation.</param>
        /// <param name="type">The type to expose.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, HostItemFlags flags, Type type)
        {
            MiscHelpers.VerifyNonNullArgument(type, "type");
            AddHostItem(itemName, flags, HostType.Wrap(type));
        }

        /// <summary>
        /// Exposes a host type to script code. The type is specified by name.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="typeName">The fully qualified name of the type to expose.</param>
        /// <param name="typeArgs">Optional generic type arguments.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, string typeName, params Type[] typeArgs)
        {
            AddHostType(itemName, HostItemFlags.None, typeName, typeArgs);
        }

        /// <summary>
        /// Exposes a host type to script code with the specified options. The type is specified by name.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="flags">A value that selects options for the operation.</param>
        /// <param name="typeName">The fully qualified name of the type to expose.</param>
        /// <param name="typeArgs">Optional generic type arguments.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, HostItemFlags flags, string typeName, params Type[] typeArgs)
        {
            AddHostItem(itemName, flags, TypeHelpers.ImportType(typeName, null, false, typeArgs));
        }

        /// <summary>
        /// Exposes a host type to script code. The type is specified by type name and assembly name.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="typeName">The fully qualified name of the type to expose.</param>
        /// <param name="assemblyName">The name of the assembly that contains the type to expose.</param>
        /// <param name="typeArgs">Optional generic type arguments.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, string typeName, string assemblyName, params Type[] typeArgs)
        {
            AddHostType(itemName, HostItemFlags.None, typeName, assemblyName, typeArgs);
        }

        /// <summary>
        /// Exposes a host type to script code with the specified options. The type is specified by
        /// type name and assembly name.
        /// </summary>
        /// <param name="itemName">A name for the new global script item that will represent the type.</param>
        /// <param name="flags">A value that selects options for the operation.</param>
        /// <param name="typeName">The fully qualified name of the type to expose.</param>
        /// <param name="assemblyName">The name of the assembly that contains the type to expose.</param>
        /// <param name="typeArgs">Optional generic type arguments.</param>
        /// <remarks>
        /// Host types are exposed to script code in the form of objects whose properties and
        /// methods are bound to the type's static members and nested types. If the type has
        /// generic parameters, the corresponding object will be invocable with type arguments to
        /// yield a specific type.
        /// <para>
        /// For more information about the mapping between host members and script-callable
        /// properties and methods, see <see cref="AddHostObject(string, HostItemFlags, object)"/>.
        /// </para>
        /// </remarks>
        public void AddHostType(string itemName, HostItemFlags flags, string typeName, string assemblyName, params Type[] typeArgs)
        {
            AddHostItem(itemName, flags, TypeHelpers.ImportType(typeName, assemblyName, true, typeArgs));
        }

        /// <summary>
        /// Executes script code.
        /// </summary>
        /// <param name="code">The script code to execute.</param>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as a statement.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with an automatically selected name. This document will not be discarded
        /// after execution.
        /// </para>
        /// </remarks>
        public void Execute(string code)
        {
            Execute(null, code);
        }

        /// <summary>
        /// Executes script code with an associated document name.
        /// </summary>
        /// <param name="documentName">A document name for the script code. Currently this name is used only as a label in presentation contexts such as debugger user interfaces.</param>
        /// <param name="code">The script code to execute.</param>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as a statement.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with the specified name. This document will not be discarded after execution.
        /// </para>
        /// </remarks>
        public void Execute(string documentName, string code)
        {
            Execute(documentName, false, code);
        }

        /// <summary>
        /// Executes script code with an associated document name, optionally discarding the document after execution.
        /// </summary>
        /// <param name="documentName">A document name for the script code. Currently this name is used only as a label in presentation contexts such as debugger user interfaces.</param>
        /// <param name="discard"><c>True</c> to discard the script document after execution, <c>false</c> otherwise.</param>
        /// <param name="code">The script code to execute.</param>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as a statement.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with the specified name. Discarding this document removes it from view but
        /// has no effect on the script engine.
        /// </para>
        /// </remarks>
        public void Execute(string documentName, bool discard, string code)
        {
            Execute(documentName, code, false, discard);
        }

        /// <summary>
        /// Executes script code as a command.
        /// </summary>
        /// <param name="command">The script command to execute.</param>
        /// <returns>The command output.</returns>
        /// <remarks>
        /// This method is similar to <see cref="Evaluate(string)"/> but optimized for command
        /// consoles. The specified command must be limited to a single expression or statement.
        /// Script engines can override this method to customize command execution as well as the
        /// process of converting the result to a string for console output.
        /// </remarks>
        public virtual string ExecuteCommand(string command)
        {
            return GetCommandResultString(Evaluate("Command", true, command, false));
        }

        /// <summary>
        /// Evaluates script code.
        /// </summary>
        /// <param name="code">The script code to evaluate.</param>
        /// <returns>The result value.</returns>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as an expression.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with an automatically selected name. This document will be discarded after
        /// execution.
        /// </para>
        /// </remarks>
        public object Evaluate(string code)
        {
            return Evaluate(null, code);
        }

        /// <summary>
        /// Evaluates script code with an associated document name.
        /// </summary>
        /// <param name="documentName">A document name for the script code. Currently this name is used only as a label in presentation contexts such as debugger user interfaces.</param>
        /// <param name="code">The script code to evaluate.</param>
        /// <returns>The result value.</returns>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as an expression.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with the specified name. This document will be discarded after execution.
        /// </para>
        /// </remarks>
        public object Evaluate(string documentName, string code)
        {
            return Evaluate(documentName, true, code);
        }

        /// <summary>
        /// Evaluates script code with an associated document name, optionally discarding the document after execution.
        /// </summary>
        /// <param name="documentName">A document name for the script code. Currently this name is used only as a label in presentation contexts such as debugger user interfaces.</param>
        /// <param name="discard"><c>True</c> to discard the script document after execution, <c>false</c> otherwise.</param>
        /// <param name="code">The script code to evaluate.</param>
        /// <returns>The result value.</returns>
        /// <remarks>
        /// In some script languages the distinction between statements and expressions is
        /// significant but ambiguous for certain syntactic elements. This method always
        /// interprets the specified script code as an expression.
        /// <para>
        /// If a debugger is attached, it will present the specified script code to the user as a
        /// document with the specified name. Discarding this document removes it from view but
        /// has no effect on the script engine.
        /// </para>
        /// </remarks>
        public object Evaluate(string documentName, bool discard, string code)
        {
            return Evaluate(documentName, discard, code, true);
        }

        /// <summary>
        /// Interrupts script execution and causes the script engine to throw an exception.
        /// </summary>
        /// <remarks>
        /// This method can be called safely from any thread.
        /// </remarks>
        public abstract void Interrupt();

        #endregion

        #region internal members

        internal abstract void AddHostItem(string itemName, HostItemFlags flags, object item);

        internal abstract object MarshalToScript(object obj, HostItemFlags flags);

        internal object MarshalToScript(object obj)
        {
            return MarshalToScript(obj, HostItemFlags.None);
        }

        internal object[] MarshalToScript(object[] args)
        {
            return args.Select(MarshalToScript).ToArray();
        }

        internal abstract object MarshalToHost(object obj);

        internal object[] MarshalToHost(object[] args)
        {
            return args.Select(MarshalToHost).ToArray();
        }

        internal abstract object Execute(string documentName, string code, bool evaluate, bool discard);

        internal object Evaluate(string documentName, bool discard, string code, bool marshalResult)
        {
            var result = Execute(documentName, code, true, discard);
            if (marshalResult)
            {
                result = MarshalToHost(result);
            }

            return result;
        }

        internal string GetCommandResultString(object result)
        {
            var hostItem = result as HostItem;
            if (hostItem != null)
            {
                if (hostItem.Target is IHostVariable)
                {
                    return result.ToString();
                }
            }

            var marshaledResult = MarshalToHost(result);

            if (marshaledResult is VoidResult)
            {
                return null;
            }

            if (marshaledResult == null)
            {
                return "[null]";
            }

            if (marshaledResult is Undefined)
            {
                return marshaledResult.ToString();
            }

            if (marshaledResult is ScriptItem)
            {
                return "[ScriptItem]";
            }

            return result.ToString();
        }

        internal void RequestInterrupt()
        {
            var tempScriptFrame = CurrentScriptFrame;
            if (tempScriptFrame != null)
            {
                tempScriptFrame.InterruptRequested = true;
            }
        }

        #endregion

        #region host-side invocation

        internal void HostInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                var registeredException = RegisterHostException(exception);
                if (registeredException != exception)
                {
                    throw registeredException;
                }

                throw;
            }
        }

        internal T HostInvoke<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception exception)
            {
                var registeredException = RegisterHostException(exception);
                if (registeredException != exception)
                {
                    throw registeredException;
                }

                throw;
            }
        }

        private Exception RegisterHostException(Exception exception)
        {
            while (exception is TargetInvocationException)
            {
                var innerException = exception.InnerException;
                if (innerException == null)
                {
                    break;
                }

                exception = innerException;
            }

            if (CurrentScriptFrame != null)
            {
                CurrentScriptFrame.SetHostException(exception);
            }

            return exception;
        }

        #endregion

        #region script-side invocation

        internal ScriptFrame CurrentScriptFrame { get; private set; }

        internal virtual void ScriptInvoke(Action action)
        {
            var prevScriptFrame = CurrentScriptFrame;
            CurrentScriptFrame = new ScriptFrame();

            try
            {
                action();
            }
            catch (Exception)
            {
                var exception = CurrentScriptFrame.Exception;
                if (exception != null)
                {
                    throw exception;
                }

                throw;
            }
            finally
            {
                CurrentScriptFrame = prevScriptFrame;
            }
        }

        internal virtual T ScriptInvoke<T>(Func<T> func)
        {
            var prevScriptFrame = CurrentScriptFrame;
            CurrentScriptFrame = new ScriptFrame();

            try
            {
                return func();
            }
            catch (Exception)
            {
                var exception = CurrentScriptFrame.Exception;
                if (exception != null)
                {
                    throw exception;
                }

                throw;
            }
            finally
            {
                CurrentScriptFrame = prevScriptFrame;
            }
        }

        internal void ThrowScriptFrameException()
        {
            if (CurrentScriptFrame != null)
            {
                var exception = CurrentScriptFrame.ReportedException;
                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        #endregion

        #region synchronized invocation

        internal virtual void SyncInvoke(Action action)
        {
            action();
        }

        internal virtual T SyncInvoke<T>(Func<T> func)
        {
            return func();
        }

        #endregion

        #region extension method table

        private readonly ExtensionMethodTable extensionMethodTable = new ExtensionMethodTable();

        internal void ProcessExtensionMethodType(Type type)
        {
            if (extensionMethodTable.ProcessType(type))
            {
                bindCache.Clear();
            }
        }

        internal ExtensionMethodSummary ExtensionMethodSummary
        {
            get { return extensionMethodTable.Summary; }
        }

        #endregion

        #region bind cache

        private readonly Dictionary<BindSignature, object> bindCache = new Dictionary<BindSignature, object>();

        internal void CacheBindResult(BindSignature signature, object result)
        {
            bindCache.Add(signature, result);
        }

        internal bool TryGetCachedBindResult(BindSignature signature, out object result)
        {
            return bindCache.TryGetValue(signature, out result);
        }

        #endregion

        #region disposition / finalization

        /// <summary>
        /// Releases all resources used by the script engine.
        /// </summary>
        /// <remarks>
        /// Call <c>Dispose()</c> when you are finished using the script engine. <c>Dispose()</c>
        /// leaves the script engine in an unusable state. After calling <c>Dispose()</c>, you must
        /// release all references to the script engine so the garbage collector can reclaim the
        /// memory that the script engine was occupying.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the script engine and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>True</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <remarks>
        /// This method is called by the public <see cref="Dispose()"/> method and the
        /// <see cref="Finalize">Finalize</see> method. <see cref="Dispose()"/> invokes the
        /// protected <c>Dispose(Boolean)</c> method with the <paramref name="disposing"/>
        /// parameter set to <c>true</c>. <see cref="Finalize">Finalize</see> invokes
        /// <c>Dispose(Boolean)</c> with <paramref name="disposing"/> set to <c>false</c>.
        /// </remarks>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the script engine is reclaimed by garbage collection.
        /// </summary>
        /// <remarks>
        /// This method overrides <see cref="System.Object.Finalize"/>. Application code should not
        /// call this method; an object's <c>Finalize()</c> method is automatically invoked during
        /// garbage collection, unless finalization by the garbage collector has been disabled by a
        /// call to <see cref="System.GC.SuppressFinalize"/>.
        /// </remarks>
        ~ScriptEngine()
        {
            Dispose(false);
        }

        #endregion

        #region Nested type: ScriptFrame

        internal class ScriptFrame
        {
            public bool InterruptRequested { get; set; }

            private Exception hostException;
            public void SetHostException(Exception value)
            {
                hostException = value;
            }

            private Exception scriptError;
            public void SetScriptError(Exception value)
            {
                scriptError = value;
            }

            private Exception pendingScriptError;
            public void SetPendingScriptError(Exception value)
            {
                pendingScriptError = value;
            }

            public Exception Exception
            {
                get { return hostException ?? scriptError ?? pendingScriptError; }
            }

            public Exception ReportedException
            {
                get { return hostException ?? scriptError; }
            }
        }

        #endregion
    }
}