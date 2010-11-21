using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using TaskMan;

namespace TaskMan.Specs {

    [TestFixture]
    public class TaskManSpec {

        string Assembly1Path = Path.GetFullPath(Directory.GetCurrentDirectory() + @"/../../../ExampleAssembly1/bin/Release/TaskMan.Specs.ExampleAssembly1.exe");
        string Assembly2Path = Path.GetFullPath(Directory.GetCurrentDirectory() + @"/../../../ExampleAssembly2/bin/Release/TaskMan.Specs.ExampleAssembly2.exe");

        List<Task> Assembly1Tasks { get { return Task.GetTasksFromAssembly(Assembly1Path); } }
        List<Task> Assembly2Tasks { get { return Task.GetTasksFromAssembly(Assembly2Path); } }

        [SetUp]
        public void Setup() {
            Task.Clear();
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
        // [Task("Foo", Before = "environemnt", After = "this andthis")]
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
        public void CallingConsoleAppWith1ArgRunsTaskIfFound() {
            var output = (Assembly1Path + " foobar").Exec();
            Assert.That(output, Is.StringContaining("foobar"));
            Assert.That(output, Is.Not.StringContaining("increment:number"));

            output = (Assembly1Path + " hithere").Exec();
            Assert.That(output, Is.StringContaining("Task not found: hithere"));
            Assert.That(output, Is.Not.StringContaining("foobar"));
        }

        [Test]
        public void CallingConsoleAppWithManyArgsRunsManyTasks() {
            var output = (Assembly1Path + " foobar increment:number").Exec();
            Assert.That(output, Is.StringContaining("foobar"));
            Assert.That(output, Is.StringContaining("increment:number"));
            Assert.That(output, Is.Not.StringContaining("hithere"));

            output = (Assembly1Path + " foobar hithere increment:number").Exec();
            Assert.That(output, Is.StringContaining("foobar"));
            Assert.That(output, Is.StringContaining("increment:number"));
            Assert.That(output, Is.StringContaining("Task not found: hithere"));
            Assert.That(output, Is.Not.StringContaining("Task not found: foobar"));
        }

        // Someday / Maybe
        // public void CanExecuteATaskAndNamedVariables() { ???? maybe.  ENV vars work great too tho.
        // public void LambdaCanBeUsedToDefineATask() {
    }
}
