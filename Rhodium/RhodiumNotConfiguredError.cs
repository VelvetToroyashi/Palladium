using Remora.Results;

namespace Rhodium;

public record RhodiumNotConfiguredError() : ResultError("You haven't configured your preferred temperature unit yet. Use `/config` to set it.");
