using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;
using System.Web.Http;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.Vision.Detection;
using Accord.Imaging;
using Accord.Math;
using Accord.Imaging.Filters;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Accord.Imaging.Textures;
using Final_Year_Project1.Models;

namespace Final_Year_Project.Controllers
{


    public class UploadImageController : ApiController
    {
        public EigenFaceRecognizer FaceRecognition { get; set; }
        public FaceRecognizer recognizer { get; set; }
        public CascadeClassifier FaceDetection { get; set; }
        public CascadeClassifier RightEyeDetection { get; set; }
        public CascadeClassifier LeftEyeDetection { get; set; }
        public Mat Frame { get; set; }
        public List<Image<Gray, byte>> Faces { get; set; }
        public List<int> IDs { get; set; }

        public int ProcessedImageWidth { get; set; } = 128;
        public int ProcessedImageHeight { get; set; } = 150;

        int Eigen_threshold = 2000;
        double label_distance { get; set; }

        private List<int> labelInt;

        private List<Image<Gray, byte>> knownFaces;
        private List<Image<Gray, byte>> unknownFaces;

        private List<string> unknownPerson;

        private List<string> namesFromID;

        private List<int> specialNumeber;

        private List<int> AllSpecialNumber;

        private List<Bitmap> BgwImages;
        private List<Bitmap> listImages;

        private string returnValue;

        public List<object> PicFile { get; set; }

        ResizeNearestNeighbor res = new ResizeNearestNeighbor(200, 200);


        //private MCvTermCriteria termCrit;

        public System.Drawing.Image fetchedImage { get; set; }

        private Image<Bgr, byte> imageFrame;
        Bitmap bitmapImage;
        byte[] uploadedFile;
        byte[] uploadedFace;

        public HttpResponseMessage response;

        private int id;



        [HttpPost]
        [Route("api/image")]
        public HttpResponseMessage UploadImage()
        {
            
            
            Initializer();
            var httpRequest = HttpContext.Current.Request;

            for (int i = 0; i < httpRequest.Files.Count; i++)
            {
                var postedFile = httpRequest.Files[i];
                if (postedFile != null)
                {
                    Bitmap convertedImage = UploadFile(postedFile);
                    //detect faces from image using EmguCV cascade classifier
                    Image<Bgr, Byte> bgrImage = new Image<Bgr, byte>(new Bitmap(convertedImage));
                    Image<Gray, byte> grayImage = bgrImage.Convert<Gray, byte>();

                    Rectangle[] faces = FaceDetection.DetectMultiScale(grayImage, 1.2, 5);

                    if (faces.Count() > 0)
                    {
                        for (int j = 0; j < faces.Count(); j++)
                        {
                            faces[j].X -= 15;
                            faces[j].Y -= 15;
                            faces[j].Height += 25;
                            faces[j].Width += 25;

                            Bitmap processedImage = new Crop(faces[j]).Apply(convertedImage);
                            processedImage = res.Apply(processedImage);

                            processedImage = ResizeBitmap
                                (processedImage, ProcessedImageWidth, ProcessedImageHeight);
                            //  bitmapImage = returnImage.ToBitmap();
                           //FaceToDb(processedImage, "JP", 2);
                            MemoryStream newStream = new MemoryStream();

                            //processedImage.Save(newStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                            //byte[] imageAsByte = newStream.ToArray();
                            //Stream stream = new MemoryStream(imageAsByte);
                            //response = new HttpResponseMessage();
                            //response.Content = new StreamContent(stream);
                            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");

                            //return response;

                            listImages.Add(processedImage);
                        }
                    }

                }


            }
            Result returnoutput = BgvwSvm(listImages.ToArray());
            string check = Check(returnoutput);
            var ob = new { returnoutput, check };

            string mail = SendMail(check + "<br> log in to your dashboard for more details");

            return Request.CreateResponse(HttpStatusCode.OK, check);

        }


        private int AddImageToDb(byte[] localFile, string fileName)
        {
            // byte[] fileByte;
            //using(var fs = new FileStream(localFile, FileMode.Open, FileAccess.Read))
            //{
            //    fileByte = new byte[fs.Length];
            //    fs.Read(fileByte, 0, Convert.ToInt32(fs.Length));

            //}

            var file = new Img()
            {
                ImgID = 0,
                ImageByte = localFile,
                Time = DateTime.Now,
                Size = localFile.Length,
                UserID = "166dff48-f79a-4257-ae38-026b7bba8d94",
            };

            using (var ctx = new ProjectEntities())
            {
                ctx.Imgs.Add(file);
                try
                {
                    ctx.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    Exception raise = dbEx;
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                            // raise a new exception nesting
                            // the current instance as InnerException
                            raise = new InvalidOperationException(message, raise);
                        }
                    }
                    throw raise;
                }

            }

            return file.ImgID;

        }

        private void Initializer()
        {
            FaceRecognition = new EigenFaceRecognizer(80, double.PositiveInfinity);
            recognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 100);
            FaceDetection = new CascadeClassifier(Path.GetFullPath(@"C:\Users\Daniel Okpala\Documents\Visual Studio 2017\Projects\Final Year Project1\Final Year Project1\Algo\haarcascade_frontalface_alt2.xml"));
            RightEyeDetection = new CascadeClassifier(@"C:\Users\Daniel Okpala\Documents\Visual Studio 2017\Projects\Final Year Project1\Final Year Project1\Algo\" + "haarcascade_mcs_righteye.xml");
            LeftEyeDetection = new CascadeClassifier(@"C:\Users\Daniel Okpala\Documents\Visual Studio 2017\Projects\Final Year Project1\Final Year Project1\Algo\" + "haarcascade_mcs_lefteye.xml");
            Faces = new List<Image<Gray, byte>>();
            IDs = new List<int>();
            PicFile = new List<object>();
            labelInt = new List<int>();
            unknownFaces = new List<Image<Gray, byte>>();
            unknownPerson = new List<string>();
            knownFaces = new List<Image<Gray, byte>>();
            namesFromID = new List<string>();
            specialNumeber = new List<int>();
            AllSpecialNumber = new List<int>();
            BgwImages = new List<Bitmap>();
            listImages = new List<Bitmap>();
        }

        private void TrainData()
        {
            System.Drawing.Image fetchedImage;
            using (var ctx = new ProjectEntities())
            {
                var faces = ctx.Faces;
                foreach (var face in faces)
                {
                    Byte[] imageByte = face.FaceImg;
                    MemoryStream myStream = new MemoryStream(imageByte);
                    fetchedImage = System.Drawing.Image.FromStream(myStream);
                    Bitmap retrievedImage = new Bitmap(fetchedImage);
                    //add image to bgwImages list
                    BgwImages.Add(retrievedImage);

                    var RecNo = face.RecNo;
                    IDs.Add(RecNo);
                }
            }

            //recognizer.Train(Faces.ToArray(), IDs.ToArray());

        }

        //save all faces predicted to the db
        //----------------------------------------SaveImgeToDb Method
        //private void saveImageToDb()
        //{
        //    if (knownFaces.Count > 0)
        //    {
        //        Image<Gray, byte>[] facesArray = new Image<Gray, byte>[knownFaces.Count];
        //        String[] faceNames = new string[namesFromID.Count];
        //        int[] number = new int[specialNumeber.Count];
        //        int[] ids = new int[labelInt.Count];
        //        ids = labelInt.ToArray();
        //        number = specialNumeber.ToArray();
        //        facesArray = knownFaces.ToArray();
        //        faceNames = namesFromID.ToArray();

        //        for (int i = 0; i < facesArray.Length; i++)
        //        {
        //            FaceToDb(facesArray[i], faceNames[i], number[i]);

        //            returnValue = returnValue + faceNames[i] + "-" + ids[i]  + " ";
        //        }
        //    }

        //    else if (unknownFaces.Count > 0)
        //    {
        //        Image<Gray, byte>[] facesArray = new Image<Gray, byte>[unknownFaces.Count];
        //        facesArray = unknownFaces.ToArray();
        //        int number;
        //        string name = "unknown";
        //        Random random = new Random();

        //        for (int i = 0; i < facesArray.Length; i++)
        //        {
        //            do
        //            {
        //                number = random.Next(10000);
        //            } while (AllSpecialNumber.Contains(number));

        //           FaceToDb(facesArray[i], name, number);

        //            returnValue = returnValue + name + " ";
        //        }


        //    }
        //    else
        //    {
        //        returnValue = "no face detected";
        //    }


        //}

        //saves uploaded images to a folder and db, then return a grayscale of the uploaded image
        private Bitmap UploadFile(HttpPostedFile postedFile)
        {
            string imageName = null;
            byte[] testupload;

            id = new int();
            System.Drawing.Image inputImage = null;
            imageName = new string(Path.GetFileNameWithoutExtension(postedFile.FileName).ToArray()).Replace(" ", "-");
            imageName = imageName + (Path.GetExtension(postedFile.FileName));
            var filePath = HttpContext.Current.Server.MapPath("~/Images/" + imageName);
            postedFile.SaveAs(filePath);

            uploadedFile = new byte[postedFile.InputStream.Length];
            postedFile.InputStream.Read(uploadedFile, 0, uploadedFile.Length);
            //save image to db and return image ID
            id = AddImageToDb(uploadedFile, imageName);

            testupload = new byte[uploadedFile.Length];
            testupload = uploadedFile;

            Stream stream = new MemoryStream(testupload);
            //response = new HttpResponseMessage();
            //response.Content = new StreamContent(stream);
            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");

            inputImage = System.Drawing.Image.FromStream(stream);
            //remember to change this code if bitmap doesn't work better than grayscale
            Bitmap returnImage = new Bitmap(inputImage);
            //Image<Bgr, Byte> bgrImage = new Image<Bgr, byte>(new Bitmap(inputImage));

            //Image<Gray, byte> convertedImage = bgrImage.Convert<Gray, byte>();

            return returnImage;

        }

        //gets the names and specialNumber of predicted faces
        private void GetNames()
        {
            using (var cttt = new ProjectEntities())
            {
                var faces = cttt.Faces;

                AllSpecialNumber = (from x in faces
                                    select x.SpecialNumber).ToList();

                foreach (var label in labelInt)
                {
                    string name = (from x in faces
                                   where x.FaceID == label
                                   select x.FaceName).SingleOrDefault();

                    int number = (from x in faces
                                  where x.FaceID == label
                                  select x.SpecialNumber).SingleOrDefault();


                    namesFromID.Add(name);
                    specialNumeber.Add(number);
                    //faces.where(x => x.id == 2).SingleOrDefault()?.FaceName;
                }
            }
        }

        // Gets each individual face thats been dedected and predicted and saves it to database
        private void FaceToDb(Bitmap bitmapImage, string Name, int number)
        {
            //Bitmap bitmapImage = grayImage.ToBitmap();
            MemoryStream newStream = new MemoryStream();


            bitmapImage.Save(newStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] imageAsByte = newStream.ToArray();

            //uploadedFile = new byte[imageAsByte.Length];
            //uploadedFile = imageAsByte;

            var newFace = new Face()
            {
                FaceID = 0,
                FaceName = Name,
                RecNo = number,
                ImgID = id,
                FaceImg = imageAsByte
            };
            var uFace = new Unknown()
            {
                UnknownID = 0,
                ImgID = id,
                UnknownFace = imageAsByte
            };


            using (var ctx = new ProjectEntities())
            {
                ctx.Unknowns.Add(uFace);
                try
                {
                    ctx.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    Exception raise = dbEx;
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                            // raise a new exception nesting
                            // the current instance as InnerException
                            raise = new InvalidOperationException(message, raise);
                        }
                    }
                    throw raise;
                }

            }
        }

        [HttpDelete]
        [Route("api/Delete")]

        public HttpResponseMessage DeleteImage()
        {
            using (var ctxx = new ProjectEntities())
            {
                var allimages = ctxx.Imgs;
                var allfaces = ctxx.Faces;

                //var unknownfaces = (from x in allfaces
                //                   where x.FaceName == "unknown"
                //                   select x);

                //foreach(var face in unknownfaces)
                //{
                //    ctxx.Faces.Remove(face);
                //}

                foreach (var pic in allimages)
                {
                    ctxx.Imgs.Remove(pic);
                }
                foreach (var face in allfaces)
                {
                    ctxx.Faces.Remove(face);
                }

                ctxx.SaveChanges();
            }

            return Request.CreateResponse(HttpStatusCode.OK);

        }

        public Rectangle[] accordDetection(Bitmap image)
        {
            //String path = @"C:\Users\Daniel Okpala\Documents\repos\Final Year Project\Final Year Project\Algo\haarcascade_frontalface_alt2.xml";
            //HaarCascade cascade1 = HaarCascade.FromXml(path);
            var cascade = new Accord.Vision.Detection.Cascades.FaceHaarCascade();
            var detector = new HaarObjectDetector(cascade, minSize: 30,
            searchMode: ObjectDetectorSearchMode.Average,
             scalingMode: ObjectDetectorScalingMode.GreaterToSmaller,
           scaleFactor: 1.1f
           )
            {
                Suppression = 1, // This should make sure we only report regions as faces if 
                                 // they have been detected at least 5 times within different cascade scales.
                UseParallelProcessing = true,
                MaxSize = new Size(320, 320)
            };

            Rectangle[] rectangles = detector.ProcessFrame(image);

            return rectangles;
        }

        public Result BgvwSvm(Bitmap[] testImage)
        {
            Accord.Math.Random.Generator.Seed = 0;

            var bgv = BagOfVisualWords.Create(numberOfWords: 30);
            //var bgv = BagOfVisualWords.Create(new HistogramsOfOrientedGradients(), new BinarySplit(10));
            //var bgv = BagOfVisualWords.Create<FastRetinaKeypointDetector, KModes<byte>, byte[]>(
            //new FastRetinaKeypointDetector(), new KModes<byte>(30, new Hamming()));
            TrainData();
            Bitmap[] images = BgwImages.ToArray();

            bgv.Learn(images);
            double[][] features = bgv.Transform(images);
            int[] labels = IDs.ToArray();

            //var teacher = new MulticlassSupportVectorLearning<Gaussian>()
            //{
            //    Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
            //    {

            //        UseKernelEstimation = true
            //    }
            //};

            //var machine = teacher.Learn(features, labels);
            //machine.Method = MulticlassComputeMethod.Voting;

            var teacher = new MultilabelSupportVectorLearning<Gaussian>() // Note: this is multi-label, not multi-class (keep the same kernel you were using before, do not change to a Gaussian, this is just an example)
            {
                //Learner = (p) => new LinearDualCoordinateDescent()
                //{
                //    Loss = Loss.L2
                //}
                Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                {

                    UseKernelEstimation = true
                }
            };

            // Learn the machine
            var machine = teacher.Learn(features, labels);

            // Create multi-label calibration algorithm for the machine
            var calibration = new MultilabelSupportVectorLearning<Gaussian>() // again, use the same kernel you were using before
            {
                Model = machine, // We will start with an existing machine

                // Configure the learning algorithm to use Platt's output calibration
                Learner = (param) => new ProbabilisticOutputCalibration<Gaussian>()
                {
                    Model = param.Model // Start with an existing machine
                }
            };

            // Calibrate the machine
            var ksvm = calibration.Learn(features, labels);

            // Now, set the machine probability method:
            machine.Method = MultilabelProbabilityMethod.SumsToOneWithEmphasisOnWinner;

            var msvm = ksvm.ToMulticlass();
            

            // Get class probabilities for each class & sample
            // double[][] prob = machine.Probabilities(inputs);


            bgv.Learn(testImage);
            double[][] test = bgv.Transform(testImage);
            int[] output = msvm.Decide(test);

            double[] prob = msvm.Probability(test);

            return new Result{ output = output, prob = prob };
        }

        public string Check(Result results)
        {
            List<Check> checklist = new List<Check>();
            

            for (int j = 0; j < results.output.Length; j++)
            {
                int index = checklist.FindIndex(i => i.RecInt == results.output[j]);
                if (index != -1)
                {
                    checklist[index].Count++;
                    if (results.prob[j] >= 0.7)
                    {
                        checklist[index].ProbCount++;
                    }
                }
                else
                {
                    if (results.prob[j] >= 0.7)
                    {
                        checklist.Add(new Check { RecInt = results.output[j], Count = 1, ProbCount = 1 });
                    }
                    else
                    {
                        checklist.Add(new Check { RecInt = results.output[j], Count = 1, ProbCount = 0 });
                    }

                    
                }
            }
            var recInts = checklist
                .Where(x => x.Count > 5 && x.ProbCount > 2)
                .Select(y => y.RecInt).ToArray();

            string report = ReturnNames(recInts);
                
            return report;
        }

        public string ReturnNames(int[] recInts)
        {
            string report = "";
            var ctx = new ProjectEntities();
            List<string> names = new List<string>();
            foreach (var recInt in recInts)
            {
                var name = ctx.Faces
                    .Where(x => x.RecNo == recInt)
                    .Select(y => y.FaceName).First();

                names.Add(name);
            }
            for (int i = 0; i < names.Count(); i++)
            {
                if(i == 0)
                {
                    report = report + names[i];
                }
                else
                {
                    report = report + " and " + names[i];
                }
            }
            if (names.Count() == 1)
            {
                return (report + " was spotted at your front door");
            }
            else
            {
                return (report + " were spotted at your front door");
            }

            
        }

        public string SendMail(string msg)
        {
            MailMessage mail = new MailMessage("final.year.project.cu@gmail.com", "danyelchidera1@gmail.com", "Home Security", msg);
            mail.IsBodyHtml = true;
            SmtpClient cl = new SmtpClient("smtp.gmail.com", 587);

            cl.UseDefaultCredentials = false;

            NetworkCredential cr = new NetworkCredential("final.year.project.cu@gmail.com", "jpphoneno");
            cl.Credentials = cr;
            cl.EnableSsl = true;
            cl.Timeout = 10000;
            cl.Send(mail);

            return "mail sent";
        }

        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }




        public Image<Gray, byte> AlignFace(Image<Gray, byte> Face)
        {
            try
            {
                int height = Face.Height / 2;
                Rectangle rect = new Rectangle(0, 0, Face.Width, height);
                Image<Gray, byte> upper_face = Face.GetSubRect(rect);

                //following variables are used to detect right eye and 
                //left eye for fixing position and hence used for face alignment
                var Right_Eye = RightEyeDetection.DetectMultiScale(upper_face, 1.3, 5);
                var Left_Eye = LeftEyeDetection.DetectMultiScale(upper_face, 1.3, 5);

                bool FLAG = false;
                //foreach (MCvAvgComp R_eye in Right_Eye[0])
                //{
                //    foreach (MCvAvgComp L_eye in Left_Eye[0])
                // {
                if (Right_Eye[0].X > (Left_Eye[0].X + Left_Eye[0].Width))
                {
                    //upper_face.Draw(R_eye.rect, new Gray(200), 2);
                    //upper_face.Draw(L_eye.rect, new Gray(200), 2);
                    var deltaY = (Left_Eye[0].Y + Left_Eye[0].Height / 2) -
                                 (Right_Eye[0].Y + Right_Eye[0].Height / 2);
                    //using horizontal position and width attribute find out the variable deltaX
                    var deltaX = (Left_Eye[0].X + Left_Eye[0].Width / 2) -
                                 (Right_Eye[0].X + Right_Eye[0].Width / 2);
                    double degrees = (Math.Atan2(deltaY, deltaX) * 180) / Math.PI;//find out 
                                                                                  //the angle as per position of eyes
                    degrees = 180 - degrees;
                    Face = Face.Rotate(degrees, new Gray(220), true);
                    FLAG = true;
                }
            }
            //        }
            //        if (FLAG == true)
            //        {
            //            break;
            //        }
            //    }
            //}
            catch (Exception d)
            {
                // op += " Align Error: " + d.Message;
            }
            //res = op;
            return Face;

        }
    }
}
        
//    Stream myStream = new MemoryStream(uploadedFile);
//    //fetchedImage = Image.FromStream(stream);
//    // stream.Write(uploadedFile, 0, uploadedFile.Length);
//    response = new HttpResponseMessage();
//    response.Content = new StreamContent(myStream);
//    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");

////TrainData();
//label_distance = new double();

//int facesCount = 0;





//FaceRecognizer.PredictionResult result = recognizer.Predict(processedImage);
//label_distance = result.Distance;
//if (result.Distance < 100)
//{
//    knownFaces.Add(processedImage);
//    labelInt.Add(result.Label);
//}
//else
//{
//    unknownFaces.Add(processedImage);
//    unknownPerson.Add("an Unknown person");
//}

//  }
//// returnValue = returnValue + "distance = " + label_distance;
//var teacher = new MulticlassSupportVectorLearning<Linear>()
//{
//    // using LIBLINEAR's L2-loss SVC dual for each SVM
//    Learner = (p) => new LinearDualCoordinateDescent()
//    {
//        Loss = Loss.L2
//    }
//};

// Obtain a learned machine

//return Request.CreateResponse(HttpStatusCode.OK,returnValue);

// Create multi-label calibration algorithm for the machine
//var calibration = new MultilabelSupportVectorLearning<Gaussian>() // again, use the same kernel you were using before
//{
//    Model = machine, // We will start with an existing machine

//    // Configure the learning algorithm to use Platt's output calibration
//    Learner = (param) => new ProbabilisticOutputCalibration<Gaussian>()
//    {
//        Model = param.Model // Start with an existing machine
//    }
//};

//// Calibrate the machine
//calibration.Learn(features, labels);

//// Now, set the machine probability method:
//machine.Method = MultilabelProbabilityMethod.SumsToOneWithEmphasisOnWinner;

// Get class probabilities for each class & sample
//double[][] prob = machine.Probabilities(features);
// Use the machine to classify the features


// Create a new Bag-of-Visual-Words (BoW) model


// var bgv = BagOfVisualWords.Create(new HistogramsOfOrientedGradients(), new BinarySplit(10));
// var bgv = BagOfVisualWords.Create(new FastRetinaKeypointDetector(), new BinarySplit(10));


//      var bgv = BagOfVisualWords.Create<FastRetinaKeypointDetector, KModes<byte>, byte[]>(
//new FastRetinaKeypointDetector(), new KModes<byte>(10, new Hamming()));
