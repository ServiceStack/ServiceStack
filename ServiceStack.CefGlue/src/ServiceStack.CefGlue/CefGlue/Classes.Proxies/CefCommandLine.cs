namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to create and/or parse command line arguments. Arguments with
    /// '--', '-' and, on Windows, '/' prefixes are considered switches. Switches
    /// will always precede any arguments without switch prefixes. Switches can
    /// optionally have a value specified using the '=' delimiter (e.g.
    /// "-switch=value"). An argument of "--" will terminate switch parsing with all
    /// subsequent tokens, regardless of prefix, being interpreted as non-switch
    /// arguments. Switch names are considered case-insensitive. This class can be
    /// used before CefInitialize() is called.
    /// </summary>
    public sealed unsafe partial class CefCommandLine
    {
        /// <summary>
        /// Create a new CefCommandLine instance.
        /// </summary>
        public static CefCommandLine Create()
        {
            return CefCommandLine.FromNative(cef_command_line_t.create());
        }

        /// <summary>
        /// Returns the singleton global CefCommandLine object. The returned object
        /// will be read-only.
        /// </summary>
        public static CefCommandLine Global
        {
            get
            {
                // FIXME: looks, like this method asserts inside in cef debug version - 'cause global cmdline is not yet set?
                return CefCommandLine.FromNative(cef_command_line_t.get_global());
            }
        }


        /// <summary>
        /// Returns true if this object is valid. Do not call any other methods if this
        /// function returns false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_command_line_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the values of this object are read-only. Some APIs may
        /// expose read-only objects.
        /// </summary>
        public bool IsReadOnly
        {
            get { return cef_command_line_t.is_read_only(_self) != 0; }
        }

        /// <summary>
        /// Returns a writable copy of this object.
        /// </summary>
        public CefCommandLine Copy()
        {
            return CefCommandLine.FromNative(cef_command_line_t.copy(_self));
        }

        // fixme: what with InitFromArgv ?
        // <summary>
        // Initialize the command line with the specified |argc| and |argv| values.
        // The first argument must be the name of the program. This method is only
        // supported on non-Windows platforms.
        // </summary>
        // public void InitFromArgv(int argc, const char* const* argv) =0;

        // fixme: what with InitFromString ?
        // <summary>
        // Initialize the command line with the string returned by calling
        // GetCommandLineW(). This method is only supported on Windows.
        // </summary>
        // public void InitFromString(const CefString& command_line) =0;

        /// <summary>
        /// Reset the command-line switches and arguments but leave the program
        /// component unchanged.
        /// </summary>
        public void Reset()
        {
            cef_command_line_t.reset(_self);
        }

        /// <summary>
        /// Retrieve the original command line string as a vector of strings.
        /// The argv array: { program, [(--|-|/)switch[=value]]*, [--], [argument]* }
        /// </summary>
        public string[] GetArgv()
        {
            var list = libcef.string_list_alloc();
            cef_command_line_t.get_argv(_self, list);
            var result = cef_string_list.ToArray(list);
            libcef.string_list_free(list);
            return result;
        }

        /// <summary>
        /// Constructs and returns the represented command line string. Use this method
        /// cautiously because quoting behavior is unclear.
        /// </summary>
        public override string ToString()
        {
            return cef_string_userfree.ToString(
                cef_command_line_t.get_command_line_string(_self)
                );
        }

        /// <summary>
        /// Get the program part of the command line string (the first item).
        /// </summary>
        public string GetProgram()
        {
            return cef_string_userfree.ToString(
                cef_command_line_t.get_program(_self)
                ) ?? "";
        }

        /// <summary>
        /// Set the program part of the command line string (the first item).
        /// </summary>
        public void SetProgram(string value)
        {
            // if (value == null) throw new ArgumentNullException("value");

            fixed (char* value_str = value)
            {
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);

                cef_command_line_t.set_program(_self, &n_value);
            }
        }

        /// <summary>
        /// Returns true if the command line has switches.
        /// </summary>
        public bool HasSwitches
        {
            get { return cef_command_line_t.has_switches(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the command line contains the given switch.
        /// </summary>
        public bool HasSwitch(string name)
        {
            // TODO: use CheckSwitchName method

            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name.Length);

                return cef_command_line_t.has_switch(_self, &n_name) != 0;
            }
        }

        /// <summary>
        /// Returns the value associated with the given switch. If the switch has no
        /// value or isn't present this method returns the empty string.
        /// </summary>
        public string GetSwitchValue(string name)
        {
            // TODO: use CheckSwitchName method

            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name.Length);

                return cef_string_userfree.ToString(
                    cef_command_line_t.get_switch_value(_self, &n_name)
                    );
            }
        }

        /// <summary>
        /// Returns the map of switch names and values. If a switch has no value an
        /// empty string is returned.
        /// </summary>
        public IDictionary<string, string> GetSwitches()
        {
            var switches = libcef.string_map_alloc();
            cef_command_line_t.get_switches(_self, switches);
            var result = cef_string_map.ToDictionary(switches);
            libcef.string_map_free(switches);
            return result;
        }

        /// <summary>
        /// Add a switch to the end of the command line. If the switch has no value
        /// pass an empty value string.
        /// </summary>
        public void AppendSwitch(string name)
        {
            // TODO: use CheckSwitchName method
            if (StringHelper.IsNullOrWhiteSpace(name))
            {
                if (name == null) throw new ArgumentNullException("name");
                throw new ArgumentException("Switch name must be non empty or whitespace only string.", "name");
            }

            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name.Length);

                cef_command_line_t.append_switch(_self, &n_name);
            }
        }

        /// <summary>
        /// Add a switch with the specified value to the end of the command line.
        /// </summary>
        public void AppendSwitch(string name, string value)
        {
            // TODO: use CheckSwitchName method
            if (StringHelper.IsNullOrWhiteSpace(name))
            {
                if (name == null) throw new ArgumentNullException("name");
                throw new ArgumentException("Switch name must be non empty or whitespace only string.", "name");
            }

            fixed (char* name_str = name)
            fixed (char* value_str = value)
            {
                var n_name = new cef_string_t(name_str, name.Length);
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);

                cef_command_line_t.append_switch_with_value(_self, &n_name, &n_value);
            }
        }

        /// <summary>
        /// True if there are remaining command line arguments.
        /// </summary>
        public bool HasArguments
        {
            get { return cef_command_line_t.has_arguments(_self) != 0; }
        }

        /// <summary>
        /// Get the remaining command line arguments.
        /// </summary>
        public string[] GetArguments()
        {
            var arguments = libcef.string_list_alloc();
            cef_command_line_t.get_arguments(_self, arguments);
            var result = cef_string_list.ToArray(arguments);
            libcef.string_list_free(arguments);
            return result;
        }

        /// <summary>
        /// Add an argument to the end of the command line.
        /// </summary>
        public void AppendArgument(string value)
        {
            fixed (char* value_str = value)
            {
                var n_value = new cef_string_t(value_str, value.Length);

                cef_command_line_t.append_argument(_self, &n_value);
            }
        }

        /// <summary>
        /// Insert a command before the current command.
        /// Common for debuggers, like "valgrind" or "gdb --args".
        /// </summary>
        public void PrependWrapper(string wrapper)
        {
            fixed (char* wrapper_str = wrapper)
            {
                var n_wrapper = new cef_string_t(wrapper_str, wrapper.Length);

                cef_command_line_t.prepend_wrapper(_self, &n_wrapper);
            }
        }

        /// <summary>
        /// Insert an argument to the beginning of the command line.
        /// Unlike PrependWrapper this method doesn't strip argument by spaces.
        /// </summary>
        public void PrependArgument(string argument)
        {
            if (argument.IndexOf(' ') >= 0)
            {
                // When argument contains spaces, we just prepend command line with dummy wrapper
                // and then replace it with actual argument.
                PrependWrapper(".");
                SetProgram(argument);
            }
            else PrependWrapper(argument);
        }
    }
}
