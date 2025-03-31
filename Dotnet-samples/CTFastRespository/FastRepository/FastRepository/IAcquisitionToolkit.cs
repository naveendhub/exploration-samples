using Philips.Platform.Common;

namespace FastRepository {
    /// <summary>
    /// 
    /// </summary>
    public interface IAcquisitionToolkit
    {
        /// <summary>
        /// 
        /// </summary>
        
        string StoreHeader(DicomObject compositeDicomObject);
        /// <summary>
        /// 
        /// </summary>
        void StorePixel(string identifier, byte[] pixelData);
        /// <summary>
        /// 
        /// </summary>
        DicomObject LoadHeaders(string identifier, string studyId, string seriesId, int studyHeaderLength, int seriesHeaderLength, int imageHeaderLength);
        /// <summary>
        /// 
        /// </summary>
        byte[] LoadPixel(string identifier);
        /// <summary>
        /// 
        /// </summary>
        
        void RemoveData(string identifier);
    } 
}
