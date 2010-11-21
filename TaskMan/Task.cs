using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace TaskMan {

    public class Task {

        // we hold a reference to the actual attribute instance 
        // so we can retrieve properties set via named parameters 
        // which are set AFTER the constructor is called
        TaskAttribute _attribute;

        public string Name        { get { return _attribute.Name;        } }
        public string Description { get { return _attribute.Description; } }
        public string Before      { get { return _attribute.Before;      } }
        public string After       { get { return _attribute.After;       } }

        public MethodInfo Method  { get; set; }

        public Task() { }
        public Task(TaskAttribute attribute) {
            _attribute = attribute;
        }

        void RunCallbacks(string taskNamesString) {
            if (taskNamesString != null) {
                var taskNames = taskNamesString.Split(' ');
                foreach (var taskName in taskNames)
                    Run(taskName);
            }
        }

        public object Run() {
            if (Method != null) {
                RunCallbacks(Before);
                var result = Method.Invoke(null, null);
                RunCallbacks(After);
                return result;
            } else
                throw new Exception("No method implementation found for Task: " + Name);
        }

        static Dictionary<string, Task> _allTasks  = new Dictionary<string, Task>();
        public static Dictionary<string, Task> _tempTasks = new Dictionary<string, Task>();

        // Clears all tasks
        public static void Clear() {
            _allTasks.Clear();
        }

        // Meant to be called from Main entry point, being passed command-line arguments
        //
        // Usage:
        //
        //   C:\ShopView.Tasks.exe          # lists all available tasks
        //   C:\ShopView.Tasks.exe foo:bar  # calls a task named "foo:bar"
        //
        public static void Run(string[] args) {
            if (args.Length == 0)
                ListTasks();
            else
                foreach (var task in args)
                    Run(task);
        }

        public static void Run(string taskName) {
            CallTask(taskName);
        }

        public static void CallTask(string taskName) {
            var task = Task.Get(taskName);

            if (task == null)
                Console.WriteLine("Task not found: {0}", taskName);
            else {
                Console.WriteLine(taskName);
                task.Run();
            }
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

        static string GetSpacesForList(string taskName, int longestTaskLength, int buffer = 4) {
            var numSpaces = longestTaskLength + buffer - taskName.Length;
            var spaces    = "";
            for (var i = 0; i < numSpaces; i++)
                spaces += " ";
            return spaces;
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
                  _allTasks.Add(task.Name, task);
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
            _tempTasks.Clear();
            var tasks = new List<Task>();
            foreach (Type type in assembly.GetTypes()) {
                foreach (MethodInfo method in type.GetMethods()) {
                    var attributes = method.GetCustomAttributes(typeof(TaskAttribute), true);
                    if (attributes.Length > 0) {
                        var taskAttribute = attributes[0] as TaskAttribute;
                        var task          = _tempTasks.First(t => t.Key == taskAttribute.Name).Value;
                        task.Method = method;
                        tasks.Add(task);
                    }
                }
            }
            if (addToGlobalTasks)
                AddTasks(tasks);
            _tempTasks.Clear();
            return tasks;
        }
    }

    // TODO this currently doesn't support using names parameters ... we REALLY need to fix this!
    public class TaskAttribute : Attribute {
        public string Name        { get; set; }
        public string Description { get; set; }
        public string Before      { get; set; }
        public string After       { get; set; }

        public TaskAttribute(string name) {
            Name = name;
            Task._tempTasks.Add(name, new Task(this));
        }
    }
}
