using Xunit.v3;

namespace OrderManagement.Tests.Common;

// Custom test attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public abstract class TestAttribute(string key, string value) : Attribute, ITraitAttribute
{
    public string Key { get; } = key;
    public string Value { get; } = value;

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
        [new(Key, Value)];
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LayerAttribute(string value) : TestAttribute("Layer", value);

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ScopeAttribute(string value) : TestAttribute("Scope", value);