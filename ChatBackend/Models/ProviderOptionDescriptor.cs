namespace ChatBackend.Models;

public class ProviderOptionDescriptor
{
    //Internal dictionary key name
    required public string Key;

    //Human friendly name
    required public string Name;
    //Human friendly description
    required public string Description;

    required public OptionType Type;

    required public object DefaultValue;

    //Only used with Type = OptionType.Enum
    public IEnumerable<string>? AllowedValues = null;

    public T GetValue<T>(ChatOptions source)
    {
        if (!source.ExtendedProperties.TryGetValue(Key, out var raw))
        {
            return ConvertValue<T>(DefaultValue);
        }

        return ConvertValue<T>(raw);
    }

    private T ConvertValue<T>(object raw)
    {
        // Handle type or conversion
        T value;
        if (raw is T casted)
        {
            value = casted;
        }
        else
        {
            try
            {
                value = (T)Convert.ChangeType(raw, typeof(T));
            }
            catch
            {
                throw new InvalidCastException(
                    $"Option '{Key}' has invalid type. Expected {typeof(T).Name}.");
            }
        }

        // Enum validation
        if (Type == OptionType.Enum && AllowedValues is { } allowed)
        {
            var strVal = value?.ToString();
            if (!allowed.Contains(strVal ?? string.Empty))
                throw new ArgumentOutOfRangeException(
                    Key, strVal,
                    $"Invalid value. Must be one of: {string.Join(", ", allowed)}");
        }

        return value;
    }

}

public enum OptionType
{
    String,
    Enum,
    Boolean,
    Integer,
    Float
}
