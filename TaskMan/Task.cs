/*
 * This one file has all of the code for the TaskMan library, making it easy to drop into your own projects.
 *
 * TaskMan is released under the MIT license.
 *
 * Copyright (c) 2010 Ryan "remi" Taylor
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TaskMan {

	public class Variables : Dictionary<string,string>, IDictionary<string,string> {}

    public class Task {

		// TODO remove this?  obsolete?  a task won't work without an attribute.
        public Task() { }

        public Task(TaskAttribute attribute) {
            _attribute = attribute;
        }

        TaskAttribute _attribute;
        static Dictionary<string, Task> _allTasks  = new Dictionary<string, Task>();

        public MethodInfo Method      { get; set; }
        public string     Description { get { return _attribute.Description;            }}
        public string     Before      { get { return _attribute.Before;                 }}
        public string     After       { get { return _attribute.After;                  }}
        public string     Name        { get { return _attribute.Name ?? NameFromMethod; }}

		public string NameFromMethod {
			get { return Regex.Replace(Method.Name, "([a-z])([A-Z])", "$1:$2").ToLower(); }
		}

        public object Run() {
			return Run(null as Variables);
		}

        public object Run(Variables vars) {
            if (Method != null) {
				Log("Run: {0}", Name);
				
				if (Before != null) {
					Log("Before: {0}", Before);
					RunCallbacks(Before);
				}

				Log("Invoke: {0}.{1}", Method.DeclaringType.FullName, Method.Name);
                object result;
				if (Method.GetParameters().Length == 1)
					result = Method.Invoke(null, new object[]{ vars });
				else
					result = Method.Invoke(null, null);
				
				if (After != null) {
					Log("After: {0}", After);
					RunCallbacks(After);
				}

                return result;
            } else
                throw new Exception("No method implementation found for Task: " + Name);
        }

		public static bool Verbose = false;

		public static void Log(string message, params object[] objects) {
			if (Verbose) Console.WriteLine(message, objects);
		}

        // Clears all tasks
        public static void Clear() {
            _allTasks.Clear();
        }

        // Meant to be called from Main entry point, being passed command-line arguments
        //
        // Usage:
        //
        //   C:\Foo.Tasks.exe          # lists all available tasks
        //   C:\Foo.Tasks.exe foo:bar  # calls a task named "foo:bar"
        //
        public static void Run(string[] args) {
			HandleAndRemoveGlobalOptions(ref args);
			var variables = GetAndRemoveVariablesFromArgs(ref args);
			SetEnvironmentVariables(variables);

            if (args.Length == 0)
                ListTasks();
            else
                foreach (var task in args)
                    Run(task, variables);
        }

        public static void Run(string taskName) {
			Run(taskName, null);
		}

        public static void Run(string taskName, Variables vars) {
            CallTask(taskName, vars);
        }

        public static void CallTask(string taskName, Variables vars) {
            var task = Task.Get(taskName);

            if (task == null)
                Console.WriteLine("Task not found: {0}", taskName);
            else
                task.Run(vars);
        }

        public static void ListTasks() {
            var tasks = Task.All;

            if (tasks.Count == 0)
                Console.WriteLine("No tasks have been defined");
            else {
                Console.WriteLine("Tasks:");
                var longestTaskLength = (int) tasks.Select(t => t.Name.Length).Max();
                foreach (var task in tasks.OrderBy(task => task.Name.ToLower()))
                    Console.WriteLine("  {0}{1}{2}", task.Name, GetSpacesForList(task.Name, longestTaskLength), task.Description);
            }
        }

        public static int Count {
            get { return _allTasks.Count; }
        }

        public static List<Task> All {
            get {
                if (_allTasks.Count == 0)
                    AddTasks(GetTasksFromAssembly(Assembly.GetEntryAssembly()));
                return new List<Task>(_allTasks.Values);
            }
        }

        // Adds a list of tasks to the global scope of tasks.
        public static List<Task> AddTasks(List<Task> tasks) {
            if (tasks != null && tasks.Count > 0)
              foreach (var task in tasks)
                  _allTasks[task.Name] = task;
            return tasks;
        }

        public static Task Get(string name) {
            return All.FirstOrDefault<Task>(task => task.Name == name);
        }

        public static List<Task> LoadTasksFromAssembly(Assembly assembly) {
            return Task.AddTasks(GetTasksFromAssembly(assembly));
        }

        public static List<Task> LoadTasksFromAssembly(string assemblyPath) {
            return Task.AddTasks(GetTasksFromAssembly(assemblyPath));
        }

        public static List<Task> GetTasksFromAssembly(string assemblyPath, bool addToGlobalTasks = false) {
            return GetTasksFromAssembly(Assembly.LoadFile(assemblyPath), addToGlobalTasks);
        }

        public static List<Task> GetTasksFromAssembly(Assembly assembly, bool addToGlobalTasks = false) {
            var tasks = new List<Task>();
            foreach (Type type in assembly.GetTypes()) {
                foreach (MethodInfo method in type.GetMethods()) { // TODO update GetMethods to just get Public Static methods
                    var attributes = method.GetCustomAttributes(typeof(TaskAttribute), true);
                    if (attributes.Length > 0)
                        tasks.Add(new Task(attributes[0] as TaskAttribute){ Method = method });
                }
            }
            if (addToGlobalTasks) AddTasks(tasks);
            return tasks;
        }

		#region Private
        void RunCallbacks(string taskNamesString) {
            if (taskNamesString != null) {
                var taskNames = taskNamesString.Split(' ');
                foreach (var taskName in taskNames)
                    Run(taskName);
            }
        }

		static void HandleAndRemoveGlobalOptions(ref string[] args) {
			var arguments = new List<string>(args);
			if (arguments.Remove("-V") || arguments.Remove("--verbose"))
				Task.Verbose = true;
			args = arguments.ToArray();
		}

		static Variables GetAndRemoveVariablesFromArgs(ref string[] args) {
			var variables    = new Variables();
			var arguments    = new List<string>(args);
			var variableArgs = arguments.Where(arg => Regex.IsMatch(arg, @"=.")).ToList();
			foreach (var variableArg in variableArgs) {
				arguments.Remove(variableArg);
				var match = Regex.Match(variableArg, @"([^=]+)=""?(.*)""?");
				variables[match.Groups[1].ToString()] = match.Groups[2].ToString();
			}
			args = arguments.ToArray();
			return variables;
		}

		static void SetEnvironmentVariables(Variables vars) {
			foreach (var variable in vars)
				Environment.SetEnvironmentVariable(variable.Key, variable.Value);
		}

        static string GetSpacesForList(string taskName, int longestTaskLength, int buffer = 4) {
            var numSpaces = longestTaskLength + buffer - taskName.Length;
            var spaces    = "";
            for (var i = 0; i < numSpaces; i++)
                spaces += " ";
            return spaces;
        }
		#endregion
    }

    public class TaskAttribute : Attribute {
		public TaskAttribute(){}
        public TaskAttribute(string description) : this () {
			Description = description;
        }
        public TaskAttribute(string name, string description) : this(description) {
			Name = name;
        }

        public string Name        { get; set; }
        public string Description { get; set; }
        public string Before      { get; set; }
        public string After       { get; set; }
    }
}
