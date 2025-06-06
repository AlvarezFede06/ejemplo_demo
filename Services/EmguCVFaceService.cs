using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace MIImages.Web.Services
{
    public class EmguCVFaceService
    {
        private readonly CascadeClassifier _faceCascade;
        private readonly ILogger<EmguCVFaceService> _logger;

        public EmguCVFaceService(IWebHostEnvironment environment, ILogger<EmguCVFaceService> logger)
        {
            _logger = logger;

            // Ruta al clasificador Haar Cascade para detección de caras
            string haarCascadePath = Path.Combine(environment.ContentRootPath, "Models", "haarcascade_frontalface_default.xml");

            // Descargar el clasificador si no existe
           /* if (!File.Exists(haarCascadePath))
            {
                _logger.LogInformation("Descargando clasificador Haar Cascade para detección de caras...");
                Directory.CreateDirectory(Path.GetDirectoryName(haarCascadePath));
                DownloadHaarCascade(haarCascadePath).Wait();
            }*/

            _faceCascade = new CascadeClassifier(haarCascadePath);
            _logger.LogInformation("Servicio de detección de caras inicializado correctamente");
        }

        private async Task DownloadHaarCascade(string filePath)
        {
            string url = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            _logger.LogInformation("Clasificador Haar Cascade descargado correctamente");
        }

        public List<Rectangle> DetectFaces(byte[] imageData)
        {
            try
            {
                // Cargar la imagen como Mat (formato nativo de OpenCV)
                Mat mat = new Mat();
                CvInvoke.Imdecode(imageData, ImreadModes.Color, mat);

                // Convertir a escala de grises
                Mat grayMat = new Mat();
                CvInvoke.CvtColor(mat, grayMat, ColorConversion.Bgr2Gray);

                // Detectar caras
                Rectangle[] faces = _faceCascade.DetectMultiScale(
                    grayMat,
                    1.1, // scaleFactor
                    5,   // minNeighbors
                    new Size(30, 30)); // minSize

                // Liberar recursos
                mat.Dispose();
                grayMat.Dispose();

                var result = faces.ToList();
                _logger.LogInformation($"Se detectaron {result.Count} caras en la imagen");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al detectar caras en la imagen");
                throw;
            }
        }

        public byte[] ExtractFace(byte[] originalImage, Rectangle faceRect, int padding = 20)
        {
            try
            {
                using var memoryStream = new MemoryStream(originalImage);
                using var bitmap = new Bitmap(memoryStream);

                // Añadir padding al rectángulo para incluir más contexto de la cara
                var x = Math.Max(0, faceRect.X - padding);
                var y = Math.Max(0, faceRect.Y - padding);
                var width = Math.Min(bitmap.Width - x, faceRect.Width + padding * 2);
                var height = Math.Min(bitmap.Height - y, faceRect.Height + padding * 2);

                var paddedRect = new Rectangle(x, y, width, height);

                // Recortar la cara
                using var faceBitmap = bitmap.Clone(paddedRect, bitmap.PixelFormat);

                // Convertir a bytes
                using var faceStream = new MemoryStream();
                faceBitmap.Save(faceStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return faceStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer la cara de la imagen");
                throw;
            }
        }
    }
}