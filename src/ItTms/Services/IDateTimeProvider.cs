namespace ItTms.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
