# Title: Face Recognition App

# Purpose: This app uses the endpoint 'api/image' is to receive image files from a particular user, store them and extract face features from them, 
...then try to match those features with a dataset of faces stored in a db. If a match is sent, an email is sent to the user consuming the api. 
...In a situation where there are no matches. The unrecognised faces can be accessed through the user's dashboard, from where the user can choose to ...
... identify the faces with a any particular name.

#back-end logic: At the server side, the request is checked for all the image files sent. 
..The method “UploadFile()” takes all the original image files sent and stores them in a database. 
...The file is converted to byte array in order to be stored in the database.
...Once successfully stored, the byte array is converted a bitmap image and passed to the variable “converted image”. 
...Hence the initial images are stored before faces are extracted.

The Bitmap image is converted to a Blue-green-red EmguCV image type, then to grayscale image. 
This is to enable the faces to be detected using EmguCv’s cascade classifier. 
The faces are detected if there are any, and rectangle array containing the location of the face on the image is returned. 
That information is used to pick out the exact portion of the image containing the face.
Each image is extracted and resized, to give the same width and height for every face image. 
The faces are then added to a list. The process is repeated till all faces are detect from all the snapshots sent.
The image list is converted to an array type. 
This array is passed to Bag of visual words that performs extraction of features (using Speeded up robust features) and Transformation. 
The output from bag of words is used to learn a multiclass KSVM using Gaussian kernel. This machine can then be used for face classification/recognition. 

A probability estimate is also obtained for which the results of each classification can be disregarded.
If the probability of a classification is less than 0.7, it won't be regarded as a suitable match for any of the faces in the database.
The faces can be labeled as an unknown face for which the user can identify or store with any suitable name.

#front-end logic: Unknown faces are sent as base 64 strings and displayed for the user to see. IDs of identified faces are then sent back to the api along with a name.

#libraries and frameworks used for feature extration and recognition are EmguCv and Accord.net
