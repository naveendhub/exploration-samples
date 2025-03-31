using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.Pooling;
using AIPDicomObject = Philips.Platform.Dicom.Data.DicomObject;

namespace FastRepository
{
    internal class FastRepositoryToolkit : IAcquisitionToolkit
    {
        private int count;
        internal long[] studyStoreTime;
        internal long[] seriesStoreTime;
        internal long[] imageStoreTime;
        
        internal long[] studyLoadTime;
        internal long[] seriesLoadTime;
        internal long[] imageLoadTime;

        private static int storeCounter = 0;
        private static int readCounter = 0;
        private bool normalizedFormat;

        private static Dictionary<string, int> studySeriesCollection = new Dictionary<string, int>();

        private static Dictionary<string, DicomObject> studySeriesDicomObjectCollection =
            new Dictionary<string, DicomObject>();



        internal FastRepositoryToolkit(int iterationCount, bool isNormalizedStore)
        {
            normalizedFormat = isNormalizedStore;
            count = iterationCount;
            studyStoreTime = new long[count];
            seriesStoreTime = new long[count];
            imageStoreTime = new long[count];
            
            studyLoadTime = new long[count];
            seriesLoadTime = new long[count];
            imageLoadTime = new long[count];
        }

        private readonly List<MemoryMappedFile> mmfCollection = new List<MemoryMappedFile>();

        public string StoreHeader(DicomObject compositeDicomObject)
        {
            var studyInstanceUid = compositeDicomObject.GetString(DicomDictionary.DicomStudyInstanceUid);
            var seriesInstanceUid = compositeDicomObject.GetString(DicomDictionary.DicomSeriesInstanceUid);
            var key = Guid.NewGuid().ToString();
            string headerLength;

            if (normalizedFormat)
            {
                headerLength = StoreInNormalizedWay(key, compositeDicomObject, studyInstanceUid, seriesInstanceUid);
            }
            else
            {
                compositeDicomObject.Remove(DicomDictionary.DicomPixelData);
                var imageHeaderLength = StoreHeaderToMmf(key, GetAipDicomObject(compositeDicomObject));
                headerLength = $"{string.Empty},{string.Empty},{imageHeaderLength}";
            }

            var identifier = $"{key},{studyInstanceUid},{seriesInstanceUid},{headerLength}";

            return identifier;
        }

        private string StoreInNormalizedWay(string key, DicomObject compositeDicomObject, string studyInstanceUid, string seriesInstanceUid)
        {
            var imageStoreStopWatch = Stopwatch.StartNew();
            compositeDicomObject.Remove(DicomDictionary.DicomPixelData);
            imageStoreStopWatch.Stop();

            if (!studySeriesCollection.ContainsKey(studyInstanceUid))
            {
                //Get study Header
                var studyStoreStopWatch = Stopwatch.StartNew();
                
                var studyDicomObject = DicomObject.CreateInstance();
                studyDicomObject.CopyTags(ConfigurationManager.StudyHeaders, compositeDicomObject);
                var studyAipDicomObject = GetAipDicomObject(studyDicomObject);
                
                //Store StudyHeaders
                var studyKey = $"study_{key}";
                var studyHeaderLength = StoreHeaderToMmf(studyKey, studyAipDicomObject);
                
                studySeriesCollection.Add(studyInstanceUid, studyHeaderLength);

                studyStoreStopWatch.Stop();
                studyStoreTime[storeCounter] = studyStoreStopWatch.ElapsedTicks;
            }

            if (!studySeriesCollection.ContainsKey(seriesInstanceUid))
            {
                //Get seriesHeader
                var seriesStoreStopWatch = Stopwatch.StartNew();

                var seriesDicomObject = DicomObject.CreateInstance();
                seriesDicomObject.CopyTags(ConfigurationManager.SeriesHeaders, compositeDicomObject);
                var seriesAipDicomObject = GetAipDicomObject(seriesDicomObject);
                
                //Store SeriesHeaders
                var seriesKey = $"series_{key}";
                var seriesHeaderLenght = StoreHeaderToMmf(seriesKey, seriesAipDicomObject);
                
                studySeriesCollection.Add(seriesInstanceUid, seriesHeaderLenght);

                seriesStoreStopWatch.Stop();
                seriesStoreTime[storeCounter] = seriesStoreStopWatch.ElapsedTicks;
            }

            //Get image header
            
            imageStoreStopWatch.Start();
            var imageDicomObject = compositeDicomObject.ShallowCopy();

            foreach (var dictionaryTag in ConfigurationManager.StudyHeaders)
            {
                imageDicomObject.Remove(dictionaryTag);
            }

            foreach (var dictionaryTag in ConfigurationManager.SeriesHeaders)
            {
                imageDicomObject.Remove(dictionaryTag);
            }

            //Store ImageHeaders
            var imageAipDicomObject = GetAipDicomObject(imageDicomObject);
            var imageKey = $"image_{key}";
            
            var imageHeaderLength = StoreHeaderToMmf(imageKey, imageAipDicomObject);
            imageStoreStopWatch.Stop();
            
            imageStoreTime[storeCounter] = imageStoreStopWatch.ElapsedTicks;

            Interlocked.Increment(ref storeCounter);

            return $"{studySeriesCollection[studyInstanceUid]},{studySeriesCollection[seriesInstanceUid]},{imageHeaderLength}";
        }

        public void StorePixel(string identifier, byte[] pixelData)
        {
            var key = $"pixel_{identifier}";
            StorePixelToMmf(key, pixelData);
        }

        public DicomObject LoadHeaders(string identifier, string studyId, string seriesId, int studyHeaderLength, int seriesHeaderLength, int imageHeaderLength)
        {
            if (normalizedFormat)
            {
                return LoadNormalizedData(identifier, studyId, seriesId, studyHeaderLength, seriesHeaderLength, imageHeaderLength);
            }

            return ReadHeaderFromMmf(identifier, imageHeaderLength);
        }

        private DicomObject LoadNormalizedData(string identifier, string studyId, string seriesId, int studyHeaderLength, int seriesHeaderLength, int imageHeaderLength)
        {
            DicomObject studyDicomObject;
            DicomObject seriesDicomObject;

            if (!studySeriesDicomObjectCollection.ContainsKey(studyId))
            {
                var studyStopWatch = Stopwatch.StartNew();
                
                var studyKey = $"study_{identifier}";
                studyDicomObject = ReadHeaderFromMmf(studyKey, studyHeaderLength);
                studySeriesDicomObjectCollection.Add(studyId, studyDicomObject);
                
                studyStopWatch.Stop();
                studyLoadTime[readCounter] = studyStopWatch.ElapsedTicks;
            }

            if (!studySeriesDicomObjectCollection.ContainsKey(seriesId))
            {
                var seriesStopWatch = Stopwatch.StartNew();
                
                var seriesKey = $"series_{identifier}";
                seriesDicomObject = ReadHeaderFromMmf(seriesKey, seriesHeaderLength);
                studySeriesDicomObjectCollection.Add(seriesId, seriesDicomObject);

                seriesStopWatch.Stop();
                seriesLoadTime[readCounter] = seriesStopWatch.ElapsedTicks;
            }

            var imageStopWatch = Stopwatch.StartNew();
            
            var imageKey = $"image_{identifier}";
            studyDicomObject = studySeriesDicomObjectCollection[studyId];
            seriesDicomObject = studySeriesDicomObjectCollection[seriesId];
            
            var imageDicomObject = ReadHeaderFromMmf(imageKey, imageHeaderLength);
            imageDicomObject.Combine(seriesDicomObject);
            imageDicomObject.Combine(studyDicomObject);

            imageStopWatch.Stop();
            imageLoadTime[readCounter] = imageStopWatch.ElapsedTicks;
            
            Interlocked.Increment(ref readCounter);

            return imageDicomObject;
        }

        public byte[] LoadPixel(string identifier)
        {
            var key = $"pixel_{identifier}";
            return ReadPixelFromMmf(key);
        }

        private void StorePixelToMmf(string key, byte[] dataToStore)
        {
            var length = dataToStore.Length;
            try
            {
                var mmf = MemoryMappedFile.CreateNew(key, length);
                using (var stream = mmf.CreateViewStream(0, length))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(dataToStore);
                    }
                }
                //to keep the reference alive
                mmfCollection.Add(mmf);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private int StoreHeaderToMmf(string key, AIPDicomObject dicomObject)
        {
            int length = 0;
            try
            {
                using (var ms = new RecyclableBufferMemoryStream())
                {
                    var binaryWriter = new BinaryWriter(ms);
                    Philips.Platform.Dicom.BinarySerializer.Store(binaryWriter, dicomObject);
                    length = (int)ms.Length;

                    var mmf = MemoryMappedFile.CreateNew(key, length);
                    using (var stream = mmf.CreateViewStream(0, length))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(stream);
                    }

                    //to keep the reference alive
                    mmfCollection.Add(mmf);
                    binaryWriter.Dispose();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return length;
        }

        private byte[] ReadPixelFromMmf(string key)
        {
            byte[] resultBytes = null;

            using (var mmf = MemoryMappedFile.OpenExisting(key))
            {
                try
                {
                    using (var stream = mmf.CreateViewStream())
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            resultBytes = reader.ReadBytes((int)stream.Length);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            return resultBytes;
        }

        private DicomObject ReadHeaderFromMmf(string key, int length)
        {
            DicomObject dicomObject = null;
            using (var mmf = MemoryMappedFile.OpenExisting(key))
            {
                try
                {
                    using (var stream = mmf.CreateViewStream(0, length))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            dicomObject = Philips.Platform.Dicom.BinarySerializer.Load(new BinaryReader(stream));
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
            return dicomObject;
        }

        public void RemoveData(string identifier)
        {
            throw new NotImplementedException();
        }

        internal static Philips.Platform.Dicom.Data.DicomObject GetAipDicomObject(DicomObject abstractDicomObject)
        {
            AIPDicomObject aipDicomObject = abstractDicomObject as AIPDicomObject;
            if (aipDicomObject == null)
            {
                aipDicomObject = new AIPDicomObject();
                aipDicomObject.Combine(abstractDicomObject);
            }
            return aipDicomObject;
        }
    }
}
