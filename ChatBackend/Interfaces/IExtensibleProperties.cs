namespace ChatBackend.Interfaces;

public interface IExtensibleProperties
{
    IDictionary<string, object> ExtendedProperties { get; }
}
