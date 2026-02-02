namespace otherServices.Infrastructure.Kafka;

public static class KafkaTopics
{
    // Requests from Backend -> AI
    public const string AiRequests = "ai.requests";

    // Results from AI -> Backend
    public const string AiResults = "ai.results";
}
