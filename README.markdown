TaskMan
=======

TaskMan makes it ridiculously easy to map methods in your code to command-line 
callable functions.  All you need to do is add an attribute to your static 
methods and you're pretty much done!

Inspired by [Rake][]

Download
--------

Latest version: 1.0.0.0

[Download .dll][]

[Browse Source][]

Usage
-----

First, you want to let TaskMan know that some of your public static methods are tasks:

    using TaskMan;

    public class Whatever {

        [Task("reports", Description = "I print stuff")]
        public static void PrintReports() {
            Console.WriteLine("I print out some complicated reports");
        }

        [Task("stats", Description = "I print stuff")]
        public static void PrintStatistics() {
            Console.WriteLine("I print out some complicated stats");
        }

    }

Then you can easily call a task by name:

    >> Task.Run("reports");
    => I print out some complicated reports

    >> Task.Run("stats");
    => I print out some complicated stats

Or you can call many tasks:

    >> Task.Run(new string[] { "reports", "stats" });
    => I print out some complicated reports
    I print out some complicated stats

And, because `Task.Run` accepts a `string[] args` ... I guess that means you can ...

    namespace MyApp {
        class Program {
            public static void Main(string[] args) {
                TaskMan.Task.Run(args);
            }
        }
    }

Yep!  That's it.  You can now excute 1 or many of your methods from the command-line.

    // Running without any arguments lists the available tasks and their descriptions
    C:\MyApp\MyApp.exe
    Tasks:
      reports
      stats

    // You can then pass 1 or many task names
    C:\MyApp\MyApp.exe reports
    reports
    I print out some complicated reports

    C:\MyApp\MyApp.exe reports stats
    reports
    I print out some complicated reports
    stats
    I print out some complicated tasks

Examples
--------

Checkout the examples that our specs use for more example usage:
[Example1][]
[Example2][]

Dependencies
------------

Some tasks might require you to run another task before you can run the current one.

    [Task("dogs:list", Description = "Prints out a list of all dog names")]
    public static PrintOutDogNames() {
	// need to start our dog database server
        // need to load dogs before anything will print
        foreach (var dogName in GetDogNames())
            Console.WriteLine(dogName);
        // after running, we'd love to do some cleanup
    }

Ofcourse, you can manually run the other task by using Task.Run ...

    [Task("dogs:list", Description = "Prints out a list of all dog names")]
    public static PrintOutDogNames() {
        Task.Run("dogs:start_database");
        Task.Run("dogs:load");
        foreach (var dogName in GetDogNames())
            Console.WriteLine(dogName);
        Task.Run("dogs:cleanup");
    }

Or by getting the actual Task instance and calling Run() on it ...

    [Task("dogs:list", Description = "Prints out a list of all dog names")]
    public static PrintOutDogNames() {
        Task.Get("dogs:start_database").Run();
        Task.Get("dogs:load").Run(); // <--- by the way, this returns the return value of the method 'dogs:load' is defined on
        foreach (var dogName in GetDogNames())
            Console.WriteLine(dogName);
        Task.Get("dogs:cleanup").Run();
    }

But you can also define tasks that run Before and/or After your task, making this much easier!

    [Task("dogs:list", Description = "Prints out a list of all dog names", Before = "dogs:start_database dogs:load", After = "dogs:cleanup" )]
    public static PrintOutDogNames() {
        foreach (var dogName in GetDogNames())
            Console.WriteLine(dogName);
    }

Other Stuff
-----------

There's currently less than 200 LOC in TaskMan, so you can read [TaskMan/Task.cs][task] if you want to see what else comes out of the box.  It's a *super* simple little library.

    var tasks = Task.All; // returns a List<Task> of all Task objects

    var task = Task.Get("db:migrate"); // returns a Task or null

    // To go through an Assembly and load all of its [Task] attributes into Task.All (which makes them available to Get() or Run()) ...
    Task.LoadTasksFromAssembly("path/to/assembly.dll");
    Task.LoadTasksFromAssembly(Assembly.GetExecutingAssembly());
    
    // Task.LoadTasksFromAssembly returns a List<Task> which is automatically added to the list of "global" tasks.
    // If you want to get all of the tasks from an assembly, but NOT add them to the "global" scope of tasks ...
    var tasks = Task.GetTasksFromAssembly("path/to/assembly.dll");
    var tasks = Task.GetTasksFromAssembly(Assembly.GetExecutingAssembly());

Also, a `Task` has a:

 * `string` Name
 * `string` Description
 * `string` Before
 * `string` After
 * `MethodInfo` Method

And it can be: `Run()`

That's pretty much it!

License
-------

TaskMan is released under the MIT license.

[rake]:          http://rake.rubyforge.org
[Download .dll]: http://github.com/remi/TaskMan/raw/1.0.0.0/TaskMan/bin/Release/TaskMan.dll
[Browse Source]: http://github.com/remi/TaskMan/tree/1.0.0.0
[Example1]:      http://github.com/remi/TaskMan/blob/master/ExampleAssembly1/Tasks.cs
[Example2]:      http://github.com/remi/TaskMan/blob/master/ExampleAssembly2/Tasks.cs
[task]:          https://github.com/remi/TaskMan/blob/master/TaskMan/Task.cs
