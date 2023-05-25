using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using Example.Instrumented.Library;
using Xunit;

namespace Pocket.Tests;

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

        result.Single(v => v.Name == "this").Value.Should().Be(true);
        result.Single(v => v.Name == "that").Value.Should().Be(42);
    }

    [Fact]
    public void When_null_is_passed_to_the_args_array_of_Format_its_ok()
    {
        var formatter = Formatter.Parse("The values are {this} and {that}");

        var result = formatter.Format(null);

        result.Should().HaveCount(2);
        result.Single(v => v.Name == "this").Value.Should().BeNull();
        result.Single(v => v.Name == "that").Value.Should().BeNull();
        result.ToString().Should().Be("The values are [null] and [null]");
    }

    [Fact]
    public void Formatter_generates_key_value_pairs_associating_null_arguments_with_tokens()
    {
        var formatter = Formatter.Parse("The value is {value}");

        var result = formatter.Format(new object[] { null });

        result.Single(v => v.Name == "value").Value.Should().BeNull();
    }

    [Fact]
    public void Formatter_templates_arguments_into_the_result()
    {
        var template = "This template contains two tokens: {this} and {that}";

        var formatter = Formatter.Parse(template);

        var result = formatter.Format(true, 42);

        result.ToString()
              .Should()
              .Be("This template contains two tokens: True and 42");
    }

    [Fact]
    public void Formatter_templates_null_arguments_into_the_result()
    {
        var formatter = Formatter.Parse("The value is {value}");

        var result = formatter.Format(new object[] { null });

        result.ToString()
              .Should()
              .Be("The value is [null]");
    }

    [Fact]
    public void Formatter_can_be_reused_with_different_arguments()
    {
        var formatter = Formatter.Parse("The value is {i}");

        var result1 = formatter.Format(1);
        var result2 = formatter.Format(2);

        result1.ToString().Should().Be("The value is 1");
        result2.ToString().Should().Be("The value is 2");
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
    public void When_there_are_additional_properties_then_they_are_appended()
    {
        var formatter = Formatter.Parse("");

        var result = formatter.Format(
            args: null,
            knownProperties: new List<(string, object)>
            {
                ("also", "this")
            });

        result.ToString()
              .Should()
              .Be(" +[ (also, this) ]");
    }

    [Fact]
    public void When_there_are_additional_properties_then_they_are_appended_after_additional_args()
    {
        var formatter = Formatter.Parse("{one} and {two}");

        var result = formatter.Format(
            args: new object[] { 1, 2, 3, 4 },
            knownProperties: new List<(string, object)>
            {
                ("also", "this")
            });

        result.ToString()
              .Should()
              .Be("1 and 2 +[ 3, 4, (also, this) ]");
    }

    [Fact]
    public void When_there_are_more_arguments_than_tokens_then_extra_arguments_are_available_as_key_value_pairs()
    {
        var formatter = Formatter.Parse("{one} and {two}");

        var result = formatter.Format(1, 2, 3, ("some-metric", 4));

        result[2].Should().Be(("arg2", 3));
        result[3].Should().Be(("arg3", ("some-metric", 4)));
    }

    [Fact]
    public void ICollection_is_expanded()
    {
        var formatter = Formatter.Parse("The values in the array are: {values}");

        var result = formatter.Format(new[] { 1, 2, 3, 4 }, new[] { 4, 5, 6 });

        result.ToString()
              .Should()
              .Be("The values in the array are: [ 1, 2, 3, 4 ] +[ [ 4, 5, 6 ] ]");
    }

    [Fact]
    public void Formatter_cache_stops_adding_items_after_specified_size()
    {
        Formatter.CacheLimit = 50;

        for (var i = 0; i < 103; i++)
        {
            Formatter.Parse($"{i}");
        }

        Formatter.CacheCount.Should().Be(50);
    }

    [Fact]
    public void Formatter_can_be_customized_by_implementing_partial_method()
    {
        var entries = new List<string>();

        using var subscription = LogEvents.Subscribe(
            e => entries.Add(e.ToLogString()),
            typeof(Class1).Assembly);

        Class1.EmitSomeLogEvents("customize me:123");

        entries.Should().Contain(e => e.Contains("parameter1 = 123 (custom)"));
    }

    [Fact]
    public void Customized_Formatter_can_choose_to_write_empty_string()
    {
        var entries = new List<string>();

        using var subscription = LogEvents.Subscribe(
            e => entries.Add(e.ToLogString()),
            typeof(Class1).Assembly);

        Class1.EmitSomeLogEvents("customize me:replace me with empty string");

        entries[1].Should().EndWith("parameter1 =  ");
    }
}