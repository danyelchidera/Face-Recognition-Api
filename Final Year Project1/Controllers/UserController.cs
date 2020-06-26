using Final_Year_Project1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Final_Year_Project1.Controllers
{
    public class UserController : ApiController
    {
        [Authorize]
        [HttpGet]
        [Route("api/face")]
        public List<UnknownFaces> GetFace()
        {
            IEnumerable<Unknown> faces;
            List<UnknownFaces> unknownFaces = new List<UnknownFaces>();
           


            var ctxx = new ProjectEntities();


            faces = ctxx.Unknowns;
            foreach(var face in faces)
            {
                UnknownFaces unknownFace = new UnknownFaces
                {
                    FaceID = face.UnknownID,
                    FaceImage = Convert.ToBase64String(face.UnknownFace)
                };
                unknownFaces.Add(unknownFace);
            }



            return unknownFaces;
        }

        [Authorize]
        [HttpPost]
        [Route("api/insert/face")]
        public HttpResponseMessage InsertFace(Identified model)
        {
            List<Unknown> Uf = new List<Unknown>();

            var ctxx = new ProjectEntities();
            var face = ctxx.Faces.ToList().ToArray();
            var last = face.Last();


            foreach( var id in model.Id)
            {
                var unknown = ctxx.Unknowns.Where(x => x.UnknownID == id).Single();

                Face nface = new Face
                {
                    FaceName = model.Name,
                    RecNo = last.RecNo + 1,
                    FaceImg = unknown.UnknownFace,
                    Size = unknown.UnknownFace.Length,
                    ImgID = unknown.ImgID,
                };
                ctxx.Faces.Add(nface);
                ctxx.Unknowns.Remove(unknown);
            }
            ctxx.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

    }
}

    
