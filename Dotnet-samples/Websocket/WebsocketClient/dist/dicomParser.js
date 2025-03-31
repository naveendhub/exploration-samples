"use strict";
// Reference the dicomParser module
var dicomParser = require("../node_modules/dicom-parser");
// Read the DICOM P10 file from disk into a Buffer
var fs = require("fs");
var filePath = "C:\\TestData\\Sample1\\MR_01.dcm";
console.log("File Path = ", filePath);
var dicomFileAsBuffer = fs.readFileSync(filePath);
// Parse the dicom file
try {
    var dataSet = dicomParser.parseDicom(dicomFileAsBuffer);
    // print the patient's name
    var patientName = dataSet.string("x00100010");
    console.log("Patient Name = " + patientName);
    var modality = dataSet.string("x00080060");
    console.log("Modality = " + modality);
    var studyDate = dataSet.string("x00080020");
    console.log("studyDate = " + studyDate);
    var sopInstanceUid = dataSet.string("x00080018");
    console.log("sopInstanceUid = " + sopInstanceUid);
    var privateTag = dataSet.string("x20051327");
    console.log("privateTag = " + privateTag);
    // Get the pixel data element and calculate the SHA1 hash for its data
    var pixelData = dataSet.elements.x7fe00010;
    var pixelDataBuffer = dicomParser.sharedCopy(dicomFileAsBuffer, pixelData.dataOffset, pixelData.length);
    console.log("Pixel Data length = ", pixelDataBuffer.length);
    if (pixelData.encapsulatedPixelData) {
        var imageFrame = dicomParser.readEncapsulatedPixelData(dataSet, pixelData, 0);
        console.log("Old Image Frame length = ", imageFrame.length);
        if (pixelData.basicOffsetTable.length) {
            var imageFrame = dicomParser.readEncapsulatedImageFrame(dataSet, pixelData, 0);
            console.log("Image Frame length = ", imageFrame.length);
        }
        else {
            var imageFrame = dicomParser.readEncapsulatedPixelDataFromFragments(dataSet, pixelData, 0, pixelData.fragments.length);
            console.log("Image Frame length = ", imageFrame.length);
        }
    }
}
catch (ex) {
    console.log(ex);
}
console.log("-----------------------------------------");
console.log("");
//# sourceMappingURL=dicomParser.js.map