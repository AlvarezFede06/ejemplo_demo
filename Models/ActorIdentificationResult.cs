namespace MIImages.Web.Models
{
    public class ActorIdentificationResult
    {
        public string ActorName { get; set; }
        public float Confidence { get; set; }
        public Dictionary<string, float> TopPredictions { get; set; }
        public string ImageData { get; set; } 
        public FaceRectangle FaceRectangle { get; set; } 
        public bool IsRecognized { get; set; } 
    }
}