using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using TaskMan;

namespace TaskMan.Specs {

	// I don't love these specs.  They're not super clear.  I was in a rush to get this done so I could start writing and running Tasks!

	[TestFixture]
	public class TaskManSpec {
		string Assembly1Path;
		string Assembly2Path;

		List<Task> Assembly1Tasks { get { return Task.GetTasksFromAssembly(Assembly1Path); } }
		List<Task> Assembly2Tasks { get { return Task.GetTasksFromAssembly(Assembly2Path); } }

		[SetUp]
		public void Setup() {
			Task.Verbose = true;
			Task.Clear();

			// Set the paths to our sample assemblies (they build in different places depending on configuration)
			var dir  = Directory.GetCurrentDirectory();
			var root = Path.Combine(dir, @"../../../ExampleAssembly1/bin/Release");
			if (! Directory.Exists(root))
				root = Path.Combine(dir, @"../../../ExampleAssembly1/bin/Debug");

			Assembly1Path = Path.GetFullPath(Path.Combine(root, "TaskMan.Specs.ExampleAssembly1.exe"));
			Assembly2Path = Assembly1Path.Replace("ExampleAssembly1", "ExampleAssembly2");
		}

		[Test]
		public void CanGetAllTasksFromAnAssembly() {
			Assert.That(Task.Count, Is.EqualTo(0));
			Assert.True(File.Exists(Assembly1Path));

			var assembly1 = Assembly.LoadFile(Assembly1Path);

			Assert.That(Task.Count, Is.EqualTo(0));

			var tasks = Task.GetTasksFromAssembly(assembly1);

			Assert.That(Task.Count, Is.EqualTo(0));
			Assert.That(tasks.Count,    Is.EqualTo(2));
			Assert.That(tasks.Select(t => t.Name).First(), Is.EqualTo("foobar"));
			Assert.That(tasks.Select(t => t.Name).Last(),  Is.EqualTo("increment:number"));
		}

		[Test]
		public void CanGetAllTasksFromAnAssemblyPath() {
			var tasks = Task.GetTasksFromAssembly(Assembly1Path);
			Assert.That(tasks.Count, Is.EqualTo(2));
			Assert.That(tasks.Select(t => t.Name).First(), Is.EqualTo("foobar"));
			Assert.That(tasks.Select(t => t.Name).Last(), Is.EqualTo("increment:number"));
		}

		[Test]
		public void CanExecuteATask() {
			Task.LoadTasksFromAssembly(Assembly1Path);

			Assert.That( Task.Get("foobar").Run(), Is.EqualTo("Foo Bar"));
		}

		[Test]
		public void CanLoadTasksFromAnAssemblyIntoGlobalTasks() {
			var tasks = Assembly1Tasks;
			Assert.That(Task.Count, Is.EqualTo(0));

			Task.LoadTasksFromAssembly(Assembly1Path);
			Assert.That(Task.Count, Is.EqualTo(2));
			Assert.That(Task.All.Select(t => t.Name).First(), Is.EqualTo("foobar"));
			Assert.That(Task.All.Select(t => t.Name).Last(),  Is.EqualTo("increment:number"));
		}

		[Test]
		public void GettingTasksFromAnAssemblyDoesNotAddToGlobalTasks() {
			var tasks = Assembly1Tasks;
			Assert.That(tasks.Count, Is.EqualTo(2));
			Assert.That(Task.Count, Is.EqualTo(0));

			Task.LoadTasksFromAssembly(Assembly1Path);
			Assert.That(Task.Count, Is.EqualTo(2));
		}

		[Test]
		public void TasksFromCallingAssemblyAreAutomaticallyLoadedIntoGlobalTasks() {
			var output = Assembly1Path.Exec();
			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("increment:number"));
			Assert.That(output, Is.Not.StringContaining("before1"));

			output = Assembly2Path.Exec();
			Assert.That(output, Is.Not.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("before1"));
			Assert.That(output, Is.StringContaining("after1"));
			Assert.That(output, Is.StringContaining("get:output"));
		}

		[Test]
		public void TasksCanHaveDescriptions() {
			var tasks = Assembly1Tasks;

			Assert.That(tasks.First().Name,        Is.EqualTo("foobar"));
			Assert.That(tasks.First().Description, Is.EqualTo("Returns 'Foo Bar'"));
		}

		//public void TasksCanRequireThatOtherTasksBeRunBeforeIt() {
		//public void TasksCanRequireThatOtherTasksBeRunAfterIt

		// NOTE: Dependencies must be global!
		// 	[Task("Foo", Before = "environment", After = "this andthistoo")]
		[Test]
		public void TasksCanRequireThatOtherTasksBeRunBeforeAndAfterIt() {
			Task.LoadTasksFromAssembly(Assembly2Path);
			var task = Task.All.First(t => t.Name == "callback:example");
			Assert.That(task.Before, Is.EqualTo("before1 before2"));
			Assert.That(task.After,  Is.EqualTo("after1 after2 after3"));
			Assert.That(task.Run(),  Is.EqualTo("BEFORE1 before2 <THE CODE>"));
			Assert.That(Task.All.First(t => t.Name == "get:output").Run(), Is.EqualTo("BEFORE1 before2 <THE CODE> after1 AFTER2 AFTER3!"));
		}

		[Test]
		public void CallingConsoleAppWithoutArgsListsTasksWithDescriptions() {
			var output = Assembly1Path.Exec();
			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("Returns 'Foo Bar'"));
			Assert.That(output, Is.StringContaining("increment:number"));
			Assert.That(output, Is.Not.StringContaining("before1"));
		}

		[Test]
		public void CallingConsoleAppWithTArgumentListsMatchingTasks() {
			var output = (Assembly1Path + " -T foo").Exec();

			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("Returns 'Foo Bar'"));
			Assert.That(output, Is.Not.StringContaining("increment:number"));
			Assert.That(output, Is.Not.StringContaining("before1"));

			output = (Assembly1Path + " -T number").Exec();

			Assert.That(output, Is.Not.StringContaining("foobar"));
			Assert.That(output, Is.Not.StringContaining("Returns 'Foo Bar'"));
			Assert.That(output, Is.StringContaining("increment:number"));
			Assert.That(output, Is.Not.StringContaining("before1"));
		}

		[Test]
		public void CallingConsoleAppWith1ArgRunsTaskIfFound() {
			var output = (Assembly1Path + " --verbose foobar").Exec();
			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.Not.StringContaining("increment:number"));

			output = (Assembly1Path + " --V hithere").Exec();
			Assert.That(output, Is.StringContaining("Task not found: hithere"));
			Assert.That(output, Is.Not.StringContaining("foobar"));
		}

		[Test]
		public void CallingConsoleAppWithManyArgsRunsManyTasks() {
			var output = (Assembly1Path + " -V foobar increment:number").Exec();
			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("increment:number"));
			Assert.That(output, Is.Not.StringContaining("hithere"));

			output = (Assembly1Path + " -V foobar hithere increment:number").Exec();
			Assert.That(output, Is.StringContaining("foobar"));
			Assert.That(output, Is.StringContaining("increment:number"));
			Assert.That(output, Is.StringContaining("Task not found: hithere"));
			Assert.That(output, Is.Not.StringContaining("Task not found: foobar"));
		}

		[Test]
		public void TasksCanBePassedCommandLineVariables() {
			var output = (Assembly2Path + " with:vars This=That \"FOO=value of foo\" Bar=\"value of Bar\"").Exec();
			Assert.That(output, Is.StringContaining("Variable This = That"));
			Assert.That(output, Is.StringContaining("Variable FOO = value of foo"));
			Assert.That(output, Is.StringContaining("Variable Bar = value of Bar"));

			output = (Assembly2Path + " with:var:collection This=That \"FOO=value of foo\" Bar=\"value of Bar\"").Exec();
			Assert.That(output, Is.StringContaining("Variable This = That"));
			Assert.That(output, Is.StringContaining("Variable FOO = value of foo"));
			Assert.That(output, Is.StringContaining("Variable Bar = value of Bar"));
		}

		[Test]
		public void CommandLineVariablesSetEnvironmentVariablesForEasyGlobalAccess() {
			var output = (Assembly2Path + " env:variables This=That \"FOO=value of foo\" Bar=\"value of Bar\"").Exec();
			Assert.That(output, Is.StringContaining("ENV This = That"));
			Assert.That(output, Is.StringContaining("ENV FOO = value of foo"));
			Assert.That(output, Is.StringContaining("ENV Bar = value of Bar"));
		}

		// Someday / Maybe
		// public void CanExecuteATaskAndNamedVariables() { ???? maybe.  ENV vars work great too tho.
		// public void LambdaCanBeUsedToDefineATask() {
	}
	
	public static class SpecHelper {

        // "ls -lrt".Exec();
        public static string Exec(this string str) {
            return SpecHelper.RunCommand(str);
        }

        public static string RunCommand(string command) {
            command   = command.Trim();
            int space = command.IndexOf(' ');
            if (space < 0)
                return RunCommandWithArguments(command, null);
            else
                return RunCommandWithArguments(command.Substring(0, space), command.Substring(space + 1));
        }

        public static string RunCommandWithArguments(string command, string arguments) {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = command;
            if (arguments != null)
                process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return stdout;
        }
	}
}
