using System.Text.RegularExpressions;
using CoreWeb.Features.Logs;

namespace CoreWeb.Tests.Features.Logs;

[TestFixture]
public class when_matching_error_report_message_filters
{
    [TestCase("Worker heartbeat timeout:17", "HeartbeatTimeout")]
    [TestCase("Unrelated failure", null)]
    public void it_matches_a_non_javascript_message_using_a_supplied_rule(string message, string? expectedReason)
    {
        ErrorReportMessageFilterRule[] rules =
        [
            new(new Regex(@"\AWorker heartbeat timeout:[0-9]+\z"), "HeartbeatTimeout")
        ];

        ErrorReportMessageFilter.GetSuppressionReason(message, rules).ShouldBe(expectedReason);
    }

    [TestCase("JS error: example", "Example")]
    [TestCase("example", null)]
    [TestCase(" JS error: example", null)]
    [TestCase("JS error: example ", null)]
    [TestCase("JS error: example\n", null)]
    [TestCase("prefix JS error: example suffix", null)]
    public void it_passes_the_complete_unmodified_message_to_each_rule(string message, string? expectedReason)
    {
        ErrorReportMessageFilterRule[] rules = [new(new Regex(@"\AJS error: example\z"), "Example")];

        ErrorReportMessageFilter.GetSuppressionReason(message, rules).ShouldBe(expectedReason);
    }

    [Test]
    public void it_returns_the_first_matching_rule_reason()
    {
        ErrorReportMessageFilterRule[] rules =
        [
            new(new Regex(@"\AOther failure\z"), "Other"),
            new(new Regex(@"\AWorker failure\z"), "First"),
            new(new Regex("failure"), "Second")
        ];

        ErrorReportMessageFilter.GetSuppressionReason("Worker failure", rules).ShouldBe("First");
    }

    [Test]
    public void it_does_not_add_anchors_to_supplied_rules()
    {
        ErrorReportMessageFilterRule[] rules = [new(new Regex("timeout"), "Timeout")];

        ErrorReportMessageFilter.GetSuppressionReason("Worker timeout occurred", rules).ShouldBe("Timeout");
    }

    [Test]
    public void it_returns_null_when_no_rule_matches()
    {
        ErrorReportMessageFilterRule[] rules = [new(new Regex(@"\AKnown failure\z"), "Known")];

        ErrorReportMessageFilter.GetSuppressionReason("Unknown failure", rules).ShouldBeNull();
        ErrorReportMessageFilter.GetSuppressionReason("Known failure", []).ShouldBeNull();
        ErrorReportMessageFilter.GetSuppressionReason(string.Empty, rules).ShouldBeNull();
    }
}
