namespace WordCreationTester.DTO
{
    public class ServiceBusRequestMessage
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
    }
}
