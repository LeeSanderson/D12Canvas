using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

public sealed class ConsoleErrorTrap
{
    private readonly List<string> _errors = [];
    private readonly Lock _gate = new();

    public void Attach(IPage page)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                Record(message.Text);
            }
        };
        page.PageError += (_, error) => Record(error);
    }

    public void AssertNone()
    {
        string[] errors;
        lock (_gate)
        {
            errors = [.. _errors];
        }

        if (errors.Length > 0)
        {
            Assert.Fail(
                "The page logged console errors during the test:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, errors)
            );
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _errors.Clear();
        }
    }

    private void Record(string error)
    {
        lock (_gate)
        {
            _errors.Add(error);
        }
    }
}
