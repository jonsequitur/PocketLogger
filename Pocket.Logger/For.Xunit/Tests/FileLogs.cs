using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static Pocket.Logger<Pocket.For.Xunit.Tests.FileLogs>;

namespace Pocket.For.Xunit.Tests
{
    public class FileLogs
    {
        [Fact]
        public void When_writeToFile_is_set_to_false_then_no_file_is_written()
        {
            var attribute = new LogToPocketLoggerAttribute(writeToFile: false);

            var methodInfo = GetType().GetMethod(nameof(When_writeToFile_is_set_to_false_then_no_file_is_written));

            attribute.Before(methodInfo);

            Log.Info("hello from {method}", methodInfo.Name);

            var file = TestLog.Current.LogFile;

            attribute.After(methodInfo);

            file.Should().BeNull();
        }

        [Fact]
        public void When_writeToFile_is_set_to_true_then_a_file_is_written()
        {
            var attribute = new LogToPocketLoggerAttribute(writeToFile: true);

            var methodInfo = GetType().GetMethod(nameof(When_writeToFile_is_set_to_true_then_a_file_is_written));

            attribute.Before(methodInfo);

            var file = TestLog.Current.LogFile;
            var message = "hello from " + methodInfo.Name + $" ({Guid.NewGuid()})";

            Log.Info(message);

            attribute.After(methodInfo);

            file.Should().NotBeNull();
            file.Exists.Should().BeTrue();

            var text = File.ReadAllText(file.FullName);

            text.Should().Contain(message);
        }

        [Fact]
        public void When_filename_is_set_then_log_output_is_written_to_the_specified_file()
        {
            var filename = $"{Guid.NewGuid()}.log";
            var attribute = new LogToPocketLoggerAttribute(filename: filename);

            var methodInfo = GetType().GetMethod(nameof(When_filename_is_set_then_log_output_is_written_to_the_specified_file));

            attribute.Before(methodInfo);

            var file = TestLog.Current.LogFile;
            var message = "hello from " + methodInfo.Name + $" ({Guid.NewGuid()})";

            Log.Info(message);

            attribute.After(methodInfo);

            file.Should().NotBeNull();
            file.Exists.Should().BeTrue();

            var text = File.ReadAllText(file.FullName);

            text.Should().Contain(message);

            file.Name.Should().Be(filename);
        }

        [Fact]
        public void When_filename_environment_variable_is_set_then_log_output_is_written_to_the_specified_file()
        {
            var filename = $"{Guid.NewGuid()}.log";
            var envVarName = nameof(When_filename_environment_variable_is_set_then_log_output_is_written_to_the_specified_file);
            Environment.SetEnvironmentVariable(envVarName, filename);
            var attribute = new LogToPocketLoggerAttribute
            {
                FileNameEnvironmentVariable = envVarName
            };

            var methodInfo = GetType().GetMethod(nameof(When_filename_is_set_then_log_output_is_written_to_the_specified_file));

            attribute.Before(methodInfo);

            var file = TestLog.Current.LogFile;
            var message = "hello from " + methodInfo.Name + $" ({Guid.NewGuid()})";

            Log.Info(message);

            attribute.After(methodInfo);

            file.Should().NotBeNull();
            file.Exists.Should().BeTrue();

            var text = File.ReadAllText(file.FullName);

            text.Should().Contain(message);

            file.Name.Should().Be(filename);
        }

        [Fact]
        public void When_FileNameEnvironmentVariable_is_set_to_an_nonexistent_variable_then_no_exception_is_thrown()
        {
            Action initialize = () => new LogToPocketLoggerAttribute
            {
                FileNameEnvironmentVariable = Guid.NewGuid().ToString("N")
            };

            initialize.Should().NotThrow();
        }

        [Fact]
        public void File_output_can_handle_concurrent_logging()
        {
            var filename = $"{Guid.NewGuid()}.log";
            var attribute = new LogToPocketLoggerAttribute(filename: filename);

            var methodInfo = GetType().GetMethod(nameof(File_output_can_handle_concurrent_logging));

            attribute.Before(methodInfo);

            Parallel.ForEach(
                Enumerable.Range(1, 25),
                new ParallelOptions { MaxDegreeOfParallelism = 25 },
                i => Log.Info(i.ToString()));

            attribute.After(methodInfo);

            var lines = File.ReadAllLines(filename);

            lines.Length.Should().Be(27, because: "we wrote 25 entries, plus there's a stop and start event for the test");
        }
    }
}
