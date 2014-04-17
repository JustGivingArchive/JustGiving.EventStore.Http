namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IStreamPositionRepository
    {
        int? GetPositionFor(string stream);
        void SetPositionFor(string stream, int position);
    }
}