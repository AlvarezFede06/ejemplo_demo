using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MIImages.Web.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using MlImages; 
using System.Linq;
using MIImages.Web.Services;
using System.Drawing;

namespace MIImages.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EmguCVFaceService _faceService;


        public HomeController(ILogger<HomeController> logger, EmguCVFaceService faceService)
        {
            _logger = logger;
            _faceService = faceService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upload()
        {
            return View();
        }

        public IActionResult UploadMultiple()
        {
            return View();
        }
        // versión con EmguCv

        [HttpPost]
        public async Task<IActionResult> IdentifyMultipleFaces(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No se ha proporcionado ninguna imagen");
            }

            try
            {
                // Leer la imagen 
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();

                // Detectar caras en la imagen
                var faces = _faceService.DetectFaces(imageBytes);

                if (faces.Count == 0)
                {
                    TempData["Message"] = "No se detectaron caras en la imagen. Intenta con otra imagen.";
                    return RedirectToAction("UploadMultiple");
                }

                // Lista para almacenar los resultados de cada cara
                var results = new List<ActorIdentificationResult>();

                // Porcentaje de confianza mínimo 
                const float CONFIDENCE_THRESHOLD = 0.60f; 

                // Procesar cada cara detectada
                foreach (var faceRect in faces)
                {
                    try
                    {
                        // Extraer la cara de la imagen original
                        byte[] faceImageBytes = _faceService.ExtractFace(imageBytes, faceRect);

                        //  entrada para el modelo
                        var input = new MLModel1.ModelInput
                        {
                            ImageSource = faceImageBytes,
                            Label = string.Empty
                        };

                        //  predicción
                        var prediction = MLModel1.Predict(input);

                        //  mejores predicciones
                        var allPredictions = MLModel1.PredictAllLabels(input)
                            .Take(3)
                            .ToDictionary(kv => kv.Key, kv => kv.Value);

                        // Verificar si la confianza está por encima del umbral
                        float maxConfidence = prediction.Score.Max();
                        string actorName = maxConfidence >= CONFIDENCE_THRESHOLD
                            ? prediction.PredictedLabel
                            : "No hay coincidencia con actores del modelo";

                        // Crear el resultado para esta cara
                        var result = new ActorIdentificationResult
                        {
                            ActorName = actorName,
                            Confidence = maxConfidence,
                            TopPredictions = allPredictions,
                            ImageData = Convert.ToBase64String(faceImageBytes),
                            FaceRectangle = new FaceRectangle
                            {
                                X = faceRect.X,
                                Y = faceRect.Y,
                                Width = faceRect.Width,
                                Height = faceRect.Height
                            },
                            IsRecognized = maxConfidence >= CONFIDENCE_THRESHOLD
                        };

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar la cara en posición {faceRect}");
                        
                    }
                }

                
                return View("MultipleResults", new MultipleActorsResult
                {
                    OriginalImageData = Convert.ToBase64String(imageBytes),
                    DetectedActors = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la imagen");
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "Error al procesar la imagen: " + ex.Message
                });
            }
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}