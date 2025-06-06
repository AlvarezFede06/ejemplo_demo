namespace MIImages.Web.Models
{
    public class MultipleActorsResult
    {
        public string OriginalImageData { get; set; }
        public List<ActorIdentificationResult> DetectedActors { get; set; }
    }
}