using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Pocket.Tests
{
    public class FormatterTests
    {
        [Fact]
        public void Formatter_identifies_format_tokens()
        {
            var template = "This template contains two tokens: {this} and {that}";

            var formatter = Formatter.Parse(template);

            formatter.Tokens.Should().BeEquivalentTo("this", "that");
        }

        [Fact]
        public void Formatter_generates_key_value_pairs_associating_arguments_with_tokens()
        {
            var template = "This template contains two tokens: {this} and {that}";

            var formatter = Formatter.Parse(template);

            var result = formatter.Format(true, 42);

            result.Single(v => v.Key == "this").Value.Should().Be(true);
            result.Single(v => v.Key == "that").Value.Should().Be(42);
        }

        [Fact]
        public void Formatter_ToString_templates_arguments_into_the_result()
        {
            var template = "This template contains two tokens: {this} and {that}";

            var formatter = Formatter.Parse(template);

            var result = formatter.Format(true, 42);

            result.ToString()
                  .Should()
                  .Be("This template contains two tokens: True and 42");
        }

        [Fact]
        public void IFormattable_args_can_have_their_format_specified_in_the_template()
        {
            var formatter = Formatter.Parse("The hour is {time:HH}");

            var result = formatter.Format(DateTime.Parse("12/12/2012 11:42pm")).ToString();

            result.Should()
                  .Be("The hour is 23");
        }

        [Fact]
        public void When_a_token_occurs_multiple_times_then_every_occurrence_is_replaced()
        {
            var formatter = Formatter.Parse("{one} and {two} and {one}");

            var result = formatter.Format(1, 2);

            result.ToString()
                  .Should()
                  .Be("1 and 2 and 1");
        }

        [Fact]
        public void When_there_are_more_tokens_than_arguments_then_some_tokens_are_not_replaced()
        {
            var formatter = Formatter.Parse("{one} and {two} and {three}");

            var result = formatter.Format(1, 2);

            result.ToString()
                  .Should()
                  .Be("1 and 2 and {three}");
        }

        [Fact]
        public void When_there_are_more_arguments_than_tokens_then_extra_arguments_are_appended()
        {
            var formatter = Formatter.Parse("{one} and {two}");

            var result = formatter.Format(1, 2, 3, 4);

            result.ToString()
                  .Should()
                  .Be("1 and 2 +[ 3, 4 ]");
        }

        [Fact]
        public void When_there_are_more_arguments_than_tokens_then_extra_arguments_are_available_as_key_value_pairs()
        {
            var formatter = Formatter.Parse("{one} and {two}");

            var result = formatter.Format(1, 2, 3, ("some-metric", 4));

            result.ElementAt(2)
                  .ShouldBeEquivalentTo(new
                  {
                      Key = "arg2",
                      Value = 3
                  });
            result.ElementAt(3)
                  .ShouldBeEquivalentTo(new
                  {
                      Key = "arg3",
                      Value = ("some-metric", 4)
                  });
        }

        [Fact]
        public void IEnumerable_types_are_expanded()
        {
            var formatter = Formatter.Parse("The values in the array are: {values}");

            var result = formatter.Format(new[] { 1, 2, 3, 4 });

            result.ToString()
                  .Should()
                  .Be("The values in the array are: [ 1, 2, 3, 4 ]");
        }
    }
}
