using System;
using System.IO;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Caching;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;

namespace FastRepository {
    /// <summary>
    /// 
    /// </summary>
    internal class AcquisitionToolkit: IAcquisitionToolkit {
        // Specify the cache name
        const string CacheName = "demoLocalCache";
        // Connect to cache
        private ICache cache;
        private Random random;
        private readonly Expiration expirationDuration = new Expiration(ExpirationType.Absolute, TimeSpan.FromSeconds(120));
        public AcquisitionToolkit()
        {
            cache = CacheManager.GetCache(CacheName);
            random = new Random();
        }
        public string StoreHeader(DicomObject compositeDicomObject)
        {
            compositeDicomObject.Remove(DicomDictionary.DicomPixelData);
            var key = $"{System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt")}";
            
            using (var destStream = new MemoryStream()) {
                compositeDicomObject.Serialize(destStream);

                var pixelCacheItem = new CacheItem(destStream)
                {
                    Expiration = expirationDuration
                };

                cache.Add(key, pixelCacheItem);
            }
            return key;
        }

        public void StorePixel(string identifier, byte[] pixelData)
        { 
            var pixelCacheItem = new CacheItem(pixelData)
                {
                    Expiration = expirationDuration
            };
            var key = $"pixel_{identifier}";
            cache.Add(key, pixelCacheItem);

        }

        public DicomObject LoadHeaders(string identifier, string studyId, string seriesId, int studyHeaderLength, int seriesHeaderLength, int imageHeaderLength)
        {
            using (var dataFromCache = cache.Get<MemoryStream>(identifier))
            {
                var resultDicomObject = DicomObject.CreateInstance(dataFromCache);
                return resultDicomObject;
            }
        }
        
        public byte[] LoadPixel(string identifier)
        {
            var key = $"pixel_{identifier}";
            var dataFromCache = cache.Get<byte[]>(key);
            return dataFromCache;
        }

        public void RemoveData(string identifier)
        {
            var key = $"pixel_{identifier}";
            cache.Remove(identifier);
            cache.Remove(key);
        }
    }
}
