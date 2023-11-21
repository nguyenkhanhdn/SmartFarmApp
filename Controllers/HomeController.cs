using Microsoft.AspNetCore.Mvc;
using SmartFarmApp.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using lobe;
using SixLabors.ImageSharp.PixelFormats;
using lobe.ImageSharp;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SmartFarmApp.Controllers
{
    public class HomeController : Controller
    {

        private IHostingEnvironment Environment;

        public HomeController(IHostingEnvironment _environment)
        {
            Environment = _environment;
        }
        //public HomeController(ILogger<HomeController> logger)
        //{
        //    _logger = logger;
        //}

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Cam()
        {
            return View();
        }
        public ActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Upload(IFormFile fileUpload)
        {
            string predictResult = "";
            try
            {
                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + fileUpload.FileName;

                var uploadDirecotroy = "UploadedFiles/";
                var uploadPath = Path.Combine(Environment.WebRootPath, uploadDirecotroy);

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    fileUpload.CopyTo(stream);
                }
                //Sau khi upload gọi hàm nhận diện xử lý bệnh trên lá cây cà chua
                predictResult = Predict(filePath);
                if (predictResult != "Tomato_healthy")
                {
                    SendEmail("tult0409@gmail.com", "Smart Farm 4.0", string.Format("Bệnh trên cây cà chua được phát hiện: {0}", predictResult));
                }
            }
            catch (Exception ex)
            {
                predictResult = ex.Message;
            }

            ViewBag.Result = predictResult;
            return View();
        }
        [HttpPost]
        public string UploadWebCamImage(string mydata)
        {
            string wwwPath = this.Environment.WebRootPath;

            string filename = wwwPath + "/UploadedFiles/img" + DateTime.Now.ToString().Replace("/", "_").Replace(" ", "_").Replace(":", "") + ".png";
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(mydata);
                    bw.Write(data);
                    bw.Close();
                }
            }
            //Sau khi upload gọi hàm nhận diện xử lý bệnh trên lá cây cà chua
            string predictResult = Predict(filename);

            if (predictResult != "Tomato_healthy")
            {
                SendEmail("tult0409@gmail.com", "Smart Farm 4.0", string.Format("Bệnh trên cây cà chua được phát hiện: {0}", predictResult));
            }

            return predictResult; //Hà trả về là nhãn (label của bệnh)
        }
        //Hàm chẩn đoán bệnh
        private string Predict(string fileName)
        {
            string wwwPath = this.Environment.WebRootPath;

            var signatureFilePath = wwwPath + @"/model/signature.json";
            var imageToClassify = fileName;

            lobe.ImageClassifier.Register("onnx", () => new OnnxImageClassifier());
            var classifier = ImageClassifier.CreateFromSignatureFile(new FileInfo(signatureFilePath));

            //var classifier2 = ImageClassifier.CreateFromSignatureFile(new FileInfo(signatureFilePath));

            var results = classifier.Classify(SixLabors.ImageSharp.Image.Load(imageToClassify).CloneAs<Rgb24>());
            return results.Prediction.Label;
        }
        public IActionResult agri()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task<ActionResult> ReadFB()
        {
            FirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = "kgqWA2Fuc7trFif2by96ElLp0Ypp5x8cnmvj4tXm",
                BasePath = "https://smart-garden-d6348-default-rtdb.asia-southeast1.firebasedatabase.app"
            };

            IFirebaseClient client;
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = await client.GetAsync("/");


            GardenData data = response.ResultAs<GardenData>();
            ViewBag.Doam = data.Doamdat;
            ViewBag.Loaihat = data.data;
            ViewBag.KK = data.Doamkk;
            ViewBag.Nhietdokk = data.Nhietdokk;
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> WriteFB(int Doamdat, int Mode, float Nhietdokk, int Doamkk)
        {

            var mydata = new GardenData();
            //mydata.Doamdat = Doamdat;
            mydata.data = Mode;
            //mydata.Doamkk = Doamkk ;
            //mydata.Nhietdokk = Nhietdokk; 
            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = "kgqWA2Fuc7trFif2by96ElLp0Ypp5x8cnmvj4tXm",
                BasePath = "https://smart-garden-d6348-default-rtdb.asia-southeast1.firebasedatabase.app"
            };

            IFirebaseClient client;
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = await client.SetAsync("/", mydata);

            return RedirectToAction("ReadFB");
        }

        public void SendEmail(string address, string subject, string message)
        {
            string email = "khanhdsp@gmail.com";
            string password = "fcstfvasswuowgub";

            var loginInfo = new NetworkCredential(email, password);
            var msg = new MailMessage();
            var smtpClient = new SmtpClient("smtp.gmail.com", 587);

            msg.From = new MailAddress(email);
            msg.To.Add(new MailAddress(address));
            msg.Subject = subject;
            msg.Body = message;
            msg.IsBodyHtml = true;

            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = loginInfo;
            smtpClient.Send(msg);
        }
    }
}